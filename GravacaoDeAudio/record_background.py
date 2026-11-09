#!/usr/bin/env python3
"""USB recorder for the INMP441 + Seeed XIAO ESP32-S3.

Receives framed PCM16 audio over USB CDC, checks CRC/sequence numbers, writes
mono 16 kHz WAV files, and appends a metadata row to manifest.csv.

Install: python -m pip install -r requirements.txt
Example: python record_background.py --port COM5 --category background \
         --label ar_condicionado --seconds 60 --clips 1 \
         --environment quarto --distance "3 m" \
         --note "Ar-condicionado ligado; microfone na posição de uso"
"""

from __future__ import annotations

import argparse
import csv
import math
import re
import struct
import sys
import time
import wave
from dataclasses import dataclass
from datetime import datetime
from pathlib import Path
from typing import Iterator, Optional

MAGIC = b"AUD0"
PROTOCOL_VERSION = 1
SAMPLE_RATE_HZ = 16_000
SAMPLE_WIDTH_BYTES = 2
HEADER = struct.Struct("<4sBBHII")  # magic, version, flags, n, seq, rate
HEADER_SIZE = HEADER.size  # 16 bytes
CRC_STRUCT = struct.Struct("<H")
MAX_PACKET_SAMPLES = 2048
SERIAL_TIMEOUT_SECONDS = 0.20
NO_DATA_TIMEOUT_SECONDS = 6.0

MANIFEST_FIELDS = [
    "file",
    "relative_path",
    "recorded_at",
    "category",
    "label",
    "duration_s",
    "sample_rate_hz",
    "channels",
    "sample_width_bytes",
    "environment",
    "distance",
    "microphone_position",
    "note",
    "rms_dbfs",
    "peak_dbfs",
    "clipped_samples",
    "zero_samples",
    "crc_errors",
    "lost_packets",
    "inserted_silence_samples",
]


def crc16_ccitt(data: bytes, crc: int = 0xFFFF) -> int:
    """CRC-16/CCITT-FALSE: poly=0x1021, init=0xFFFF, xorout=0."""
    for value in data:
        crc ^= value << 8
        for _ in range(8):
            if crc & 0x8000:
                crc = ((crc << 1) ^ 0x1021) & 0xFFFF
            else:
                crc = (crc << 1) & 0xFFFF
    return crc


@dataclass
class AudioPacket:
    sequence: int
    sample_rate_hz: int
    pcm_bytes: bytes
    sample_count: int
    missing_samples_before: int = 0


class PacketDecoder:
    """Incrementally synchronize and decode the ESP32 binary byte stream."""

    def __init__(self) -> None:
        self.buffer = bytearray()
        self.crc_errors = 0
        self.invalid_headers = 0
        self.discarded_bytes = 0
        self.lost_packets = 0
        self.expected_sequence: Optional[int] = None
        self.last_packet_samples: Optional[int] = None

    def feed(self, incoming: bytes) -> list[AudioPacket]:
        if incoming:
            self.buffer.extend(incoming)
        packets: list[AudioPacket] = []

        while True:
            magic_at = self.buffer.find(MAGIC)
            if magic_at < 0:
                # Keep the last 3 bytes in case they are the beginning of MAGIC.
                keep = min(len(self.buffer), len(MAGIC) - 1)
                discard_count = len(self.buffer) - keep
                if discard_count > 0:
                    self.discarded_bytes += discard_count
                    del self.buffer[:discard_count]
                break

            if magic_at > 0:
                self.discarded_bytes += magic_at
                del self.buffer[:magic_at]

            if len(self.buffer) < HEADER_SIZE:
                break

            magic, version, _flags, sample_count, sequence, sample_rate = (
                HEADER.unpack_from(self.buffer)
            )
            if (
                magic != MAGIC
                or version != PROTOCOL_VERSION
                or sample_count < 1
                or sample_count > MAX_PACKET_SAMPLES
                or sample_rate != SAMPLE_RATE_HZ
            ):
                self.invalid_headers += 1
                del self.buffer[0]
                continue

            payload_size = sample_count * SAMPLE_WIDTH_BYTES
            frame_size = HEADER_SIZE + payload_size + CRC_STRUCT.size
            if len(self.buffer) < frame_size:
                break

            stored_crc = CRC_STRUCT.unpack_from(
                self.buffer, HEADER_SIZE + payload_size
            )[0]
            # CRC covers header fields after MAGIC (bytes 4..15) and PCM payload.
            calculated_crc = crc16_ccitt(
                bytes(self.buffer[4 : HEADER_SIZE + payload_size])
            )
            if stored_crc != calculated_crc:
                self.crc_errors += 1
                del self.buffer[0]
                continue

            pcm_bytes = bytes(self.buffer[HEADER_SIZE : HEADER_SIZE + payload_size])
            gap = 0
            if self.expected_sequence is not None:
                candidate_gap = (sequence - self.expected_sequence) & 0xFFFFFFFF
                # Only count forward gaps; ignore implausible backward jumps.
                if 0 < candidate_gap < 0x80000000:
                    gap = candidate_gap
                    self.lost_packets += gap
            estimated_packet_samples = self.last_packet_samples or sample_count
            missing_samples = gap * estimated_packet_samples
            packets.append(
                AudioPacket(
                    sequence, sample_rate, pcm_bytes, sample_count, missing_samples
                )
            )
            del self.buffer[:frame_size]

            self.expected_sequence = (sequence + 1) & 0xFFFFFFFF
            self.last_packet_samples = sample_count

        return packets


