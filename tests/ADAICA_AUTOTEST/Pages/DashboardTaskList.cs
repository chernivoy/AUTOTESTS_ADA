using System.Text.RegularExpressions;
using ADAICA_AUTOTEST.Controls;
using ADAICA_AUTOTEST.Infrastructure;
using Allure.Net.Commons;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using VirtualKeyShort = FlaUI.Core.WindowsAPI.VirtualKeyShort;

namespace ADAICA_AUTOTEST.Pages;

/// <summary>
/// Список «Meine Aufgaben», який навігатор Dashboard показує на вузлі користувача: швидкий пошук, кнопки-фільтри
/// за станом (<see cref="TaskListFilter"/>) і «+» для нового завдання. Список показує завдання, де користувач
/// виконавець, з датами поблизу сьогодні. Стан кнопок ADAICA не запам'ятовує: у новому сеансі ввімкнено лише «Unerledigt».
/// </summary>
/// <remarks>
/// Як поєднуються фільтри (з коду ADAICA, <c>RecordListHelper.FilterTask</c>): групи «Ungelesen/Akzeptieren/
/// Angenommen/Abgelehnt», «Erledigt/Unerledigt», «Offen/Geschlossen», «Aktuell/Geplant» і «Überfällig» діють разом
/// (через «і»). Група не відсіює нічого, коли в ній увімкнено всі кнопки або жодної. Непрочитане завдання фільтри
/// прийняття не показують: його видно лише з «Ungelesen». Фільтри й швидкий пошук працюють над уже завантаженим
/// списком, тож він змінюється одразу після натискання.
/// </remarks>
public sealed partial class DashboardTaskList
{
    private const string ViewId = "gvTasksGeneralView";

    // Нові завдання серії ADAICA створює на сервері, і в списку вони з'являються за кілька секунд.
    private static readonly TimeSpan RowsTimeout = TimeSpan.FromSeconds(30);

    // UIA_ScrollPatternNoScroll: не змінювати прокрутку по цій осі.
    private const double NoScroll = -1;

    /// <summary>
    /// Колонки таблиці: підпис у «Spaltenkonfiguration» → AutomationId комірки. У заголовку підпис є не в усіх
    /// колонок («Erledigt» і «Aktion» — картинки), а колонок поза екраном UI Automation не підписує.
    /// </summary>
    public static readonly IReadOnlyDictionary<string, string> ColumnIds = new Dictionary<string, string>
    {
        ["Erledigt"] = "RowData.Row.IsDone",
        ["Name"] = "RowData.Row.Name",
        ["Beginnt am"] = "RowData.Row.BeginDate",
        ["Erstellt durch"] = "RowData.Row.EntryUser",
        ["Kurzname"] = "RowData.Row.ShortName",
        ["Bemerkung"] = "RowData.Row.Remark",
        ["% erledigt"] = "RowData.Row.Executed",
        ["Ablehnungsdatum"] = "RowData.Row.RefuseDate",
        ["Aktion"] = "RowData.Row.State",
        ["Änderungsdatum"] = "RowData.Row.ChangeDate",
        ["Annahmeart"] = "RowData.Row.AuthorityName",
        ["Annahmedatum"] = "RowData.Row.AcceptDate",
        ["Auftragsnummer"] = "RowData.Row.OrderNo",
        ["Ausführende"] = "RowData.Row.Addressee",
        ["Bestell-Nr."] = "RowData.Row.BookingNo",
        ["Dauer (gesch.)"] = "RowData.Row.EstimateString",
        ["Dauer (real)"] = "RowData.Row.TimeDurationString",
        ["Erledigt am"] = "RowData.Row.DoneDate",
        ["Erstelldatum"] = "RowData.Row.EntryDate",
        ["Fällig am"] = "RowData.Row.EndDate",
        ["Geändert durch"] = "RowData.Row.ChangeUserName",
        ["Geschlossen am"] = "RowData.Row.CloseDate",
        ["Hinweis"] = "RowData.Row.Reference",
        ["Hinweis (Wert)"] = "RowData.Row.ReferenceRemark",
        ["Icon"] = "RowData.Row.Record",
        ["ID"] = "RowData.Row.ID",
        ["Klasse"] = "RowData.Row.VUBClass",
        ["Klassentyp"] = "RowData.Row.TaskClassType",
        ["Kundennutzen"] = "RowData.Row.CustomerValue",
        ["Kundennutzen (Wert)"] = "RowData.Row.CustomerRemark",
        ["Nummer"] = "RowData.Row.Number",
        ["Priorität"] = "RowData.Row.PriorityName",
        ["Standort"] = "RowData.Row.Place",
        ["Standort (Wert)"] = "RowData.Row.PlaceRemark",
        ["Status"] = "RowData.Row.StatusName",
        ["Systembemerkung"] = "RowData.Row.SysRemark",
        ["Systemnotiz"] = "RowData.Row.SysNote",
        ["Unterklasse"] = "RowData.Row.VUBSubClass",
        ["Verantwortlich"] = "RowData.Row.ExecuteUserName",
        ["Wichtigkeit"] = "RowData.Row.SignificanceString",
    };

