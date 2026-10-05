using ADAICA_AUTOTEST.Controls;
using ADAICA_AUTOTEST.Infrastructure;
using Allure.Net.Commons;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;

namespace ADAICA_AUTOTEST.Pages;

/// <summary>
/// Вікно «Klasse» — конфігурація класів (кнопка «Konfiguration der Klassenliste» у вікні вибору класу). Ліворуч
/// список класів, поруч — підкласів: прапорець підкласу пов'язує його з виділеним класом. «Klassentypen» (типи
/// записів) і «Verwendung für Objekte» задаються для виділеної пари клас + підклас. Нові клас і підклас створює
/// контекстне меню відповідного списку → «Neu». OK зберігає все в базу; перезапуск ADAICA не потрібен.
/// </summary>
public sealed class ClassesConfigWindow
{
    private const string WindowId = "wndVubClassesSubClasses";

    private readonly Window _mainWindow;

    public ClassesConfigWindow(Window mainWindow)
    {
        _mainWindow = mainWindow;
        Root = Wait.For(() => Find(mainWindow), "вікна конфігурації класів");
        OkButton = new ButtonControl("OK", Locate.ById(Root, "btnOkVubClassesSubClasses"));
    }

    public ButtonControl OkButton { get; }

    private Window Root { get; }

    private AutomationElement ClassList => Root.FindByAutomationId("lstVubClassVubClassList");

    private AutomationElement SubClassList => Root.FindByAutomationId("lstVubSubClassVubClassList");

    /// <summary>
    /// Створює клас і підклас (яких ще немає), пов'язує їх і вмикає пару для всіх типів записів («Klassentypen») і всіх
    /// об'єктів («Verwendung für Objekte» → «Alle auswählen»), потім зберігає кнопкою OK.
    /// </summary>
    public void Create(RecordClass recordClass) => AllureApi.Step($"Створити клас і підклас {recordClass}", () =>
    {
        if (Item(ClassList, recordClass.ClassNode) is null)
            CreateIn(ClassList, "mnuNewVubClassList", "класів", recordClass.Class, recordClass.ClassShortName, recordClass.ClassNode);
        Select(ClassList, recordClass.ClassNode);

        if (Item(SubClassList, recordClass.SubClassNode) is null)
            CreateIn(SubClassList, "mnuNewVubSubClassList", "підкласів", recordClass.SubClass, recordClass.SubClassShortName, recordClass.SubClassNode);
        Check($"підклас «{recordClass.SubClassNode}» у класі «{recordClass.ClassNode}»", () => ItemCheckBox(SubClassList, recordClass.SubClassNode));
        Select(SubClassList, recordClass.SubClassNode);

        foreach (var classType in ClassTypeCheckBoxes())
            Check($"Klassentyp «{classType.Name}»", () => Root.FindFirstDescendant(cf => cf.ByAutomationId(classType.AutomationId)));
        Check("«Verwendung für Objekte: Alle auswählen»", AllUsagesCheckBox);

        OkButton.Click();
        Wait.Until(() => Find(_mainWindow) is null, "закриття вікна конфігурації класів");
    });

    private void CreateIn(AutomationElement list, string newMenuItemId, string what, string name, string shortName, string node) =>
        AllureApi.Step($"Новий запис у списку {what}: контекстне меню → Neu", () =>
        {
            // Меню — Popup вікна конфігурації, а коли ADAICA неактивна, — окреме вікно робочого столу.
            AutomationElement? NewItem() => Popups.FindDescendant(Root, cf => cf.ByAutomationId(newMenuItemId));

            UiInput.RightClick(() => list, () => NewItem() is not null, $"контекстного меню списку {what}");
            new MenuItemControl("Neu", () => Wait.For(NewItem, "пункту «Neu»")).Click();
            new ClassNameDialog(_mainWindow).Save(name, shortName);
            Wait.For(() => Item(list, node), $"«{node}» у списку {what}");
        });

    // Виділення через SelectionItem у цих списках діє: вікно оновлює підкласи й прапорці.
    private static void Select(AutomationElement list, string node) => AllureApi.Step($"Виділити «{node}»", () =>
    {
        var item = Wait.For(() => Item(list, node), $"«{node}» у списку");
        item.Patterns.SelectionItem.Pattern.Select();
        Wait.Until(() => item.Patterns.SelectionItem.Pattern.IsSelected.Value, $"виділення «{node}»");
    });

    // Прапорці ставимо кліком: після TogglePattern вікно може не помітити зміни (як у CheckTreeDialog).
    private static void Check(string name, Func<AutomationElement?> checkBox) => AllureApi.Step($"Позначити {name}", () =>
    {
        AutomationElement CheckBox() => Wait.For(checkBox, $"прапорця {name}");
        bool IsChecked() => CheckBox().Patterns.Toggle.Pattern.ToggleState.Value == ToggleState.On;

        if (IsChecked())
            return;

        Wait.Until(() => CheckBox().IsEnabled, $"доступності прапорця {name}");
        UiInput.LeftClick(CheckBox, IsChecked, $"прапорця {name}");
    });

    private List<(string Name, string AutomationId)> ClassTypeCheckBoxes() =>
        Root.FindByAutomationId("grpClassTypesClassTypeList")
            .FindAllDescendants(cf => cf.ByControlType(ControlType.CheckBox))
            .Select(checkBox => (checkBox.Name, checkBox.AutomationId))
            .ToList();

    private AutomationElement? AllUsagesCheckBox() =>
        Root.FindByAutomationId("treeObjUsageClassSubClass")
            .FindFirstChild(cf => cf.ByControlType(ControlType.TreeItem))
            ?.FindFirstChild(cf => cf.ByControlType(ControlType.CheckBox));

    private static AutomationElement? Item(AutomationElement list, string node) =>
        list.FindAllChildren(cf => cf.ByControlType(ControlType.ListItem))
            .FirstOrDefault(item => item.FindFirstChild(cf => cf.ByControlType(ControlType.Text).And(cf.ByName(node))) is not null);

    private static AutomationElement? ItemCheckBox(AutomationElement list, string node) =>
        Item(list, node)?.FindFirstChild(cf => cf.ByControlType(ControlType.CheckBox));

    private static Window? Find(Window mainWindow) =>
        mainWindow.FindFirstDescendant(cf => cf.ByAutomationId(WindowId))?.AsWindow();
}
