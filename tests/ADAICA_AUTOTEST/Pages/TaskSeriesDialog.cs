using ADAICA_AUTOTEST.Controls;
using ADAICA_AUTOTEST.Infrastructure;
using Allure.Net.Commons;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;

namespace ADAICA_AUTOTEST.Pages;

/// <summary>
/// Вікно «Aufgabenzyklus» (кнопка «Serientyp» на бланку завдання або «Bearbeiten» на вкладці «Aufgabenzyklus»): як
/// повторюється завдання. Це дочірнє вікно вікна завдання (в огляді — головного вікна) без AutomationId, тому
/// впізнаємо його за перемикачем «Täglich». За замовчуванням серія щоденна, кожен день, без кінця, а наступне
/// завдання стає актуальним за добу до свого початку.
/// </summary>
/// <remarks>
/// AutomationId мають не всі поля: поля Serienmuster лежать усередині своїх перемикачів, а «Jeden Arbeitstag», «Ohne
/// Enddatum» і перемикачі «Enddatum für einzelne Zyklus-Aufgaben» шукаємо за підписом чи порядком. Поля невибраного
/// перемикача ADAICA вимикає, тож перемикач вибирається першим.
/// </remarks>
public sealed class TaskSeriesDialog
{
    private const string DailyId = "radioDayRepeatCycleSetting";
    private const string EveryNDaysId = "radioDayRepeatingCycleSetting";
    private const string MonthlyOnDayId = "radioDateRepeatingCycleSetting";
    private const string MonthlyOnWeekdayId = "radioDateInMonthRepeatingCycleSetting";
    private const string YearlyOnDateId = "radioDateInMonthYearCycleSetting";
    private const string YearlyOnWeekdayId = "radioDateInYearCycleSetting";
    private const string EndsAfterId = "radioCountCyclingCycleSetting";
    private const string TaskDueId = "EndDateCycleTasksControl";

    private readonly Window _owner;

