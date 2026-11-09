using System.Collections.ObjectModel;
using System.Text;
using Plugin.BLE;
using Plugin.BLE.Abstractions;
using Plugin.BLE.Abstractions.Contracts;
using Plugin.BLE.Abstractions.EventArgs;
using Soundlet.Models;

namespace Soundlet.Services;

// All state changes and GATT operations run on the MAUI main thread.
public sealed class BluetoothService : IAsyncDisposable
{
    public static readonly Guid ServiceId = Guid.Parse("4fafc201-1fb5-459e-8fcc-c5c9c331914b");
    public static readonly Guid CharacteristicId = Guid.Parse("beb5483e-36e1-4688-b7f5-ea07361b26a8");
    private readonly IBluetoothLE _ble = CrossBluetoothLE.Current;
    private IAdapter Adapter => _ble.Adapter;
    private readonly ObservableCollection<IDevice> _devices = [];
    public ReadOnlyObservableCollection<IDevice> Devices { get; }
    private readonly SemaphoreSlim _connectionGate = new(1, 1);
    private CancellationTokenSource? _scan;
    private CancellationTokenSource? _connection;
    private Task? _scanTask;
    private IDevice? _device;
    private ICharacteristic? _characteristic;
    public bool IsConnected { get; private set; }
    public bool IsScanning { get; private set; }
    public event EventHandler? StateChanged;
    public event EventHandler<AlertRecord>? AlertReceived;
    public event EventHandler<string>? Error;

    public BluetoothService()
    {
        Devices = new(_devices);
        Adapter.DeviceAdvertised += OnDeviceFound;
        Adapter.DeviceDisconnected += OnDisconnected;
        Adapter.DeviceConnectionLost += OnConnectionLost;
        _ble.StateChanged += OnBluetoothStateChanged;
    }

    public async Task EnsureReadyAsync()
    {
        if (!_ble.IsAvailable) throw new InvalidOperationException("Este aparelho não oferece suporte a Bluetooth Low Energy.");
#if ANDROID
        if (await Permissions.RequestAsync<Platforms.Android.BlePermissions>() != PermissionStatus.Granted)
            throw new InvalidOperationException("Permita o acesso a dispositivos próximos nas configurações do Soundlet para procurar a pulseira.");
#else
        throw new PlatformNotSupportedException("A integração BLE está disponível nesta etapa no Android.");
#endif
        if (!_ble.IsOn) throw new InvalidOperationException("Ative o Bluetooth do celular para procurar a pulseira.");
#if ANDROID
        if (!OperatingSystem.IsAndroidVersionAtLeast(31) && !Geolocation.IsEnabled)
            throw new InvalidOperationException("Ative a localização do celular para permitir a busca BLE nesta versão do Android.");
#endif
    }

