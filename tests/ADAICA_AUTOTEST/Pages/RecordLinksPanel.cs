using System.Drawing;
using System.Runtime.InteropServices;
using ADAICA_AUTOTEST.Controls;
using ADAICA_AUTOTEST.Infrastructure;
using Allure.Net.Commons;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Conditions;
using FlaUI.Core.Definitions;
using VirtualKeyShort = FlaUI.Core.WindowsAPI.VirtualKeyShort;

namespace ADAICA_AUTOTEST.Pages;

/// <summary>
/// Панель зв'язків запису (RecordLinkView): вкладки «Kontakte», «Dokumentation» і «Objekte». Вона є у вікні збереження
/// листа (<see cref="MailSaveDialog"/>) і внизу вікна нового проєкту, Ordner, завдання, терміну й інфозапису
/// (<see cref="RecordDialog.Links"/>). На «Dokumentation» — записи, під якими буде новий запис чи лист, і записи
/// контактів, які ADAICA пропонує за налаштуваннями «Vorschläge für Verknüpfung der Dokumentation»
/// (<see cref="LinkSuggestionOptions"/>): без позначки «Verknüpft», зі значком контакту в колонці «Kontext».
/// </summary>
/// <remarks>
/// Вміст вкладок (списки, рядок пошуку, кнопки над списком) UI Automation не показує навіть у raw-дереві і через
/// FromPoint: TabControlEx ADAICA тримає вміст вкладок у шаблоні поза TabItem. Тому рядки тест знаходить за
/// координатами від кута панелі — розмітка в незалежних від DPI одиницях WPF, помножених на масштаб вікна, — а що в
/// рядку, бачить з його контекстного меню (меню UI Automation бачить). У вікні нового запису панель має висоту 155
/// одиниць, тож без прокрутки видно п'ять рядків.
/// </remarks>
public sealed class RecordLinksPanel
{
    // Пункт меню рядка «Dokumentation»: «Neu» → тип запису.
    private const string NewItem = "Neu";
    private const string ProjectItem = "Projekt";
    private const string FolderItem = "Ordner";
    private const string TaskItem = "Aufgabe";
    private const string NewTaskItem = "Neue Aufgabe";
    private const string AppointmentItem = "Termin";
    private const string NoteItem = "Notiz";

    // Пункт, який є лише в меню рядка «Dokumentation», а в меню порожнього місця (тільки «Neu» і «Finden») — ні.
    private const string RecordRowMenuItem = "Beziehung lösen";

    // Пункт меню рядка «Dokumentation», що ставить позначку «Verknüpft».
    private const string LinkItem = "Verknüpfung erstellen";

    // Меню рядка у вікні нового запису: «Öffnen» → «In separatem Fenster» відкриває запис рядка.
    private const string OpenItem = "Öffnen";
    private const string SeparateWindowItem = "In separatem Fenster";
    private const string CancelButtonId = "btnCancelCtrMainForm";

    // Меню «+» на вкладці «Kontakte» вікна нового запису і його пункт «Kontakt».
    private const string AddContactMenuId = "ContactsLinkGroupContextMenu";
    private const string AddContactItemId = "CONTACTID_VOID_CONTACT_000000";

    // Вікно завдання відкривається до 6 с.
    private static readonly TimeSpan RecordWindowTimeout = TimeSpan.FromSeconds(30);

    // Заголовок вікна дочірнього запису: «Neue Notiz (übergeordnet: Назва батька)».
    private const string ParentTitlePrefix = "(übergeordnet: ";

    // Розмітка вкладок в одиницях WPF (1/96 дюйма) від кута панелі: середина першого рядка списку й висота рядка,
    // середина колонки «Funktion» на «Kontakte» і колонки «Name» на «Dokumentation» (праворуч від значка й відступів
    // вкладеності), середина рядка кнопок над списком.
    private const double FirstRowCenter = 64.5;
    private const double RowHeight = 20;
    private const double FirstColumnCenter = 60;
    private const double RecordNameCenter = 150;
    private const double ButtonsCenter = 16.5;