    /// <param name="owner">Вікно завдання або, для завдання в огляді, головне вікно ADAICA.</param>
    public TaskSeriesDialog(Window owner)
    {
        _owner = owner;
        Root = Wait.For(
            () => ChildWindows(owner).FirstOrDefault(window => window.FindFirstDescendant(cf => cf.ByAutomationId(DailyId)) is not null),
            "вікна «Aufgabenzyklus»");

        Daily = Radio("Täglich", DailyId);
        Weekly = Radio("Wöchentlich", "radioWeekRepeatCycleSetting");
        Monthly = Radio("Monatlich", "radioMonthRepeatCycleSetting");
        Yearly = Radio("Jährlich", "radioYearRepeatCycleSetting");

        EveryNDays = Radio("Jeden/Alle … Tag(e)", EveryNDaysId);
        DaysInterval = new NumberBoxControl("Jeden/Alle … Tag(e)", () => EditIn(EveryNDaysId, 0));
        EveryWorkday = new RadioButtonControl("Jeden Arbeitstag", () => Named(PatternPanel, ControlType.RadioButton, "Jeden Arbeitstag"));

        // У Wöchentlich і Jährlich інтервал — перше поле панелі Serienmuster, поза перемикачами.
        WeeksInterval = new NumberBoxControl("Jede/Alle … Woche(n) am", PatternInterval);
        YearsInterval = new NumberBoxControl("Jede/Alle … Jahr(e)", PatternInterval);

        MonthlyOnDay = Radio("Am … Tag jedes … Monats", MonthlyOnDayId);
        MonthlyDay = new NumberBoxControl("Am … Tag", () => EditIn(MonthlyOnDayId, 0));
        MonthlyOnDayInterval = new NumberBoxControl("Tag jedes … Monats", () => EditIn(MonthlyOnDayId, 1));
        MonthlyOnWeekday = Radio("Am [Woche] [Wochentag] jedes … Monats", MonthlyOnWeekdayId);
        MonthlyWeek = new ComboBoxControl("Am [Woche] (Monatlich)", () => ComboIn(MonthlyOnWeekdayId, 0));
        MonthlyWeekday = new ComboBoxControl("Am [Wochentag] (Monatlich)", () => ComboIn(MonthlyOnWeekdayId, 1));
        MonthlyOnWeekdayInterval = new NumberBoxControl("[Wochentag] jedes … Monats", () => EditIn(MonthlyOnWeekdayId, 0));

        YearlyOnDate = Radio("Am [Tag] [Monat]", YearlyOnDateId);
        YearlyDay = new NumberBoxControl("Am [Tag] (Jährlich)", Locate.ById(Root, YearlyOnDateId, "tbRepeatDayOfMonth"));
        YearlyDateMonth = new ComboBoxControl("Am [Tag] [Monat]", () => ComboIn(YearlyOnDateId, 0));
        YearlyOnWeekday = Radio("Am [Woche] [Wochentag] im [Monat]", YearlyOnWeekdayId);
        YearlyWeek = new ComboBoxControl("Am [Woche] (Jährlich)", () => ComboIn(YearlyOnWeekdayId, 0));
        YearlyWeekday = new ComboBoxControl("Am [Wochentag] (Jährlich)", () => ComboIn(YearlyOnWeekdayId, 1));
        YearlyWeekdayMonth = new ComboBoxControl("im [Monat]", () => ComboIn(YearlyOnWeekdayId, 2));

        // «Enddatum für einzelne Zyklus-Aufgaben»: «Ohne Enddatum», «Am …ende beenden», «Über Dauer berechnen» і, для
        // Monatlich та Jährlich, «Month»/«Year» з днем (і місяцем).
        TaskWithoutDue = new RadioButtonControl("Enddatum der Aufgaben: Ohne Enddatum", () => TaskDueRadio(0));
        TaskDueAtEndOfPeriod = new RadioButtonControl("Am …ende beenden", () => TaskDueRadio(1));
        TaskDueByDuration = new RadioButtonControl("Über Dauer berechnen", () => TaskDueRadio(2));
        TaskDueOnDayOfMonth = Radio("Month: Am …", "rbtMonth");
        TaskDueOnDayOfYear = Radio("Year: Am …", "rbtYear");
        TaskDueDay = new NumberBoxControl("Month/Year: Am …", Locate.ById(Root, TaskDueId, "tbDay"));
        TaskDueMonth = new ComboBoxControl("Year: Am … [Monat]", () => Wait.For(
            () => Root.FindByAutomationId(TaskDueId).FindFirstDescendant(cf => cf.ByControlType(ControlType.ComboBox)),
            "списку місяців у «Year: Am …»"));

        Duration = new NumberBoxControl("Dauer", Locate.ById(Root, "tbDuration"));
        DurationUnit = new ComboBoxControl("Dauer: Einheit", () => Wait.For(
            () => Group("Dauer").FindFirstDescendant(cf => cf.ByControlType(ControlType.ComboBox)),
            "одиниці «Dauer»"));

        Start = new DatePickerControl("Beginn", Locate.ById(Root, "dateStart"));
        // «Ohne Enddatum» є і в «Enddatum für einzelne Zyklus-Aufgaben», тож шукаємо його в групі «Seriendauer».
        WithoutEnd = new RadioButtonControl("Seriendauer: Ohne Enddatum", () => Named(Group("Seriendauer"), ControlType.RadioButton, "Ohne Enddatum"));
        EndsAfter = Radio("Endet nach", EndsAfterId);
        // Поле кількості завдань не має AutomationId: це поле всередині перемикача «Endet nach».
        Count = new NumberBoxControl("Endet nach … Aufgaben", () => EditIn(EndsAfterId, 0));
        EndsOn = Radio("Endet am", "radioEndDateCyclingCycleSetting");
        EndDate = new DatePickerControl("Endet am", Locate.ById(Root, "dateEnd"));

        OkButton = new ButtonControl("OK", () => ButtonNamed("OK"));
        CancelButton = new ButtonControl("Abbrechen", () => ButtonNamed("Abbrechen"));
    }

    public RadioButtonControl Daily { get; }

    public RadioButtonControl Weekly { get; }

    public RadioButtonControl Monthly { get; }

    public RadioButtonControl Yearly { get; }

    public RadioButtonControl EveryNDays { get; }

    public NumberBoxControl DaysInterval { get; }

    public RadioButtonControl EveryWorkday { get; }

    public NumberBoxControl WeeksInterval { get; }

