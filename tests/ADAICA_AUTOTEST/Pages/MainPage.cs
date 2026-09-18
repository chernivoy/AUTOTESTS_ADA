using System.Text.RegularExpressions;
using ADAICA_AUTOTEST.Controls;
using ADAICA_AUTOTEST.Infrastructure;
using Allure.Net.Commons.Attributes;
using FlaUI.Core.AutomationElements;

namespace ADAICA_AUTOTEST.Pages;

/// <summary>Робочий простір ADAICA після входу.</summary>
public sealed partial class MainPage : AdaicaWindow
{
    private const string MainMenuId = "mnuSystemMenuWndMain";
    private const string DemoVersionWindowId = "wndDemoVersion";

    // Така сама смуга вкладок і кнопка «+» є й унизу робочої області, тому верхні шукаємо серед дочірніх елементів вікна.
    private const string NavigatorTabsId = "PART_TabItems";
    private const string AddNavigatorButtonId = "PART_AddTab";

    private const string ProjectsNodeName = "Projekte";
    private const string FoldersNodeName = "Ordner";
    private const string TasksNodeName = "Aufgaben";
    private const string AppointmentsNodeName = "Termine";
    private const string NotesNodeName = "Notizen";
    private const string NewRecordMenuItemId = "mnuFolderNewRecords";
    private const string NewTaskMenuItemId = "mnuNewTaskDefault";

    // Контекстне меню запису (наприклад, проєкту): «Neu» — підменю з типами дочірніх записів.
    private const string NewChildMenuItemId = "mnuNew";

    public MainPage(AppSession session)
        : base(session)
    {
        Element(MainMenuId);
        CloseDemoVersionWindowIfShown();

        NavigatorTitle = new LabelControl("Navigator", Locate.ById(Window, "tbNameOfNavigator"));
        AddNavigatorButton = new ToggleButtonControl("+", () => Window.FindChildByAutomationId(AddNavigatorButtonId));
    }

    /// <summary>База даних, до якої підключено застосунок (із заголовка «… [БАЗА:СХЕМА] …»).</summary>
    public string Database => ParseTitle().Groups["database"].Value;

    /// <summary>Схема, до якої підключено застосунок.</summary>
    public string Schema => ParseTitle().Groups["schema"].Value;

    /// <summary>Назва навігатора, відкритого зараз (заголовок над деревом зліва).</summary>
    public LabelControl NavigatorTitle { get; }

    /// <summary>Кнопка «+» праворуч від вкладок: відкриває список навігаторів.</summary>
    public ToggleButtonControl AddNavigatorButton { get; }

    /// <summary>Назви вкладок навігаторів у верхній смузі, зліва направо.</summary>
    public IReadOnlyList<string> NavigatorTabNames => NavigatorTabs.Select(TabName).ToList();

    private AutomationElement[] NavigatorTabs => Window.FindChildByAutomationId(NavigatorTabsId).FindAllChildren();

    public static bool IsShownIn(Window window) =>
        window.FindFirstDescendant(cf => cf.ByAutomationId(MainMenuId)) is not null;

    [AllureStep("Відкрити навігатор {navigatorName} кнопкою «+»")]
    public MainPage OpenNavigator(string navigatorName)
    {
        var tabCount = NavigatorTabs.Length;

        AddNavigatorButton.SwitchOn();

        var item = Wait.For(
            () => FindInPopups("listView")?.FindFirstChild(cf => cf.ByName(navigatorName)),
            $"пункту «{navigatorName}» у списку навігаторів");

        // ADAICA відкриває навігатор лише на клік мишею: SelectionItem.Select() тільки виділяє пункт, Enter не діє.
        AtomicMouse.LeftClick(item);

        // Кожне відкриття додає нову вкладку, навіть якщо такий навігатор уже відкритий.
        Wait.Until(
            () => NavigatorTabs.Length == tabCount + 1 && NavigatorTitle.Text == navigatorName,
            $"відкриття навігатора «{navigatorName}»");
        return this;
    }

