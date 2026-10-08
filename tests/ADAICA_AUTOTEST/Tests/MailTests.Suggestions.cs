using ADAICA_AUTOTEST.Infrastructure;
using ADAICA_AUTOTEST.Pages;
using Allure.Net.Commons;
using Allure.Net.Commons.Attributes;

namespace ADAICA_AUTOTEST.Tests;

/// <summary>
/// Пропозиції завдань і термінів у вікні збереження листа: на вкладці «Dokumentation» ADAICA показує записи контакту
/// листа без позначки «Verknüpft», які відповідають налаштуванням «Vorschläge für Verknüpfung der Dokumentation» →
/// «E-Mails» (<see cref="MailSuggestionOptions"/>). Тест створює персону з e-mail і під нею записи з потрібними датами,
/// ставить налаштування в колонці «Anwender» (діють одразу, без перезапуску) і надсилає їй листи. Після тесту обидві
/// колонки такі, як були до тесту: якщо «Anwender» збігався із «Systemweit», він знову його успадковує. Для колег — користувачів ADAICA — пропозицій немає (так задумано), тож
/// відправник їх не дає.
/// </summary>
public partial class MailTests
{
    // Рядок 0 вкладки «Dokumentation» — «ROOT EMAIL», пропозиції — нижче.
    private const int FirstSuggestionRow = 1;

    // Пропозиції ADAICA додає, коли розпізнає контакти листа, тож перший прочитаний список може бути ще неповним.
    private static readonly TimeSpan SuggestionsSettle = TimeSpan.FromSeconds(3);

    // Чи вже запам'ятовано налаштування до тесту, які поверне прибирання (див. UseMailSuggestions).
    private bool _suggestionsSaved;

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

        var tasksOnly = MailSuggestionOptions.Default with
        {
            Appointments = MailSuggestionOptions.Default.Appointments with { Enabled = false },
        };
        var tasks = tasksOnly.Tasks;

        UseMailSuggestions(mainPage, tasksOnly);
        MailShouldSuggest(mainPage, customer, person, email, openRecent, openSoon).Cancel();

        UseMailSuggestions(mainPage, tasksOnly with { Tasks = tasks with { Status = TaskStatusFilter.Done } });
        MailShouldSuggest(mainPage, customer, person, email, doneRecent).Cancel();

        UseMailSuggestions(mainPage, tasksOnly with { Tasks = tasks with { Status = TaskStatusFilter.All } });
        MailShouldSuggest(mainPage, customer, person, email, openRecent, openSoon, doneRecent).Cancel();

        UseMailSuggestions(mainPage, tasksOnly with { Tasks = tasks with { Previous = tasks.Previous.Off(), Next = SuggestionPeriod.Days(60) } });
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

        var appointmentsOnly = MailSuggestionOptions.Default with
        {
            Tasks = MailSuggestionOptions.Default.Tasks with { Enabled = false },
        };
        var appointments = appointmentsOnly.Appointments;

        UseMailSuggestions(mainPage, appointmentsOnly);
        MailShouldSuggest(mainPage, customer, person, email, past.Label, soon.Label, later.Label).Cancel();

        UseMailSuggestions(mainPage, appointmentsOnly with { Appointments = appointments with { Field = AppointmentDateField.Begin } });
        MailShouldSuggest(mainPage, customer, person, email, soon.Label).Cancel();

