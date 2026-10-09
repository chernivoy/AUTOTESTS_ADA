using ADAICA_AUTOTEST.Pages;
using Allure.Net.Commons;
using Allure.Net.Commons.Attributes;

namespace ADAICA_AUTOTEST.Tests;

/// <summary>
/// Пропозиції завдань і термінів у вікні нового запису: коли на вкладці «Kontakte» панелі зв'язків є контакт (запис
/// створюється під ним або контакт додано вручну), на вкладці «Dokumentation» ADAICA показує його записи без позначки
/// «Verknüpft», які відповідають налаштуванням «Vorschläge für Verknüpfung der Dokumentation» вкладки свого типу:
/// «Aufgaben», «Termine», «Projekte», «Ordner», «Notizen», «Dokumente» (<see cref="SuggestionTab"/>). Тест створює
/// персону й під нею записи з потрібними датами, ставить налаштування в колонці «Anwender» (діють одразу) і відкриває
/// вікна нових записів під персоною, не зберігаючи їх. Після тесту обидві колонки кожної зміненої вкладки такі, як були.
/// Ті самі налаштування для листів — <see cref="MailTests"/> («E-Mails»).
/// </summary>
/// <remarks>
/// Запис у рядку панелі тест упізнає із заголовка його вікна («Öffnen» → «In separatem Fenster»), кілька секунд на
/// рядок. Без прокрутки панель показує п'ять рядків, тож пропозицій у кожному вікні не більше.
/// </remarks>
[AllureSuite("Навігатори")]
public class RecordSuggestionTests : AdaicaTestBase
{
    // У вікні нового запису під контактом пропозиції починаються з першого рядка «Dokumentation»; у вікні запису
    // верхнього рівня рядок 0 — корінь («Aufgaben»), якого не відкрити.
    private const int ChildRecordFirstRow = 0;
    private const int TopLevelFirstRow = 1;

    private static readonly SuggestionTab[] RecordTabs =
    [
        SuggestionTab.Tasks, SuggestionTab.Appointments, SuggestionTab.Projects, SuggestionTab.Folders, SuggestionTab.Notes, SuggestionTab.Documents,
    ];

    /// <summary>
    /// Стандартні налаштування на всіх вкладках записів (незавершені завдання ±2 місяці від початку, терміни ±2 місяці
    /// від створення). Під персоною — незавершені завдання з початком 15 і 90 днів тому, завершене 5 днів тому і термін
    /// через 10 днів. У вікні нового завдання, терміну, проєкту, Ordner, інфозапису й документа (Editor) під персоною
    /// персона вже стоїть на вкладці «Kontakte», а пропозиції — незавершене завдання 15 днів тому й термін.
    /// </summary>
    [Test]
    [AllureSeverity(SeverityLevel.critical)]
    public void KontakteNavigator_NewRecordsSuggestContactRecords()
    {
        var (mainPage, customer, person, _) = CreateContact();
        var today = DateTime.Today;
        var openRecent = CreateContactTask(mainPage, customer, person.SortName, today.AddDays(-15));
        CreateContactTask(mainPage, customer, person.SortName, today.AddDays(-90));
        CreateContactTask(mainPage, customer, person.SortName, today.AddDays(-20), done: today.AddDays(-5));
        var appointment = CreateContactAppointment(mainPage, customer, person.SortName, today.AddDays(10));
        UseLinkSuggestions(mainPage, RecordTabs.ToDictionary(tab => tab, _ => LinkSuggestionOptions.Default));

        string[] expected = [openRecent, appointment.Label];
        ChildDialogsShouldSuggest(mainPage, person.SortName, new Dictionary<SuggestionTab, string[]>
        {
            [SuggestionTab.Tasks] = expected,
            [SuggestionTab.Appointments] = expected,
            [SuggestionTab.Projects] = expected,
            [SuggestionTab.Folders] = expected,
            [SuggestionTab.Notes] = expected,
            [SuggestionTab.Documents] = expected,
        });
    }