def safe_slug(value: str) -> str:
    value = value.strip().lower()
    value = re.sub(r"[^a-z0-9_-]+", "_", value, flags=re.IGNORECASE)
    value = re.sub(r"_+", "_", value).strip("_-")
    if not value:
        raise ValueError("O nome/label ficou vazio depois da limpeza.")
    return value[:80]


def dbfs_from_rms(sum_squares: int, sample_count: int) -> float:
    if sample_count <= 0:
        return -120.0
    rms = math.sqrt(sum_squares / sample_count)
    if rms <= 0:
        return -120.0
    return 20.0 * math.log10(rms / 32768.0)


def append_manifest(output_root: Path, row: dict[str, object]) -> None:
    manifest_path = output_root / "manifest.csv"
    output_root.mkdir(parents=True, exist_ok=True)
    new_file = not manifest_path.exists() or manifest_path.stat().st_size == 0
    with manifest_path.open("a", newline="", encoding="utf-8-sig") as handle:
        writer = csv.DictWriter(
            handle, fieldnames=MANIFEST_FIELDS, extrasaction="ignore"
        )
        if new_file:
            writer.writeheader()
        writer.writerow(row)


def load_serial_module():
    try:
        import serial  # type: ignore
        from serial.tools import list_ports  # type: ignore
    except ImportError as exc:
        raise RuntimeError(
            "PySerial não está instalado. Execute: python -m pip install -r requirements.txt"
        ) from exc
    return serial, list_ports


def list_serial_ports() -> int:
    _serial, list_ports = load_serial_module()
    ports = list(list_ports.comports())
    if not ports:
        print("Nenhuma porta serial encontrada.")
        return 1
    print("Portas seriais encontradas:")
    for port in ports:
        description = port.description or "sem descrição"
        print(f"  {port.device:12} {description}")
    return 0


def ask_if_missing(value: Optional[str], prompt: str, default: str = "") -> str:
    if value is not None and value != "":
        return value
    suffix = f" [{default}]" if default else ""
    answer = input(f"{prompt}{suffix}: ").strip()
    return answer or default


