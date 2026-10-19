using ADAICA_AUTOTEST.Infrastructure;
using ADAICA_AUTOTEST.Pages;
using Allure.Net.Commons;
using Allure.Net.Commons.Attributes;
using FlaUI.Core.Tools;

namespace ADAICA_AUTOTEST.Tests;

/// <summary>
/// Календар вузла «Aufgaben» під фірмою в навігаторі Kontakte (<see cref="RecordTaskCalendar"/>): кнопка «Kalender» над
/// списком показує завдання фірми на планувальнику, і з нього можна додати завдання — з меню клітинки, подвійним кліком,
/// кнопкою «+», з клітинки дня в Monatsansicht і копією «Duplizieren». Тест створює фірму й під нею (меню фірми «Neu»
/// → «Aufgabe») завдання на сьогодні 10:00–11:00, від смужки якого рахує клітинки. Нове завдання має належати фірмі:
/// вікно називається «Neue Aufgabe (übergeordnet: фірма)», а після збереження завдання є в списку вузла «Aufgaben»
/// фірми. Фірми й завдання лишаються в базі.
/// </summary>
public partial class ContactTests
{
    private const string TasksFolder = "Aufgaben";

    /// <summary>
    /// «Kalender» над списком «Aufgaben» фірми: на місці списку — планувальник у Wochenansicht на поточному тижні зі
    /// смужкою завдання фірми, лічильник «1/1», кнопки стану без «Ungelesen», «Akzeptieren», «Angenommen» і «Abgelehnt»
    /// (усі вимкнені), у «Kontakt» позначено лише користувача. Додати завдання можна з меню клітинки («Neu» → «Neue
    /// Aufgabe», «Einfügen als Duplikat» недоступне, поки нічого не скопійовано) і кнопкою «+». «Listenansicht» повертає
    /// список.
    /// </summary>
    [Test]
    [AllureSeverity(SeverityLevel.critical)]
    public void KontakteNavigator_CompanyTaskCalendar()
    {
        var mainPage = LogInAndOpenNavigator("Kontakte");
        var (company, task, number) = CreateCompanyWithCalendarTask(mainPage, CustomerCase.Random());

        var list = mainPage.OpenRecordFolder(company.Name, TasksFolder);
        ShouldListTasks(list, number);
        var calendar = list.OpenTaskCalendar();
        AllureApi.Step("Перевірити календар: Wochenansicht на поточному тижні, смужка завдання фірми, лічильник, кнопки стану", () =>
            calendar.ShouldSatisfyAllConditions(
                () => calendar.View.ShouldBe(CalendarView.Week, "Вид"),
                () => ShouldSpan(calendar.Period, 7, DateTime.Today),
                () => calendar.Bars.ShouldBe(new[] { Row(task) }, ignoreOrder: false, "Смужки тижня"),
                () => calendar.Count.ShouldBe(new CalendarCount(1, 1), "Лічильник"),
                () => calendar.StateFilterLabels.ShouldBe(RecordTaskCalendar.StateFilters.Select(filter => filter.Label()), ignoreOrder: false, "Кнопки стану"),
                () => calendar.Filters.Where(pair => pair.Value.IsOn).Select(pair => pair.Key.Label()).ShouldBeEmpty("Увімкнені кнопки стану")));

        var contacts = calendar.OpenContacts();
        AllureApi.Step($"Перевірити, що в «Kontakt» позначено лише «{mainPage.UserDisplayName}»", () =>
            contacts.Items.Where(item => item.IsChecked == true).Select(item => item.Name)
                .ShouldBe(new[] { mainPage.UserDisplayName }, ignoreOrder: false, "Позначені учасники"));
        contacts.Close();

        var menu = calendar.OpenCellMenu();
        menu.Items.ShouldBe(
            new CalendarMenuItem[] { new("Neu", true), new("Einfügen als Duplikat", false), new("Heute", true), new("Gehe zu Datum...", true), new("Ansichtwechsel", true) },
            ignoreOrder: false,
            "Меню клітинки");
        menu.Submenu("Neu").Take(2).ShouldBe(
            new CalendarMenuItem[] { new("Neue Aufgabe", true), new("Vorlage erstellen/bearbeiten", true) },
            ignoreOrder: false,
            "Підменю «Neu»");
        menu.Close();
        calendar.AddButtonMenuItems().ShouldBe(new[] { "Neue Aufgabe", "Vorlage erstellen/bearbeiten" }, ignoreOrder: false, "Меню кнопки «+»");

        ShouldListTasks(calendar.ShowList(), number);
    }