    // Кнопки над списком — квадрати по 24 одиниці, праворуч; відстань від правого краю до середини кнопки.
    private const double LastButtonFromRight = 15;
    private const double ButtonStep = 25;

    private readonly Window _window;
    private readonly Window _mainWindow;
    private readonly string _viewId;
    private readonly bool _labelsFromRecordWindow;

    /// <param name="window">Вікно з панеллю (діалоги нових записів — дочірні вікна цього вікна).</param>
    /// <param name="mainWindow">Головне вікно ADAICA: меню неактивної ADAICA можуть з'являтися під ним.</param>
    /// <param name="viewId">AutomationId панелі: «recordLinkView» у вікні листа, «ctrRecNewCO» у вікні нового запису.</param>
    /// <param name="labelsFromRecordWindow">
    /// Підпис рядка «Dokumentation» — із заголовка вікна запису («Öffnen»), а не вікна нового інфозапису під ним
    /// (див. <see cref="RecordRowLabel"/>).
    /// </param>
    public RecordLinksPanel(Window window, Window mainWindow, string viewId, bool labelsFromRecordWindow = false)
    {
        _window = window;
        _mainWindow = mainWindow;
        _viewId = viewId;
        _labelsFromRecordWindow = labelsFromRecordWindow;
    }

    /// <summary>Сама панель — ціль кліків за координатами.</summary>
    public AutomationElement Element => _window.FindByAutomationId(_viewId);

    /// <summary>Вкладка за підписом («Kontakte», «Dokumentation», «Objekte»); заголовки вкладок UI Automation бачить.</summary>
    public void SelectTab(string header)
    {
        AutomationElement Tab() => Wait.For(
            () => Element.FindFirstDescendant(cf => cf.ByAutomationId("tcLinkGroups"))?.FindAllChildren(cf => cf.ByControlType(ControlType.TabItem))
                .FirstOrDefault(tab => tab.FindFirstChild(cf => cf.ByControlType(ControlType.Text))?.Name == header),
            $"вкладки «{header}»");
        var selection = Tab().Patterns.SelectionItem.Pattern;
        if (!selection.IsSelected.Value)
        {
            selection.Select();
            Wait.Until(() => Tab().Patterns.SelectionItem.Pattern.IsSelected.Value, $"вибору вкладки «{header}»");
        }
    }

    /// <summary>
    /// Контекстне меню рядка <paramref name="contactRow"/> (з нуля) на вкладці «Kontakte»: пункти з доступністю. Меню
    /// закривається клавішею Esc у самому меню — Esc у вікні закрив би вікно.
    /// </summary>
    public IReadOnlyList<MenuEntry> ContactMenu(int contactRow) =>
        AllureApi.Step($"Прочитати контекстне меню рядка {contactRow + 1} на вкладці «Kontakte»", () =>
        {
            SelectTab("Kontakte");
            var menu = OpenRowMenu(contactRow, "ContactMenu");
            var entries = menu.FindAllChildren(cf => cf.ByControlType(ControlType.MenuItem))
                .Select(item => new MenuEntry(item.Name, item.IsEnabled))
                .ToList();
            CloseMenu("ContactMenu");
            AllureApi.Step($"Пункти меню: {string.Join("; ", entries)}");
            return (IReadOnlyList<MenuEntry>)entries;
        });

    /// <summary>
    /// Чи є на вкладці «Kontakte» рядок <paramref name="contactRow"/>: правий клік по рядку відкриває меню рядка
    /// («ContactMenu»), а по порожньому місцю під рядками — меню списку («ContactsListContextMenu»).
    /// </summary>
    public bool HasContactRow(int contactRow) =>
        AllureApi.Step($"Перевірити, чи є рядок {contactRow + 1} на вкладці «Kontakte»", () =>
        {
            SelectTab("Kontakte");
            UiInput.RightClick(
                () => Element,
                () => FindPopup("ContactMenu") is not null || FindPopup("ContactsListContextMenu") is not null,
                $"контекстного меню на місці рядка {contactRow + 1}",
                offset: Row(contactRow));
            var exists = FindPopup("ContactMenu") is not null;
            CloseMenu(exists ? "ContactMenu" : "ContactsListContextMenu");
            return exists;
        });