        UseMailSuggestions(mainPage, appointmentsOnly with
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

        UseMailSuggestions(mainPage, appointmentsOnly with { Appointments = appointments with { Previous = appointments.Previous.Off() } });
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
        var defaults = MailSuggestionOptions.Default;

        UseMailSuggestions(mainPage, defaults with { Tasks = defaults.Tasks with { Enabled = false } });
        MailShouldSuggest(mainPage, customer, person, email, appointment.Label).Cancel();

        UseMailSuggestions(mainPage, defaults with { Appointments = defaults.Appointments with { Enabled = false } }, systemTasks: false);
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
        UseMailSuggestions(mainPage, MailSuggestionOptions.Default);

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
        TurnOffSalutationForSending();
        var mainPage = LogInAndOpenNavigator("Kontakte");
        EnsureOutlookAccount(mainPage, OutlookApplication.DefaultAccountAddress());
        var customer = CustomerCase.Random();
        var (employee, email, company) = CreateEmployeeWithEmail(mainPage, customer);
        var today = DateTime.Today;
        var employeeTask = CreateContactTask(mainPage, customer, employee.NodeName, today.AddDays(-15));
        var companyTask = CreateContactTask(mainPage, customer, company, today.AddDays(-10));
        var companyAppointment = CreateContactAppointment(mainPage, customer, company, today.AddDays(10));
        mainPage.OpenNavigator("Dokumentation");
        var defaults = MailSuggestionOptions.Default;

        UseMailSuggestions(mainPage, defaults);
        MailShouldSuggest(mainPage, customer, employee, email, employeeTask).Cancel();

        UseMailSuggestions(mainPage, defaults with { Tasks = defaults.Tasks with { CompanyRecords = true } });
        var saveDialog = MailShouldSuggest(mainPage, customer, employee, email, employeeTask, companyTask);
        saveDialog.ChooseInContactMenu(RecipientRow, "Ignorieren");
        SuggestionsShouldBe(saveDialog);
        saveDialog.ChooseInContactMenu(RecipientRow, "Wiederherstellen");
        SuggestionsShouldBe(saveDialog, employeeTask, companyTask);
        saveDialog.Cancel();

        UseMailSuggestions(mainPage, defaults with { Appointments = defaults.Appointments with { CompanyRecords = true } });
        MailShouldSuggest(mainPage, customer, employee, email, employeeTask, companyAppointment.Label).Cancel();
    }

    /// <summary>
    /// Вимикає звертання для надсилання, входить у навігатор Kontakte й створює персону з унікальною адресою, під якою
    /// тест далі створює записи (вона виділена в «Aktuelle»).
    /// </summary>
    private (MainPage MainPage, CustomerCase Customer, EmployeeData Person, string Email) CreateMailContact()
    {
        TurnOffSalutationForSending();
        var mainPage = LogInAndOpenNavigator("Kontakte");
        EnsureOutlookAccount(mainPage, OutlookApplication.DefaultAccountAddress());
        var customer = CustomerCase.Random();
        var person = customer.NewMailPerson();
        var email = CustomerCase.NewMailAddress(person);
        CreatePersonWithEmail(mainPage, person, email);
        return (mainPage, customer, person, email);
    }

    /// <summary>
    /// Співробітник нової фірми з адресою: Gesamtliste → «Neue Person» → у рядку пошуку персона, фірма й e-mail →
    /// «Neue Person/Firma» → OK. ADAICA кладе його у відділ «andere Tätigkeiten» і виділяє в «Aktuelle → Фірма → …».
    /// </summary>
    private (EmployeeData Employee, string Email, string Company) CreateEmployeeWithEmail(MainPage mainPage, CustomerCase customer)
    {
        var employee = customer.NewMailPerson();
        var email = CustomerCase.NewMailAddress(employee);
        var company = customer.NewCompany().Name;
        return AllureApi.Step($"Створити співробітника {employee.SortName} <{email}> нової фірми {company}", () =>
        {
            var findCreateWindow = mainPage.OpenNewPersonWindow();
            OnCleanup(findCreateWindow.CloseIfOpen);
            findCreateWindow.Person.Fill(employee.PrefilledName());
            findCreateWindow.Company.Fill(company);
            findCreateWindow.Email.Fill(email);
            findCreateWindow.Action.WaitForText("Firma erstellen, Person erstellen, Mitarbeiter zur Firma hinzufügen");

            var personDialog = findCreateWindow.OpenNewPersonDialog();
            OnCleanup(personDialog.CancelIfOpen);
            personDialog.Salutation.Select(employee.Salutation);
            personDialog.Save();
            findCreateWindow.WaitUntilClosed();
            return (employee, email, company);
        });
    }

    /// <summary>Завдання під контактом <paramref name="parentNode"/> (меню вузла «Neu» → «Aufgabe»). Повертає його назву.</summary>
    private string CreateContactTask(
        MainPage mainPage, CustomerCase customer, string parentNode, DateTime begin, DateTime? done = null, DateTime? closedOn = null)
    {
        var dialog = mainPage.OpenNewChildTaskDialog(parentNode);
        OnCleanup(dialog.CancelIfOpen);
        var task = customer.NewContactTask(begin, done, closedOn);
        dialog.Form.Fill(task);
        var number = dialog.Form.Number.Text;
        dialog.Save();
        mainPage.WaitForOpenedTask(number);
        return task.Name;
    }

