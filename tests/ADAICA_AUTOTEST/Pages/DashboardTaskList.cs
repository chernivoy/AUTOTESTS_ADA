using System.Text.RegularExpressions;
using ADAICA_AUTOTEST.Controls;
using ADAICA_AUTOTEST.Infrastructure;
using Allure.Net.Commons;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using Keyboard = FlaUI.Core.Input.Keyboard;
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

    private readonly Window _owner;

    /// <param name="owner">Головне вікно ADAICA.</param>
    public DashboardTaskList(Window owner)
    {
        _owner = owner;
        QuickSearch = new TextBoxControl("Suchbegriff eingeben…", Locate.ById(owner, ViewId, "txtQuickSearch"));
        AddButton = new ButtonControl("+", Locate.ById(owner, ViewId, "btnAdd"));
        Filters = Enum.GetValues<TaskListFilter>().ToDictionary(
            filter => filter,
            filter => new ToggleButtonControl(filter.Label(), Locate.ById(owner, ViewId, filter.AutomationId())));
        // Список з'являється, коли ADAICA покаже день користувача.
        _ = View;
    }

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
            // Пошук застосовує лише клік мишею по кнопці з лупою або Enter: це ToggleButton, і Toggle з UI Automation
            // його команду не виконує.
            AtomicMouse.LeftClick(View.FindByAutomationId("btnQuickSearchApplyFilter"));
            Wait.Until(
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
                Keyboard.Type(VirtualKeyShort.ESCAPE);
                Wait.Until(() => FindInPopups("TaskContextMenu") is null, "закриття контекстного меню");
                return isAccepting;
            },
            $"відмови від «{row}» (пункт «Annehmen» стає доступним)");
    });

    /// <summary>Контекстне меню рядка відкривається лише правим кліком мишею.</summary>
    private MenuItemControl ContextMenuItem(TaskListRow row, string itemName)
    {
        AtomicMouse.RightClick(RowCell(row, "RowData.Row.Name"));
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

    // Меню ADAICA відкриваються у WPF Popup — дочірньому вікні головного вікна.
    private AutomationElement? FindInPopups(string automationId) =>
        _owner.FindAllChildren(cf => cf.ByClassName("Popup"))
            .Select(popup => popup.FindFirstDescendant(cf => cf.ByAutomationId(automationId)))
            .FirstOrDefault(element => element is not null);

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
