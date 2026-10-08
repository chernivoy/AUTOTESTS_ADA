namespace ADAICA_AUTOTEST.Pages;

/// <summary>
/// Налаштування «Vorschläge für Verknüpfung der Dokumentation» → вкладка «E-Mails» (Konfiguration → Einstellungen →
/// Dokumentation): які завдання й терміни контактів листа ADAICA пропонує на вкладці «Dokumentation» вікна збереження
/// листа. Значення «Anwender» перекриває «Systemweit»; доки воно збігається із системним, воно успадковує системне.
/// Об'єкти тест не перевіряє.
/// </summary>
public sealed record MailSuggestionOptions(TaskSuggestionOptions Tasks, AppointmentSuggestionOptions Appointments)
{
    /// <summary>Як у BLANK_IGOR стоїть «Systemweit»: незавершені завдання й терміни, створені ±2 місяці від сьогодні.</summary>
    public static MailSuggestionOptions Default { get; } = new(
        new TaskSuggestionOptions(true, TaskStatusFilter.Undone, SuggestionPeriod.Months(2), SuggestionPeriod.Months(2), CompanyRecords: false),
        new AppointmentSuggestionOptions(
            true, AppointmentDateField.Created, AppointmentPeriodMode.Period, SuggestionPeriod.Months(2), SuggestionPeriod.Months(2),
            CompanyRecords: false));

    public override string ToString() => $"Aufgaben: {Tasks}; Termine: {Appointments}";
}

/// <summary>
/// «Aufgaben»: чи пропонувати, статус («Art des Datums» — насправді статус) і період від сьогодні. Для незавершених
/// і всіх завдань ADAICA бере дату «Beginnt am», для завершених — «Erledigt am». Хоча б один із періодів «Letzte(n)» і
/// «Nächste(n)» увімкнений завжди: якщо зняти останній, ADAICA вмикає інший. «0» — сьогодні, з точністю до дня.
/// </summary>
/// <param name="CompanyRecords">«Für Mitarbeiterkontakte auch die Aufgaben der Firma mit vorschlagen».</param>
public sealed record TaskSuggestionOptions(
    bool Enabled, TaskStatusFilter Status, SuggestionPeriod Previous, SuggestionPeriod Next, bool CompanyRecords)
{
    public override string ToString() =>
        Enabled ? $"{Status}, {Period(Previous, Next)}{(CompanyRecords ? ", mit Firma" : string.Empty)}" : "aus";

    internal static string Period(SuggestionPeriod previous, SuggestionPeriod next) => $"Letzte: {previous}, Nächste: {next}";
}

/// <summary>
/// «Termine»: чи пропонувати, дата («Erstelldatum», «Beginnt», «Endet») і проміжок: «Alle», «Zwischen» дат
/// <paramref name="From"/>–<paramref name="To"/> (останній день включно) чи «Zeitraum» від сьогодні.
/// </summary>
/// <param name="CompanyRecords">«Für Mitarbeiterkontakte auch die Termine der Firma mit vorschlagen».</param>
public sealed record AppointmentSuggestionOptions(
    bool Enabled,
    AppointmentDateField Field,
    AppointmentPeriodMode Mode,
    SuggestionPeriod Previous,
    SuggestionPeriod Next,
    bool CompanyRecords,
    DateTime? From = null,
    DateTime? To = null)
{
    public override string ToString()
    {
        if (!Enabled)
            return "aus";

        var period = Mode switch
        {
            AppointmentPeriodMode.All => "Alle",
            AppointmentPeriodMode.Between => $"Zwischen {From:dd.MM.yyyy} – {To:dd.MM.yyyy}",
            _ => TaskSuggestionOptions.Period(Previous, Next),
        };
        return $"{Field}, {period}{(CompanyRecords ? ", mit Firma" : string.Empty)}";
    }
}

/// <summary>
/// Період від сьогодні: прапорець «Letzte(n)» чи «Nächste(n)», кількість і одиниця («2 Monat(e)»). Кількість і одиницю
/// ADAICA зберігає й для вимкненого періоду і враховує, коли вирішує, чи «Anwender» збігається із «Systemweit».
/// </summary>
public sealed record SuggestionPeriod(int Count, PeriodUnit Unit, bool IsOn = true)
{
    public static SuggestionPeriod Days(int count) => new(count, PeriodUnit.Days);

    public static SuggestionPeriod Months(int count) => new(count, PeriodUnit.Months);

    /// <summary>Той самий період із вимкненим прапорцем.</summary>
    public SuggestionPeriod Off() => this with { IsOn = false };

    public override string ToString() => IsOn ? $"{Count} {Unit}" : "aus";
}

/// <summary>Статус завдань: «Unerledigt», «Erledigt und Unerledigt», «Erledigt».</summary>
public enum TaskStatusFilter
{
    Undone,
    All,
    Done,
}

/// <summary>Одиниці періоду: «Stunde(n)», «Tag(e)», «Woche(n)», «Monat(e)», «Jahr(e)».</summary>
public enum PeriodUnit
{
    Hours,
    Days,
    Weeks,
    Months,
    Years,
}

/// <summary>Дата терміну, за якою ADAICA відбирає пропозиції: «Erstelldatum», «Beginnt», «Endet».</summary>
public enum AppointmentDateField
{
    Created,
    Begin,
    End,
}

/// <summary>Проміжок для термінів: «Alle», «Zwischen», «Zeitraum».</summary>
public enum AppointmentPeriodMode
{
    All,
    Between,
    Period,
}