    /// <summary>Перевіряє, скільки рядків на вкладці «Kontakte».</summary>
    public void ShouldHaveContactRows(int expected) =>
        AllureApi.Step($"Перевірити, що на вкладці «Kontakte» {expected} рядки", () =>
            this.ShouldSatisfyAllConditions(
                () => HasContactRow(expected - 1).ShouldBeTrue($"Рядка {expected} на вкладці «Kontakte» немає"),
                () => HasContactRow(expected).ShouldBeFalse($"На вкладці «Kontakte» є зайвий рядок {expected + 1}")));

    /// <summary>Пункт <paramref name="item"/> контекстного меню рядка <paramref name="contactRow"/> на вкладці «Kontakte».</summary>
    public void ChooseInContactMenu(int contactRow, string item) =>
        AllureApi.Step($"Рядок {contactRow + 1} на вкладці «Kontakte» → контекстне меню → «{item}»", () =>
        {
            SelectTab("Kontakte");
            var menu = OpenRowMenu(contactRow, "ContactMenu");
            new MenuItemControl(item, () => Wait.For(
                () => menu.FindFirstChild(cf => cf.ByControlType(ControlType.MenuItem).And(cf.ByName(item))),
                $"пункту «{item}»")).Click();
        });

    /// <summary>
    /// У вікні нового запису: «+» праворуч над списком «Kontakte» → «Kontakt». Відкриває «Kontaktzuordnungs-Dialog»
    /// (дочірнє вікно вікна з панеллю). Інші пункти меню — «Kostenträger» і «Von».
    /// </summary>
    public ChooseContactsDialog OpenAddContact() => AllureApi.Step("«Kontakte» → «+» → «Kontakt»", () =>
    {
        SelectTab("Kontakte");
        UiInput.LeftClickOpeningMenu(() => Element, () => FindPopup(AddContactMenuId) is not null, "меню «+» на вкладці «Kontakte»", offset: Button(fromRight: 0));
        new MenuItemControl("Kontakt", () => Wait.For(() => FindPopup(AddContactItemId), "пункту «Kontakt»")).Click();
        return new ChooseContactsDialog(_window);
    });

    /// <summary>
    /// Меню рядка <paramref name="recordRow"/> (з нуля, згори) на вкладці «Dokumentation» → «Neu» → «Projekt»: проєкт
    /// під записом цього рядка. Вікно нового запису — дочірнє вікно вікна з панеллю, заголовок — «Neues Projekt
    /// (übergeordnet: батько)».
    /// </summary>
    public ProjectDialog OpenNewChildProject(int recordRow) => OpenNewChildRecord(recordRow, ProjectItem, () => new ProjectDialog(_window));

    /// <summary>Меню рядка <paramref name="recordRow"/> на вкладці «Dokumentation» → «Neu» → «Ordner».</summary>
    public FolderDialog OpenNewChildFolder(int recordRow) => OpenNewChildRecord(recordRow, FolderItem, () => new FolderDialog(_window));

    /// <summary>Меню рядка <paramref name="recordRow"/> на вкладці «Dokumentation» → «Neu» → «Aufgabe» → «Neue Aufgabe».</summary>
    public TaskDialog OpenNewChildTask(int recordRow) => OpenNewChildRecord(recordRow, TaskItem, () => new TaskDialog(_window));

    /// <summary>Меню рядка <paramref name="recordRow"/> на вкладці «Dokumentation» → «Neu» → «Termin».</summary>
    public AppointmentDialog OpenNewChildAppointment(int recordRow) =>
        OpenNewChildRecord(recordRow, AppointmentItem, () => new AppointmentDialog(_window));

