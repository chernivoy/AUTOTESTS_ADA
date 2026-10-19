using System.Drawing;
using System.Globalization;
using System.Text.RegularExpressions;
using ADAICA_AUTOTEST.Controls;
using ADAICA_AUTOTEST.Infrastructure;
using Allure.Net.Commons;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;

namespace ADAICA_AUTOTEST.Pages;

/// <summary>
/// Планувальник завдань DevExpress з панеллю фільтрів над ним: вікно «Kalender» навігатора Dashboard
/// (<see cref="TaskCalendarWindow"/>) і вид «Kalender» вузла «Aufgaben» під записом (<see cref="RecordTaskCalendar"/>).
/// На панелі — швидкий пошук, «Kontakt» (стовпці учасників), кнопки стану <see cref="TaskListFilter"/>, лічильник, «+» і
/// оновлення. Завдання — смужка від «Beginnt am» до «Fällig am» у стовпці кожного свого учасника; смужки від доби — у
/// верхній зоні, коротші — у сітці часу.
/// </summary>
/// <remarks>
/// <para>
/// Кнопки стану поєднуються за тими самими правилами, що й над списком «Meine Aufgaben» (<c>RecordListHelper.FilterTask</c>),
/// але всі вимкнені й нічого не запам'ятовується: планувальник відкривається у «Wochenansicht» на поточному тижні. Після
/// будь-якої зміни фільтра чи пошуку планувальник сам переходить на тиждень завдання з найпізнішим «Beginnt am» серед
/// тих, що лишилися.
/// </para>
/// <para>
/// UI Automation бачить у планувальнику лише смужки: ListItem з назвою «Appointment Oktober 12, 2026, from 09: 30 , to
/// Oktober 12, 2026 11: 30 , Subject Назва» (час — 12-годинний без AM/PM, тож береться лише дата). Вид і період — у
/// назві самого планувальника. Клітинки, заголовки днів, «‹ ›» і кнопки видів UI Automation не показує: вид і дату
/// тест змінює меню клітинки, а клітинку знаходить за координатами.
/// </para>
/// </remarks>
public abstract partial class TaskCalendar
{
    private const string SchedulerId = "schedulerControlV2";
    private const string CountId = "tabScrollCount";

    // Планувальник завантажує завдання вже показаним.
    protected static readonly TimeSpan LoadTimeout = TimeSpan.FromSeconds(60);

    // Фільтр діє одразу, але смужки й лічильник WPF оновлює не за один прохід: планувальник вважаємо готовим, коли вони
    // стільки часу не змінюються.
    private static readonly TimeSpan SettleTime = TimeSpan.FromMilliseconds(700);

    private static readonly CultureInfo German = CultureInfo.GetCultureInfo("de-DE");

    /// <param name="owner">Головне вікно ADAICA.</param>
    /// <param name="root">Елемент, у якому панель фільтрів і планувальник.</param>
    /// <param name="place">Де планувальник, для звіту: «у вікні «Kalender»».</param>
    /// <param name="filters">Кнопки стану на панелі фільтрів.</param>
    protected TaskCalendar(Window owner, AutomationElement root, string place, IEnumerable<TaskListFilter> filters)
    {
        Owner = owner;
        Root = root;
        Place = place;
        QuickSearch = new TextBoxControl("Suchbegriff eingeben...", Locate.ById(Root, "QsFilterControl", "txtQuickSearch"));
        SearchModeList = new ComboBoxControl("Suchmodus", Locate.ById(Root, "cmbRecordSearchOptCtrFilterRecordFields"));
        Filters = filters.ToDictionary(
            filter => filter,
            filter => new ToggleButtonControl(filter.Label(), Locate.ById(Root, "TaskStatesFilter", filter.AutomationId())));

        // У тесту завжди є своє завдання, тож порожній лічильник означає, що завдання ще не завантажено.
        Wait.Until(() => Count.Total > 0, $"завдань {Place}", LoadTimeout);
        WaitUntilSettled($"завантаження планувальника {Place}");
    }

    public TextBoxControl QuickSearch { get; }

    /// <summary>
    /// Режим швидкого пошуку: «Alle Wörter», «Exakte Suche», «Еinzelnes Wort» (як і у «Finden», з кириличною «Е», тож
    /// пункт вибираємо за позицією, <see cref="Pages.SearchMode"/>). Доступний, коли поле не порожнє.
    /// </summary>
    public ComboBoxControl SearchModeList { get; }

    public IReadOnlyDictionary<TaskListFilter, ToggleButtonControl> Filters { get; }

