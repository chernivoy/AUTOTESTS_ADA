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
/// «E-Mail-Speicherndialog» — асистент збереження листа в ADAICA після надсилання з Outlook. Угорі — листи, що
/// зберігаються (`lvMail`), унизу — вкладки зв'язків вибраного листа: «Kontakte» (відправник «Von», отримувачі «An»,
/// «CC», додані вручну «Kontakt»), «Dokumentation» (записи, під якими буде лист; спершу — вузол, з якого його
/// створено, «ROOT EMAIL») і «Objekte». Поки хоч один отримувач не зв'язаний з контактом, OK недоступна, а внизу —
/// «Gemäß der Konfiguration müssen eine oder mehrere Verknüpfungen erfolgen».
/// </summary>
/// <remarks>
/// <para>
/// Вміст вкладок зв'язків (списки, рядок пошуку, кнопки над списком) UI Automation не показує навіть у raw-дереві
/// і через FromPoint: TabControlEx ADAICA тримає вміст вкладок у шаблоні поза TabItem. Тому рядки й кнопки тест
/// знаходить за координатами в межах «recordLinkView» — розмітка в незалежних від DPI одиницях WPF, помножених на
/// масштаб вікна, — а стан контакту бачить зі складу його контекстного меню (меню UI Automation бачить): ADAICA
/// показує в ньому лише доступні дії (<see cref="MailContactState"/>).
/// </para>
/// <para>
/// Прапорець «Keine Kontaktzuordnung» виконує ту саму команду, що й кнопка AUTO, а ADAICA після збереження
/// запам'ятовує його для користувача: наступні листи відкриваються вже з ним. «Abbrechen» нічого не запам'ятовує.
/// </para>
/// </remarks>
public sealed class MailSaveDialog
{
    private const string WindowTitle = "E-Mail-Speicherndialog";
    private const string RequiredLinksMessage = "Gemäß der Konfiguration müssen eine oder mehrere Verknüpfungen erfolgen";
    private const string FindCreateContactItem = "Zugehörigen Kontakt finden/erstellen";

    // Меню «+» на вкладці «Dokumentation» і пункт «Neu» меню її рядка: типи нових записів.
    private const string NewRecordMenuId = "RecordsLinkGroupContextMenu";
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

    // Заголовок вікна дочірнього запису: «Neue Notiz (übergeordnet: Назва батька)».
    private const string ParentTitlePrefix = "(übergeordnet: ";

    // Розмітка вкладок зв'язків в одиницях WPF (1/96 дюйма) від кута «recordLinkView»: середина першого рядка списку
    // й висота рядка, середина колонки «Funktion» (у «Dokumentation» — «Name»), середина рядка кнопок над списком.
    private const double FirstRowCenter = 64.5;
    private const double RowHeight = 20;
    private const double FirstColumnCenter = 60;
    private const double ButtonsCenter = 16.5;

    // Кнопки над списком — квадрати по 24 одиниці, праворуч; відстань від правого краю до середини кнопки.
    private const double LastButtonFromRight = 15;
    private const double ButtonStep = 25;

    // «Dokumentation»: середина колонки «Name» (праворуч від значка й відступів вкладеності), поле
    // «Suchbegriff eingeben…» з хрестиком і лупа праворуч від нього.
    private const double RecordNameCenter = 150;
    private const double SearchBoxCenter = 280;
    private const double SearchClearCenter = 374;
    private const double SearchButtonCenter = 401.5;

    private const int StateReadAttempts = 3;
    private static readonly TimeSpan StateReadInterval = TimeSpan.FromSeconds(1);

    // Після OK ADAICA зберігає лист і його вкладення.
    private static readonly TimeSpan SaveTimeout = TimeSpan.FromSeconds(60);

    // Зазвичай ADAICA готує надісланий лист до вікна за 1–7 с, але буває й майже 30 с (08.10.2026: один лист із 41 —
    // 28,6 с, причини в логах немає; від кількості листів у сесії час не залежить). Виклики UI Automation тим часом
    // закінчуються таймаутом COM.
    private static readonly TimeSpan OpenTimeout = TimeSpan.FromSeconds(30);

    private readonly Window _mainWindow;