def get_options() -> argparse.Namespace:
    parser = argparse.ArgumentParser(
        description="Grava WAVs do INMP441 via USB e registra metadados em manifest.csv."
    )
    parser.add_argument(
        "--list", action="store_true", help="Lista as portas seriais e sai."
    )
    parser.add_argument(
        "--wizard", action="store_true", help="Pergunta os dados da gravação."
    )
    parser.add_argument(
        "--port", help="Porta USB/serial, por exemplo COM5 ou /dev/ttyACM0."
    )
    parser.add_argument(
        "--baud",
        type=int,
        default=921600,
        help="Baud nominal (USB CDC normalmente ignora).",
    )
    parser.add_argument(
        "--category", choices=("background", "events"), default="background"
    )
    parser.add_argument(
        "--label", help="Rótulo sem espaços, por exemplo ar_condicionado ou choro_bebe."
    )
    parser.add_argument(
        "--seconds",
        type=float,
        default=60.0,
        help="Duração de cada arquivo em segundos.",
    )
    parser.add_argument(
        "--clips", type=int, default=1, help="Quantidade de arquivos independentes."
    )
    parser.add_argument(
        "--environment", default="", help="Ex.: quarto, sala, faculdade, ônibus, rua."
    )
    parser.add_argument(
        "--distance", default="", help="Ex.: 1 m, 3 m, distante, variável."
    )
    parser.add_argument(
        "--position", default="", help="Posição do microfone: pulso, mesa, bolso etc."
    )
    parser.add_argument(
        "--note",
        default="",
        help="Descrição do cenário e de sons/eventos que apareceram.",
    )
    parser.add_argument(
        "--output", default="dataset", help="Pasta de saída (padrão: ./dataset)."
    )
    parser.add_argument(
        "--no-countdown",
        action="store_true",
        help="Desativa a contagem regressiva de 3 s.",
    )
    args = parser.parse_args()

    if args.list:
        return args

    if args.wizard:
        args.port = ask_if_missing(
            args.port, "Porta serial (ex.: COM5 ou /dev/ttyACM0)"
        )
        print("\nCategoria: background = ruído/ambiente; events = som de interesse.")
        category = (
            input(f"Categoria [{args.category}]: ").strip().lower() or args.category
        )
        if category not in ("background", "events"):
            parser.error("Categoria deve ser background ou events.")
        args.category = category
        args.label = ask_if_missing(args.label, "Nome do som (ex.: ar_condicionado)")
        args.seconds = float(
            ask_if_missing(
                str(args.seconds) if args.seconds != 60.0 else None,
                "Duração de cada arquivo em segundos",
                "60",
            )
        )
        args.clips = int(
            ask_if_missing(
                str(args.clips) if args.clips != 1 else None,
                "Quantidade de arquivos",
                "1",
            )
        )
        args.environment = ask_if_missing(args.environment, "Ambiente", "não informado")
        args.distance = ask_if_missing(
            args.distance, "Distância da fonte", "não informada"
        )
        args.position = ask_if_missing(
            args.position, "Posição do microfone", "posição de uso"
        )
        args.note = ask_if_missing(args.note, "Observações", "")

    if not args.port:
        parser.error(
            "Informe --port COM5 (ou use --wizard). Para descobrir a porta, execute --list."
        )
    if not args.label:
        parser.error("Informe --label (ou use --wizard).")
    if not math.isfinite(args.seconds) or args.seconds <= 0 or args.seconds > 3600:
        parser.error("--seconds deve ser maior que 0 e no máximo 3600.")
    if args.clips < 1 or args.clips > 100:
        parser.error("--clips deve estar entre 1 e 100.")
    return args


def drain_serial_for(ser, seconds: float) -> None:
    """Read and discard stream bytes while waiting, avoiding USB backpressure."""
    deadline = time.monotonic() + seconds
    while time.monotonic() < deadline:
        ser.read(4096)