    /// <summary>
    /// Кожен тип запису бере налаштування своєї вкладки. Під персоною — незавершені завдання з початком 15 днів тому,
    /// через 40 і 90 днів тому, завершене 5 днів тому (початок 20 днів тому), терміни через 10 і 90 днів тому. На
    /// вкладках різні налаштування, а пропозиції у вікнах:
    /// «Aufgaben» — лише завершені → завершене;
    /// «Termine» — лише терміни за «Beginnt» ±2 місяці → термін через 10 днів;
    /// «Projekte» — усі завдання ±2 місяці → два незавершені й завершене;
    /// «Ordner» — усі терміни («Alle») → обидва терміни;
    /// «Notizen» — незавершені завдання лише на 60 днів уперед → завдання через 40 днів;
    /// «Dokumente» — терміни «Beginnt» «Zwischen» дня терміну 90 днів тому → цей термін.
    /// </summary>
    [Test]
    [AllureSeverity(SeverityLevel.critical)]
    public void KontakteNavigator_EachRecordTypeHasOwnSuggestionSettings()
    {
        var (mainPage, customer, person, _) = CreateContact();
        var today = DateTime.Today;
        var openRecent = CreateContactTask(mainPage, customer, person.SortName, today.AddDays(-15));
        var openSoon = CreateContactTask(mainPage, customer, person.SortName, today.AddDays(40));
        CreateContactTask(mainPage, customer, person.SortName, today.AddDays(-90));
        var doneRecent = CreateContactTask(mainPage, customer, person.SortName, today.AddDays(-20), done: today.AddDays(-5));
        var appointmentSoon = CreateContactAppointment(mainPage, customer, person.SortName, today.AddDays(10));
        var appointmentPast = CreateContactAppointment(mainPage, customer, person.SortName, today.AddDays(-90));

        var defaults = LinkSuggestionOptions.Default;
        var noTasks = defaults.Tasks with { Enabled = false };
        var noAppointments = defaults.Appointments with { Enabled = false };
        UseLinkSuggestions(mainPage, new Dictionary<SuggestionTab, LinkSuggestionOptions>
        {
            [SuggestionTab.Tasks] = new(defaults.Tasks with { Status = TaskStatusFilter.Done }, noAppointments),
            [SuggestionTab.Appointments] = new(noTasks, defaults.Appointments with { Field = AppointmentDateField.Begin }),
            [SuggestionTab.Projects] = new(defaults.Tasks with { Status = TaskStatusFilter.All }, noAppointments),
            [SuggestionTab.Folders] = new(noTasks, defaults.Appointments with { Mode = AppointmentPeriodMode.All }),
            [SuggestionTab.Notes] = new(
                defaults.Tasks with { Previous = defaults.Tasks.Previous.Off(), Next = SuggestionPeriod.Days(60) }, noAppointments),
            [SuggestionTab.Documents] = new(noTasks, defaults.Appointments with
            {
                Field = AppointmentDateField.Begin,
                Mode = AppointmentPeriodMode.Between,
                From = appointmentPast.Begin.Date,
                To = appointmentPast.Begin.Date,
            }),
        });

        ChildDialogsShouldSuggest(mainPage, person.SortName, new Dictionary<SuggestionTab, string[]>
        {
            [SuggestionTab.Tasks] = [doneRecent],
            [SuggestionTab.Appointments] = [appointmentSoon.Label],
            [SuggestionTab.Projects] = [openRecent, openSoon, doneRecent],
            [SuggestionTab.Folders] = [appointmentSoon.Label, appointmentPast.Label],
            [SuggestionTab.Notes] = [openSoon],
            [SuggestionTab.Documents] = [appointmentPast.Label],
        });
    }

