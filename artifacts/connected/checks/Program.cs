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
}
finally { Directory.Delete(FileSystem.AppDataDirectory, true); }

// Host substitute only for MAUI's platform storage path.
static class FileSystem
{
    public static string AppDataDirectory { get; } = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "soundlet-checks-" + Guid.NewGuid())).FullName;
}
