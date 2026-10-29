using System.Text.Json;
using Soundlet.Models;

namespace Soundlet.Services;

// Shared ingestion path for the simulator and future BLE notifications.
public sealed class AlertHistory
{
    public static AlertHistory Current { get; } = new();
    private readonly SemaphoreSlim _gate = new(1, 1);
    private List<AlertRecord> _records = [];
    private bool _loaded;
    private string FileName => Path.Combine(FileSystem.AppDataDirectory, "alert-history.json");
    public IReadOnlyList<AlertRecord> Records => _records;

    public async Task LoadAsync()
    {
        await _gate.WaitAsync();
        try
        {
            if (_loaded) return;
            if (File.Exists(FileName))
            {
                try { _records = JsonSerializer.Deserialize<List<AlertRecord>>(await File.ReadAllTextAsync(FileName)) ?? []; }
                catch (JsonException) { File.Copy(FileName, FileName + ".invalid", true); }
            }
            _loaded = true;
        }
        finally { _gate.Release(); }
    }

    public async Task<AlertRecord> AddAsync(AlertType type, DateTime occurredAt)
    {
        await LoadAsync();
        await _gate.WaitAsync();
        try
        {
            var record = new AlertRecord(Guid.NewGuid(), type, occurredAt);
            _records.Add(record);
            try
            {
                await File.WriteAllTextAsync(FileName + ".tmp", JsonSerializer.Serialize(_records));
                File.Move(FileName + ".tmp", FileName, true);
            }
            catch { _records.Remove(record); throw; }
            return record;
        }
        finally { _gate.Release(); }
    }
}