    /// <summary>Меню рядка <paramref name="recordRow"/> на вкладці «Dokumentation» → «Neu» → «Notiz».</summary>
    public NoteDialog OpenNewChildNote(int recordRow) => OpenNewChildRecord(recordRow, NoteItem, () => new NoteDialog(_window));

    /// <summary>
    /// Як ADAICA підписує запис у рядку <paramref name="recordRow"/> на вкладці «Dokumentation». Сам список UI Automation
    /// не бачить. У вікні листа тест відкриває для рядка «Neu» → «Notiz», бере назву батька з заголовка вікна («Neue
    /// Notiz (übergeordnet: …)») і закриває вікно без збереження. У вікні нового запису в меню рядка «Neu» немає, тож
    /// тест відкриває запис («Öffnen» → «In separatem Fenster») і бере заголовок його вікна.
    /// </summary>
    public string RecordRowLabel(int recordRow) => AllureApi.Step($"Прочитати запис у рядку {recordRow + 1} на вкладці «Dokumentation»", () =>
    {
        if (_labelsFromRecordWindow)
        {
            var opened = OpenedRecordTitle(recordRow);
            AllureApi.Step($"Рядок {recordRow + 1}: «{opened}»");
            return opened;
        }

        var dialog = OpenNewChildNote(recordRow);
        var title = dialog.Title;
        dialog.Cancel();
        var start = title.IndexOf(ParentTitlePrefix, StringComparison.Ordinal);
        if (start < 0 || !title.EndsWith(')'))
            throw new InvalidOperationException($"Заголовок вікна нового інфозапису без батька: «{title}».");

        var label = title[(start + ParentTitlePrefix.Length)..^1];
        AllureApi.Step($"Рядок {recordRow + 1}: «{label}»");
        return label;
    });

    /// <summary>
    /// Підписи всіх записів на вкладці «Dokumentation», починаючи з рядка <paramref name="fromRow"/>, згори вниз
    /// (<see cref="RecordRowLabel"/>; кілька секунд на рядок).
    /// </summary>
    public IReadOnlyList<string> RecordRowLabels(int fromRow = 0) =>
        AllureApi.Step($"Прочитати записи на вкладці «Dokumentation» з рядка {fromRow + 1}", () =>
        {
            var labels = new List<string>();
            for (var row = fromRow; HasRecordRow(row); row++)
                labels.Add(RecordRowLabel(row));
            AllureApi.Step($"Записи: {(labels.Count == 0 ? "немає" : string.Join("; ", labels))}");
            return (IReadOnlyList<string>)labels;
        });

    /// <summary>
    /// Чи позначено запис у рядку <paramref name="recordRow"/> «Verknüpft»: у меню такого рядка доступний
    /// «Beziehung lösen», а в запропонованого ADAICA запису — лише «Verknüpfung erstellen».
    /// </summary>
    public bool IsRecordRowLinked(int recordRow) =>
        AllureApi.Step($"Перевірити, чи позначено «Verknüpft» рядок {recordRow + 1} на вкладці «Dokumentation»", () =>
        {
            SelectTab("Dokumentation");
            UiInput.RightClick(() => Element, () => RecordRowMenu() is not null, $"контекстного меню рядка {recordRow + 1}", offset: RecordRow(recordRow));
            var linked = MenuItem(RecordRowMenu, RecordRowMenuItem).IsEnabled;
            CloseMenu(RecordListMenu);
            return linked;
        });

    /// <summary>Меню рядка <paramref name="recordRow"/> на вкладці «Dokumentation» → «Verknüpfung erstellen»: позначка «Verknüpft».</summary>
    public void LinkRecordRow(int recordRow) =>
        AllureApi.Step($"Рядок {recordRow + 1} на вкладці «Dokumentation» → контекстне меню → «{LinkItem}»", () =>
        {
            SelectTab("Dokumentation");
            UiInput.RightClick(() => Element, () => RecordRowMenu() is not null, $"контекстного меню рядка {recordRow + 1}", offset: RecordRow(recordRow));
            MenuItem(RecordRowMenu, LinkItem).Click();
            Wait.Until(() => IsRecordRowLinked(recordRow), $"позначки «Verknüpft» у рядку {recordRow + 1}");
        });

