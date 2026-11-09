using Soundlet.Models;
using Soundlet.Services;

static void Check(bool condition, string name)
{
    if (!condition) throw new Exception(name);
    Console.WriteLine("PASS " + name);
}
try
{
    foreach (var year in new[] { 1, 1900, 2000, 2024, 2026, 9999 })
    foreach (var month in Enumerable.Range(1, 12))
    {
        var days = CalendarDates.ForMonth(new DateTime(year, month, 1));
        Check(days.Count == 42 && days.Count(d => d?.Year == year && d?.Month == month) == DateTime.DaysInMonth(year, month), $"Calendar {year}/{month}");
        Check(days.Select((day, i) => !day.HasValue || (int)day.Value.DayOfWeek == i % 7).All(x => x), "Weekday alignment");
    }
    var history = new AlertHistory();
    await history.LoadAsync();
    Check(history.Records.Count == 0, "Empty initial history");
    var instant = DateTime.Now;
    for (var i = 0; i < 7; i++) await history.AddAsync((AlertType)(i % 6), instant.AddSeconds(i));
    Check(history.Records.Select(r => r.Type).SequenceEqual(new[] { AlertType.Fire, AlertType.Door, AlertType.Siren, AlertType.Doorbell, AlertType.Baby, AlertType.Dog, AlertType.Fire }), "Six types and sequence wrap");
    Check(history.Records.Select(r => r.Id).Distinct().Count() == 7, "Unique identifiers");
    var reloaded = new AlertHistory();
    await reloaded.LoadAsync();
    Check(reloaded.Records.SequenceEqual(history.Records), "JSON persistence preserves timestamps and types");
    Check(reloaded.Records.OrderByDescending(r => r.OccurredAt).First().OccurredAt == instant.AddSeconds(6), "Newest first");
    Check(!reloaded.Records.Any(r => r.OccurredAt.Date == instant.AddDays(-1).Date), "Empty date filter");
    Check(reloaded.Records.All(r => r.Source == AlertSource.Manual), "Simulator records remain manual");
    var claps = await Task.WhenAll(Enumerable.Range(0, 12)
        .Select(_ => history.AddAsync(AlertType.Clap, instant, AlertSource.Bracelet)));
    Check(claps.Select(r => r.Id).Distinct().Count() == 12, "Separate notifications at the same timestamp are retained");
    var withBle = new AlertHistory();
    await withBle.LoadAsync();
    Check(withBle.Records.Count == 19 && withBle.Records.Count(r => r.Source == AlertSource.Bracelet) == 12,
        "Concurrent BLE ingestion persists exactly once per call beside manual records");
    Check(withBle.Records.Last().Title == "Palma detectada"
          && withBle.Records.Last().Description == "A pulseira identificou o som de uma palma."
          && withBle.Records.Last().OccurredAt == instant, "Clap presentation and receipt timestamp survive JSON");
    // Clearing must cover every date and both old simulator records and real BLE records.
    await history.AddAsync(AlertType.Clap, instant.AddDays(-1), AlertSource.Bracelet);
    await history.ClearAsync();
    Check(history.Records.Count == 0, "Clear removes all records across dates and sources");
    var afterClear = new AlertHistory();
    await afterClear.LoadAsync();
    Check(afterClear.Records.Count == 0, "Cleared records do not return after reloading storage");
    var nextClap = await history.AddAsync(AlertType.Clap, instant.AddMinutes(1), AlertSource.Bracelet);
    var afterNewClap = new AlertHistory();
    await afterNewClap.LoadAsync();
    Check(afterNewClap.Records.Count == 1 && afterNewClap.Records[0] == nextClap,
        "A new BLE record persists normally after clearing");
    var temporaryPath = Path.Combine(FileSystem.AppDataDirectory, "alert-history.json.tmp");
    Directory.CreateDirectory(temporaryPath);
    var failed = false;
    try { await history.ClearAsync(); }
    catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { failed = true; }
    finally { Directory.Delete(temporaryPath); }
    Check(failed && history.Records.Single() == nextClap, "Failed clear preserves in-memory history");
    var afterFailedClear = new AlertHistory();
    await afterFailedClear.LoadAsync();
    Check(afterFailedClear.Records.Single() == nextClap, "Failed clear preserves persisted history");
    await Task.WhenAll(history.ClearAsync(), history.AddAsync(AlertType.Clap, instant, AlertSource.Bracelet));
    var afterConcurrentClear = new AlertHistory();
    await afterConcurrentClear.LoadAsync();
    Check(afterConcurrentClear.Records.SequenceEqual(history.Records) && history.Records.Count <= 1,
        "Concurrent clear and BLE arrival keep storage consistent without duplicates");
    await history.ClearAsync();
    await history.ClearAsync();
    Check(history.Records.Count == 0, "Clearing an empty history is safe");
    // The old JSON format did not include Source. Existing records must remain readable.
    await File.WriteAllTextAsync(Path.Combine(FileSystem.AppDataDirectory, "alert-history.json"),
        "[{\"Id\":\"" + Guid.NewGuid() + "\",\"Type\":0,\"OccurredAt\":\"2026-10-01T12:30:00\"}]");
    var legacy = new AlertHistory();
    await legacy.LoadAsync();
    Check(legacy.Records.Count == 1 && legacy.Records[0].Type == AlertType.Fire
        && legacy.Records[0].Source == AlertSource.Manual, "Legacy history without Source remains compatible");
}
finally { Directory.Delete(FileSystem.AppDataDirectory, true); }

// Host substitute only for MAUI's platform storage path.
static class FileSystem
{
    public static string AppDataDirectory { get; } = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "soundlet-checks-" + Guid.NewGuid())).FullName;
}
