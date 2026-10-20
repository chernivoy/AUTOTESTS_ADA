using ADAICA_AUTOTEST.Controls;
using ADAICA_AUTOTEST.Infrastructure;
using Allure.Net.Commons;
using Allure.Net.Commons.Attributes;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using FlaUI.Core.WindowsAPI;

namespace ADAICA_AUTOTEST.Pages;

/// <summary>
/// Шаблони завдань у навігаторі Dokumentation. Підменю нового завдання — «Neu» вузла Aufgaben і «Neu» → «Aufgabe» будь-якого
/// запису — має «Neue Aufgabe», «Vorlage erstellen/bearbeiten» (вікно «Aufgabenvorlagen» з «Verwenden») і за роздільником
/// шаблони: спершу обрані користувача, далі за назвою. Пункт шаблону відкриває вікно нового завдання з його полями.
/// </summary>
public sealed partial class MainPage
{
    private const string TaskTemplatesMenuItemId = "mnuMoreTaskTemplates";
    private const string TaskFromTemplateMenuItemId = "mnuNewTaskFromTemplate";

    // Контекстне меню запису: «Neu» → «Aufgabe» — підменю нового завдання.
    private const string NewChildTaskMenuItemId = "mnuNewTask";

    /// <summary>
    /// «Aufgabenvorlagen» для вибору шаблону: «Verwenden» відкриває вікно нового завдання з полями шаблону. Там же
    /// створюють, змінюють і видаляють шаблони.
    /// </summary>
    [AllureStep("Відкрити «Aufgabenvorlagen»: вузол Aufgaben → контекстне меню → Neu → Vorlage erstellen/bearbeiten")]
    public TaskTemplatesWindow OpenTaskTemplates()
    {
        OpenNodeContextMenu(TasksNodeName);
        PopupMenuItem("Neu", NewRecordMenuItemId).Expand();
        PopupMenuItem("Vorlage erstellen/bearbeiten", TaskTemplatesMenuItemId, NewRecordMenuItemId).Click();
        return new TaskTemplatesWindow(Window);
    }

    /// <summary>
    /// «Aufgabenvorlagen» з контекстного меню запису <paramref name="parentNode"/>: «Verwenden» відкриває вікно нового
    /// завдання під цим записом.
    /// </summary>
    [AllureStep("Відкрити «Aufgabenvorlagen»: вузол {parentNode} → контекстне меню → Neu → Aufgabe → Vorlage erstellen/bearbeiten")]
    public TaskTemplatesWindow OpenChildTaskTemplates(string parentNode)
    {
        OpenNewChildTaskMenu(parentNode);
        PopupMenuItem("Vorlage erstellen/bearbeiten", TaskTemplatesMenuItemId, NewChildMenuItemId, NewChildTaskMenuItemId).Click();
        return new TaskTemplatesWindow(Window);
    }

    [AllureStep("Відкрити нове вікно завдання з шаблону: вузол Aufgaben → контекстне меню → Neu → {templateName}")]
    public TaskDialog OpenNewTaskFromTemplate(string templateName)
    {
        OpenNodeContextMenu(TasksNodeName);
        PopupMenuItem("Neu", NewRecordMenuItemId).Expand();
        TemplateMenuItem(templateName, NewRecordMenuItemId).Click();
        return new TaskDialog(Window);
    }

    [AllureStep("Відкрити нове вікно дочірнього завдання з шаблону: вузол {parentNode} → контекстне меню → Neu → Aufgabe → {templateName}")]
    public TaskDialog OpenNewChildTaskFromTemplate(string parentNode, string templateName)
    {
        OpenNewChildTaskMenu(parentNode);
        TemplateMenuItem(templateName, NewChildMenuItemId, NewChildTaskMenuItemId).Click();
        return new TaskDialog(Window);
    }