def record_one_clip(
    ser,
    output_root: Path,
    category: str,
    label: str,
    seconds: float,
    clip_index: int,
    environment: str,
    distance: str,
    position: str,
    note: str,
    countdown: bool,
) -> Path:
    decoder = PacketDecoder()
    wanted_samples = int(round(seconds * SAMPLE_RATE_HZ))
    pcm_output = bytearray()
    captured_samples = 0
    sum_squares = 0
    peak = 0
    clipped_samples = 0
    zero_samples = 0
    inserted_silence_samples = 0
    last_valid_packet_time = time.monotonic()
    last_progress_time = 0.0

    print(
        "\nO cenário já deve estar preparado. Não toque na placa nem nos fios durante a captura."
    )
    if countdown:
        # Drain incoming USB data during countdown. This keeps the ESP32 from
        # blocking on a full USB CDC transmit buffer while waiting to record.
        for number in (3, 2, 1):
            print(f"Começando em {number}...")
            drain_serial_for(ser, 1.0)

    # Discard the few bytes left in the host buffer. The decoder starts at the
    # next complete AUD0 frame; the first sequence value establishes baseline.
    ser.reset_input_buffer()
    time.sleep(0.02)
    capture_started = datetime.now().astimezone()
    start_monotonic = time.monotonic()

    def append_silence(sample_count: int) -> None:
        nonlocal captured_samples, zero_samples, inserted_silence_samples
        take = min(sample_count, wanted_samples - captured_samples)
        if take <= 0:
            return
        pcm_output.extend(b"\x00\x00" * take)
        captured_samples += take
        zero_samples += take
        inserted_silence_samples += take

    while captured_samples < wanted_samples:
        chunk = ser.read(4096)
        now = time.monotonic()
        packets = decoder.feed(chunk)

        if packets:
            last_valid_packet_time = now

        for packet in packets:
            if packet.sample_rate_hz != SAMPLE_RATE_HZ:
                raise RuntimeError(
                    f"Taxa de amostragem inesperada: {packet.sample_rate_hz} Hz (esperado {SAMPLE_RATE_HZ})."
                )

            # Preserve gaps in the serial stream as silence instead of silently
            # compressing time. The missing sample count is estimated from the
            # previous packet size and is explicitly recorded in manifest.csv.
            if packet.missing_samples_before:
                append_silence(packet.missing_samples_before)
                if captured_samples >= wanted_samples:
                    break

            samples = struct.unpack(f"<{packet.sample_count}h", packet.pcm_bytes)
            take = min(packet.sample_count, wanted_samples - captured_samples)
            if take:
                selected = samples[:take]
                pcm_output.extend(struct.pack(f"<{take}h", *selected))
                captured_samples += take
                for sample in selected:
                    magnitude = abs(sample)
                    if magnitude > peak:
                        peak = magnitude
                    if magnitude >= 32760:
                        clipped_samples += 1
                    if sample == 0:
                        zero_samples += 1
                    sum_squares += sample * sample

        if now - last_valid_packet_time > NO_DATA_TIMEOUT_SECONDS:
            raise RuntimeError(
                "Nenhum pacote de áudio válido chegou por 6 segundos. "
                "Confira USB CDC On Boot, a porta, a alimentação e a inicialização I2S."
            )

        if now - last_progress_time >= 0.5:
            last_progress_time = now
            percent = min(100.0, captured_samples * 100.0 / wanted_samples)
            elapsed = captured_samples / SAMPLE_RATE_HZ
            print(
                f"\rGravando: {elapsed:6.1f}/{seconds:5.1f} s ({percent:5.1f}%)",
                end="",
                flush=True,
            )

    elapsed_wall = time.monotonic() - start_monotonic
    print(
        f"\rGravação concluída: {captured_samples / SAMPLE_RATE_HZ:.2f} s de áudio.{' ' * 20}"
    )

    label_slug = safe_slug(label)
    category_slug = "background" if category == "background" else "events"
    target_dir = output_root / category_slug / label_slug
    target_dir.mkdir(parents=True, exist_ok=True)

    timestamp = capture_started.strftime("%Y%m%d_%H%M%S_%f")
    filename = f"{timestamp}_{label_slug}_{clip_index:02d}.wav"
    wav_path = target_dir / filename

    # Defensive collision handling; never overwrite an existing recording.
    suffix = 1
    while wav_path.exists():
        wav_path = (
            target_dir / f"{timestamp}_{label_slug}_{clip_index:02d}_{suffix}.wav"
        )
        suffix += 1

    with wave.open(str(wav_path), "wb") as wav_file:
        wav_file.setnchannels(1)
        wav_file.setsampwidth(SAMPLE_WIDTH_BYTES)
        wav_file.setframerate(SAMPLE_RATE_HZ)
        wav_file.writeframes(pcm_output)

    rms_dbfs = dbfs_from_rms(sum_squares, captured_samples - inserted_silence_samples)
    peak_dbfs = 20.0 * math.log10(peak / 32768.0) if peak > 0 else -120.0
    relative_path = wav_path.relative_to(output_root).as_posix()

    row = {
        "file": wav_path.name,
        "relative_path": relative_path,
        "recorded_at": capture_started.isoformat(timespec="seconds"),
        "category": category_slug,
        "label": label_slug,
        "duration_s": f"{captured_samples / SAMPLE_RATE_HZ:.3f}",
        "sample_rate_hz": SAMPLE_RATE_HZ,
        "channels": 1,
        "sample_width_bytes": SAMPLE_WIDTH_BYTES,
        "environment": environment,
        "distance": distance,
        "microphone_position": position,
        "note": note,
        "rms_dbfs": f"{rms_dbfs:.2f}",
        "peak_dbfs": f"{peak_dbfs:.2f}",
        "clipped_samples": clipped_samples,
        "zero_samples": zero_samples,
        "crc_errors": decoder.crc_errors,
        "lost_packets": decoder.lost_packets,
        "inserted_silence_samples": inserted_silence_samples,
    }
    append_manifest(output_root, row)

    print(f"Arquivo: {wav_path}")
    print(
        f"RMS: {rms_dbfs:.1f} dBFS | Pico: {peak_dbfs:.1f} dBFS | clipping: {clipped_samples}"
    )
    print(
        f"Pacotes: CRC inválido={decoder.crc_errors}, bytes descartados={decoder.discarded_bytes}, "
        f"cabeçalhos inválidos={decoder.invalid_headers}, pacotes perdidos={decoder.lost_packets}"
    )
    if decoder.crc_errors or decoder.lost_packets:
        print(
            "ATENÇÃO: houve corrupção/perda de pacotes. Confira o WAV e considere repetir a gravação."
        )
    if clipped_samples:
        print(
            "ATENÇÃO: há amostras próximas do limite digital; verifique se o som estava muito perto/alto."
        )
    if elapsed_wall > seconds + 5:
        print(
            "Aviso: a captura demorou mais que o esperado; confira a conexão USB e a carga do PC."
        )
    return wav_path