    public MailSaveDialog(Window mainWindow)
    {
        _mainWindow = mainWindow;
        Root = Wait.For(
            () => mainWindow.FindAllChildren(cf => cf.ByControlType(ControlType.Window))
                .FirstOrDefault(window => window.FindFirstDescendant(cf => cf.ByAutomationId("lvMail")) is not null)?.AsWindow(),
            $"вікна «{WindowTitle}»",
            OpenTimeout);

        OkButton = new ButtonControl("OK", () => Root.FindFirstChild(cf => cf.ByControlType(ControlType.Button).And(cf.ByName("OK"))));
        CancelButton = new ButtonControl("Abbrechen", () => Root.FindFirstChild(cf => cf.ByControlType(ControlType.Button).And(cf.ByName("Abbrechen"))));
        NoContactAssignment = new CheckBoxControl(
            "Keine Kontaktzuordnung",
            () => Root.FindFirstChild(cf => cf.ByControlType(ControlType.CheckBox).And(cf.ByName("Keine Kontaktzuordnung"))));
    }

    public ButtonControl OkButton { get; }

    public ButtonControl CancelButton { get; }

    /// <summary>
    /// «Keine Kontaktzuordnung» («Nur zu den bekannten Kontakten speichern»). Лише для читання: Toggle з UI Automation
    /// змінює галочку, але не виконує команду ADAICA. Увімкнути — <see cref="AssignNoContacts"/>.
    /// </summary>
    public CheckBoxControl NoContactAssignment { get; }

    /// <summary>Теми листів у списку «Betreff».</summary>
    public IReadOnlyList<string> Subjects =>
        Root.FindByAutomationId("lvMail")
            .FindAllChildren(cf => cf.ByControlType(ControlType.DataItem))
            .Select(row => row.FindFirstDescendant(cf => cf.ByAutomationId("TextElement"))?.Name ?? string.Empty)
            .ToList();

    /// <summary>Чи показує вікно «Gemäß der Konfiguration müssen eine oder mehrere Verknüpfungen erfolgen».</summary>
    public bool RequiredLinksMissing =>
        Root.FindFirstChild(cf => cf.ByControlType(ControlType.Text).And(cf.ByName(RequiredLinksMessage))) is not null;

    // IsAvailable для закритого вікна WPF лишається true, тому перевіряємо, чи вікно ще серед дочірніх.
    public bool IsOpen => _mainWindow.FindAllChildren(cf => cf.ByControlType(ControlType.Window)).Contains(Root);

    private Window Root { get; }

    private AutomationElement Links => Root.FindByAutomationId("recordLinkView");

    public void ShouldHaveSubjects(params string[] expected) =>
        AllureApi.Step($"Перевірити листи у вікні «{WindowTitle}»: {string.Join("; ", expected)}",
            () => Subjects.ShouldBe(expected, $"Список «Betreff» у вікні «{WindowTitle}»"));

    /// <summary>
    /// Перевіряє, чи можна зберегти лист: OK доступна і внизу немає «Gemäß der Konfiguration…», або навпаки.
    /// </summary>
    public void ShouldBeSavable(bool expected) =>
        AllureApi.Step(expected
                ? "Перевірити, що лист можна зберегти: OK доступна, зв'язків вистачає"
                : "Перевірити, що лист не зберегти: OK недоступна, «Gemäß der Konfiguration müssen eine oder mehrere Verknüpfungen erfolgen»",
            () => this.ShouldSatisfyAllConditions(
                () => Wait.Until(() => OkButton.IsEnabled == expected, $"стану кнопки OK: {(expected ? "доступна" : "недоступна")}"),
                () => Wait.Until(() => RequiredLinksMissing != expected, $"повідомлення «{RequiredLinksMessage}»: {(expected ? "немає" : "є")}")));