    public async Task ScanAsync(CancellationToken cancellationToken)
    {
        await EnsureReadyAsync();
        cancellationToken.ThrowIfCancellationRequested();
        await StopScanAsync();
        if (IsConnected) return;
        _devices.Clear();
        using var scan = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _scan = scan;
        IsScanning = true;
        StateChanged?.Invoke(this, EventArgs.Empty);
        try
        {
            Adapter.ScanTimeout = 15000;
            Adapter.ScanMode = ScanMode.LowLatency;
            _scanTask = Adapter.StartScanningForDevicesAsync(new ScanFilterOptions(), cancellationToken: scan.Token);
            await _scanTask;
        }
        catch (OperationCanceledException) when (scan.IsCancellationRequested) { }
        finally
        {
            _scanTask = null;
            _scan = null;
            IsScanning = false;
            StateChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public async Task StopScanAsync()
    {
        _scan?.Cancel();
        if (Adapter.IsScanning) await Adapter.StopScanningForDevicesAsync();
        if (_scanTask is { } task)
        {
            try { await task; }
            catch (OperationCanceledException) { }
        }
    }

    private static bool IsSoundlet(IDevice device)
    {
        if (string.Equals(device.Name?.Trim(), "Soundlet Pulseira", StringComparison.OrdinalIgnoreCase)) return true;
        // 128-bit service UUIDs in advertising packets are transmitted least significant byte first.
        foreach (var record in device.AdvertisementRecords.Where(r => r.Type is
                     AdvertisementRecordType.UuidsComplete128Bit or AdvertisementRecordType.UuidsIncomplete128Bit))
        {
            for (var i = 0; i + 16 <= record.Data.Length; i += 16)
            {
                var bytes = record.Data.Skip(i).Take(16).Reverse().ToArray();
                if (Convert.ToHexString(bytes).Equals(ServiceId.ToString("N"), StringComparison.OrdinalIgnoreCase)) return true;
            }
        }
        return false;
    }

    private void OnDeviceFound(object? sender, DeviceEventArgs e) => MainThread.BeginInvokeOnMainThread(() =>
    {
        if (IsScanning && IsSoundlet(e.Device) && !_devices.Any(d => d.Id == e.Device.Id))
            _devices.Add(e.Device);
    });

    public async Task ConnectAsync(IDevice device, CancellationToken cancellationToken)
    {
        if (!await _connectionGate.WaitAsync(0, cancellationToken)) return;
        try
        {
            if (IsConnected && _device?.Id == device.Id) return;
            await EnsureReadyAsync();
            await StopScanAsync();
            await DisconnectCoreAsync();
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(TimeSpan.FromSeconds(25));
            _connection = timeout;
            _device = device;
            await Adapter.ConnectToDeviceAsync(device, cancellationToken: timeout.Token).WaitAsync(timeout.Token);
            var service = await device.GetServiceAsync(ServiceId, timeout.Token).WaitAsync(timeout.Token)
                ?? throw new InvalidOperationException("O serviço Soundlet não foi encontrado na pulseira.");
            _characteristic = await service.GetCharacteristicAsync(CharacteristicId, timeout.Token).WaitAsync(timeout.Token)
                ?? throw new InvalidOperationException("A característica de alertas não foi encontrada.");
            if (!_characteristic.CanUpdate) throw new InvalidOperationException("A pulseira não oferece notificações de alertas.");
            _characteristic.ValueUpdated += OnValueUpdated;
            await _characteristic.StartUpdatesAsync(timeout.Token).WaitAsync(timeout.Token);
            timeout.Token.ThrowIfCancellationRequested();
            if (device.State != DeviceState.Connected) throw new InvalidOperationException("A pulseira desconectou durante a conexão.");
            IsConnected = true;
            StateChanged?.Invoke(this, EventArgs.Empty);
        }
        catch
        {
            await DisconnectCoreAsync();
            throw;
        }
        finally { _connection = null; _connectionGate.Release(); }
    }

    private void OnValueUpdated(object? sender, CharacteristicUpdatedEventArgs e)
    {
        if (!ReferenceEquals(e.Characteristic, _characteristic)) return;
        var message = Encoding.UTF8.GetString(e.Characteristic.Value ?? []).Trim('\0', '\r', '\n', ' ');
        var receivedAt = DateTime.Now;
        if (message != "PALMA") return;
        // Each callback is persisted once, even if pages change or the link drops immediately afterwards.
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            try
            {
                var record = await AlertHistory.Current.AddAsync(AlertType.Clap, receivedAt, AlertSource.Bracelet);
                AlertReceived?.Invoke(this, record);
            }
            catch (Exception ex) { Error?.Invoke(this, "Não foi possível salvar o alerta da pulseira: " + ex.Message); }
        });
    }

    private void ClearConnection()
    {
        if (_characteristic is { } characteristic) characteristic.ValueUpdated -= OnValueUpdated;
        _characteristic = null;
        _device = null;
        IsConnected = false;
        StateChanged?.Invoke(this, EventArgs.Empty);
    }

    private void OnDisconnected(object? sender, DeviceEventArgs e) => HandleDisconnection(e.Device);
    private void OnConnectionLost(object? sender, DeviceErrorEventArgs e) => HandleDisconnection(e.Device);
    private void HandleDisconnection(IDevice device) => MainThread.BeginInvokeOnMainThread(() =>
    {
        if (_device?.Id != device.Id) return;
        _connection?.Cancel();
        ClearConnection();
        var discovered = _devices.FirstOrDefault(d => d.Id == device.Id);
        if (discovered is not null) _devices.Remove(discovered);
        device.Dispose();
    });
    private void OnBluetoothStateChanged(object? sender, BluetoothStateChangedArgs e) => MainThread.BeginInvokeOnMainThread(() =>
    {
        if (e.NewState == BluetoothState.On) return;
        _scan?.Cancel();
        _connection?.Cancel();
        var device = _device;
        ClearConnection();
        _devices.Clear();
        device?.Dispose();
    });

    public async Task DisconnectAsync()
    {
        _connection?.Cancel();
        await _connectionGate.WaitAsync();
        try { await StopScanAsync(); await DisconnectCoreAsync(); }
        finally { _connectionGate.Release(); }
    }

    private async Task DisconnectCoreAsync()
    {
        var device = _device;
        var characteristic = _characteristic;
        ClearConnection();
        if (device is null) return;
        try
        {
            if (characteristic is not null && device.State == DeviceState.Connected)
                await characteristic.StopUpdatesAsync().WaitAsync(TimeSpan.FromSeconds(3));
        }
        catch (Exception ex) { System.Diagnostics.Debug.WriteLine(ex); }
        try { await Adapter.DisconnectDeviceAsync(device).WaitAsync(TimeSpan.FromSeconds(5)); }
        catch (Exception ex) { System.Diagnostics.Debug.WriteLine(ex); }
        finally { device.Dispose(); }
    }

    public async ValueTask DisposeAsync()
    {
        await DisconnectAsync();
        Adapter.DeviceAdvertised -= OnDeviceFound;
        Adapter.DeviceDisconnected -= OnDisconnected;
        Adapter.DeviceConnectionLost -= OnConnectionLost;
        _ble.StateChanged -= OnBluetoothStateChanged;
    }
}