    /// <summary>Підписи кнопок стану, які показує панель фільтрів, зліва направо (приховані кнопки в дереві відсутні).</summary>
    public IReadOnlyList<string> StateFilterLabels =>
        Root.FindByAutomationId("TaskStatesFilter")
            .FindAllChildren(cf => cf.ByControlType(ControlType.Button))
            .Select(button => button.Name)
            .ToList();

    /// <summary>
    /// Лічильник праворуч від фільтрів: «показані/усього» — скільки різних завдань у показаному періоді й скільки
    /// всього лишилося після фільтрів і пошуку; без завдань — «0».
    /// </summary>
    public CalendarCount Count => CalendarCount.Parse(CountText);

    /// <summary>Вид планувальника з його назви («Scheduler Control, Wochenansicht, …»).</summary>
    public CalendarView View => CalendarViews.Parse(ParsedSchedulerName.Groups["view"].Value);

    /// <summary>Показаний період: перший день і день після останнього.</summary>
    public (DateTime From, DateTime To) Period
    {
        get
        {
            var match = ParsedSchedulerName;
            return (ParseDate(match.Groups["from"].Value), ParseDate(match.Groups["to"].Value));
        }
    }

    /// <summary>
    /// Смужки показаного періоду як рядки «назва + дата початку» (<see cref="TaskListRow"/>), у стовпцях усіх
    /// показаних учасників. Смужок, що лише заходять у період, теж стосується: дата — день початку завдання.
    /// </summary>
    public IReadOnlyList<TaskListRow> Bars =>
        BarElements().Select(ParseBar).OfType<TaskListRow>().ToList();

    /// <summary>
    /// Бланк завдання, вибраного в планувальнику (<see cref="SelectTask"/>), — у панелі під ним.
    /// </summary>
    public abstract TaskForm Details { get; }

    /// <summary>Головне вікно ADAICA.</summary>
    protected Window Owner { get; }

    /// <summary>Елемент, у якому панель фільтрів і планувальник.</summary>
    protected AutomationElement Root { get; }

    /// <summary>Де планувальник, для звіту: «у вікні «Kalender»».</summary>
    protected string Place { get; }

    /// <summary>Вікно, якому належать вікна, що відкривають меню й кнопки планувальника (нове завдання, питання).</summary>
    protected abstract Window DialogOwner { get; }

    /// <summary>Вікна WPF Popup, серед яких ADAICA відкриває меню й випадні списки планувальника (див. <see cref="Popups"/>).</summary>
    protected abstract IEnumerable<AutomationElement> PopupWindows { get; }

    private AutomationElement Scheduler => Root.FindByAutomationId(SchedulerId);

    private string CountText => TextInput.Read(CounterElement());

    // Лічильник лише показує число, тож клік по ньому нічого не змінює й закриває відкриті списки та меню.
    protected AutomationElement CounterElement() => Root.FindByAutomationId(CountId);

    private Match ParsedSchedulerName
    {
        get
        {
            var name = Scheduler.Name;
            var match = SchedulerName().Match(name);
            return match.Success ? match : throw new FormatException($"Незрозуміла назва планувальника: «{name}».");
        }
    }

    /// <summary>Чи відкрите меню планувальника або випадний список панелі фільтрів.</summary>
    protected bool HasOpenPopup() =>
        PopupWindows.Any(popup => popup.FindFirstChild(cf => cf.ByClassName("PopupMenu")) is not null
            || popup.FindFirstDescendant(cf => cf.ByControlType(ControlType.Tree)) is not null);

    /// <summary>Вимикає всі кнопки стану: планувальник показує всі завдання (так він і відкривається).</summary>
    public void ShowAll() => AllureApi.Step($"Вимкнути всі фільтри стану {Place}", () =>
    {
        foreach (var toggle in Filters.Values)
            toggle.SwitchOff();
        WaitUntilSettled("застосування фільтрів");
    });

    /// <summary>Вмикає лише фільтр <paramref name="filter"/>, решту вимикає.</summary>
    public void ShowOnly(TaskListFilter filter) => AllureApi.Step($"Увімкнути {Place} лише фільтр «{filter.Label()}»", () =>
    {
        Filters[filter].SwitchOn();
        foreach (var (other, toggle) in Filters)
        {
            if (other != filter)
                toggle.SwitchOff();
        }

        WaitUntilSettled("застосування фільтрів");
    });

    /// <summary>
    /// Вмикає фільтри по одному й повертає ті, під якими планувальник показує <paramref name="task"/>. Планувальник краще
    /// заздалегідь звузити швидким пошуком: тоді після фільтра він сам переходить на тиждень завдання.
    /// </summary>
    public IReadOnlyList<TaskListFilter> FiltersShowing(TaskListRow task) =>
        AllureApi.Step($"Увімкнути {Place} фільтри по одному й перевірити, під якими видно «{task}»", () =>
        {
            var showing = new List<TaskListFilter>();
            foreach (var filter in Filters.Keys)
            {
                ShowOnly(filter);
                if (IsShown(task))
                    showing.Add(filter);
            }

            AllureApi.Step($"Завдання видно під фільтрами: {string.Join(", ", showing.Select(filter => filter.Label()))}");
            return showing;
        });

