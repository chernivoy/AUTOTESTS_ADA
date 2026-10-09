using ADAICA_AUTOTEST.Controls;
using ADAICA_AUTOTEST.Infrastructure;
using Allure.Net.Commons;
using FlaUI.Core.AutomationElements;
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
/// Вміст вкладок зв'язків UI Automation не показує, тож рядки й кнопки тест знаходить за координатами
/// (<see cref="RecordLinksPanel"/>, та сама панель, що й у вікні нового запису), а стан контакту бачить зі складу його
/// контекстного меню: ADAICA показує в ньому лише доступні дії (<see cref="MailContactState"/>).
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

    // Меню «+» на вкладці «Dokumentation»: типи нових записів.
    private const string NewRecordMenuId = "RecordsLinkGroupContextMenu";
    private const string ProjectItem = "Projekt";
    private const string FolderItem = "Ordner";
    private const string TaskItem = "Aufgabe";
    private const string AppointmentItem = "Termin";
    private const string NoteItem = "Notiz";

    // «Dokumentation»: середина рядка кнопок, поле «Suchbegriff eingeben…» з хрестиком і лупа праворуч від нього
    // (в одиницях WPF від кута панелі, див. RecordLinksPanel).
    private const double ButtonsCenter = 16.5;
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
        Links = new RecordLinksPanel(Root, mainWindow, "recordLinkView");
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

    /// <summary>Вкладки зв'язків листа: «Kontakte», «Dokumentation», «Objekte».</summary>
    public RecordLinksPanel Links { get; }

    private Window Root { get; }

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
    /// «Kontakte»: пункти з доступністю.
    /// </summary>
    public IReadOnlyList<MenuEntry> ContactMenu(int contactRow) => Links.ContactMenu(contactRow);

    /// <summary>Чи є на вкладці «Kontakte» рядок <paramref name="contactRow"/>.</summary>
    public bool HasContactRow(int contactRow) => Links.HasContactRow(contactRow);

    /// <summary>Перевіряє, скільки рядків на вкладці «Kontakte»: відправник, отримувачі, копії й додаткові контакти.</summary>
    public void ShouldHaveContactRows(int expected) => Links.ShouldHaveContactRows(expected);

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
    public void ChooseInContactMenu(int contactRow, string item) => Links.ChooseInContactMenu(contactRow, item);

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
        Links.SelectTab("Kontakte");
        // Що кнопка подіяла, видно лише з меню рядків, тож тест перевіряє стан рядків окремо.
        UiInput.LeftClick(() => Links.Element, () => true, "кнопки «CC»", offset: Links.Button(fromRight: 3));
    });

    /// <summary>
    /// Кнопка AUTO над списком контактів: усі нерозпізнані адреси — без зв'язку з контактом («Keine Kontaktzuordnung»).
    /// Те саме робить прапорець «Keine Kontaktzuordnung» унизу, і він після цього ввімкнений.
    /// </summary>
    public void AssignNoContacts() => AllureApi.Step("Натиснути AUTO на вкладці «Kontakte» («Keine Kontaktzuordnung» для всіх нерозпізнаних)", () =>
    {
        Links.SelectTab("Kontakte");
        UiInput.LeftClick(() => Links.Element, () => NoContactAssignment.IsChecked, "прапорця «Keine Kontaktzuordnung» після AUTO", offset: Links.Button(fromRight: 2));
    });

    /// <summary>
    /// Лупа на вкладці «Dokumentation»: пошук запису, під яким буде лист (окрім вузла, з якого його створено).
    /// Вибраний запис з'являється в списку з позначкою «Verknüpft».
    /// </summary>
    public void LinkRecord(string words, string number) => AllureApi.Step($"Зв'язати лист із записом {number}: «Dokumentation» → лупа", () =>
    {
        Links.SelectTab("Dokumentation");
        UiInput.LeftClick(
            () => Links.Element,
            () => Root.FindFirstDescendant(cf => cf.ByAutomationId("tcSearchParts")) is not null,
            "вікна «Finden» з вкладки «Dokumentation»",
            offset: Links.Button(fromRight: 0));
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
    /// під записом цього рядка. Після OK запис з'являється в списку під батьком з позначкою «Verknüpft».
    /// </summary>
    public ProjectDialog OpenNewChildProject(int recordRow) => Links.OpenNewChildProject(recordRow);

    /// <summary>Меню рядка <paramref name="recordRow"/> на вкладці «Dokumentation» → «Neu» → «Ordner».</summary>
    public FolderDialog OpenNewChildFolder(int recordRow) => Links.OpenNewChildFolder(recordRow);

    /// <summary>Меню рядка <paramref name="recordRow"/> на вкладці «Dokumentation» → «Neu» → «Aufgabe» → «Neue Aufgabe».</summary>
    public TaskDialog OpenNewChildTask(int recordRow) => Links.OpenNewChildTask(recordRow);

    /// <summary>Меню рядка <paramref name="recordRow"/> на вкладці «Dokumentation» → «Neu» → «Termin».</summary>
    public AppointmentDialog OpenNewChildAppointment(int recordRow) => Links.OpenNewChildAppointment(recordRow);

    /// <summary>Меню рядка <paramref name="recordRow"/> на вкладці «Dokumentation» → «Neu» → «Notiz».</summary>
    public NoteDialog OpenNewChildNote(int recordRow) => Links.OpenNewChildNote(recordRow);

    /// <summary>Пункти меню «+» на вкладці «Dokumentation» з доступністю.</summary>
    public IReadOnlyList<MenuEntry> NewRecordMenu() => AllureApi.Step("Прочитати меню «+» на вкладці «Dokumentation»", () =>
    {
        var menu = OpenNewRecordMenu();
        var entries = menu.FindAllChildren(cf => cf.ByControlType(ControlType.MenuItem))
            .Select(item => new MenuEntry(item.Name, item.IsEnabled))
            .ToList();
        Links.CloseMenu(NewRecordMenuId);
        AllureApi.Step($"Пункти меню: {string.Join("; ", entries)}");
        return (IReadOnlyList<MenuEntry>)entries;
    });

    /// <summary>Як ADAICA підписує запис у рядку <paramref name="recordRow"/> на вкладці «Dokumentation».</summary>
    public string RecordRowLabel(int recordRow) => Links.RecordRowLabel(recordRow);

    /// <summary>Підписи записів на вкладці «Dokumentation» з рядка <paramref name="fromRow"/> (рядок 0 — «ROOT EMAIL»).</summary>
    public IReadOnlyList<string> RecordRowLabels(int fromRow = 0) => Links.RecordRowLabels(fromRow);

    /// <summary>Чи позначено запис у рядку <paramref name="recordRow"/> «Verknüpft» (лист буде під ним).</summary>
    public bool IsRecordRowLinked(int recordRow) => Links.IsRecordRowLinked(recordRow);

    /// <summary>Меню рядка <paramref name="recordRow"/> на вкладці «Dokumentation» → «Verknüpfung erstellen».</summary>
    public void LinkRecordRow(int recordRow) => Links.LinkRecordRow(recordRow);

    /// <summary>Чи є на вкладці «Dokumentation» рядок <paramref name="recordRow"/>.</summary>
    public bool HasRecordRow(int recordRow) => Links.HasRecordRow(recordRow);

    /// <summary>Перевіряє, скільки рядків на вкладці «Dokumentation» (з «ROOT EMAIL» і предками, яких показує дерево).</summary>
    public void ShouldHaveRecordRows(int expected) => Links.ShouldHaveRecordRows(expected);

    /// <summary>
    /// Фільтр списку «Dokumentation»: текст у поле «Suchbegriff eingeben…» і лупа поруч із ним
    /// (<paramref name="withEnter"/> — клавіша Enter у полі замість лупи). У списку лишаються записи, у назві чи номері
    /// яких є цей текст, з їхніми предками; поле з фільтром стає жовтим. Поле має бути порожнім.
    /// </summary>
    public void FilterRecords(string text, bool withEnter = false) =>
        AllureApi.Step($"Відфільтрувати «Dokumentation»: «{text}» → {(withEnter ? "Enter" : "лупа біля поля")}", () =>
        {
            Links.SelectTab("Dokumentation");
            UiInput.LeftClick(() => Links.Element, () => true, "поля «Suchbegriff eingeben…»", offset: Links.Scaled(SearchBoxCenter, ButtonsCenter));
            // Що текст у полі, видно лише після застосування фільтра, тож перевіряє його тест кількістю рядків.
            UiInput.TypeText(() => Links.Element, text, () => true, $"тексту «{text}» у полі «Suchbegriff eingeben…»");
            if (withEnter)
                UiInput.PressKey(() => Links.Element, VirtualKeyShort.RETURN, () => true, "застосування фільтра клавішею Enter");
            else
                UiInput.LeftClick(() => Links.Element, () => true, "лупи біля поля «Suchbegriff eingeben…»", offset: Links.Scaled(SearchButtonCenter, ButtonsCenter));
        });

    /// <summary>Хрестик у полі «Suchbegriff eingeben…»: очищує поле й показує весь список.</summary>
    public void ClearRecordFilter() => AllureApi.Step("Очистити фільтр «Dokumentation» хрестиком у полі", () =>
    {
        Links.SelectTab("Dokumentation");
        UiInput.LeftClick(() => Links.Element, () => true, "хрестика в полі «Suchbegriff eingeben…»", offset: Links.Scaled(SearchClearCenter, ButtonsCenter));
    });

    /// <summary>
    /// Кнопка ієрархії (ліва з трьох над списком «Dokumentation»): показує над записами їхніх предків, яких у списку
    /// немає, без позначки «Verknüpft»; повторне натискання ховає їх. Записи, батьки яких уже в списку, вкладені й так.
    /// </summary>
    public void ToggleHierarchy() => AllureApi.Step("Натиснути кнопку ієрархії на вкладці «Dokumentation»", () =>
    {
        Links.SelectTab("Dokumentation");
        UiInput.LeftClick(() => Links.Element, () => true, "кнопки ієрархії", offset: Links.Button(fromRight: 2));
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

    private T OpenNewRecord<T>(string item, Func<T> dialog) =>
        AllureApi.Step($"Новий запис для листа: «Dokumentation» → «+» → «{item}»", () =>
        {
            OpenNewRecordMenu();
            RecordLinksPanel.ChooseRecordType(() => FindPopup(NewRecordMenuId), item);
            return dialog();
        });

    private AutomationElement OpenNewRecordMenu()
    {
        Links.SelectTab("Dokumentation");
        UiInput.LeftClickOpeningMenu(() => Links.Element, () => FindPopup(NewRecordMenuId) is not null, "меню «+» на вкладці «Dokumentation»", offset: Links.Button(fromRight: 1));
        return Wait.For(() => FindPopup(NewRecordMenuId), "меню «+»");
    }

    private AutomationElement? FindPopup(string automationId) => Links.FindPopup(automationId);
}

/// <summary>Пункт контекстного меню і чи він доступний.</summary>
public sealed record MenuEntry(string Name, bool IsEnabled)
{
    public override string ToString() => IsEnabled ? Name : $"{Name} (недоступний)";
}