    /// <summary>
    /// Закриває останні вкладки навігаторів, доки їх не залишиться <paramref name="keep"/>.
    /// Відкриті вкладки ADAICA запам'ятовує між сеансами, тому тести прибирають за собою.
    /// Назва вкладки змінюється разом із вибраним вузлом, тому орієнтуємося на кількість, а не на назву:
    /// нові вкладки завжди додаються в кінець.
    /// </summary>
    [AllureStep("Закрити останні вкладки навігаторів, залишити {keep}")]
    public MainPage CloseLastNavigatorTabs(int keep)
    {
        for (var tabs = NavigatorTabs; tabs.Length > keep; tabs = NavigatorTabs)
        {
            tabs[^1].FindByAutomationId("PART_CloseButton").AsButton().Invoke();
            Wait.Until(() => NavigatorTabs.Length == tabs.Length - 1, "закриття вкладки навігатора");
        }

        return this;
    }

    /// <summary>
    /// Вузол у дереві активного навігатора. До назви ADAICA може додавати діапазон записів,
    /// наприклад «Projekte (1-25)», тому порівнюємо лише саму назву.
    /// </summary>
    public AutomationElement NavigatorNode(string nodeName) =>
        Wait.For(
            () => Element("treeView").FindAllChildren()
                .FirstOrDefault(node => NodeText(node) is { } text && WithoutRecordRange(text.Name) == nodeName),
            $"вузла «{nodeName}» у дереві навігатора");

    /// <summary>Назва вузла, виділеного в дереві навігатора, або null, якщо нічого не виділено.</summary>
    public string? SelectedNavigatorNodeName =>
        Element("treeView").Patterns.Selection.Pattern.Selection.Value
            .Select(node => NodeText(node)?.Name)
            .FirstOrDefault();

    /// <summary>
    /// Шлях до виділеного вузла: назви його предків від кореня дерева й назва самого вузла, без діапазонів записів.
    /// </summary>
    public IReadOnlyList<string> SelectedNavigatorNodePath =>
        SelectedNodeWithAncestors().Select(node => WithoutRecordRange(NodeText(node)!.Name)).ToList();

    [AllureStep("Дочекатися відкриття проєкту {number}")]
    public ProjectForm WaitForOpenedProject(string number) =>
        WaitForOpenedRecord(ProjectForm.NumberId, number, view => new ProjectForm(view));

    [AllureStep("Дочекатися відкриття Ordner {number}")]
    public ProjectForm WaitForOpenedFolder(string number) =>
        WaitForOpenedRecord(ProjectForm.NumberId, number, view => new ProjectForm(view, "Ordner"));

    [AllureStep("Дочекатися відкриття завдання {number}")]
    public TaskForm WaitForOpenedTask(string number) =>
        WaitForOpenedRecord(TaskForm.NumberId, number, view => new TaskForm(view));

    [AllureStep("Дочекатися відкриття терміну {number}")]
    public AppointmentForm WaitForOpenedAppointment(string number) =>
        WaitForOpenedRecord(AppointmentForm.NumberId, number, view => new AppointmentForm(view));

    /// <summary>
    /// Номера в інфозапису немає, тому його впізнаємо за текстом у полі «Bemerkung»,
    /// який відрізняється від прогону до прогону (тест додає туди час запису).
    /// </summary>
    [AllureStep("Дочекатися відкриття інфозапису з Bemerkung {remark}")]
    public NoteForm WaitForOpenedNote(string remark) =>
        WaitForOpenedRecord(NoteForm.RemarkId, remark, view => new NoteForm(view));

    [AllureStep("Відкрити нове вікно проєкту: вузол Projekte → контекстне меню → Neu")]
    public ProjectDialog OpenNewProjectDialog()
    {
        OpenNodeContextMenu(ProjectsNodeName);
        PopupMenuItem("Neu", NewRecordMenuItemId).Click();
        return new ProjectDialog(Window);
    }

    [AllureStep("Відкрити нове вікно Ordner: вузол Ordner → контекстне меню → Neu")]
    public FolderDialog OpenNewFolderDialog()
    {
        OpenNodeContextMenu(FoldersNodeName);
        PopupMenuItem("Neu", NewRecordMenuItemId).Click();
        return new FolderDialog(Window);
    }