    public NumberBoxControl YearsInterval { get; }

    public RadioButtonControl MonthlyOnDay { get; }

    /// <summary>«Am N. Tag» у Monatlich.</summary>
    public NumberBoxControl MonthlyDay { get; }

    public NumberBoxControl MonthlyOnDayInterval { get; }

    public RadioButtonControl MonthlyOnWeekday { get; }

    /// <summary>Тиждень місяця в Monatlich: «1» … «4», «Letzten».</summary>
    public ComboBoxControl MonthlyWeek { get; }

    public ComboBoxControl MonthlyWeekday { get; }

    public NumberBoxControl MonthlyOnWeekdayInterval { get; }

    public RadioButtonControl YearlyOnDate { get; }

    /// <summary>«Am N.» у Jährlich.</summary>
    public NumberBoxControl YearlyDay { get; }

    public ComboBoxControl YearlyDateMonth { get; }

    public RadioButtonControl YearlyOnWeekday { get; }

    /// <summary>Тиждень місяця в Jährlich: «1» … «4», «Letzten».</summary>
    public ComboBoxControl YearlyWeek { get; }

    public ComboBoxControl YearlyWeekday { get; }

    public ComboBoxControl YearlyWeekdayMonth { get; }

    public RadioButtonControl TaskWithoutDue { get; }

    public RadioButtonControl TaskDueAtEndOfPeriod { get; }

    public RadioButtonControl TaskDueByDuration { get; }

    /// <summary>Monatlich: «Month» → «Am N».</summary>
    public RadioButtonControl TaskDueOnDayOfMonth { get; }

    /// <summary>Jährlich: «Year» → «Am N [Monat]».</summary>
    public RadioButtonControl TaskDueOnDayOfYear { get; }

    public NumberBoxControl TaskDueDay { get; }

    public ComboBoxControl TaskDueMonth { get; }

    public NumberBoxControl Duration { get; }

    public ComboBoxControl DurationUnit { get; }

    public DatePickerControl Start { get; }

    public RadioButtonControl WithoutEnd { get; }

    public RadioButtonControl EndsAfter { get; }

    public NumberBoxControl Count { get; }

    public RadioButtonControl EndsOn { get; }

    public DatePickerControl EndDate { get; }

    public ButtonControl OkButton { get; }

    public ButtonControl CancelButton { get; }

    // IsAvailable для закритого вікна WPF лишається true, тому перевіряємо, чи вікно ще серед дочірніх.
    public bool IsOpen => ChildWindows(_owner).Contains(Root);

    private Window Root { get; }

    // Панель полів вибраного Serienmuster: єдина непорожня панель у групі «Serienmuster».
    private AutomationElement PatternPanel => Wait.For(
        () => Group("Serienmuster").FindAllChildren(cf => cf.ByControlType(ControlType.Custom))
            .FirstOrDefault(panel => panel.FindFirstDescendant() is not null),
        "полів Serienmuster");

    /// <summary>Щоденна серія, яка закінчується після <paramref name="count"/> завдань.</summary>
    public void SetDaily(int count) => AllureApi.Step($"Задати щоденну серію з {count} завдань", () =>
    {
        Daily.Select();
        EndsAfter.Select();
        Count.Fill(count);
        Confirm();
    });

    /// <summary>
    /// Заповнює вікно: спершу Serienmuster (від нього залежать решта полів і перевірки), потім «Beginn», кінець серії
    /// й «Enddatum für einzelne Zyklus-Aufgaben». Вікно лишається відкритим: закриває його <see cref="Confirm"/>.
    /// </summary>
    public void Fill(TaskSeriesData series) => AllureApi.Step($"Задати серію: {series}", () =>
    {
        FillPattern(series.Pattern);
        Start.Fill(series.Start);
        FillEnd(series.End);
        FillTaskDue(series.Due);
    });

