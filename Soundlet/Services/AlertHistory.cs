using System.Text.Json;
using Soundlet.Models;

namespace Soundlet.Services;

// Local history used by BLE notifications.
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

    public async Task ClearAsync()
    {
        await LoadAsync();
        await _gate.WaitAsync();
        try
        {
            // Replace storage first so a failed write preserves the in-memory history.
            await File.WriteAllTextAsync(FileName + ".tmp", "[]");
            File.Move(FileName + ".tmp", FileName, true);
            _records.Clear();
        }
        finally { _gate.Release(); }
    }

    public async Task<AlertRecord> AddAsync(AlertType type, DateTime occurredAt, AlertSource source = AlertSource.Manual)
    {
        await LoadAsync();
        await _gate.WaitAsync();
        try
        {
            var record = new AlertRecord(Guid.NewGuid(), type, occurredAt) { Source = source };
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