    private readonly Window _owner;

    /// <param name="owner">Головне вікно ADAICA.</param>
    /// <param name="userName">Користувач, чий день показує список («Chernivoy, Igor»), — назва вузла в дереві.</param>
    public DashboardTaskList(Window owner, string userName)
    {
        _owner = owner;
        UserName = userName;
        QuickSearch = new TextBoxControl("Suchbegriff eingeben…", Locate.ById(owner, ViewId, "txtQuickSearch"));
        AddButton = new ButtonControl("+", Locate.ById(owner, ViewId, "btnAdd"));
        Filters = Enum.GetValues<TaskListFilter>().ToDictionary(
            filter => filter,
            filter => new ToggleButtonControl(filter.Label(), Locate.ById(owner, ViewId, filter.AutomationId())));
        // Список з'являється, коли ADAICA покаже день користувача.
        _ = View;
    }

    /// <summary>Користувач, чий день показує список, як його підписує ADAICA: «Nachname, Vorname».</summary>
    public string UserName { get; }

    /// <summary>Підписи колонок, які зараз показує таблиця, зліва направо.</summary>
    public IReadOnlyList<string> ColumnNames =>
        View.FindByAutomationId("HeaderPanel").FindAllChildren(cf => cf.ByControlType(ControlType.HeaderItem))
            .Select(header => ColumnIds.FirstOrDefault(column => column.Value == header.Name).Key ?? header.Name)
            .ToList();

    public TextBoxControl QuickSearch { get; }

    public ButtonControl AddButton { get; }

    public IReadOnlyDictionary<TaskListFilter, ToggleButtonControl> Filters { get; }

    /// <summary>Рядки, які зараз показує список. Список віртуалізований: рядків поза видимою областю тут немає.</summary>
    public IReadOnlyList<TaskListRow> Rows =>
        View.FindAllDescendants(cf => cf.ByControlType(ControlType.DataItem))
            .Select(row => new TaskListRow(
                CellValue(row, "RowData.Row.Name"),
                AdaicaFormat.ParseDate(CellValue(row, "RowData.Row.BeginDate")) ?? DateTime.MinValue))
            .ToList();

    private AutomationElement View => Wait.For(() => _owner.FindFirstDescendant(cf => cf.ByAutomationId(ViewId)), "списку «Meine Aufgaben»");

    public TaskDialog OpenNewTaskDialog() => AllureApi.Step("Відкрити нове вікно завдання кнопкою «+» над списком «Meine Aufgaben»", () =>
    {
        AddButton.Click();
        return new TaskDialog(_owner);
    });

    /// <summary>Вимикає всі фільтри: список показує всі завдання.</summary>
    public void ShowAll() => AllureApi.Step("Вимкнути всі фільтри списку «Meine Aufgaben»", () =>
    {
        foreach (var toggle in Filters.Values)
            toggle.SwitchOff();
    });

    /// <summary>Вмикає лише фільтр <paramref name="filter"/>, решту вимикає.</summary>
    public void ShowOnly(TaskListFilter filter) => AllureApi.Step($"Увімкнути лише фільтр «{filter.Label()}»", () =>
    {
        Filters[filter].SwitchOn();
        foreach (var (other, toggle) in Filters)
        {
            if (other != filter)
                toggle.SwitchOff();
        }
    });