    /// <summary>
    /// Зберігається лише позначене. Під персоною — два незавершені завдання й термін. У вікні нового проєкту під персоною
    /// тест позначає одне завдання «Verknüpft» («Verknüpfung erstellen») і зберігає проєкт: у «Zugriffspfade» проєкту
    /// серед записів лише це завдання, а персона — серед контактів. Потім у вікні нового інфозапису тест прибирає
    /// персону з «Kontakte» («Entfernen»): зникають і всі її пропозиції.
    /// </summary>
    [Test]
    [AllureSeverity(SeverityLevel.critical)]
    public void KontakteNavigator_NewRecordKeepsOnlyLinkedSuggestion()
    {
        var (mainPage, customer, person, _) = CreateContact();
        var today = DateTime.Today;
        var linked = CreateContactTask(mainPage, customer, person.SortName, today.AddDays(-15));
        var other = CreateContactTask(mainPage, customer, person.SortName, today.AddDays(-10));
        var appointment = CreateContactAppointment(mainPage, customer, person.SortName, today.AddDays(10));
        UseLinkSuggestions(mainPage, new Dictionary<SuggestionTab, LinkSuggestionOptions>
        {
            [SuggestionTab.Projects] = LinkSuggestionOptions.Default,
            [SuggestionTab.Notes] = LinkSuggestionOptions.Default,
        });

        var projectDialog = mainPage.OpenNewChildProjectDialog(person.SortName);
        OnCleanup(projectDialog.CancelIfOpen);
        var project = customer.NewSubproject();
        projectDialog.Form.Fill(project);
        var number = projectDialog.Form.Number.Text;
        var rows = SuggestionsShouldBe(projectDialog.Links, ChildRecordFirstRow, linked, other, appointment.Label);
        projectDialog.Links.LinkRecordRow(ChildRecordFirstRow + rows.ToList().IndexOf(linked));
        projectDialog.Save();

        mainPage.WaitForOpenedProject(number);
        var paths = mainPage.ReadOpenedRecordAccessPaths();
        AllureApi.Step($"Перевірити «Zugriffspfade» проєкту: серед записів лише «{linked}», серед контактів — {person.SortName}", () =>
            paths.ShouldSatisfyAllConditions(
                () => paths.Records.Select(chain => chain[0]).ShouldBe([linked], "Записи в «Zugriffspfade»"),
                () => paths.Contacts.ShouldContain(contact => contact.StartsWith(person.SortName), "Контакти в «Zugriffspfade»")));

        var noteDialog = mainPage.OpenNewChildNoteDialog(person.SortName);
        OnCleanup(noteDialog.CancelIfOpen);
        SuggestionsShouldBe(noteDialog.Links, ChildRecordFirstRow, linked, other, appointment.Label);
        noteDialog.Links.ChooseInContactMenu(0, "Entfernen");
        noteDialog.Links.HasContactRow(0).ShouldBeFalse($"{person.SortName} лишилася на вкладці «Kontakte» після «Entfernen»");
        SuggestionsShouldBe(noteDialog.Links, ChildRecordFirstRow);
        noteDialog.Cancel();
    }

    /// <summary>
    /// Записи фірми для співробітника. Співробітник нової фірми; під ним — завдання, під фірмою — завдання й термін.
    /// «Aufgaben» стандартно → лише завдання співробітника; «Projekte» з «Für Mitarbeiterkontakte auch die Aufgaben der
    /// Firma» → ще завдання фірми (не термін); «Ordner» з тим самим прапорцем для термінів → ще термін фірми (не завдання).
    /// </summary>
    [Test]
    [AllureSeverity(SeverityLevel.normal)]
    public void KontakteNavigator_NewRecordSuggestsCompanyRecordsForEmployee()
    {
        var mainPage = LogInAndOpenNavigator("Kontakte");
        var customer = CustomerCase.Random();
        var (employee, _, company) = CreateEmployeeWithEmail(mainPage, customer);
        var today = DateTime.Today;
        var employeeTask = CreateContactTask(mainPage, customer, employee.NodeName, today.AddDays(-15));
        var companyTask = CreateContactTask(mainPage, customer, company, today.AddDays(-10));
        var companyAppointment = CreateContactAppointment(mainPage, customer, company, today.AddDays(10));

        var defaults = LinkSuggestionOptions.Default;
        UseLinkSuggestions(mainPage, new Dictionary<SuggestionTab, LinkSuggestionOptions>
        {
            [SuggestionTab.Tasks] = defaults,
            [SuggestionTab.Projects] = defaults with { Tasks = defaults.Tasks with { CompanyRecords = true } },
            [SuggestionTab.Folders] = defaults with { Appointments = defaults.Appointments with { CompanyRecords = true } },
        });

        ChildDialogsShouldSuggest(mainPage, employee.NodeName, new Dictionary<SuggestionTab, string[]>
        {
            [SuggestionTab.Tasks] = [employeeTask],
            [SuggestionTab.Projects] = [employeeTask, companyTask],
            [SuggestionTab.Folders] = [employeeTask, companyAppointment.Label],
        });
    }