def main() -> int:
    args = get_options()
    if args.list:
        return list_serial_ports()

    serial, _list_ports = load_serial_module()
    try:
        label = safe_slug(args.label)
    except ValueError as exc:
        print(f"Erro: {exc}", file=sys.stderr)
        return 2

    output_root = Path(args.output).expanduser().resolve()
    print("INMP441 USB Recorder")
    print(f"Porta: {args.port} | {SAMPLE_RATE_HZ} Hz | mono | PCM16")
    print(f"Destino: {output_root}")
    print("Abra o Serial Monitor do Arduino IDE? Feche-o antes de continuar.")

    try:
        with serial.Serial(
            args.port, args.baud, timeout=SERIAL_TIMEOUT_SECONDS, write_timeout=2
        ) as ser:
            # Some USB CDC boards reset or reconnect when the port opens. Drain
            # the live stream while it settles to avoid stalling its USB writer.
            drain_serial_for(ser, 2.0)
            for clip_index in range(1, args.clips + 1):
                record_one_clip(
                    ser=ser,
                    output_root=output_root,
                    category=args.category,
                    label=label,
                    seconds=args.seconds,
                    clip_index=clip_index,
                    environment=args.environment,
                    distance=args.distance,
                    position=args.position,
                    note=args.note,
                    countdown=not args.no_countdown,
                )
                if clip_index < args.clips:
                    print(
                        f"\nArquivo {clip_index}/{args.clips} concluído. Prepare a próxima amostra."
                    )
        print(f"\nFinalizado. Metadados: {output_root / 'manifest.csv'}")
        return 0
    except KeyboardInterrupt:
        print("\nGravação cancelada pelo usuário.", file=sys.stderr)
        return 130
    except serial.SerialException as exc:
        print(f"Erro na porta serial: {exc}", file=sys.stderr)
        print(
            "Confira a porta, o cabo USB de dados e se o Serial Monitor está fechado.",
            file=sys.stderr,
        )
        return 3
    except (OSError, RuntimeError, ValueError) as exc:
        print(f"Erro durante a gravação: {exc}", file=sys.stderr)
        return 4


if __name__ == "__main__":
    raise SystemExit(main())