    /// <summary>
    /// Натискає OK і чекає, доки вікно закриється. Деякі серії ADAICA перед закриттям пояснює повідомленням (наприклад,
    /// «Einige Monate haben weniger als 31 Tage…»): його текст повертаємо, а повідомлення закриваємо кнопкою OK.
    /// </summary>
    /// <returns>Текст повідомлення або null, якщо його не було.</returns>
    public string? Confirm() => AllureApi.Step("Натиснути OK у вікні «Aufgabenzyklus»", () =>
    {
        OkButton.Click();
        string? message = null;
        Wait.Until(
            () =>
            {
                if (FindMessageBox() is { } dialog)
                {
                    message = dialog.Message.Text;
                    AllureApi.Step($"ADAICA повідомила: {message}");
                    dialog.Button("OK").Click();
                    return false;
                }

                return !IsOpen;
            },
            "закриття вікна «Aufgabenzyklus»");
        return message;
    });

    public void Cancel() => AllureApi.Step("Закрити вікно «Aufgabenzyklus» без змін", () =>
    {
        CancelButton.Click();
        Wait.Until(() => !IsOpen, "закриття вікна «Aufgabenzyklus»");
    });

    /// <summary>Для прибирання після тесту: закриває вікно, лише якщо воно ще відкрите.</summary>
    public void CancelIfOpen()
    {
        if (IsOpen)
            Cancel();
    }