    /// <summary>
    /// Меню порожньої клітинки на пів години нижче за смужку завдання фірми → «Neu» → «Neue Aufgabe»: вікно нового
    /// завдання фірми з датами клітинки (11:30–12:00). Збережене завдання з'являється в календарі, а в списку
    /// «Aufgaben» фірми — обидва завдання.
    /// </summary>
    [Test]
    [AllureSeverity(SeverityLevel.critical)]
    public void KontakteNavigator_CompanyTaskCalendarNewTaskFromCell()
    {
        var mainPage = LogInAndOpenNavigator("Kontakte");
        var customer = CustomerCase.Random();
        var (company, task, number) = CreateCompanyWithCalendarTask(mainPage, customer);
        var calendar = mainPage.OpenRecordFolder(company.Name, TasksFolder).OpenTaskCalendar();

        var cell = CellBelow(task);
        var dialog = calendar.NewTaskFromCell(cell);
        OnCleanup(dialog.CancelIfOpen);
        var (newTask, newNumber) = SaveCompanyTask(dialog, customer, company, cell.Start, cell.Start.AddMinutes(30));

        ShouldShowNewTask(calendar, newTask, total: 2);
        ShouldListTasks(ReopenTasksFolder(mainPage, company), number, newNumber);
    }

    /// <summary>
    /// Подвійний клік по порожній клітинці на пів години нижче за смужку завдання фірми: те саме вікно нового завдання
    /// фірми з датами клітинки (11:30–12:00), що й з меню клітинки. Збережене завдання з'являється в календарі й у списку
    /// «Aufgaben» фірми.
    /// </summary>
    [Test]
    [AllureSeverity(SeverityLevel.critical)]
    public void KontakteNavigator_CompanyTaskCalendarNewTaskByDoubleClick()
    {
        var mainPage = LogInAndOpenNavigator("Kontakte");
        var customer = CustomerCase.Random();
        var (company, task, number) = CreateCompanyWithCalendarTask(mainPage, customer);
        var calendar = mainPage.OpenRecordFolder(company.Name, TasksFolder).OpenTaskCalendar();

        var cell = CellBelow(task);
        var dialog = calendar.NewTaskByDoubleClick(cell);
        OnCleanup(dialog.CancelIfOpen);
        var (newTask, newNumber) = SaveCompanyTask(dialog, customer, company, cell.Start, cell.Start.AddMinutes(30));

        ShouldShowNewTask(calendar, newTask, total: 2);
        ShouldListTasks(ReopenTasksFolder(mainPage, company), number, newNumber);
    }

    /// <summary>
    /// Кнопка «+» → «Neue Aufgabe»: доки в планувальнику не виділено клітинку, ADAICA бере дати з півгодини, у яку
    /// відкрито вікно. Вікно — нового завдання фірми; збережене завдання з'являється в календарі й у списку «Aufgaben»
    /// фірми.
    /// </summary>
    [Test]
    [AllureSeverity(SeverityLevel.critical)]
    public void KontakteNavigator_CompanyTaskCalendarNewTaskFromAddButton()
    {
        var mainPage = LogInAndOpenNavigator("Kontakte");
        var customer = CustomerCase.Random();
        var (company, _, number) = CreateCompanyWithCalendarTask(mainPage, customer);
        var calendar = mainPage.OpenRecordFolder(company.Name, TasksFolder).OpenTaskCalendar();

        var openedFrom = DateTime.Now;
        var dialog = calendar.NewTaskFromAddButton();
        OnCleanup(dialog.CancelIfOpen);
        var openedTo = DateTime.Now;
        var begin = AllureApi.Step("Перевірити, що «Beginnt am» — початок півгодини, у яку відкрито вікно", () =>
        {
            var value = dialog.Form.Begin.Value;
            value.ShouldNotBeNull("Beginnt am");
            value.Value.ShouldBeOneOf(HalfHourStart(openedFrom), HalfHourStart(openedTo));
            return value.Value;
        });
        var (newTask, newNumber) = SaveCompanyTask(dialog, customer, company, begin, begin.AddMinutes(30));

        ShouldShowNewTask(calendar, newTask, total: 2);
        ShouldListTasks(ReopenTasksFolder(mainPage, company), number, newNumber);
    }