    /// <summary>
    /// Чи показує планувальник завдання <paramref name="task"/>. Після пошуку й фільтрів планувальник сам переходить на
    /// тиждень завдання з найпізнішим «Beginnt am», тож коли його звужено до одного завдання, смужку видно в показаному
    /// періоді. Якщо знайдено й пізніші завдання, тест повертається на поточний тиждень («Heute»): завдання тестів
    /// здебільшого сьогоднішні. На інший тиждень перейти не можна: «Gehe zu Datum...» не переходить (README, «Вікно
    /// «Kalender» над «Meine Aufgaben»»), тож тоді відповіді немає.
    /// </summary>
    public bool IsShown(TaskListRow task)
    {
        if (Count.Total == 0)
            return false;
        if (Bars.Contains(task))
            return true;
        if (Contains(Period, task.Begin))
            return false;

        GoToToday();
        if (!Contains(Period, task.Begin))
            throw new InvalidOperationException(
                $"Планувальник {Place} показує {Period.From:dd.MM.yyyy}–{Period.To.AddDays(-1):dd.MM.yyyy}, а перейти на день «{task}» тест не може.");

        return Bars.Contains(task);
    }

    /// <summary>
    /// Звужує планувальник швидким пошуком: слова <paramref name="text"/> шукаються в полях, позначених у «Suchen in»
    /// (Name, Kurzname, Nummer, Bemerkung), у режимі <paramref name="mode"/> (без нього — як є). Пошук застосовує лише
    /// клік по лупі або Enter, а лупа свого стану не змінює, тож подіяв клік чи ні, видно з лічильника. Тому попередній
    /// пошук спершу скасовується, а <paramref name="text"/> не має знаходити всі завдання планувальника.
    /// </summary>
    public void Search(string text, SearchMode? mode = null) =>
        AllureApi.Step($"Знайти «{text}» швидким пошуком {Place}{(mode is { } chosen ? $" ({chosen.Label()})" : string.Empty)}", () =>
        {
            ClearSearch();
            var before = CountText;
            QuickSearch.Fill(text);
            if (mode is { } searchMode)
                SearchModeList.SelectAt((int)searchMode);

            UiInput.LeftClick(
                () => Root.FindByAutomationId("btnQuickSearchApplyFilter"),
                () => CountText != before,
                $"результату пошуку «{text}» у лічильнику {Place}");
            WaitUntilSettled($"результату пошуку «{text}»");
        });

    /// <summary>Скасовує швидкий пошук кнопкою «×» у полі, якщо в полі є текст.</summary>
    public void ClearSearch()
    {
        if (QuickSearch.Text.Length == 0)
            return;

        AllureApi.Step($"Скасувати швидкий пошук {Place}", () =>
        {
            Root.FindByAutomationId("btnQuickSearchClear").Patterns.Invoke.Pattern.Invoke();
            Wait.Until(() => QuickSearch.Text.Length == 0, "порожнього поля швидкого пошуку");
            WaitUntilSettled("скасування швидкого пошуку");
        });
    }

    /// <summary>Відкриває «Suchen in (Suchkriterien)» — поля, у яких шукає швидкий пошук.</summary>
    public TreeFilterPopup OpenSearchFields() => AllureApi.Step("Відкрити «Suchen in (Suchkriterien)» кнопкою біля режиму пошуку", () =>
        new TreeFilterPopup(
            CounterElement,
            () => PopupWindows,
            "Suchen in (Suchkriterien)",
            () => UiInput.LeftClickOpeningMenu(
                () => Root.FindByAutomationId("btnRecordFields"),
                () => PopupWindows.Any(popup => popup.FindFirstDescendant(cf => cf.ByAutomationId("FilterRecordFields2")) is not null),
                "списку «Suchen in (Suchkriterien)»"),
            () => WaitUntilSettled("застосування полів пошуку")));

    /// <summary>Відкриває «Kontakt»: учасники, чиї стовпці показує планувальник.</summary>
    public TreeFilterPopup OpenContacts() => AllureApi.Step("Відкрити список «Kontakt»", () =>
        new TreeFilterPopup(
            CounterElement,
            () => PopupWindows,
            "Kontakt",
            () => UiInput.LeftClickOpeningMenu(
                () => Root.FindByAutomationId("TaskStatesFilter").FindByAutomationId("filterOpenButton"),
                () => PopupWindows.Any(popup => popup.FindFirstDescendant(cf => cf.ByControlType(ControlType.Tree)) is not null),
                "списку «Kontakt»"),
            () => WaitUntilSettled("зміни стовпців учасників")));

