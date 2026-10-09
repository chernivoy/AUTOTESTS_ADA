using ADAICA_AUTOTEST.Infrastructure;
using ADAICA_AUTOTEST.Pages;
using Allure.Net.Commons;
using Allure.Net.Commons.Attributes;

namespace ADAICA_AUTOTEST.Tests;

/// <summary>
/// Пропозиції завдань і термінів у вікні збереження листа: на вкладці «Dokumentation» ADAICA показує записи контакту
/// листа без позначки «Verknüpft», які відповідають налаштуванням «Vorschläge für Verknüpfung der Dokumentation» →
/// «E-Mails» (<see cref="LinkSuggestionOptions"/>, <see cref="SuggestionTab.Mails"/>). Тест створює персону з e-mail і
/// під нею записи з потрібними датами, ставить налаштування в колонці «Anwender» (діють одразу, без перезапуску) і
/// надсилає їй листи. Після тесту обидві колонки такі, як були до тесту: якщо «Anwender» збігався із «Systemweit», він
/// знову його успадковує. Для колег — користувачів ADAICA — пропозицій немає (так задумано), тож відправник їх не дає.
/// Ті самі налаштування для нових записів — <see cref="RecordSuggestionTests"/>.
/// </summary>
public partial class MailTests
{
    // Рядок 0 вкладки «Dokumentation» — «ROOT EMAIL», пропозиції — нижче.
    private const int FirstSuggestionRow = 1;

    /// <summary>
    /// Завдання за статусом і періодом (терміни вимкнено). Під персоною — незавершені завдання з початком 15 днів
    /// тому, 90 днів тому, через 40 і через 90 днів, а також завершені: 5 днів тому і 90 днів тому (останнє закрите
    /// вчора — для «Erledigt» ADAICA бере «Erledigt am», а не «Geschlossen an»). Листи: стандартно (незавершені ±2 місяці),
    /// «Erledigt», «Erledigt und Unerledigt», лише «Nächste(n)» 60 днів.
    /// </summary>
    [Test]
    [AllureSeverity(SeverityLevel.critical)]
    public void DokumentationNavigator_MailSuggestsTasksByStatusAndPeriod()
    {
        var (mainPage, customer, person, email) = CreateMailContact();
        var today = DateTime.Today;
        var openRecent = CreateContactTask(mainPage, customer, person.SortName, today.AddDays(-15));
        CreateContactTask(mainPage, customer, person.SortName, today.AddDays(-90));
        var openSoon = CreateContactTask(mainPage, customer, person.SortName, today.AddDays(40));
        CreateContactTask(mainPage, customer, person.SortName, today.AddDays(90));
        var doneRecent = CreateContactTask(mainPage, customer, person.SortName, today.AddDays(-20), done: today.AddDays(-5));
        CreateContactTask(mainPage, customer, person.SortName, today.AddDays(-100), done: today.AddDays(-90), closedOn: today.AddDays(-1));
        mainPage.OpenNavigator("Dokumentation");

        var tasksOnly = LinkSuggestionOptions.Default with
        {
            Appointments = LinkSuggestionOptions.Default.Appointments with { Enabled = false },
        };
        var tasks = tasksOnly.Tasks;

        UseLinkSuggestions(mainPage, SuggestionTab.Mails, tasksOnly);
        MailShouldSuggest(mainPage, customer, person, email, openRecent, openSoon).Cancel();

        UseLinkSuggestions(mainPage, SuggestionTab.Mails, tasksOnly with { Tasks = tasks with { Status = TaskStatusFilter.Done } });
        MailShouldSuggest(mainPage, customer, person, email, doneRecent).Cancel();

        UseLinkSuggestions(mainPage, SuggestionTab.Mails, tasksOnly with { Tasks = tasks with { Status = TaskStatusFilter.All } });
        MailShouldSuggest(mainPage, customer, person, email, openRecent, openSoon, doneRecent).Cancel();

        UseLinkSuggestions(mainPage, SuggestionTab.Mails, tasksOnly with { Tasks = tasks with { Previous = tasks.Previous.Off(), Next = SuggestionPeriod.Days(60) } });
        MailShouldSuggest(mainPage, customer, person, email, openSoon).Cancel();
    }