    /// <summary>
    /// Monatsansicht: клітинка — цілий день. Меню клітинки дня завдання фірми (під його смужкою) → «Neu» → «Neue
    /// Aufgabe»: «Beginnt am» — сьогодні 00:00, «Fällig am» — сьогодні 23:59. Збережене завдання з'являється в календарі
    /// й у списку «Aufgaben» фірми.
    /// </summary>
    [Test]
    [AllureSeverity(SeverityLevel.normal)]
    public void KontakteNavigator_CompanyTaskCalendarNewTaskInMonthView()
    {
        var mainPage = LogInAndOpenNavigator("Kontakte");
        var customer = CustomerCase.Random();
        var (company, task, number) = CreateCompanyWithCalendarTask(mainPage, customer);
        var calendar = mainPage.OpenRecordFolder(company.Name, TasksFolder).OpenTaskCalendar();

        calendar.SwitchView(CalendarView.Month);
        var dialog = calendar.NewTaskInDayOf(Row(task));
        OnCleanup(dialog.CancelIfOpen);
        var (newTask, newNumber) = SaveCompanyTask(dialog, customer, company, DateTime.Today, DateTime.Today.AddHours(23).AddMinutes(59));

        ShouldShowNewTask(calendar, newTask, total: 2);
        ShouldListTasks(ReopenTasksFolder(mainPage, company), number, newNumber);
    }

    /// <summary>
    /// Меню смужки завдання фірми → «Duplizieren», потім меню клітинки на пів години нижче → «Einfügen als Duplikat» і OK у
    /// «Festlegungen für das Duplikat»: у клітинці — копія з тією самою назвою, іншим номером і датами клітинки. Копія
    /// теж належить фірмі: у списку «Aufgaben» фірми — обидва завдання.
    /// </summary>
    [Test]
    [AllureSeverity(SeverityLevel.critical)]
    public void KontakteNavigator_CompanyTaskCalendarDuplicate()
    {
        var mainPage = LogInAndOpenNavigator("Kontakte");
        var (company, task, number) = CreateCompanyWithCalendarTask(mainPage, CustomerCase.Random());
        var calendar = mainPage.OpenRecordFolder(company.Name, TasksFolder).OpenTaskCalendar();
        var row = Row(task);

        OnCleanup(calendar.CancelPendingPaste);
        calendar.Duplicate(row);
        var cell = CellBelow(task);
        var options = calendar.PasteDuplicate(cell);
        OnCleanup(options.CancelIfOpen);
        options.Confirm();
        AllureApi.Step("Дочекатися копії в календарі: дві смужки завдання, лічильник «2/2»", () =>
            Wait.Until(() => calendar.Bars.Count(bar => bar == row) == 2 && calendar.Count == new CalendarCount(2, 2), "двох смужок завдання"));

        // Смужки впорядковано згори вниз: оригінал о 10:00, копія — о 11:30.
        calendar.SelectTask(row, index: 1);
        var copy = calendar.Details;
        var copyNumber = copy.Number.Text;
        AllureApi.Step("Перевірити бланк копії", () => copy.ShouldSatisfyAllConditions(
            () => copy.Name.ShouldHaveText(task.Name),
            () => copyNumber.ShouldNotBe(number, "Номер копії"),
            () => copy.Begin.Value.ShouldBe(cell.Start, "Beginnt am"),
            () => copy.Due.Value.ShouldBe(cell.Start.AddMinutes(30), "Fällig am")));

        ShouldListTasks(ReopenTasksFolder(mainPage, company), number, copyNumber);
    }

    /// <summary>
    /// Завдання, додане в календарі, має бути й у списку, коли «Listenansicht» повертає його замість календаря. Насправді
    /// список лишається таким, яким був до календаря (лічильник «1», без нового завдання), доки вузол «Aufgaben» не
    /// вибрати знову. Імовірний баг ADAICA, тест падає навмисно (README, «Тести, що падають навмисно»).
    /// </summary>
    [Test]
    [AllureSeverity(SeverityLevel.normal)]
    public void KontakteNavigator_CompanyTaskCalendarListShowsNewTask()
    {
        var mainPage = LogInAndOpenNavigator("Kontakte");
        var customer = CustomerCase.Random();
        var (company, task, number) = CreateCompanyWithCalendarTask(mainPage, customer);
        var calendar = mainPage.OpenRecordFolder(company.Name, TasksFolder).OpenTaskCalendar();

        var cell = CellBelow(task);
        var dialog = calendar.NewTaskFromCell(cell);
        OnCleanup(dialog.CancelIfOpen);
        var (newTask, newNumber) = SaveCompanyTask(dialog, customer, company, cell.Start, cell.Start.AddMinutes(30));
        ShouldShowNewTask(calendar, newTask, total: 2);

        ShouldListTasks(calendar.ShowList(), number, newNumber);
    }

