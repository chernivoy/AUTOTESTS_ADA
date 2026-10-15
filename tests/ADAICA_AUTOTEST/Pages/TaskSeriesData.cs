using ADAICA_AUTOTEST.Controls;

namespace ADAICA_AUTOTEST.Pages;

/// <summary>
/// Серія завдання у вікні «Aufgabenzyklus» (<see cref="TaskSeriesDialog"/>): як часто повторюється завдання
/// («Serienmuster»), з якого дня й до коли («Seriendauer») і коли закінчується кожне завдання серії
/// («Enddatum für einzelne Zyklus-Aufgaben»).
/// </summary>
/// <param name="Start">«Beginn:» — з цього дня ADAICA шукає перше завдання серії.</param>
public sealed record TaskSeriesData(SeriesPattern Pattern, DateTime Start, SeriesEnd End, SeriesTaskDue Due)
{
    public override string ToString() =>
        $"{Pattern}, Beginn {AdaicaFormat.FormatDate(Start)}, {End}, Enddatum der Aufgaben: {Due}";
}

/// <summary>Тиждень місяця в списку «Am … » (Monatlich і Jährlich): «1» … «4» або «Letzten».</summary>
public enum WeekOfMonth
{
    First = 1,
    Second,
    Third,
    Fourth,
    Last,
}

/// <summary>«Serienmuster»: як часто повторюється завдання. Підписи — як у вікні.</summary>
public abstract record SeriesPattern
{
    /// <summary>Підпис пункту списку тижнів: «1» … «4», «Letzten».</summary>
    public static string WeekLabel(WeekOfMonth week) => week == WeekOfMonth.Last ? "Letzten" : ((int)week).ToString();

    /// <summary>«Täglich» → «Jeden/Alle N Tag(e)».</summary>
    public sealed record Daily(int EveryDays) : SeriesPattern
    {
        public override string ToString() => $"Täglich, alle {EveryDays} Tag(e)";
    }

    /// <summary>«Täglich» → «Jeden Arbeitstag»: з понеділка по п'ятницю.</summary>
    public sealed record Workdays : SeriesPattern
    {
        public override string ToString() => "Täglich, jeden Arbeitstag";
    }

    /// <summary>«Wöchentlich» → «Jede/Alle N Woche(n) am» і прапорці днів тижня.</summary>
    public sealed record Weekly(int EveryWeeks, IReadOnlyList<DayOfWeek> Days) : SeriesPattern
    {
        public override string ToString() =>
            $"Wöchentlich, alle {EveryWeeks} Woche(n) am {string.Join(", ", Days.Select(AdaicaFormat.FormatDayOfWeek))}";
    }

    /// <summary>«Monatlich» → «Am N. Tag jedes M. Monats».</summary>
    public sealed record MonthlyOnDay(int Day, int EveryMonths) : SeriesPattern
    {
        public override string ToString() => $"Monatlich, am {Day}. Tag jedes {EveryMonths}. Monats";
    }

    /// <summary>«Monatlich» → «Am [1…4, Letzten] [Wochentag] jedes M. Monats».</summary>
    public sealed record MonthlyOnWeekday(WeekOfMonth Week, DayOfWeek Day, int EveryMonths) : SeriesPattern
    {
        public override string ToString() =>
            $"Monatlich, am {WeekLabel(Week)} {AdaicaFormat.FormatDayOfWeek(Day)} jedes {EveryMonths}. Monats";
    }

    /// <summary>«Jährlich» → «Jede/Alle N Jahr(e)», «Am N. [Monat]».</summary>
    public sealed record YearlyOnDate(int Day, int Month, int EveryYears) : SeriesPattern
    {
        public override string ToString() => $"Jährlich, alle {EveryYears} Jahr(e) am {Day}. {AdaicaFormat.FormatMonth(Month)}";
    }

    /// <summary>«Jährlich» → «Jede/Alle N Jahr(e)», «Am [1…4, Letzten] [Wochentag] im [Monat]».</summary>
    public sealed record YearlyOnWeekday(WeekOfMonth Week, DayOfWeek Day, int Month, int EveryYears) : SeriesPattern
    {
        public override string ToString() =>
            $"Jährlich, alle {EveryYears} Jahr(e) am {WeekLabel(Week)} {AdaicaFormat.FormatDayOfWeek(Day)} im {AdaicaFormat.FormatMonth(Month)}";
    }
}

/// <summary>Кінець серії в групі «Seriendauer».</summary>
public abstract record SeriesEnd
{
    /// <summary>«Ohne Enddatum»: серія не закінчується.</summary>
    public sealed record Never : SeriesEnd
    {
        public override string ToString() => "ohne Enddatum";
    }

    /// <summary>«Endet nach N Aufgaben» — разом із першим завданням.</summary>
    public sealed record AfterCount(int Count) : SeriesEnd
    {
        public override string ToString() => $"endet nach {Count} Aufgaben";
    }

    /// <summary>«Endet am:» — завдання цього дня ще входить у серію.</summary>
    public sealed record OnDate(DateTime Date) : SeriesEnd
    {
        public override string ToString() => $"endet am {AdaicaFormat.FormatDate(Date)}";
    }
}

/// <summary>«Enddatum für einzelne Zyklus-Aufgaben»: як ADAICA заповнює «Fällig am» кожного завдання серії.</summary>
public abstract record SeriesTaskDue
{
    /// <summary>«Ohne Enddatum»: «Fällig am» порожнє.</summary>
    public sealed record None : SeriesTaskDue
    {
        public override string ToString() => "ohne Enddatum";
    }

    /// <summary>
    /// «Am Tagesende / Wochenende / Monatsende / Jahresende beenden» (підпис залежить від Serienmuster): о 23:59
    /// останнього дня доби, тижня (неділі), місяця чи року, у якому почалося завдання.
    /// </summary>
    public sealed record EndOfPeriod : SeriesTaskDue
    {
        public override string ToString() => "am Ende der Periode";
    }

    /// <summary>«Über Dauer berechnen» з «Dauer» N «Stunde(n)»: через N годин після початку завдання.</summary>
    public sealed record AfterHours(int Hours) : SeriesTaskDue
    {
        public override string ToString() => $"über Dauer {Hours} Stunde(n)";
    }

    /// <summary>Monatlich, «Month: Am N»: о 23:59 N-го дня того самого місяця.</summary>
    public sealed record OnDayOfMonth(int Day) : SeriesTaskDue
    {
        public override string ToString() => $"am {Day}. des Monats";
    }

    /// <summary>Jährlich, «Year: Am N [Monat]»: о 23:59 цього дня того самого року.</summary>
    public sealed record OnDayOfYear(int Day, int Month) : SeriesTaskDue
    {
        public override string ToString() => $"am {Day}. {AdaicaFormat.FormatMonth(Month)}";
    }
}
