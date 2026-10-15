using ADAICA_AUTOTEST.Pages;

namespace ADAICA_AUTOTEST.Tests;

/// <summary>
/// Завдання серії, як їх має створити ADAICA: «Beginnt am», «Fällig am» і чи завдання ще заплановане («Aufgabe geplant»).
/// </summary>
public sealed record SeriesTask(DateTime Begin, DateTime? Due, bool IsPlanned = false);

/// <summary>
/// Які завдання дає серія (<see cref="TaskSeriesData"/>) — тест рахує це сам, незалежно від ADAICA. Перше завдання серії —
/// те, для якого задали серію: після вікна «Aufgabenzyklus» ADAICA переносить його на перший день серії, а час лишає.
/// </summary>
/// <remarks>
/// Правила, перевірені в застосунку: тиждень починається з понеділка, а інтервал «кожні N тижнів / місяців / років»
/// рахується від тижня, місяця чи року, у який припадає «Beginn»; день місяця, якого в місяці немає (31-ше), стає
/// останнім днем місяця; «Endet am» входить у серію; кінець доби, тижня, місяця чи року — 23:59.
/// </remarks>
public static class TaskSeriesSchedule
{
    // «Die nächste Aufgabe soll erst 1 Tag(e) vor Startzeit der neuen Aufgabe aktuell werden» — значення за замовчуванням.
    private static readonly TimeSpan BecomesCurrentBefore = TimeSpan.FromDays(1);

    /// <summary>Усі завдання серії по черзі, о годині <paramref name="timeOfDay"/>; у серії без кінця — без кінця.</summary>
    public static IEnumerable<SeriesTask> Tasks(TaskSeriesData series, TimeSpan timeOfDay) =>
        Limit(Days(series.Pattern, series.Start.Date), series.End)
            .Select(day => day + timeOfDay)
            .Select(begin => new SeriesTask(begin, Due(series, begin)));

    /// <summary>
    /// Завдання, які ADAICA створює одразу, коли серію збережено в <paramref name="savedAt"/>: усі, що вже актуальні
    /// (почнуться не пізніше ніж за добу), і ще одне наступне — заплановане. Решту створює пізніше джоб бекенду.
    /// </summary>
    public static IReadOnlyList<SeriesTask> CreatedAt(TaskSeriesData series, TimeSpan timeOfDay, DateTime savedAt)
    {
        var created = new List<SeriesTask>();
        foreach (var task in Tasks(series, timeOfDay))
        {
            if (task.Begin - BecomesCurrentBefore > savedAt)
            {
                created.Add(task with { IsPlanned = true });
                break;
            }

            created.Add(task);
        }

        return created;
    }

    private static IEnumerable<DateTime> Days(SeriesPattern pattern, DateTime start) => pattern switch
    {
        SeriesPattern.Daily daily => Every(start, day => day.AddDays(daily.EveryDays)),
        SeriesPattern.Workdays => Every(start, day => day.AddDays(1)).Where(day => day.DayOfWeek is not (DayOfWeek.Saturday or DayOfWeek.Sunday)),
        SeriesPattern.Weekly weekly => WeeklyDays(weekly, start),
        SeriesPattern.MonthlyOnDay monthly => Months(start, monthly.EveryMonths, month => DayOfMonth(month.Year, month.Month, monthly.Day)),
        SeriesPattern.MonthlyOnWeekday monthly => Months(start, monthly.EveryMonths, month => Weekday(month, monthly.Week, monthly.Day)),
        SeriesPattern.YearlyOnDate yearly => Years(start, yearly.EveryYears, year => DayOfMonth(year, yearly.Month, yearly.Day)),
        SeriesPattern.YearlyOnWeekday yearly => Years(start, yearly.EveryYears, year => Weekday(new DateTime(year, yearly.Month, 1), yearly.Week, yearly.Day)),
        _ => throw new ArgumentOutOfRangeException(nameof(pattern), pattern, "Невідомий Serienmuster."),
    };