    /// <summary>
    /// Контекстне меню рядка <paramref name="contactRow"/> (з нуля: «Von», далі «An», «CC», додані контакти) на вкладці
    /// «Kontakte»: пункти з доступністю. Меню закривається клавішею Esc у самому меню — Esc у вікні закрив би вікно.
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
                () => Links,
                () => FindPopup("ContactMenu") is not null || FindPopup("ContactsListContextMenu") is not null,
                $"контекстного меню на місці рядка {contactRow + 1}",
                offset: Row(contactRow));
            var exists = FindPopup("ContactMenu") is not null;
            CloseMenu(exists ? "ContactMenu" : "ContactsListContextMenu");
            return exists;
        });

    /// <summary>Перевіряє, скільки рядків на вкладці «Kontakte»: відправник, отримувачі, копії й додаткові контакти.</summary>
    public void ShouldHaveContactRows(int expected) =>
        AllureApi.Step($"Перевірити, що на вкладці «Kontakte» {expected} рядки", () =>
            this.ShouldSatisfyAllConditions(
                () => HasContactRow(expected - 1).ShouldBeTrue($"Рядка {expected} на вкладці «Kontakte» немає"),
                () => HasContactRow(expected).ShouldBeFalse($"На вкладці «Kontakte» є зайвий рядок {expected + 1}")));

    /// <summary>
    /// Перевіряє стан контакту в рядку <paramref name="contactRow"/> за його контекстним меню: ADAICA показує в ньому
    /// лише ті дії, які в цьому стані можливі (<see cref="MailContactState"/>).
    /// </summary>
    public void ShouldHaveContactState(int contactRow, MailContactState expected) =>
        AllureApi.Step($"Перевірити стан контакту в рядку {contactRow + 1}: {expected}", () =>
        {
            // Після дії ADAICA оновлює стан не миттєво, тож меню читаємо кілька разів.
            var expectedMenu = MailContactStates.Menu(expected);
            var menu = ContactMenu(contactRow);
            for (var attempt = 1; attempt < StateReadAttempts && !menu.SequenceEqual(expectedMenu); attempt++)
            {
                Thread.Sleep(StateReadInterval);
                menu = ContactMenu(contactRow);
            }

            menu.ShouldBe(expectedMenu, $"Контекстне меню рядка {contactRow + 1} на вкладці «Kontakte»");
        });

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
    /// «Zugehörigen Kontakt finden/erstellen» у меню рядка <paramref name="contactRow"/>. ADAICA відкриває пошук
    /// контакту «Finden &amp; Erstellen» як дочірнє вікно цього вікна. Якщо адреса має вигляд «vorname.nachname@…»,
    /// ADAICA сама вписує в рядок пошуку «Vorname Nachname»; e-mail не вписує.
    /// </summary>
    public ContactFindCreateWindow OpenFindCreateContact(int contactRow)
    {
        ChooseInContactMenu(contactRow, FindCreateContactItem);
        return new ContactFindCreateWindow(Root);
    }

    /// <summary>Кнопка «CC» над списком контактів: ігнорувати всіх отримувачів копії.</summary>
    public void IgnoreCopyRecipients() => AllureApi.Step("Натиснути «CC» на вкладці «Kontakte» (ігнорувати отримувачів копії)", () =>
    {
        SelectTab("Kontakte");
        // Що кнопка подіяла, видно лише з меню рядків, тож тест перевіряє стан рядків окремо.
        UiInput.LeftClick(() => Links, () => true, "кнопки «CC»", offset: Button(fromRight: 3));
    });

    /// <summary>
    /// Кнопка AUTO над списком контактів: усі нерозпізнані адреси — без зв'язку з контактом («Keine Kontaktzuordnung»).
    /// Те саме робить прапорець «Keine Kontaktzuordnung» унизу, і він після цього ввімкнений.
    /// </summary>
    public void AssignNoContacts() => AllureApi.Step("Натиснути AUTO на вкладці «Kontakte» («Keine Kontaktzuordnung» для всіх нерозпізнаних)", () =>
    {
        SelectTab("Kontakte");
        UiInput.LeftClick(() => Links, () => NoContactAssignment.IsChecked, "прапорця «Keine Kontaktzuordnung» після AUTO", offset: Button(fromRight: 2));
    });

    /// <summary>
    /// Лупа на вкладці «Dokumentation»: пошук запису, під яким буде лист (окрім вузла, з якого його створено).
    /// Вибраний запис з'являється в списку з позначкою «Verknüpft».
    /// </summary>
    public void LinkRecord(string words, string number) => AllureApi.Step($"Зв'язати лист із записом {number}: «Dokumentation» → лупа", () =>
    {
        SelectTab("Dokumentation");
        UiInput.LeftClick(
            () => Links,
            () => Root.FindFirstDescendant(cf => cf.ByAutomationId("tcSearchParts")) is not null,
            "вікна «Finden» з вкладки «Dokumentation»",
            offset: Button(fromRight: 0));
        new SearchWindow(Root).Choose(words, number);
    });

    /// <summary>
    /// «+» на вкладці «Dokumentation» → «Projekt»: новий проєкт верхнього рівня, з яким буде зв'язано лист. Вікно нового
    /// запису — дочірнє вікно цього вікна; після OK запис з'являється в списку з позначкою «Verknüpft».
    /// </summary>
    public ProjectDialog OpenNewProject() => OpenNewRecord(ProjectItem, () => new ProjectDialog(Root));

    /// <summary>«+» на вкладці «Dokumentation» → «Ordner».</summary>
    public FolderDialog OpenNewFolder() => OpenNewRecord(FolderItem, () => new FolderDialog(Root));

    /// <summary>«+» на вкладці «Dokumentation» → «Aufgabe» → «Neue Aufgabe».</summary>
    public TaskDialog OpenNewTask() => OpenNewRecord(TaskItem, () => new TaskDialog(Root));

    /// <summary>«+» на вкладці «Dokumentation» → «Termin».</summary>
    public AppointmentDialog OpenNewAppointment() => OpenNewRecord(AppointmentItem, () => new AppointmentDialog(Root));

    /// <summary>«+» на вкладці «Dokumentation» → «Notiz».</summary>
    public NoteDialog OpenNewNote() => OpenNewRecord(NoteItem, () => new NoteDialog(Root));

    /// <summary>
    /// Меню рядка <paramref name="recordRow"/> (з нуля, згори) на вкладці «Dokumentation» → «Neu» → «Projekt»: проєкт
    /// під записом цього рядка. Заголовок вікна — «Neues Projekt (übergeordnet: батько)», а після OK запис
    /// з'являється в списку під батьком з позначкою «Verknüpft».
    /// </summary>
    public ProjectDialog OpenNewChildProject(int recordRow) => OpenNewChildRecord(recordRow, ProjectItem, () => new ProjectDialog(Root));

    /// <summary>Меню рядка <paramref name="recordRow"/> на вкладці «Dokumentation» → «Neu» → «Ordner».</summary>
    public FolderDialog OpenNewChildFolder(int recordRow) => OpenNewChildRecord(recordRow, FolderItem, () => new FolderDialog(Root));

    /// <summary>Меню рядка <paramref name="recordRow"/> на вкладці «Dokumentation» → «Neu» → «Aufgabe» → «Neue Aufgabe».</summary>
    public TaskDialog OpenNewChildTask(int recordRow) => OpenNewChildRecord(recordRow, TaskItem, () => new TaskDialog(Root));

    /// <summary>Меню рядка <paramref name="recordRow"/> на вкладці «Dokumentation» → «Neu» → «Termin».</summary>
    public AppointmentDialog OpenNewChildAppointment(int recordRow) => OpenNewChildRecord(recordRow, AppointmentItem, () => new AppointmentDialog(Root));

    /// <summary>Меню рядка <paramref name="recordRow"/> на вкладці «Dokumentation» → «Neu» → «Notiz».</summary>
    public NoteDialog OpenNewChildNote(int recordRow) => OpenNewChildRecord(recordRow, NoteItem, () => new NoteDialog(Root));

    /// <summary>Пункти меню «+» на вкладці «Dokumentation» з доступністю.</summary>
    public IReadOnlyList<MenuEntry> NewRecordMenu() => AllureApi.Step("Прочитати меню «+» на вкладці «Dokumentation»", () =>
    {
        var menu = OpenNewRecordMenu();
        var entries = menu.FindAllChildren(cf => cf.ByControlType(ControlType.MenuItem))
            .Select(item => new MenuEntry(item.Name, item.IsEnabled))
            .ToList();
        CloseMenu(NewRecordMenuId);
        AllureApi.Step($"Пункти меню: {string.Join("; ", entries)}");
        return (IReadOnlyList<MenuEntry>)entries;
    });

    /// <summary>
    /// Як ADAICA підписує запис у рядку <paramref name="recordRow"/> на вкладці «Dokumentation». Сам список UI Automation
    /// не бачить, тож тест відкриває для рядка «Neu» → «Notiz», бере назву батька з заголовка вікна («Neue Notiz
    /// (übergeordnet: …)») і закриває вікно без збереження.
    /// </summary>
    public string RecordRowLabel(int recordRow) => AllureApi.Step($"Прочитати запис у рядку {recordRow + 1} на вкладці «Dokumentation»", () =>
    {
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
    /// Підписи всіх записів на вкладці «Dokumentation», починаючи з рядка <paramref name="fromRow"/> (рядок 0 —
    /// «ROOT EMAIL»), згори вниз (<see cref="RecordRowLabel"/>; кілька секунд на рядок).
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
    /// Чи позначено запис у рядку <paramref name="recordRow"/> «Verknüpft» (лист буде під ним): у меню такого рядка
    /// доступний «Beziehung lösen», а в запропонованого ADAICA запису — лише «Verknüpfung erstellen».
    /// </summary>
    public bool IsRecordRowLinked(int recordRow) =>
        AllureApi.Step($"Перевірити, чи позначено «Verknüpft» рядок {recordRow + 1} на вкладці «Dokumentation»", () =>
        {
            SelectTab("Dokumentation");
            UiInput.RightClick(() => Links, () => RecordRowMenu() is not null, $"контекстного меню рядка {recordRow + 1}", offset: RecordRow(recordRow));
            var linked = MenuItem(RecordRowMenu, RecordRowMenuItem).IsEnabled;
            CloseMenu(RecordListMenu);
            return linked;
        });

    /// <summary>Меню рядка <paramref name="recordRow"/> на вкладці «Dokumentation» → «Verknüpfung erstellen»: позначка «Verknüpft».</summary>
    public void LinkRecordRow(int recordRow) =>
        AllureApi.Step($"Рядок {recordRow + 1} на вкладці «Dokumentation» → контекстне меню → «{LinkItem}»", () =>
        {
            SelectTab("Dokumentation");
            UiInput.RightClick(() => Links, () => RecordRowMenu() is not null, $"контекстного меню рядка {recordRow + 1}", offset: RecordRow(recordRow));
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
                () => Links,
                () => RecordListMenu() is not null,
                $"контекстного меню на місці рядка {recordRow + 1}",
                offset: RecordRow(recordRow));
            var exists = RecordRowMenu() is not null;
            CloseMenu(RecordListMenu);
            return exists;
        });

    /// <summary>Перевіряє, скільки рядків на вкладці «Dokumentation» (з «ROOT EMAIL» і предками, яких показує дерево).</summary>
    public void ShouldHaveRecordRows(int expected) =>
        AllureApi.Step($"Перевірити, що на вкладці «Dokumentation» рядків: {expected}", () =>
            this.ShouldSatisfyAllConditions(
                () => (expected == 0 || HasRecordRow(expected - 1)).ShouldBeTrue($"Рядка {expected} на вкладці «Dokumentation» немає"),
                () => HasRecordRow(expected).ShouldBeFalse($"На вкладці «Dokumentation» є зайвий рядок {expected + 1}")));

    /// <summary>
    /// Фільтр списку «Dokumentation»: текст у поле «Suchbegriff eingeben…» і лупа поруч із ним
    /// (<paramref name="withEnter"/> — клавіша Enter у полі замість лупи). У списку лишаються записи, у назві чи номері
    /// яких є цей текст, з їхніми предками; поле з фільтром стає жовтим. Поле має бути порожнім.
    /// </summary>
    public void FilterRecords(string text, bool withEnter = false) =>
        AllureApi.Step($"Відфільтрувати «Dokumentation»: «{text}» → {(withEnter ? "Enter" : "лупа біля поля")}", () =>
        {
            SelectTab("Dokumentation");
            UiInput.LeftClick(() => Links, () => true, "поля «Suchbegriff eingeben…»", offset: Scaled(SearchBoxCenter, ButtonsCenter));
            // Що текст у полі, видно лише після застосування фільтра, тож перевіряє його тест кількістю рядків.
            UiInput.TypeText(() => Links, text, () => true, $"тексту «{text}» у полі «Suchbegriff eingeben…»");
            if (withEnter)
                UiInput.PressKey(() => Links, VirtualKeyShort.RETURN, () => true, "застосування фільтра клавішею Enter");
            else
                UiInput.LeftClick(() => Links, () => true, "лупи біля поля «Suchbegriff eingeben…»", offset: Scaled(SearchButtonCenter, ButtonsCenter));
        });

    /// <summary>Хрестик у полі «Suchbegriff eingeben…»: очищує поле й показує весь список.</summary>
    public void ClearRecordFilter() => AllureApi.Step("Очистити фільтр «Dokumentation» хрестиком у полі", () =>
    {
        SelectTab("Dokumentation");
        UiInput.LeftClick(() => Links, () => true, "хрестика в полі «Suchbegriff eingeben…»", offset: Scaled(SearchClearCenter, ButtonsCenter));
    });

    /// <summary>
    /// Кнопка ієрархії (ліва з трьох над списком «Dokumentation»): показує над записами їхніх предків, яких у списку
    /// немає, без позначки «Verknüpft»; повторне натискання ховає їх. Записи, батьки яких уже в списку, вкладені й так.
    /// </summary>
    public void ToggleHierarchy() => AllureApi.Step("Натиснути кнопку ієрархії на вкладці «Dokumentation»", () =>
    {
        SelectTab("Dokumentation");
        UiInput.LeftClick(() => Links, () => true, "кнопки ієрархії", offset: Button(fromRight: 2));
    });

    /// <summary>OK: зберігає лист із зв'язками; ADAICA закриває вікно й відкриває збережений лист.</summary>
    public void Save() => AllureApi.Step($"Зберегти лист кнопкою OK у вікні «{WindowTitle}»", () =>
    {
        Wait.Until(() => OkButton.IsEnabled, $"доступної кнопки OK у вікні «{WindowTitle}» (усі зв'язки задано)");
        OkButton.Click();

        // Якщо лист зберегти не вдалося, ADAICA лишає вікно відкритим і показує «E-Mail-Speicherdialog» з причиною.
        string? error = null;
        Wait.Until(() =>
        {
            if (!IsOpen)
                return true;
            if (MessageBoxDialog.Find(Root) is not { } message)
                return false;

            error = message.Message.Text;
            return true;
        }, $"закриття вікна «{WindowTitle}»", SaveTimeout);

        if (error is not null)
            throw new InvalidOperationException($"ADAICA не зберегла лист: «{error}».");
    });

    public void Cancel() => AllureApi.Step($"Закрити вікно «{WindowTitle}» без збереження листа", () =>
    {
        CancelButton.Click();
        Wait.Until(() => !IsOpen, $"закриття вікна «{WindowTitle}»");
    });

    /// <summary>Для прибирання після тесту: закриває вікно, лише якщо воно ще відкрите.</summary>
    public void CancelIfOpen()
    {
        if (IsOpen)
            Cancel();
    }

    /// <summary>Вкладка зв'язків за підписом («Kontakte», «Dokumentation», «Objekte»); заголовки вкладок UI Automation бачить.</summary>
    private void SelectTab(string header)
    {
        AutomationElement Tab() => Wait.For(
            () => Root.FindByAutomationId("tcLinkGroups").FindAllChildren(cf => cf.ByControlType(ControlType.TabItem))
                .FirstOrDefault(tab => tab.FindFirstChild(cf => cf.ByControlType(ControlType.Text))?.Name == header),
            $"вкладки «{header}»");
        var selection = Tab().Patterns.SelectionItem.Pattern;
        if (!selection.IsSelected.Value)
        {
            selection.Select();
            Wait.Until(() => Tab().Patterns.SelectionItem.Pattern.IsSelected.Value, $"вибору вкладки «{header}»");
        }
    }

    private T OpenNewRecord<T>(string item, Func<T> dialog) =>
        AllureApi.Step($"Новий запис для листа: «Dokumentation» → «+» → «{item}»", () =>
        {
            OpenNewRecordMenu();
            ChooseRecordType(() => FindPopup(NewRecordMenuId), item);
            return dialog();
        });

    private T OpenNewChildRecord<T>(int recordRow, string item, Func<T> dialog) =>
        AllureApi.Step($"Новий запис під рядком {recordRow + 1}: «Dokumentation» → меню рядка → «{NewItem}» → «{item}»", () =>
        {
            SelectTab("Dokumentation");
            UiInput.RightClick(() => Links, () => RecordRowMenu() is not null, $"контекстного меню рядка {recordRow + 1}", offset: RecordRow(recordRow));
            // ADAICA дописує до «Neu» останній вибраний тип («Neu: Notiz»), тож пункт шукаємо за початком назви.
            new MenuItemControl(NewItem, () => Wait.For(
                () => RecordRowMenu()?.FindAllDescendants(cf => cf.ByControlType(ControlType.MenuItem))
                    .FirstOrDefault(menuItem => menuItem.Name.StartsWith(NewItem, StringComparison.Ordinal)),
                $"пункту «{NewItem}…»")).Expand();
            ChooseRecordType(RecordRowMenu, item);
            return dialog();
        });

    private AutomationElement OpenNewRecordMenu()
    {
        SelectTab("Dokumentation");
        UiInput.LeftClickOpeningMenu(() => Links, () => FindPopup(NewRecordMenuId) is not null, "меню «+» на вкладці «Dokumentation»", offset: Button(fromRight: 1));
        return Wait.For(() => FindPopup(NewRecordMenuId), "меню «+»");
    }

    // «Aufgabe» відкриває підменю: «Neue Aufgabe» і шаблони.
    private static void ChooseRecordType(Func<AutomationElement?> menu, string item)
    {
        if (item != TaskItem)
        {
            MenuItem(menu, item).Click();
            return;
        }

        MenuItem(menu, TaskItem).Expand();
        MenuItem(menu, NewTaskItem).Click();
    }

    private static MenuItemControl MenuItem(Func<AutomationElement?> menu, string name) =>
        new(name, () => Wait.For(
            () => menu()?.FindFirstDescendant(cf => cf.ByControlType(ControlType.MenuItem).And(cf.ByName(name))),
            $"пункту «{name}»"));

    // Меню рядка «Dokumentation» без AutomationId: упізнаємо його за пунктом, якого немає в меню порожнього місця.
    private AutomationElement? RecordRowMenu() => VisiblePopupWith(RecordRowMenuItem);

    // Меню рядка або порожнього місця списку «Dokumentation»: в обох є «Finden».
    private AutomationElement? RecordListMenu() => VisiblePopupWith("Finden");

    // Закриті меню WPF може лишати в дереві прихованими, тож беремо лише Popup на екрані.
    private AutomationElement? VisiblePopupWith(string menuItem) =>
        Popups.Of(Root).Concat(Popups.Of(_mainWindow))
            .FirstOrDefault(popup => !popup.IsOffscreen
                && popup.FindFirstDescendant(cf => cf.ByControlType(ControlType.MenuItem).And(cf.ByName(menuItem))) is not null);

    private AutomationElement OpenRowMenu(int row, string menuId)
    {
        UiInput.RightClick(() => Links, () => FindPopup(menuId) is not null, $"контекстного меню рядка {row + 1}", offset: Row(row));
        return Wait.For(() => FindPopup(menuId), $"контекстного меню «{menuId}»");
    }

    private void CloseMenu(string menuId) => CloseMenu(() => FindPopup(menuId));

    // WPF сама закриває меню, коли ADAICA втрачає фокус, тож меню може вже не бути.
    private void CloseMenu(Func<AutomationElement?> findMenu)
    {
        if (findMenu() is not { } menu)
            return;

        UiInput.PressKey(() => findMenu() ?? menu, VirtualKeyShort.ESCAPE, () => findMenu() is null, "закриття контекстного меню");
    }

    private AutomationElement? FindPopup(string automationId) => FindPopupWith(cf => cf.ByAutomationId(automationId));

    private AutomationElement? FindPopupWith(Func<ConditionFactory, ConditionBase> condition) =>
        Popups.FindDescendant(Root, condition) ?? Popups.FindDescendant(_mainWindow, condition);

    private Point Row(int row) => Scaled(FirstColumnCenter, FirstRowCenter + row * RowHeight);

    private Point RecordRow(int row) => Scaled(RecordNameCenter, FirstRowCenter + row * RowHeight);

    // fromRight: 0 — крайня права кнопка, 1 — сусідня ліворуч тощо.
    private Point Button(int fromRight)
    {
        var scale = Scale;
        return new Point(
            Links.BoundingRectangle.Width - (int)Math.Round((LastButtonFromRight + fromRight * ButtonStep) * scale),
            (int)Math.Round(ButtonsCenter * scale));
    }

    private Point Scaled(double x, double y) => new((int)Math.Round(x * Scale), (int)Math.Round(y * Scale));

    private double Scale => GetDpiForWindow(Root.Properties.NativeWindowHandle.Value) / 96.0;

    [DllImport("user32.dll")]
    private static extern uint GetDpiForWindow(nint window);
}

/// <summary>Пункт контекстного меню і чи він доступний.</summary>
public sealed record MenuEntry(string Name, bool IsEnabled)
{
    public override string ToString() => IsEnabled ? Name : $"{Name} (недоступний)";
}