    /// <summary>
    /// Створює фірму, а під нею з меню фірми («Neu» → «Aufgabe») завдання для календаря: сьогодні 10:00–11:00, з номером
    /// у примітці (<see cref="CustomerCase.NewCalendarTask"/>). Після збереження ADAICA відкриває завдання в області
    /// перегляду.
    /// </summary>
    private (CompanyData Company, DashboardTaskData Task, string Number) CreateCompanyWithCalendarTask(MainPage mainPage, CustomerCase customer)
    {
        var company = customer.NewCompany();
        CreateCompany(mainPage, company);

        var dialog = mainPage.OpenNewChildTaskDialog(company.Name);
        OnCleanup(dialog.CancelIfOpen);
        var number = dialog.Form.Number.Text;
        var task = customer.NewCalendarTask(number);
        dialog.Form.Fill(task);
        dialog.Save();
        mainPage.WaitForOpenedTask(number);
        return (company, task, number);
    }

    /// <summary>
    /// Перевіряє вікно нового завдання з календаря фірми (заголовок називає фірму батьком, дати — з клітинки чи кнопки),
    /// вписує свої назву, Kurzname, Annahmeart і примітку з номером завдання й зберігає завдання. Назву ADAICA може
    /// заповнити назвою фірми (README, «Name des übergeordneten Elements vorausfüllen»), тож тест її перезаписує.
    /// </summary>
    private static (DashboardTaskData Task, string Number) SaveCompanyTask(
        TaskDialog dialog, CustomerCase customer, CompanyData company, DateTime begin, DateTime due)
    {
        AllureApi.Step($"Перевірити вікно нового завдання фірми «{company.Name}»", () =>
            dialog.Title.ShouldBe($"Neue Aufgabe (übergeordnet: {company.Name})", "Заголовок вікна"));
        dialog.Form.ShouldHaveDates(begin, due);

        var number = dialog.Form.Number.Text;
        var task = customer.NewCalendarTask(number) with { Begin = begin, Due = due };
        dialog.Form.Fill(task);
        dialog.Save();
        return (task, number);
    }

    /// <summary>
    /// Після збереження календар сам довантажує нове завдання: лічильник показує <paramref name="total"/> завдань фірми,
    /// і смужку нового видно.
    /// </summary>
    private static void ShouldShowNewTask(RecordTaskCalendar calendar, DashboardTaskData task, int total) =>
        AllureApi.Step($"Перевірити, що календар показує нове завдання «{Row(task)}», а завдань фірми — {total}", () =>
        {
            Wait.Until(() => calendar.Count.Total == total, $"{total} завдань фірми в лічильнику календаря");
            ShouldBeFound(calendar, Row(task), true, "нове завдання");
        });

    /// <summary>
    /// Вибирає фірму, а потім її вузол «Aufgaben» заново: так ADAICA перечитує список завдань фірми (після календаря
    /// «Listenansicht» показує той список, що був до нього).
    /// </summary>
    private static RecordList ReopenTasksFolder(MainPage mainPage, CompanyData company)
    {
        mainPage.SelectNavigatorNode(company.Name);
        return mainPage.OpenRecordFolder(company.Name, TasksFolder);
    }

    /// <summary>У списку «Aufgaben» — саме завдання з номерами <paramref name="numbers"/>.</summary>
    private static void ShouldListTasks(RecordList list, params string[] numbers) =>
        AllureApi.Step($"Перевірити, що в списку «Aufgaben» завдання {string.Join(", ", numbers)}", () =>
        {
            IEnumerable<string> Listed() => list.Rows.Select(row => row.GetValueOrDefault("Nummer") ?? string.Empty);
            Retry.WhileFalse(() => Listed().ToHashSet().SetEquals(numbers), TestSettings.DefaultTimeout, ignoreException: true);
            Listed().ShouldBe(numbers, ignoreOrder: true, "Номери завдань у списку «Aufgaben»");
        });
}
