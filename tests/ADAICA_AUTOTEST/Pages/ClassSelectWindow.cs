using ADAICA_AUTOTEST.Controls;
using ADAICA_AUTOTEST.Infrastructure;
using Allure.Net.Commons;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;

namespace ADAICA_AUTOTEST.Pages;

/// <summary>
/// Вікно «Wählen Sie eine Klasse und ggf. auch eine Unterklasse aus» (кнопка «Klasse» на бланку запису). Дерево:
/// «Klasse» → «Blanko-Eintrag» і класи, доступні для цього типу запису → їхні підкласи. Дерево — плаский ListBox,
/// вкладеність видно лише з відступу тексту вузла; вузол вибирається лише кліком, а розгортається трикутником
/// (TogglePattern діє). Кнопка «Konfiguration der Klassenliste» відкриває <see cref="ClassesConfigWindow"/>.
/// </summary>
public sealed class ClassSelectWindow
{
    private const string WindowId = "wndVUBClassAndSubclassSelect";

    // Рівні дерева: 0 — «Klasse», 1 — класи, 2 — підкласи.
    private const int ClassLevel = 1;
    private const int SubClassLevel = 2;

    private readonly Window _mainWindow;

    public ClassSelectWindow(Window mainWindow)
    {
        _mainWindow = mainWindow;
        Root = Wait.For(() => Find(mainWindow), "вікна вибору класу");
        OkButton = new ButtonControl("OK", Locate.ById(Root, "btnOkWndVUBClassSelect"));
        CancelButton = new ButtonControl("Abbrechen", Locate.ById(Root, "btnCancelWndVUBClassSelect"));
        ConfigurationButton = new ButtonControl("Konfiguration der Klassenliste", Locate.ById(Root, "btnConfigurationWndVUBClassSelect"));
    }

    public ButtonControl OkButton { get; }

    public ButtonControl CancelButton { get; }

    public ButtonControl ConfigurationButton { get; }

    public bool IsOpen => Find(_mainWindow) is not null;

    private Window Root { get; }

    private AutomationElement Tree => Root.FindByAutomationId("treeView");

    /// <summary>Чи є в дереві клас <paramref name="recordClass"/> із його підкласом (клас при цьому розгортається).</summary>
    public bool Contains(RecordClass recordClass)
    {
        if (Node(recordClass.ClassNode, ClassLevel) is not { } classNode)
            return false;

        Expand(classNode);
        return Retry(() => SubClassNode(recordClass) is not null);
    }

    /// <summary>Відкриває конфігурацію класів, створює в ній клас і підклас і чекає, доки вони з'являться в дереві.</summary>
    public void CreateClass(RecordClass recordClass) =>
        AllureApi.Step($"Створити клас {recordClass} через «Konfiguration der Klassenliste»", () =>
        {
            ConfigurationButton.Click();
            new ClassesConfigWindow(_mainWindow).Create(recordClass);
            Wait.Until(() => Contains(recordClass), $"класу {recordClass} у вікні вибору класу");
        });

    /// <summary>Вибирає підклас <paramref name="recordClass"/> (клік по вузлу) і закриває вікно кнопкою OK.</summary>
    public void Choose(RecordClass recordClass) => AllureApi.Step($"Вибрати клас {recordClass}", () =>
    {
        Expand(Wait.For(() => Node(recordClass.ClassNode, ClassLevel), $"класу «{recordClass.ClassNode}»"));
        AutomationElement Target() => Wait.For(() => SubClassNode(recordClass), $"підкласу «{recordClass.SubClassNode}»");
        UiInput.LeftClick(() => NodeText(Target()) ?? Target(), () => Root.FindByAutomationId("btnOkWndVUBClassSelect").IsEnabled, $"вибору «{recordClass.SubClassNode}»");
        OkButton.Click();
        Wait.Until(() => !IsOpen, "закриття вікна вибору класу");
    });

    public void Cancel() => AllureApi.Step("Закрити вікно вибору класу (Abbrechen)", () =>
    {
        CancelButton.Click();
        Wait.Until(() => !IsOpen, "закриття вікна вибору класу");
    });

    public void CancelIfOpen()
    {
        if (IsOpen)
            Cancel();
    }

    private AutomationElement? SubClassNode(RecordClass recordClass)
    {
        var nodes = Nodes();
        var classIndex = nodes.FindIndex(node => node.Level == ClassLevel && node.Text == recordClass.ClassNode);
        if (classIndex < 0)
            return null;

        return nodes.Skip(classIndex + 1)
            .TakeWhile(node => node.Level > ClassLevel)
            .FirstOrDefault(node => node.Level == SubClassLevel && node.Text == recordClass.SubClassNode)
            .Element;
    }

    private AutomationElement? Node(string text, int level) =>
        Nodes().FirstOrDefault(node => node.Level == level && node.Text == text).Element;

    private List<(AutomationElement Element, string Text, int Level)> Nodes()
    {
        var items = Tree.FindAllChildren(cf => cf.ByControlType(ControlType.ListItem))
            .Select(item => (Element: item, Text: NodeText(item)))
            .Where(node => node.Text is not null)
            .ToList();
        if (items.Count == 0)
            return [];

        // Відступ одного рівня — найменша різниця між коренем «Klasse» і вузлами під ним.
        var rootLeft = items[0].Text!.BoundingRectangle.Left;
        var step = items.Select(node => node.Text!.BoundingRectangle.Left - rootLeft).Where(dx => dx > 0).DefaultIfEmpty(1).Min();
        return items
            .Select(node => (node.Element, node.Text!.Name, Level: (int)Math.Round((node.Text.BoundingRectangle.Left - rootLeft) / (double)step)))
            .ToList();
    }

    private static void Expand(AutomationElement node)
    {
        var toggle = node.FindFirstChild(cf => cf.ByAutomationId("ToggleButton"));
        if (toggle is not null && toggle.Patterns.Toggle.Pattern.ToggleState.Value != ToggleState.On)
            toggle.Patterns.Toggle.Pattern.Toggle();
    }

    // Розгорнутий клас показує підкласи не одразу.
    private static bool Retry(Func<bool> condition)
    {
        try
        {
            Wait.Until(condition, "підкласів розгорнутого класу", TimeSpan.FromSeconds(3));
            return true;
        }
        catch (TimeoutException)
        {
            return false;
        }
    }

    private static AutomationElement? NodeText(AutomationElement node) =>
        node.FindFirstChild(cf => cf.ByAutomationId("TextBlock"));

    private static Window? Find(Window mainWindow) =>
        mainWindow.FindFirstDescendant(cf => cf.ByAutomationId(WindowId))?.AsWindow();
}