    /// <summary>Пункти підменю «Neu» вузла Aufgaben (див. опис класу). Меню після цього закривається.</summary>
    [AllureStep("Прочитати підменю «Neu» вузла Aufgaben")]
    public IReadOnlyList<string> NewTaskMenuItems()
    {
        OpenNodeContextMenu(TasksNodeName);
        PopupMenuItem("Neu", NewRecordMenuItemId).Expand();
        return TaskSubmenuItemsThenClose(NewRecordMenuItemId);
    }

    /// <summary>Пункти підменю «Neu» → «Aufgabe» запису <paramref name="parentNode"/>. Меню після цього закривається.</summary>
    [AllureStep("Прочитати підменю «Neu» → «Aufgabe» вузла {parentNode}")]
    public IReadOnlyList<string> NewChildTaskMenuItems(string parentNode)
    {
        OpenNewChildTaskMenu(parentNode);
        return TaskSubmenuItemsThenClose(NewChildMenuItemId, NewChildTaskMenuItemId);
    }

    private void OpenNewChildTaskMenu(string parentNode)
    {
        OpenNewChildMenu(parentNode);
        PopupMenuItem("Aufgabe", NewChildTaskMenuItemId, NewChildMenuItemId).Expand();
    }

    // Пункти шаблонів мають однаковий AutomationId, а назва шаблону — у Name.
    private MenuItemControl TemplateMenuItem(string templateName, params string[] submenuIds) =>
        new(templateName, () => Wait.For(
            () => Popups.FindDescendant(Window, cf => cf.ByAutomationId(TaskFromTemplateMenuItemId).And(cf.ByName(templateName)))
                ?? ExpandCollapsedSubmenus(submenuIds),
            $"шаблону «{templateName}» у контекстному меню"));

    /// <summary>
    /// Пункти підменю нового завдання згори донизу; потім меню закривається. Кожен пункт підменю UI Automation показує
    /// двічі — у Popup підменю і всередині пункту меню, що його розгорнув, — тож беремо їх за AutomationId з усіх Popup,
    /// лишаємо по одному з кожною назвою й упорядковуємо за положенням.
    /// </summary>
    private IReadOnlyList<string> TaskSubmenuItemsThenClose(params string[] submenuIds)
    {
        Wait.For(() => FindInPopups(NewTaskMenuItemId) ?? ExpandCollapsedSubmenus(submenuIds), "пункту «Neue Aufgabe» у контекстному меню");
        var items = Popups.Of(Window)
            .SelectMany(popup => popup.FindAllDescendants(cf => cf.ByAutomationId(NewTaskMenuItemId)
                .Or(cf.ByAutomationId(TaskTemplatesMenuItemId))
                .Or(cf.ByAutomationId(TaskFromTemplateMenuItemId))))
            .GroupBy(item => item.Name)
            .Select(sameName => sameName.FirstOrDefault(item => !item.BoundingRectangle.IsEmpty) ?? sameName.First())
            .OrderBy(item => item.BoundingRectangle.Top)
            .Select(item => item.Name)
            .ToList();
        CloseContextMenu();
        return items;
    }

    /// <summary>
    /// Закриває контекстне меню з розгорнутими підменю клавішею Esc: кожне натискання закриває один рівень. Підменю —
    /// Popup усередині Popup меню, тож рахуємо всі вкладені Popup (Popup нагадувань теж рахується, але не зникає).
    /// </summary>
    private void CloseContextMenu() => AllureApi.Step("Закрити контекстне меню", () =>
    {
        while (Popups.FindDescendant(Window, cf => cf.ByControlType(ControlType.Menu)) is not null)
        {
            var before = PopupCount();
            UiInput.PressKey(() => Window, VirtualKeyShort.ESCAPE, () => PopupCount() < before, "закриття рівня контекстного меню");
        }
    });

    private int PopupCount() =>
        Popups.Of(Window).Sum(popup => 1 + popup.FindAllDescendants(cf => cf.ByClassName("Popup")).Length);
}