    /// <summary>
    /// Чи є на вкладці «Dokumentation» рядок <paramref name="recordRow"/>: правий клік по рядку відкриває меню рядка
    /// (з «Beziehung lösen»), а по порожньому місцю — меню списку лише з «Neu» і «Finden».
    /// </summary>
    public bool HasRecordRow(int recordRow) =>
        AllureApi.Step($"Перевірити, чи є рядок {recordRow + 1} на вкладці «Dokumentation»", () =>
        {
            SelectTab("Dokumentation");
            UiInput.RightClick(
                () => Element,
                () => RecordListMenu() is not null,
                $"контекстного меню на місці рядка {recordRow + 1}",
                offset: RecordRow(recordRow));
            var exists = RecordRowMenu() is not null;
            CloseMenu(RecordListMenu);
            return exists;
        });

    /// <summary>Перевіряє, скільки рядків на вкладці «Dokumentation» (з предками, яких показує дерево).</summary>
    public void ShouldHaveRecordRows(int expected) =>
        AllureApi.Step($"Перевірити, що на вкладці «Dokumentation» рядків: {expected}", () =>
            this.ShouldSatisfyAllConditions(
                () => (expected == 0 || HasRecordRow(expected - 1)).ShouldBeTrue($"Рядка {expected} на вкладці «Dokumentation» немає"),
                () => HasRecordRow(expected).ShouldBeFalse($"На вкладці «Dokumentation» є зайвий рядок {expected + 1}")));

    /// <summary>Точка кнопки над списком: <paramref name="fromRight"/> 0 — крайня права кнопка, 1 — сусідня ліворуч тощо.</summary>
    public Point Button(int fromRight)
    {
        var scale = Scale;
        return new Point(
            Element.BoundingRectangle.Width - (int)Math.Round((LastButtonFromRight + fromRight * ButtonStep) * scale),
            (int)Math.Round(ButtonsCenter * scale));
    }

    /// <summary>Точка від кута панелі в одиницях WPF.</summary>
    public Point Scaled(double x, double y) => new((int)Math.Round(x * Scale), (int)Math.Round(y * Scale));

    /// <summary>Popup з елементом за умовою: меню відкриваються під вікном панелі або під головним вікном.</summary>
    public AutomationElement? FindPopupWith(Func<ConditionFactory, ConditionBase> condition) =>
        Popups.FindDescendant(_window, condition) ?? Popups.FindDescendant(_mainWindow, condition);

    public AutomationElement? FindPopup(string automationId) => FindPopupWith(cf => cf.ByAutomationId(automationId));

    public void CloseMenu(string menuId) => CloseMenu(() => FindPopup(menuId));

    // WPF сама закриває меню, коли ADAICA втрачає фокус, тож меню може вже не бути.
    public void CloseMenu(Func<AutomationElement?> findMenu)
    {
        if (findMenu() is not { } menu)
            return;

        UiInput.PressKey(() => findMenu() ?? menu, VirtualKeyShort.ESCAPE, () => findMenu() is null, "закриття контекстного меню");
    }

    public static MenuItemControl MenuItem(Func<AutomationElement?> menu, string name) =>
        new(name, () => Wait.For(
            () => menu()?.FindFirstDescendant(cf => cf.ByControlType(ControlType.MenuItem).And(cf.ByName(name))),
            $"пункту «{name}»"));

    // «Aufgabe» відкриває підменю: «Neue Aufgabe» і шаблони.
    public static void ChooseRecordType(Func<AutomationElement?> menu, string item)
    {
        if (item != TaskItem)
        {
            MenuItem(menu, item).Click();
            return;
        }

        MenuItem(menu, TaskItem).Expand();
        MenuItem(menu, NewTaskItem).Click();
    }

