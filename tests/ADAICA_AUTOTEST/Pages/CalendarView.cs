namespace ADAICA_AUTOTEST.Pages;

/// <summary>Вид планувальника у вікні «Kalender» (меню клітинки → «Ansichtwechsel»).</summary>
public enum CalendarView
{
    Day,
    Week,
    WorkWeek,
    Month,
}

public static class CalendarViews
{
    /// <summary>Назва виду, як її пише ADAICA в меню й у назві планувальника.</summary>
    public static string Label(this CalendarView view) => view switch
    {
        CalendarView.Day => "Tagesansicht",
        CalendarView.Week => "Wochenansicht",
        CalendarView.WorkWeek => "Arbeitswochenansicht",
        CalendarView.Month => "Monatsansicht",
        _ => throw new ArgumentOutOfRangeException(nameof(view), view, null),
    };

    public static CalendarView Parse(string label) =>
        Enum.GetValues<CalendarView>().Cast<CalendarView?>().FirstOrDefault(view => view!.Value.Label() == label)
        ?? throw new FormatException($"Невідомий вид планувальника «{label}».");
}