    /// <summary>
    /// Звужує список швидким пошуком (шукає в назві, Kurzname, номері й примітці) і чекає, доки список покаже саме
    /// <paramref name="expectedRows"/>. Номера в списку не видно, тож без очікуваних рядків не зрозуміти, що пошук уже застосовано.
    /// </summary>
    public void Search(string text, IReadOnlyCollection<TaskListRow> expectedRows) =>
        AllureApi.Step($"Знайти «{text}» швидким пошуком над списком «Meine Aufgaben»", () =>
        {
            QuickSearch.Fill(text);
            // Пошук застосовує лише клік по кнопці з лупою або Enter: це ToggleButton, і Toggle з UI Automation
            // його команду не виконує.
            UiInput.LeftClick(
                () => View.FindByAutomationId("btnQuickSearchApplyFilter"),
                () => Rows.ToHashSet().SetEquals(expectedRows),
                $"рядків {string.Join(", ", expectedRows)} у списку «Meine Aufgaben»",
                RowsTimeout);
        });

    public bool IsShown(TaskListRow row) => Rows.Contains(row);

    /// <summary>
    /// Вмикає фільтри по одному й повертає ті, під якими список показує <paramref name="row"/>. Список треба
    /// заздалегідь звузити швидким пошуком: у довгому списку рядок може бути поза видимою областю.
    /// </summary>
    public IReadOnlyList<TaskListFilter> FiltersShowing(TaskListRow row) =>
        AllureApi.Step($"Увімкнути фільтри по одному й перевірити, під якими видно «{row}»", () =>
        {
            var showing = new List<TaskListFilter>();
            foreach (var filter in Filters.Keys)
            {
                ShowOnly(filter);
                if (IsShown(row))
                    showing.Add(filter);
            }

            AllureApi.Step($"Завдання видно під фільтрами: {string.Join(", ", showing.Select(filter => filter.Label()))}");
            return showing;
        });

    /// <summary>Відхиляє завдання з контекстного меню рядка («Ablehnen»).</summary>
    public void Refuse(TaskListRow row) => AllureApi.Step($"Відхилити «{row}»: контекстне меню → Ablehnen", () =>
    {
        ContextMenuItem(row, "Ablehnen").Click();

        // Пункт меню виконує команду асинхронно. Коли відмову збережено, у меню стає доступним «Annehmen».
        Wait.Until(
            () =>
            {
                var isAccepting = ContextMenuItem(row, "Annehmen").IsEnabled;
                if (FindInPopups("TaskContextMenu") is { } menu)
                    UiInput.PressKey(() => menu, VirtualKeyShort.ESCAPE, () => FindInPopups("TaskContextMenu") is null, "закриття контекстного меню");
                return isAccepting;
            },
            $"відмови від «{row}» (пункт «Annehmen» стає доступним)");
    });

    /// <summary>
    /// Відкриває «Spaltenkonfiguration» з контекстного меню заголовка колонки «Name». Меню відкривається лише правим
    /// кліком; пункти в ньому — кнопки DevExpress з AutomationId, у якому є випадковий GUID, тож шукаємо за підписом.
    /// </summary>
    public ColumnsConfigDialog OpenColumnsConfig() =>
        AllureApi.Step("Відкрити «Spaltenkonfiguration»: заголовок колонки Name → контекстне меню → Änderung der Spaltenkonfiguration", () =>
        {
            // Заголовок колонки не має AutomationId, а його Name — ідентифікатор колонки («RowData.Row.Name»).
            UiInput.RightClick(
                () => Wait.For(
                    () => View.FindByAutomationId("HeaderPanel").FindFirstChild(cf => cf.ByControlType(ControlType.HeaderItem).And(cf.ByName(ColumnIds["Name"]))),
                    "заголовка колонки «Name»"),
                () => FindInPopups("GridContextMenu") is not null,
                "контекстного меню заголовка колонки");
            var menu = Wait.For(() => FindInPopups("GridContextMenu"), "контекстного меню заголовка колонки");
            new ButtonControl("Änderung der Spaltenkonfiguration", () => Wait.For(
                    () => menu.FindFirstDescendant(cf => cf.ByControlType(ControlType.Button).And(cf.ByName("Änderung der Spaltenkonfiguration"))),
                    "пункту «Änderung der Spaltenkonfiguration»"))
                .Click();
            return new ColumnsConfigDialog(_owner);
        });

