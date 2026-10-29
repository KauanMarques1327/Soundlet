namespace Soundlet.Models;

public static class CalendarDates
{
    public static IReadOnlyList<DateTime?> ForMonth(DateTime month)
    {
        var first = new DateTime(month.Year, month.Month, 1);
        var result = new DateTime?[42];
        for (var i = 0; i < result.Length; i++)
        {
            var ticks = first.Ticks + (i - (int)first.DayOfWeek) * TimeSpan.TicksPerDay;
            if (ticks >= DateTime.MinValue.Ticks && ticks <= DateTime.MaxValue.Ticks)
                result[i] = new DateTime(ticks);
        }
        return result;
    }
}