    /// <summary>
    /// Вибирає смужку завдання <paramref name="task"/> кліком; <paramref name="index"/> — котра з однакових смужок
    /// (за назвою й днем) згори вниз. Бланк вибраного завдання ADAICA показує в панелі під планувальником
    /// (<see cref="Details"/>) на вкладці, яку відкривали останньою, тож тест відкриває головну.
    /// </summary>
    public void SelectTask(TaskListRow task, int index = 0) =>
        AllureApi.Step($"Вибрати смужку «{task}»{(index > 0 ? $" (№{index + 1} згори)" : string.Empty)}", () =>
        {
            AutomationElement Bar() => Wait.For(
                () => BarElements()
                    .Where(bar => ParseBar(bar) == task && !bar.IsOffscreen && bar.BoundingRectangle.Width > 0)
                    .OrderBy(bar => bar.BoundingRectangle.Top)
                    .ElementAtOrDefault(index),
                $"смужки «{task}» №{index + 1} згори");
            RemindersPopup.Collapse(Owner);
            UiInput.LeftClick(Bar, () => Bar().Patterns.SelectionItem.Pattern.IsSelected.Value, $"вибору смужки «{task}»");
            Details.ShowMainTab();
            Wait.Until(() => Details.Number.Text.Length > 0, "бланка вибраного завдання під планувальником");
        });

    /// <summary>
    /// Чекає, доки планувальник перестане змінюватися: лічильник, вид, період і кількість смужок не змінюються
    /// <see cref="SettleTime"/>.
    /// </summary>
    protected void WaitUntilSettled(string description, TimeSpan? timeout = null)
    {
        string? last = null;
        var stableSince = DateTime.MaxValue;
        Wait.Until(
            () =>
            {
                var current = $"{CountText}|{Scheduler.Name}|{BarElements().Count}";
                if (current != last)
                {
                    last = current;
                    stableSince = DateTime.Now;
                    return false;
                }

                return DateTime.Now - stableSince >= SettleTime;
            },
            description,
            timeout ?? LoadTimeout);
    }

    private IReadOnlyList<AutomationElement> BarElements() =>
        Scheduler.FindAllChildren(cf => cf.ByControlType(ControlType.ListItem));

    /// <summary>Смужка завдання <paramref name="task"/> на екрані (перша, якщо їх кілька).</summary>
    private AutomationElement BarElement(TaskListRow task) =>
        Wait.For(
            () => BarElements().FirstOrDefault(bar => ParseBar(bar) == task && !bar.IsOffscreen && bar.BoundingRectangle.Width > 0),
            $"смужки «{task}» {Place}");

    private static TaskListRow? ParseBar(AutomationElement bar)
    {
        var match = BarName().Match(bar.Name);
        return match.Success ? new TaskListRow(match.Groups["subject"].Value, ParseDate(match.Groups["from"].Value)) : null;
    }

    private static DateTime ParseDate(string text) => DateTime.ParseExact(text, "MMMM d, yyyy", German);

    private static Point Offset(Point point, Rectangle origin) => new(point.X - origin.X, point.Y - origin.Y);

    [GeneratedRegex(@"^Scheduler Control, (?<view>[^,]+), (?<from>\p{L}+ \d{1,2}, \d{4}), from [\d: ]+ , to (?<to>\p{L}+ \d{1,2}, \d{4}) ")]
    private static partial Regex SchedulerName();

    [GeneratedRegex(@"^Appointment (?<from>\p{L}+ \d{1,2}, \d{4}), from [\d: ]+ , to (?<to>\p{L}+ \d{1,2}, \d{4}) [\d: ]+ , Subject (?<subject>.*)$", RegexOptions.Singleline)]
    private static partial Regex BarName();
}

/// <summary>Лічильник планувальника: завдання в показаному періоді й усього після фільтрів і пошуку.</summary>
public sealed record CalendarCount(int Shown, int Total)
{
    public static CalendarCount Parse(string text)
    {
        var parts = text.Split('/');
        return parts.Length == 2
            ? new CalendarCount(int.Parse(parts[0], CultureInfo.InvariantCulture), int.Parse(parts[1], CultureInfo.InvariantCulture))
            : new CalendarCount(int.Parse(text, CultureInfo.InvariantCulture), int.Parse(text, CultureInfo.InvariantCulture));
    }

    public override string ToString() => $"{Shown}/{Total}";
}