    /// <summary>
    /// Терміни за датою (завдання вимкнено). Під персоною — терміни 90 днів тому, через 10 і через 90 днів; створені
    /// вони всі сьогодні. Листи: стандартно («Erstelldatum» ±2 місяці — усі три), «Beginnt» ±2 місяці, «Zwischen» з
    /// одного дня (останній день включно), «Erstelldatum» лише «Nächste(n)» (створені раніше сьогодні — жодного).
    /// </summary>
    [Test]
    [AllureSeverity(SeverityLevel.critical)]
    public void DokumentationNavigator_MailSuggestsAppointmentsByDate()
    {
        var (mainPage, customer, person, email) = CreateMailContact();
        var today = DateTime.Today;
        var past = CreateContactAppointment(mainPage, customer, person.SortName, today.AddDays(-90));
        var soon = CreateContactAppointment(mainPage, customer, person.SortName, today.AddDays(10));
        var later = CreateContactAppointment(mainPage, customer, person.SortName, today.AddDays(90));
        mainPage.OpenNavigator("Dokumentation");

        var appointmentsOnly = LinkSuggestionOptions.Default with
        {
            Tasks = LinkSuggestionOptions.Default.Tasks with { Enabled = false },
        };
        var appointments = appointmentsOnly.Appointments;

        UseLinkSuggestions(mainPage, SuggestionTab.Mails, appointmentsOnly);
        MailShouldSuggest(mainPage, customer, person, email, past.Label, soon.Label, later.Label).Cancel();

        UseLinkSuggestions(mainPage, SuggestionTab.Mails, appointmentsOnly with { Appointments = appointments with { Field = AppointmentDateField.Begin } });
        MailShouldSuggest(mainPage, customer, person, email, soon.Label).Cancel();

        UseLinkSuggestions(mainPage, SuggestionTab.Mails, appointmentsOnly with
        {
            Appointments = appointments with
            {
                Field = AppointmentDateField.Begin,
                Mode = AppointmentPeriodMode.Between,
                From = later.Begin.Date,
                To = later.Begin.Date,
            },
        });
        MailShouldSuggest(mainPage, customer, person, email, later.Label).Cancel();

        UseLinkSuggestions(mainPage, SuggestionTab.Mails, appointmentsOnly with { Appointments = appointments with { Previous = appointments.Previous.Off() } });
        MailShouldSuggest(mainPage, customer, person, email).Cancel();
    }

    /// <summary>
    /// «Anwender» перекриває «Systemweit» в обидва боки. Під персоною — незавершене завдання й термін. Спершу завдання
    /// вимкнено лише для користувача — лишається термін. Потім завдання вимкнено системно, але явно ввімкнено для
    /// користувача, а терміни для користувача вимкнено — лишається завдання.
    /// </summary>
    [Test]
    [AllureSeverity(SeverityLevel.normal)]
    public void DokumentationNavigator_MailSuggestionsOfUserOverrideSystem()
    {
        var (mainPage, customer, person, email) = CreateMailContact();
        var today = DateTime.Today;
        var task = CreateContactTask(mainPage, customer, person.SortName, today.AddDays(-15));
        var appointment = CreateContactAppointment(mainPage, customer, person.SortName, today.AddDays(10));
        mainPage.OpenNavigator("Dokumentation");
        var defaults = LinkSuggestionOptions.Default;

        UseLinkSuggestions(mainPage, SuggestionTab.Mails, defaults with { Tasks = defaults.Tasks with { Enabled = false } });
        MailShouldSuggest(mainPage, customer, person, email, appointment.Label).Cancel();

        UseLinkSuggestions(mainPage, SuggestionTab.Mails, defaults with { Appointments = defaults.Appointments with { Enabled = false } }, systemTasks: false);
        MailShouldSuggest(mainPage, customer, person, email, task).Cancel();
    }

    /// <summary>
    /// Пропозиції йдуть за станом отримувача. Під персоною — два незавершені завдання й термін. Одне завдання тест
    /// позначає «Verknüpft» («Verknüpfung erstellen»), далі «Ignorieren» для отримувача: решта пропозицій зникає, а
    /// позначене лишається. «Wiederherstellen» повертає всі (позначка на місці). Збережений лист лежить лише під
    /// позначеним завданням і «ROOT EMAIL»: непозначені пропозиції не зв'язуються.
    /// </summary>
    [Test]
    [AllureSeverity(SeverityLevel.critical)]
    public void DokumentationNavigator_MailSuggestionsFollowRecipientState()
    {
        var (mainPage, customer, person, email) = CreateMailContact();
        var today = DateTime.Today;
        var linked = CreateContactTask(mainPage, customer, person.SortName, today.AddDays(-15));
        var other = CreateContactTask(mainPage, customer, person.SortName, today.AddDays(-10));
        var appointment = CreateContactAppointment(mainPage, customer, person.SortName, today.AddDays(10));
        mainPage.OpenNavigator("Dokumentation");
        UseLinkSuggestions(mainPage, SuggestionTab.Mails, LinkSuggestionOptions.Default);

        var saveDialog = MailShouldSuggest(mainPage, customer, person, email, linked, other, appointment.Label);
        saveDialog.LinkRecordRow(SuggestionRow(saveDialog, linked));

        saveDialog.ChooseInContactMenu(RecipientRow, "Ignorieren");
        SuggestionsShouldBe(saveDialog, linked);
        saveDialog.IsRecordRowLinked(FirstSuggestionRow).ShouldBeTrue($"«{linked}» більше не позначено «Verknüpft»");

        saveDialog.ChooseInContactMenu(RecipientRow, "Wiederherstellen");
        var rows = SuggestionsShouldBe(saveDialog, linked, other, appointment.Label);
        AllureApi.Step($"Перевірити, що «Verknüpft» позначено лише «{linked}»", () =>
            rows.Select((label, index) => (label, saveDialog.IsRecordRowLinked(FirstSuggestionRow + index)))
                .Where(row => row.Item2)
                .Select(row => row.label)
                .ShouldBe([linked], "Рядки з позначкою «Verknüpft»"));

        saveDialog.Save();
        var paths = mainPage.WaitForOpenedMail(email).ReadAccessPaths();
        AllureApi.Step($"Перевірити «Zugriffspfade»: лише «ROOT EMAIL» і «{linked}»", () =>
            paths.Records.Select(chain => chain[0]).ShouldBe(["ROOT EMAIL", linked], ignoreOrder: true, "Записи в «Zugriffspfade»"));
    }