    /// <summary>Двогодинний термін у день <paramref name="day"/> під контактом <paramref name="parentNode"/>.</summary>
    private ContactAppointment CreateContactAppointment(MainPage mainPage, CustomerCase customer, string parentNode, DateTime day)
    {
        var dialog = mainPage.OpenNewChildAppointmentDialog(parentNode);
        OnCleanup(dialog.CancelIfOpen);
        var random = customer.NewAppointment();
        var begin = day.Date + random.Begin.TimeOfDay;
        var appointment = random with { Begin = begin, End = begin + (random.End - random.Begin) };
        dialog.Form.Fill(appointment);
        var number = dialog.Form.Number.Text;
        dialog.Save();
        mainPage.WaitForOpenedAppointment(number);
        return new ContactAppointment(AppointmentNode(appointment), appointment.Begin);
    }

    /// <summary>
    /// Ставить налаштування пропозицій для листів у колонці «Anwender» (і, якщо задано <paramref name="systemTasks"/>,
    /// «Aufgaben vorschlagen» у колонці «Systemweit» — до користувацьких). Уперше запам'ятовує обидві колонки: після тесту
    /// прибирання повертає системний прапорець, а «Anwender» — власне значення користувача або, якщо він збігався із
    /// системним, знову успадкування від «Systemweit».
    /// </summary>
    private void UseMailSuggestions(MainPage mainPage, MailSuggestionOptions options, bool? systemTasks = null)
    {
        var window = mainPage.OpenOptions();
        OnCleanup(window.CloseIfOpen);
        window.OpenMailSuggestions();
        if (!_suggestionsSaved)
        {
            var system = window.ReadMailSuggestions(user: false);
            var user = window.ReadMailSuggestions(user: true);
            _suggestionsSaved = true;
            OnCleanup(() => RestoreMailSuggestions(system, user));
        }

        if (systemTasks is { } enabled)
            window.SetSystemTaskSuggestions(enabled);
        window.SetMailSuggestions(options);
        window.SaveChanges();
    }

    private void RestoreMailSuggestions(MailSuggestionOptions system, MailSuggestionOptions user)
    {
        _suggestionsSaved = false;
        var window = new MainPage(Session).OpenOptions();
        window.OpenMailSuggestions();
        window.SetSystemTaskSuggestions(system.Tasks.Enabled);
        window.RestoreUserMailSuggestions(system, user);
        window.SaveChanges();
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

    /// <summary>
    /// Перевіряє записи під «ROOT EMAIL» на вкладці «Dokumentation» і повертає їх у порядку рядків. Якщо список ще не
    /// такий, читає його ще раз за кілька секунд: ADAICA додає й прибирає пропозиції не одразу.
    /// </summary>
    private static IReadOnlyList<string> SuggestionsShouldBe(MailSaveDialog saveDialog, params string[] expected) =>
        AllureApi.Step($"Перевірити пропозиції на вкладці «Dokumentation»: {(expected.Length == 0 ? "немає" : string.Join("; ", expected))}", () =>
        {
            var rows = saveDialog.RecordRowLabels(FirstSuggestionRow);
            if (!rows.Order().SequenceEqual(expected.Order()))
            {
                Thread.Sleep(SuggestionsSettle);
                rows = saveDialog.RecordRowLabels(FirstSuggestionRow);
            }

            rows.ShouldBe(expected, ignoreOrder: true, "Записи під «ROOT EMAIL» на вкладці «Dokumentation»");
            return rows;
        });

    private static int SuggestionRow(MailSaveDialog saveDialog, string label) =>
        FirstSuggestionRow + saveDialog.RecordRowLabels(FirstSuggestionRow).ToList().IndexOf(label);

    /// <summary>Термін під контактом: підпис у дереві й на вкладці «Dokumentation» і початок.</summary>
    private sealed record ContactAppointment(string Label, DateTime Begin);
}