    private void FillPattern(SeriesPattern pattern)
    {
        switch (pattern)
        {
            case SeriesPattern.Daily daily:
                Daily.Select();
                EveryNDays.Select();
                DaysInterval.Fill(daily.EveryDays);
                break;
            case SeriesPattern.Workdays:
                Daily.Select();
                EveryWorkday.Select();
                break;
            case SeriesPattern.Weekly weekly:
                Weekly.Select();
                WeeksInterval.Fill(weekly.EveryWeeks);
                // Спершу вмикаємо потрібні дні, потім вимикаємо решту: без жодного дня ADAICA сама вмикає понеділок.
                foreach (var day in weekly.Days)
                    WeekDay(day).SetChecked(true);
                foreach (var day in Enum.GetValues<DayOfWeek>().Except(weekly.Days))
                    WeekDay(day).SetChecked(false);
                break;
            case SeriesPattern.MonthlyOnDay monthly:
                Monthly.Select();
                MonthlyOnDay.Select();
                MonthlyDay.Fill(monthly.Day);
                MonthlyOnDayInterval.Fill(monthly.EveryMonths);
                break;
            case SeriesPattern.MonthlyOnWeekday monthly:
                Monthly.Select();
                MonthlyOnWeekday.Select();
                MonthlyWeek.Select(SeriesPattern.WeekLabel(monthly.Week));
                MonthlyWeekday.Select(AdaicaFormat.FormatDayOfWeek(monthly.Day));
                MonthlyOnWeekdayInterval.Fill(monthly.EveryMonths);
                break;
            case SeriesPattern.YearlyOnDate yearly:
                Yearly.Select();
                YearsInterval.Fill(yearly.EveryYears);
                YearlyOnDate.Select();
                YearlyDateMonth.Select(AdaicaFormat.FormatMonth(yearly.Month));
                YearlyDay.Fill(yearly.Day);
                break;
            case SeriesPattern.YearlyOnWeekday yearly:
                Yearly.Select();
                YearsInterval.Fill(yearly.EveryYears);
                YearlyOnWeekday.Select();
                YearlyWeek.Select(SeriesPattern.WeekLabel(yearly.Week));
                YearlyWeekday.Select(AdaicaFormat.FormatDayOfWeek(yearly.Day));
                YearlyWeekdayMonth.Select(AdaicaFormat.FormatMonth(yearly.Month));
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(pattern), pattern, "Невідомий Serienmuster.");
        }
    }

    private void FillEnd(SeriesEnd end)
    {
        switch (end)
        {
            case SeriesEnd.Never:
                WithoutEnd.Select();
                break;
            case SeriesEnd.AfterCount afterCount:
                EndsAfter.Select();
                Count.Fill(afterCount.Count);
                break;
            case SeriesEnd.OnDate onDate:
                EndsOn.Select();
                EndDate.Fill(onDate.Date);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(end), end, "Невідомий кінець серії.");
        }
    }

    private void FillTaskDue(SeriesTaskDue due)
    {
        switch (due)
        {
            case SeriesTaskDue.None:
                TaskWithoutDue.Select();
                break;
            case SeriesTaskDue.EndOfPeriod:
                TaskDueAtEndOfPeriod.Select();
                break;
            case SeriesTaskDue.AfterHours afterHours:
                TaskDueByDuration.Select();
                DurationUnit.Select("Stunde(n)");
                Duration.Fill(afterHours.Hours);
                break;
            case SeriesTaskDue.OnDayOfMonth onDay:
                TaskDueOnDayOfMonth.Select();
                TaskDueDay.Fill(onDay.Day);
                break;
            case SeriesTaskDue.OnDayOfYear onDay:
                TaskDueOnDayOfYear.Select();
                TaskDueMonth.Select(AdaicaFormat.FormatMonth(onDay.Month));
                TaskDueDay.Fill(onDay.Day);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(due), due, "Невідомий кінець завдань серії.");
        }
    }

    /// <summary>Прапорець дня тижня в Wöchentlich («Sonntag» … «Samstag»).</summary>
    public CheckBoxControl WeekDay(DayOfWeek day)
    {
        var name = AdaicaFormat.FormatDayOfWeek(day);
        return new CheckBoxControl(name, () => Named(PatternPanel, ControlType.CheckBox, name));
    }

    // Повідомлення ADAICA показує, поки вікно серії ще відкрите, але належати воно може й вікну завдання чи головному.
    private MessageBoxDialog? FindMessageBox() =>
        MessageBoxDialog.Find(Root)
        ?? MessageBoxDialog.Find(_owner)
        ?? (MainWindow() is { } main ? MessageBoxDialog.Find(main) : null);

    // Головне вікно ADAICA, якщо вікно серії відкрили з вікна завдання; якщо з огляду, _owner — вже головне вікно.
    private Window? MainWindow() =>
        !IsMainWindow(_owner) && _owner.Parent is { } parent && IsMainWindow(parent) ? parent.AsWindow() : null;

    // AutomationId є не в усіх елементів (у робочого столу його немає).
    private static bool IsMainWindow(AutomationElement element) =>
        element.Properties.AutomationId.ValueOrDefault == AdaicaWindow.AutomationId;

    private RadioButtonControl Radio(string name, string automationId) => new(name, Locate.ById(Root, automationId));

    private AutomationElement Group(string name) => Wait.For(
        () => Root.FindFirstDescendant(cf => cf.ByControlType(ControlType.Group).And(cf.ByName(name))),
        $"групи «{name}»");

    private AutomationElement PatternInterval() => Wait.For(
        () => PatternPanel.FindFirstChild(cf => cf.ByControlType(ControlType.Edit)),
        "поля інтервалу Serienmuster");

    private AutomationElement TaskDueRadio(int index) => Wait.For(
        () => Root.FindByAutomationId(TaskDueId).FindAllDescendants(cf => cf.ByControlType(ControlType.RadioButton)).ElementAtOrDefault(index),
        $"перемикача №{index + 1} у «Enddatum für einzelne Zyklus-Aufgaben»");

    private AutomationElement EditIn(string radioId, int index) => Wait.For(
        () => Root.FindByAutomationId(radioId).FindAllDescendants(cf => cf.ByControlType(ControlType.Edit)).ElementAtOrDefault(index),
        $"поля №{index + 1} у перемикачі {radioId}");

    private AutomationElement ComboIn(string radioId, int index) => Wait.For(
        () => Root.FindByAutomationId(radioId).FindAllDescendants(cf => cf.ByControlType(ControlType.ComboBox)).ElementAtOrDefault(index),
        $"списку №{index + 1} у перемикачі {radioId}");

    private static AutomationElement Named(AutomationElement scope, ControlType type, string name) => Wait.For(
        () => scope.FindFirstDescendant(cf => cf.ByControlType(type).And(cf.ByName(name))),
        $"«{name}»");

    private AutomationElement ButtonNamed(string name) => Wait.For(
        () => Root.FindFirstChild(cf => cf.ByControlType(ControlType.Button).And(cf.ByName(name)))
            ?? Root.FindFirstDescendant(cf => cf.ByControlType(ControlType.Button).And(cf.ByName(name))),
        $"кнопки «{name}» у вікні «Aufgabenzyklus»");

    private static IEnumerable<Window> ChildWindows(Window owner) =>
        owner.FindAllChildren(cf => cf.ByControlType(ControlType.Window)).Select(window => window.AsWindow());
}