    private static IEnumerable<DateTime> Limit(IEnumerable<DateTime> days, SeriesEnd end) => end switch
    {
        SeriesEnd.AfterCount afterCount => days.Take(afterCount.Count),
        SeriesEnd.OnDate onDate => days.TakeWhile(day => day <= onDate.Date),
        _ => days,
    };

    private static DateTime? Due(TaskSeriesData series, DateTime begin)
    {
        var day = begin.Date;
        return series.Due switch
        {
            SeriesTaskDue.None => null,
            SeriesTaskDue.AfterHours afterHours => begin.AddHours(afterHours.Hours),
            SeriesTaskDue.OnDayOfMonth onDay => EndOfDay(new DateTime(day.Year, day.Month, onDay.Day)),
            SeriesTaskDue.OnDayOfYear onDay => EndOfDay(new DateTime(day.Year, onDay.Month, onDay.Day)),
            SeriesTaskDue.EndOfPeriod => EndOfDay(series.Pattern switch
            {
                SeriesPattern.Daily or SeriesPattern.Workdays => day,
                SeriesPattern.Weekly => StartOfWeek(day).AddDays(6),
                SeriesPattern.MonthlyOnDay or SeriesPattern.MonthlyOnWeekday => DayOfMonth(day.Year, day.Month, 31),
                _ => new DateTime(day.Year, 12, 31),
            }),
            _ => throw new ArgumentOutOfRangeException(nameof(series), series.Due, "Невідомий кінець завдань серії."),
        };
    }

    private static IEnumerable<DateTime> Every(DateTime first, Func<DateTime, DateTime> next)
    {
        for (var day = first; ; day = next(day))
            yield return day;
    }

    private static IEnumerable<DateTime> WeeklyDays(SeriesPattern.Weekly weekly, DateTime start)
    {
        var offsets = weekly.Days.Select(DaysFromMonday).Order().ToList();
        for (var monday = StartOfWeek(start); ; monday = monday.AddDays(7 * weekly.EveryWeeks))
        {
            foreach (var day in offsets.Select(offset => monday.AddDays(offset)).Where(day => day >= start))
                yield return day;
        }
    }

    private static IEnumerable<DateTime> Months(DateTime start, int everyMonths, Func<DateTime, DateTime> dayIn)
    {
        for (var month = new DateTime(start.Year, start.Month, 1); ; month = month.AddMonths(everyMonths))
        {
            if (dayIn(month) is var day && day >= start)
                yield return day;
        }
    }

    private static IEnumerable<DateTime> Years(DateTime start, int everyYears, Func<int, DateTime> dayIn)
    {
        for (var year = start.Year; ; year += everyYears)
        {
            if (dayIn(year) is var day && day >= start)
                yield return day;
        }
    }

    // День місяця; якщо в місяці стільки днів немає — останній день місяця.
    private static DateTime DayOfMonth(int year, int month, int day) =>
        new(year, month, Math.Min(day, DateTime.DaysInMonth(year, month)));

    private static DateTime Weekday(DateTime firstOfMonth, WeekOfMonth week, DayOfWeek dayOfWeek)
    {
        var first = firstOfMonth.AddDays(((int)dayOfWeek - (int)firstOfMonth.DayOfWeek + 7) % 7);
        if (week != WeekOfMonth.Last)
            return first.AddDays(7 * ((int)week - 1));

        var last = first;
        while (last.AddDays(7).Month == firstOfMonth.Month)
            last = last.AddDays(7);
        return last;
    }

    private static DateTime StartOfWeek(DateTime day) => day.AddDays(-DaysFromMonday(day.DayOfWeek));

    private static int DaysFromMonday(DayOfWeek day) => ((int)day + 6) % 7;

    private static DateTime EndOfDay(DateTime day) => day.Date.AddHours(23).AddMinutes(59);
}