    /// <summary>
    /// Записи фірми для співробітника. Співробітник нової фірми з e-mail; під ним — завдання, під фірмою — завдання й
    /// термін. Стандартно пропонується лише завдання співробітника. «Für Mitarbeiterkontakte auch die Aufgaben der
    /// Firma» додає завдання фірми (не термін); «Ignorieren» для отримувача прибирає й записи фірми, «Wiederherstellen»
    /// повертає. Той самий прапорець для термінів додає термін фірми, а не її завдання.
    /// </summary>
    [Test]
    [AllureSeverity(SeverityLevel.normal)]
    public void DokumentationNavigator_MailSuggestsCompanyRecordsForEmployee()
    {
        var mainPage = LogInAndOpenNavigator("Kontakte");
        EnsureOutlookAccount(mainPage, OutlookApplication.DefaultAccountAddress());
        var customer = CustomerCase.Random();
        var (employee, email, company) = CreateEmployeeWithEmail(mainPage, customer);
        var today = DateTime.Today;
        var employeeTask = CreateContactTask(mainPage, customer, employee.NodeName, today.AddDays(-15));
        var companyTask = CreateContactTask(mainPage, customer, company, today.AddDays(-10));
        var companyAppointment = CreateContactAppointment(mainPage, customer, company, today.AddDays(10));
        mainPage.OpenNavigator("Dokumentation");
        var defaults = LinkSuggestionOptions.Default;

        UseLinkSuggestions(mainPage, SuggestionTab.Mails, defaults);
        MailShouldSuggest(mainPage, customer, employee, email, employeeTask).Cancel();

        UseLinkSuggestions(mainPage, SuggestionTab.Mails, defaults with { Tasks = defaults.Tasks with { CompanyRecords = true } });
        var saveDialog = MailShouldSuggest(mainPage, customer, employee, email, employeeTask, companyTask);
        saveDialog.ChooseInContactMenu(RecipientRow, "Ignorieren");
        SuggestionsShouldBe(saveDialog);
        saveDialog.ChooseInContactMenu(RecipientRow, "Wiederherstellen");
        SuggestionsShouldBe(saveDialog, employeeTask, companyTask);
        saveDialog.Cancel();

        UseLinkSuggestions(mainPage, SuggestionTab.Mails, defaults with { Appointments = defaults.Appointments with { CompanyRecords = true } });
        MailShouldSuggest(mainPage, customer, employee, email, employeeTask, companyAppointment.Label).Cancel();
    }

    /// <summary>
    /// Вимикає звертання для надсилання, входить у навігатор Kontakte й створює персону з унікальною адресою, під якою
    /// тест далі створює записи (вона виділена в «Aktuelle»).
    /// </summary>
    private (MainPage MainPage, CustomerCase Customer, EmployeeData Person, string Email) CreateMailContact()
    {
        var mainPage = LogInAndOpenNavigator("Kontakte");
        EnsureOutlookAccount(mainPage, OutlookApplication.DefaultAccountAddress());
        var customer = CustomerCase.Random();
        var person = customer.NewMailPerson();
        var email = CustomerCase.NewMailAddress(person);
        CreatePersonWithEmail(mainPage, person, email);
        return (mainPage, customer, person, email);
    }

    /// <summary>
    /// Надсилає лист контакту <paramref name="recipient"/> і перевіряє пропозиції на вкладці «Dokumentation»: рівно
    /// <paramref name="expected"/> (підписи, як у дереві), у будь-якому порядку. Повертає вікно збереження.
    /// </summary>
    private MailSaveDialog MailShouldSuggest(
        MainPage mainPage, CustomerCase customer, EmployeeData recipient, string email, params string[] expected)
    {
        var (saveDialog, _) = SendMail(mainPage, customer.NewMailTo(recipient) with { To = email });
        saveDialog.ShouldHaveContactState(RecipientRow, MailContactState.Linked);
        SuggestionsShouldBe(saveDialog, expected);
        return saveDialog;
    }

    private static int SuggestionRow(MailSaveDialog saveDialog, string label) =>
        FirstSuggestionRow + saveDialog.RecordRowLabels(FirstSuggestionRow).ToList().IndexOf(label);

    /// <summary>Перевіряє записи під «ROOT EMAIL» на вкладці «Dokumentation» вікна збереження листа.</summary>
    private static IReadOnlyList<string> SuggestionsShouldBe(MailSaveDialog saveDialog, params string[] expected) =>
        SuggestionsShouldBe(saveDialog.Links, FirstSuggestionRow, expected);
}