    /// <summary>
    /// Контакт, доданий у вікні нового запису вручну. Під персоною — незавершене завдання й термін. Нове завдання
    /// верхнього рівня (навігатор Dokumentation, вузол «Aufgaben») — без контактів і пропозицій. «Kontakte» → «+» →
    /// «Kontakt» → «Kontaktzuordnungs-Dialog» → «Finden» за e-mail персони → OK: з'являються її завдання й термін.
    /// «Entfernen» для персони прибирає їх знову.
    /// </summary>
    [Test]
    [AllureSeverity(SeverityLevel.normal)]
    public void DokumentationNavigator_ContactAddedToNewRecordBringsSuggestions()
    {
        var (mainPage, customer, person, email) = CreateContact();
        var today = DateTime.Today;
        var task = CreateContactTask(mainPage, customer, person.SortName, today.AddDays(-15));
        var appointment = CreateContactAppointment(mainPage, customer, person.SortName, today.AddDays(10));
        UseLinkSuggestions(mainPage, SuggestionTab.Tasks, LinkSuggestionOptions.Default);
        mainPage.OpenNavigator("Dokumentation");

        var dialog = mainPage.OpenNewTaskDialog();
        OnCleanup(dialog.CancelIfOpen);
        dialog.Links.HasContactRow(0).ShouldBeFalse("У нового завдання верхнього рівня вже є контакт");
        SuggestionsShouldBe(dialog.Links, TopLevelFirstRow);

        var contactsDialog = dialog.Links.OpenAddContact();
        OnCleanup(contactsDialog.CancelIfOpen);
        contactsDialog.AddFound(email);
        SuggestionsShouldBe(dialog.Links, TopLevelFirstRow, task, appointment.Label);

        dialog.Links.ChooseInContactMenu(0, "Entfernen");
        SuggestionsShouldBe(dialog.Links, TopLevelFirstRow);
        dialog.Cancel();
    }

    /// <summary>Входить у навігатор Kontakte й створює персону з унікальною адресою (вона виділена в «Aktuelle»).</summary>
    private (MainPage MainPage, CustomerCase Customer, EmployeeData Person, string Email) CreateContact()
    {
        var mainPage = LogInAndOpenNavigator("Kontakte");
        var customer = CustomerCase.Random();
        var person = customer.NewMailPerson();
        var email = CustomerCase.NewMailAddress(person);
        CreatePersonWithEmail(mainPage, person, email);
        return (mainPage, customer, person, email);
    }

    /// <summary>
    /// Для кожного типу запису з <paramref name="expected"/> відкриває вікно нового запису під контактом
    /// <paramref name="parentNode"/>, перевіряє пропозиції й закриває вікно без збереження.
    /// </summary>
    private void ChildDialogsShouldSuggest(MainPage mainPage, string parentNode, IReadOnlyDictionary<SuggestionTab, string[]> expected)
    {
        foreach (var (tab, labels) in expected)
        {
            AllureApi.Step($"Вікно нового запису ({tab}) під {parentNode}: пропозиції", () =>
            {
                switch (tab)
                {
                    case SuggestionTab.Documents:
                        var editor = mainPage.OpenNewChildDocumentEditor(parentNode);
                        OnCleanup(editor.CancelIfOpen);
                        SuggestionsShouldBe(editor.Links, ChildRecordFirstRow, labels);
                        editor.Cancel();
                        break;
                    default:
                        var dialog = OpenNewChildDialog(mainPage, tab, parentNode);
                        OnCleanup(dialog.CancelIfOpen);
                        SuggestionsShouldBe(dialog.Links, ChildRecordFirstRow, labels);
                        dialog.Cancel();
                        break;
                }
            });
        }
    }

    private static RecordDialog OpenNewChildDialog(MainPage mainPage, SuggestionTab tab, string parentNode) => tab switch
    {
        SuggestionTab.Tasks => mainPage.OpenNewChildTaskDialog(parentNode),
        SuggestionTab.Appointments => mainPage.OpenNewChildAppointmentDialog(parentNode),
        SuggestionTab.Projects => mainPage.OpenNewChildProjectDialog(parentNode),
        SuggestionTab.Folders => mainPage.OpenNewChildFolderDialog(parentNode),
        SuggestionTab.Notes => mainPage.OpenNewChildNoteDialog(parentNode),
        _ => throw new ArgumentOutOfRangeException(nameof(tab), tab, "Для цієї вкладки немає вікна нового запису."),
    };
}