    [AllureStep("Відкрити нове вікно завдання: вузол Aufgaben → контекстне меню → Neu → Neue Aufgabe")]
    public TaskDialog OpenNewTaskDialog()
    {
        OpenNodeContextMenu(TasksNodeName);
        // На вузлі Aufgaben «Neu» — підменю з варіантами створення.
        PopupMenuItem("Neu", NewRecordMenuItemId).Expand();
        PopupMenuItem("Neue Aufgabe", NewTaskMenuItemId).Click();
        return new TaskDialog(Window);
    }

    [AllureStep("Відкрити нове вікно терміну: вузол Termine → контекстне меню → Neu")]
    public AppointmentDialog OpenNewAppointmentDialog()
    {
        OpenNodeContextMenu(AppointmentsNodeName);
        PopupMenuItem("Neu", NewRecordMenuItemId).Click();
        return new AppointmentDialog(Window);
    }

    [AllureStep("Відкрити нове вікно інфозапису: вузол Notizen → контекстне меню → Neu")]
    public NoteDialog OpenNewNoteDialog()
    {
        OpenNodeContextMenu(NotesNodeName);
        PopupMenuItem("Neu", NewRecordMenuItemId).Click();
        return new NoteDialog(Window);
    }

    [AllureStep("Відкрити нове вікно підпроєкту: вузол {parentNode} → контекстне меню → Neu → Projekt")]
    public ProjectDialog OpenNewChildProjectDialog(string parentNode)
    {
        OpenNewChildMenu(parentNode);
        PopupMenuItem("Projekt", "mnuNewProject").Click();
        return new ProjectDialog(Window);
    }

    [AllureStep("Відкрити нове вікно дочірнього Ordner: вузол {parentNode} → контекстне меню → Neu → Ordner")]
    public FolderDialog OpenNewChildFolderDialog(string parentNode)
    {
        OpenNewChildMenu(parentNode);
        PopupMenuItem("Ordner", "mnuNewProcess").Click();
        return new FolderDialog(Window);
    }

    [AllureStep("Відкрити нове вікно дочірнього завдання: вузол {parentNode} → контекстне меню → Neu → Aufgabe → Neue Aufgabe")]
    public TaskDialog OpenNewChildTaskDialog(string parentNode)
    {
        OpenNewChildMenu(parentNode);
        PopupMenuItem("Aufgabe", "mnuNewTask").Expand();
        PopupMenuItem("Neue Aufgabe", NewTaskMenuItemId).Click();
        return new TaskDialog(Window);
    }

    [AllureStep("Відкрити нове вікно дочірнього терміну: вузол {parentNode} → контекстне меню → Neu → Termin")]
    public AppointmentDialog OpenNewChildAppointmentDialog(string parentNode)
    {
        OpenNewChildMenu(parentNode);
        PopupMenuItem("Termin", "mnuNewAppoint").Click();
        return new AppointmentDialog(Window);
    }

    [AllureStep("Відкрити нове вікно дочірнього інфозапису: вузол {parentNode} → контекстне меню → Neu → Notiz")]
    public NoteDialog OpenNewChildNoteDialog(string parentNode)
    {
        OpenNewChildMenu(parentNode);
        PopupMenuItem("Notiz", "mnuNewHistory").Click();
        return new NoteDialog(Window);
    }

    /// <summary>
    /// Чекає, доки в області перегляду відкриється запис, у якому поле <paramref name="keyFieldId"/>
    /// має значення <paramref name="keyValue"/> (номер запису або інша унікальна ознака).
    /// Шукаємо лише в області перегляду: у відкритому діалозі запису є поля з тими самими AutomationId.
    /// </summary>
    private TForm WaitForOpenedRecord<TForm>(string keyFieldId, string keyValue, Func<AutomationElement, TForm> createForm)
        where TForm : class =>
        Wait.For(
            () =>
            {
                var view = Window.FindFirstDescendant(cf => cf.ByClassName("InformationView"));
                var shownValue = view?.FindFirstDescendant(cf => cf.ByAutomationId(keyFieldId))?.AsTextBox().Text;
                return shownValue == keyValue ? createForm(view!) : null;
            },
            $"запису «{keyValue}» в області перегляду");