    // Вікно запису («In separatem Fenster») — дочірнє вікно вікна з панеллю, заголовок — підпис запису, як у дереві.
    private string OpenedRecordTitle(int recordRow)
    {
        SelectTab("Dokumentation");
        var before = ChildWindows().ToList();
        UiInput.RightClick(() => Element, () => RecordRowMenu() is not null, $"контекстного меню рядка {recordRow + 1}", offset: RecordRow(recordRow));
        MenuItem(RecordRowMenu, OpenItem).Expand();
        MenuItem(RecordRowMenu, SeparateWindowItem).Click();
        var window = Wait.For(
            () => ChildWindows().FirstOrDefault(window => !before.Contains(window)
                && window.FindFirstDescendant(cf => cf.ByAutomationId(CancelButtonId)) is not null),
            $"вікна запису з рядка {recordRow + 1}",
            RecordWindowTimeout);
        var title = window.Name;
        new ButtonControl($"Abbrechen у вікні «{title}»", Locate.ById(window, CancelButtonId)).Click();
        Wait.Until(() => !ChildWindows().Contains(window), $"закриття вікна «{title}»");
        return title;
    }

    private IEnumerable<AutomationElement> ChildWindows() => _window.FindAllChildren(cf => cf.ByControlType(ControlType.Window));

    private T OpenNewChildRecord<T>(int recordRow, string item, Func<T> dialog) =>
        AllureApi.Step($"Новий запис під рядком {recordRow + 1}: «Dokumentation» → меню рядка → «{NewItem}» → «{item}»", () =>
        {
            SelectTab("Dokumentation");
            UiInput.RightClick(() => Element, () => RecordRowMenu() is not null, $"контекстного меню рядка {recordRow + 1}", offset: RecordRow(recordRow));
            // ADAICA дописує до «Neu» останній вибраний тип («Neu: Notiz»), тож пункт шукаємо за початком назви.
            new MenuItemControl(NewItem, () => Wait.For(
                () => RecordRowMenu()?.FindAllDescendants(cf => cf.ByControlType(ControlType.MenuItem))
                    .FirstOrDefault(menuItem => menuItem.Name.StartsWith(NewItem, StringComparison.Ordinal)),
                $"пункту «{NewItem}…»")).Expand();
            ChooseRecordType(RecordRowMenu, item);
            return dialog();
        });

    // Меню рядка «Dokumentation» без AutomationId: упізнаємо його за пунктом, якого немає в меню порожнього місця.
    private AutomationElement? RecordRowMenu() => VisiblePopupWith(RecordRowMenuItem);

    // Меню рядка або порожнього місця списку «Dokumentation»: в обох є «Finden».
    private AutomationElement? RecordListMenu() => VisiblePopupWith("Finden");

    // Закриті меню WPF може лишати в дереві прихованими, тож беремо лише Popup на екрані.
    private AutomationElement? VisiblePopupWith(string menuItem) =>
        Popups.Of(_window).Concat(Popups.Of(_mainWindow))
            .FirstOrDefault(popup => !popup.IsOffscreen
                && popup.FindFirstDescendant(cf => cf.ByControlType(ControlType.MenuItem).And(cf.ByName(menuItem))) is not null);

    private AutomationElement OpenRowMenu(int row, string menuId)
    {
        UiInput.RightClick(() => Element, () => FindPopup(menuId) is not null, $"контекстного меню рядка {row + 1}", offset: Row(row));
        return Wait.For(() => FindPopup(menuId), $"контекстного меню «{menuId}»");
    }

    private Point Row(int row) => Scaled(FirstColumnCenter, FirstRowCenter + row * RowHeight);

    private Point RecordRow(int row) => Scaled(RecordNameCenter, FirstRowCenter + row * RowHeight);

    private double Scale => GetDpiForWindow(_window.Properties.NativeWindowHandle.Value) / 96.0;

    [DllImport("user32.dll")]
    private static extern uint GetDpiForWindow(nint window);
}
