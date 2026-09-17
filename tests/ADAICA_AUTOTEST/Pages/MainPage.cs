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
    private const string NewRecordMenuItemId = "mnuFolderNewRecords";

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
        AtomicMouse.LeftClick(item.GetClickablePoint());

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
    public AutomationElement NavigatorNode(string nodeName)
    {
        var nameWithOptionalRange = new Regex($@"^{Regex.Escape(nodeName)}( \(\d+-\d+\))?$");
        return Wait.For(
            () => Element("treeView").FindAllChildren()
                .FirstOrDefault(node => nameWithOptionalRange.IsMatch(NodeText(node)?.Name ?? string.Empty)),
            $"вузла «{nodeName}» у дереві навігатора");
    }

    /// <summary>Назва вузла, виділеного в дереві навігатора, або null, якщо нічого не виділено.</summary>
    public string? SelectedNavigatorNodeName =>
        Element("treeView").Patterns.Selection.Pattern.Selection.Value
            .Select(node => NodeText(node)?.Name)
            .FirstOrDefault();

    /// <summary>
    /// Чекає, доки в області перегляду відкриється проєкт із заданим номером.
    /// Шукаємо лише в області перегляду: у відкритому діалозі проєкту є поля з тими самими AutomationId.
    /// </summary>
    [AllureStep("Дочекатися відкриття проєкту {number}")]
    public ProjectForm WaitForOpenedProject(string number) =>
        Wait.For(
            () =>
            {
                var view = Window.FindFirstDescendant(cf => cf.ByClassName("InformationView"));
                var shownNumber = view?.FindFirstDescendant(cf => cf.ByAutomationId(ProjectForm.NumberId))?.AsTextBox().Text;
                return shownNumber == number ? new ProjectForm(view!) : null;
            },
            $"проєкту {number} в області перегляду");

    [AllureStep("Відкрити нове вікно проєкту: вузол Projekte → контекстне меню → Neu")]
    public ProjectDialog OpenNewProjectDialog()
    {
        // Контекстне меню вузла відкривається лише правим кліком мишею: ні ShowContextMenu з UIA, ні Shift+F10 не діють.
        var node = NavigatorNode(ProjectsNodeName);
        AtomicMouse.RightClick((NodeText(node) ?? node).GetClickablePoint());

        new MenuItemControl("Neu", () => Wait.For(() => FindInPopups(NewRecordMenuItemId), "пункту «Neu» у контекстному меню"))
            .Click();

        return new ProjectDialog(Window);
    }

    private static AutomationElement? NodeText(AutomationElement node) =>
        node.FindFirstChild(cf => cf.ByAutomationId("TextBlock"));

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
}