    // Контекстне меню вузла відкривається лише правим кліком мишею: ні ShowContextMenu з UIA, ні Shift+F10 не діють.
    private void OpenNodeContextMenu(string nodeName) => OpenNodeContextMenu(NavigatorNode(nodeName));

    private static void OpenNodeContextMenu(AutomationElement node) => AtomicMouse.RightClick(NodeText(node) ?? node);

    /// <summary>
    /// Відкриває підменю «Neu» батьківського запису. Назви записів можуть повторюватися (у завдання й інфозапису
    /// немає номера), тому батька шукаємо серед виділеного вузла та його предків: після збереження ADAICA виділяє
    /// новий запис, тож і сам батько, і його дочірні записи лежать на цьому ланцюжку.
    /// </summary>
    private void OpenNewChildMenu(string parentNode)
    {
        var parent = SelectedNodeWithAncestors().LastOrDefault(node => WithoutRecordRange(NodeText(node)!.Name) == parentNode)
            ?? NavigatorNode(parentNode);
        OpenNodeContextMenu(parent);
        PopupMenuItem("Neu", NewChildMenuItemId).Expand();
    }

    /// <summary>
    /// Виділений вузол дерева та його предки, від кореня. У UI Automation дерево — плаский список, тому предка
    /// визначаємо за відступом: це найближчий вузол вище, текст якого починається лівіше.
    /// </summary>
    private List<AutomationElement> SelectedNodeWithAncestors()
    {
        var nodes = Element("treeView").FindAllChildren();
        var selected = Array.FindLastIndex(nodes, node => node.Patterns.SelectionItem.Pattern.IsSelected.Value);

        var chain = new List<AutomationElement>();
        var indent = int.MaxValue;
        for (var i = selected; i >= 0; i--)
        {
            var text = NodeText(nodes[i]);
            if (text is null || text.BoundingRectangle.Left >= indent)
                continue;

            chain.Insert(0, nodes[i]);
            indent = text.BoundingRectangle.Left;
        }

        return chain;
    }

    private MenuItemControl PopupMenuItem(string name, string automationId) =>
        new(name, () => Wait.For(() => FindInPopups(automationId), $"пункту «{name}» у контекстному меню"));

    private static AutomationElement? NodeText(AutomationElement node) =>
        node.FindFirstChild(cf => cf.ByAutomationId("TextBlock"));

    private static string WithoutRecordRange(string nodeText) => RecordRange().Replace(nodeText, string.Empty);

    // Списки й контекстні меню ADAICA відкриваються у WPF Popup — дочірньому вікні головного вікна.
    private AutomationElement? FindInPopups(string automationId) =>
        Window.FindAllChildren(cf => cf.ByClassName("Popup"))
            .Select(popup => popup.FindFirstDescendant(cf => cf.ByAutomationId(automationId)))
            .FirstOrDefault(element => element is not null);

    // Заголовок вкладки — TextBlock із шаблону, тому його видно лише в raw-дереві.
    private static string TabName(AutomationElement tab) =>
        tab.FindFirstDescendantRaw(cf => cf.ByAutomationId("PART_Text"))?.Name ?? string.Empty;

    private void CloseDemoVersionWindowIfShown()
    {
        // Без ліцензії («Lizenziert für: Not Registered») після входу завжди відкривається вікно демоверсії.
        if (!Title.Contains("Not Registered", StringComparison.Ordinal))
            return;

        Element(DemoVersionWindowId).FindByAutomationId("btnExitWndDemoVersion").AsButton().Invoke();
        Wait.Until(
            () => Window.FindFirstChild(cf => cf.ByAutomationId(DemoVersionWindowId)) is null,
            "закриття вікна демоверсії");
    }

    private Match ParseTitle()
    {
        var match = ConnectionInTitle().Match(Title);
        return match.Success
            ? match
            : throw new InvalidOperationException($"У заголовку вікна немає бази й схеми: «{Title}».");
    }

    [GeneratedRegex(@"\[(?<database>[^:\]]+):(?<schema>[^\]]+)\]")]
    private static partial Regex ConnectionInTitle();

    // Діапазон записів, який ADAICA додає до назви вузла: «Projekte (1-25)».
    [GeneratedRegex(@" \(\d+-\d+\)$")]
    private static partial Regex RecordRange();
}
