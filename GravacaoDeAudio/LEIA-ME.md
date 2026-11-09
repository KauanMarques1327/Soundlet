# Gravador USB INMP441 — XIAO ESP32-S3

Pacote para gravar áudio diretamente do INMP441 no computador por USB, sem módulo SD.
O ESP32-S3 captura o I2S e transmite quadros PCM16; o Python reconstrói os quadros,
confere CRC e sequência, salva WAV mono de 16 kHz e registra metadados em `manifest.csv`.

> **Importante:** o firmware foi preparado para a ligação informada pelo usuário. O código
> Python foi testado localmente com quadros simulados (3 testes passaram). O firmware não
> foi compilado nem executado em uma placa física nesta sessão; faça o primeiro teste de
> 10 segundos antes de uma sessão longa.

## 1. Ligação exata

| INMP441 | XIAO ESP32-S3 | Função |
|---|---|---|
| SCK | D0 / GPIO1 | I2S BCLK |
| WS | D1 / GPIO2 | I2S WS/LRCLK |
| SD | D2 / GPIO3 | I2S DIN do ESP32 |
| L/R | GND | Seleciona o slot esquerdo |
| VDD | 3V3 | Alimentação |
| GND | GND | Terra comum |

Não alimente o INMP441 com 5 V. Mantenha os fios I2S curtos e firmes. O mau contato na
protoboard pode gerar estalos que não são sons reais do ambiente.

## 2. Configuração I2S usada

- ESP32-S3 como master; INMP441 como slave.
- Modo padrão I2S Philips (com deslocamento de um bit).
- 16.000 amostras por segundo.
- Slots de 32 bits, modo mono, slot LEFT.
- `L/R = GND` corresponde ao slot esquerdo.
- O INMP441 fornece áudio signed de 24 bits em slot I2S de 32 bits.
- O firmware converte para PCM signed de 16 bits usando `raw >> 16` (os 16 bits mais
  significativos); não aplica AGC, normalização ou ganho digital.

## 3. Enviar o firmware

1. Abra `firmware/INMP441_USB_Recorder.ino` no Arduino IDE.
2. Instale/atualize **esp32 by Espressif Systems** para a linha **3.x** no Boards Manager.
   O sketch usa o driver I2S standard novo (`driver/i2s_std.h`).
3. Selecione a placa **XIAO ESP32-S3**.
4. Em Tools, habilite **USB CDC On Boot: Enabled**.
5. Selecione a porta da placa e envie o sketch.
6. Depois do upload, feche o Serial Monitor e o Serial Plotter. O protocolo USB é binário;
   texto de debug no mesmo `Serial` corromperia os arquivos.

O sketch não exige biblioteca extra quando `ENABLE_OLED` está em `0` (padrão).

### OLED opcional

Se o SSD1306 estiver conectado em D4/GPIO5 (SDA) e D5/GPIO6 (SCL):

1. Instale **Adafruit GFX Library** e **Adafruit SSD1306** no Arduino IDE.
2. Mude `#define ENABLE_OLED 0` para `#define ENABLE_OLED 1`.
3. Compile e envie novamente.

O display mostra RMS em dBFS, pico e sequência de pacotes. Não conecte o OLED em D0/D1/D2,
pois esses pinos já estão reservados ao microfone.

## 4. Preparar o Python

Use Python 3.10 ou mais recente. No terminal, entre na pasta `pc_recorder`:

```bash
cd pc_recorder
python -m pip install -r requirements.txt
```

Liste as portas disponíveis:

```bash
python record_background.py --list
```

No Windows, a porta pode aparecer como `COM5`, `COM6` etc. No Linux, costuma ser
`/dev/ttyACM0` ou `/dev/ttyUSB0`.

## 5. Gravação de um arquivo

Prepare o cenário antes de executar o comando. Deixe a placa imóvel e não encoste nos fios.
O programa abre a porta USB, faz uma contagem regressiva, captura a duração solicitada e
salva um WAV. O áudio não é normalizado nem amplificado.

Exemplo: 60 segundos de ar-condicionado no quarto, aproximadamente 3 m do microfone:

```bash
python record_background.py --port COM9 --category background --label ar_condicionado --seconds 60 --clips 1 --environment quarto --distance "3 m" --position pulso --note "Ar-condicionado ligado; computador desligado" --output ../dataset
```

Exemplo: conversa próxima:

```bash
python record_background.py --port COM5 --category background --label conversa_proxima --seconds 60 --clips 1 --environment sala --distance "1 m" --position pulso --note "Uma pessoa conversando em volume normal" --output ../dataset
```

Exemplo: som de interesse gravado por você (categoria `events`):

```bash
python record_background.py --port COM5 --category events --label choro_bebe --seconds 10 --clips 3 --environment casa --distance "variável" --position pulso --note "Choro reproduzido/gravado; anotar a fonte real" --output ../dataset
```

`--clips 3` gera três WAVs independentes do mesmo cenário, com contagem regressiva entre
eles. Use `--clips 1` e execute um comando novo quando for mudar o cenário, distância ou
posição do microfone.

### Assistente interativo

Também é possível preencher os metadados por perguntas:

```bash
python record_background.py --wizard --port COM5 --output ../dataset
```

## 6. Arquivos produzidos

A pasta de saída fica organizada assim:

```text
dataset/
├── background/
│   ├── ar_condicionado/
│   │   └── 20261109_161700_123456_ar_condicionado_01.wav
│   └── conversa_proxima/
│       └── ...wav
├── events/
│   └── choro_bebe/
│       └── ...wav
└── manifest.csv
```

Cada WAV é mono, PCM signed de 16 bits, 16 kHz. `manifest.csv` registra ambiente, distância,
posição do microfone, observações, RMS, pico, clipping, erros CRC e pacotes perdidos.

O Python verifica os quadros USB. Se detectar um pacote perdido, insere silêncio estimado
para preservar o tempo do áudio e informa isso no manifesto; se houver CRC inválido ou
perda de pacotes, considere repetir a gravação e não use o arquivo automaticamente no treino.

## 7. Teste inicial recomendado

Comece com um arquivo curto:

```bash
python record_background.py --port COM5 --category background --label teste_voz --seconds 10 --clips 1 --environment quarto --distance "20 cm" --position mesa --note "Falar por alguns segundos e depois ficar em silêncio" --output ../dataset
```

Fale a cerca de 20 cm nos primeiros segundos e depois fique em silêncio. Confira se:

- o WAV abre e tem 10 segundos;
- a fala é audível sem estalos contínuos;
- RMS e pico aparecem no terminal;
- `crc_errors` e `lost_packets` ficam em zero;
- o `manifest.csv` recebe uma linha.

Se o programa disser que não recebeu pacotes válidos, confira USB CDC On Boot, porta correta,
cabo USB de dados, alimentação 3V3 e inicialização I2S. Não abra o Serial Monitor ao mesmo tempo.

## 8. Regras para o dataset da pulseira

- Grave cada situação em arquivo separado e preserve os WAVs originais.
- Registre quando algum som-alvo (sirene, buzina, cachorro, choro, campainha etc.) aparecer
  durante uma gravação de fundo. Esse trecho precisa ser anotado como contendo o evento, não
  tratado como fundo puro.
- Se tocar na protoboard ou houver mau contato, anote o intervalo e não use esse trecho como
  ruído ambiental normal.
- Não divida janelas de uma mesma gravação original entre treino e validação/teste. Separe
  os dados por arquivo/sessão para evitar vazamento.
- Não use normalização permanente nos WAVs originais. Versões processadas podem ser criadas
  separadamente para experimentos controlados.