    /// <summary>
    /// Текст рядка <paramref name="row"/> у кожній показаній колонці (підпис колонки → текст). Показаний текст є лише
    /// в комірках на екрані, тож таблицю гортаємо праворуч, а потім повертаємо на початок. Колонки-картинки
    /// («Erledigt», «Aktion», «Icon») тексту не мають: для них береться значення, яке називає UI Automation
    /// («Angewählt», «Done»).
    /// </summary>
    public IReadOnlyDictionary<string, string> CellTexts(TaskListRow row) =>
        AllureApi.Step($"Прочитати всі колонки рядка «{row}»", () =>
        {
            var texts = new Dictionary<string, string>();
            var scroll = View.FindByAutomationId("dataControl").Patterns.Scroll.Pattern;
            scroll.SetScrollPercent(0, NoScroll);
            try
            {
                while (true)
                {
                    var cells = RowCell(row, ColumnIds["Name"]).Parent.FindAllChildren();
                    foreach (var cell in cells.Where(cell => !cell.IsOffscreen))
                    {
                        var column = ColumnIds.FirstOrDefault(pair => pair.Value == cell.AutomationId).Key ?? cell.AutomationId;
                        texts.TryAdd(column, CellText(cell));
                    }

                    if (!scroll.HorizontallyScrollable.Value || scroll.HorizontalScrollPercent.Value >= 99.9)
                        break;

                    var before = scroll.HorizontalScrollPercent.Value;
                    scroll.Scroll(ScrollAmount.LargeIncrement, ScrollAmount.NoAmount);
                    Wait.Until(() => scroll.HorizontalScrollPercent.Value > before, "прокрутки таблиці праворуч");
                }
            }
            finally
            {
                scroll.SetScrollPercent(0, NoScroll);
            }

            AllureApi.AddAttachment(
                "Колонки рядка",
                "text/plain",
                System.Text.Encoding.UTF8.GetBytes(string.Join(Environment.NewLine, texts.Select(pair => $"{pair.Key}: {pair.Value}"))),
                ".txt");
            return (IReadOnlyDictionary<string, string>)texts;
        });

    /// <summary>Контекстне меню рядка відкривається лише правим кліком.</summary>
    private MenuItemControl ContextMenuItem(TaskListRow row, string itemName)
    {
        UiInput.RightClick(
            () => RowCell(row, "RowData.Row.Name"),
            () => FindInPopups("TaskContextMenu") is not null,
            $"контекстного меню «{row}»");
        var menu = Wait.For(() => FindInPopups("TaskContextMenu"), $"контекстного меню «{row}»");
        return new MenuItemControl(itemName, () => Wait.For(
            () => menu.FindFirstDescendant(cf => cf.ByControlType(ControlType.MenuItem).And(cf.ByName(itemName))),
            $"пункту «{itemName}» у контекстному меню"));
    }

    private AutomationElement RowCell(TaskListRow row, string columnId) =>
        Wait.For(
            () => View.FindAllDescendants(cf => cf.ByControlType(ControlType.DataItem))
                .FirstOrDefault(item => CellValue(item, "RowData.Row.Name") == row.Name
                    && AdaicaFormat.ParseDate(CellValue(item, "RowData.Row.BeginDate")) == row.Begin)
                ?.FindFirstChild(cf => cf.ByAutomationId(columnId)),
            $"рядка «{row}» у списку «Meine Aufgaben»");

    // Меню ADAICA відкриваються у WPF Popup (див. Popups).
    private AutomationElement? FindInPopups(string automationId) =>
        Popups.FindDescendant(_owner, cf => cf.ByAutomationId(automationId));

    // Показаний текст лежить у полі редагування комірки. Порожня дата в назві комірки — «01.01.0001» чи «11.11.1911»,
    // тож назву беремо лише для колонок без поля (картинок).
    private static string CellText(AutomationElement cell) =>
        cell.FindFirstDescendant(cf => cf.ByControlType(ControlType.Edit)) is { } editor
            ? editor.Patterns.Value.Pattern.Value.Value
            : CellName().Replace(cell.Name, string.Empty);

    // Комірка підписана «значення, Eintrag: 0, angezeigter Spaltenindex: 1».
    private static string CellValue(AutomationElement row, string columnId) =>
        row.FindFirstChild(cf => cf.ByAutomationId(columnId)) is { } cell
            ? CellName().Replace(cell.Name, string.Empty)
            : string.Empty;

    [GeneratedRegex(@", Eintrag: \d+, angezeigter Spaltenindex: \d+$")]
    private static partial Regex CellName();
}

/// <summary>Рядок списку «Meine Aufgaben»: назва й дата початку (без часу).</summary>
public sealed record TaskListRow(string Name, DateTime Begin)
{
    public override string ToString() => $"{Name} ({AdaicaFormat.FormatDate(Begin)})";
}
