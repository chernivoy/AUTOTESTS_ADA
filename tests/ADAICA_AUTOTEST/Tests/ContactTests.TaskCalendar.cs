using ADAICA_AUTOTEST.Infrastructure;
using ADAICA_AUTOTEST.Pages;
using Allure.Net.Commons;
using Allure.Net.Commons.Attributes;
using FlaUI.Core.Tools;
using static ADAICA_AUTOTEST.Tests.ContactTests.TaskContact;

namespace ADAICA_AUTOTEST.Tests;

/// <summary>
/// Календар вузла «Aufgaben» під контактом у навігаторі Kontakte — фірмою, окремою персоною чи співробітником відділу
/// фірми (<see cref="RecordTaskCalendar"/>): кнопка «Kalender» над списком показує завдання контакту на планувальнику,
/// і з нього можна додати завдання — з меню клітинки, подвійним кліком, кнопкою «+», з клітинки дня в Monatsansicht і
/// копією «Duplizieren». Тест створює контакт
/// і під ним (меню контакту «Neu» → «Aufgabe») завдання на сьогодні 10:00–11:00, від смужки якого рахує клітинки. Нове
/// завдання має належати контакту: вікно називається «Neue Aufgabe (übergeordnet: контакт)», а після збереження
/// завдання є в списку вузла «Aufgaben» контакту. Контакти й завдання лишаються в базі.
/// </summary>
public partial class ContactTests
{
    private const string TasksFolder = "Aufgaben";
    private const string CurrentNode = "Aktuelle";

    /// <summary>Контакт, під яким тест створює завдання для календаря.</summary>
    public enum TaskContact
    {
        /// <summary>Фірма з унікальною назвою (<see cref="CustomerCase.NewCompany"/>); вузол — назва фірми.</summary>
        Company,

        /// <summary>Окрема персона без фірми з унікальним e-mail; вузол — «Nachname, Vorname».</summary>
        Person,

        /// <summary>
        /// Співробітник нового відділу нової фірми; вузол — «Nachname, Vorname (Mitarbeiter)» під «Фірма → Mitarbeiter →
        /// Відділ», батько нового завдання — «Nachname, Vorname # Firma».
        /// </summary>
        Employee,
    }

    /// <summary>
    /// Вузол контакту в дереві: шлях від «Aktuelle» (<paramref name="Path"/>, останній — сам контакт) і як ADAICA називає
    /// контакт батьком нового завдання (<paramref name="ParentName"/>).
    /// </summary>
    private sealed record ContactNode(IReadOnlyList<string> Path, string ParentName)
    {
        public string Name => Path[^1];
    }

    /// <summary>
    /// «Kalender» над списком «Aufgaben» контакту: на місці списку — планувальник у Wochenansicht на поточному тижні зі
    /// смужкою завдання контакту, лічильник «1/1», кнопки стану без «Ungelesen», «Akzeptieren», «Angenommen» і «Abgelehnt»
    /// (усі вимкнені), у «Kontakt» позначено лише користувача. Додати завдання можна з меню клітинки («Neu» → «Neue
    /// Aufgabe», «Einfügen als Duplikat» недоступне, поки нічого не скопійовано) і кнопкою «+». «Listenansicht» повертає
    /// список.
    /// </summary>
    [TestCase(Company)]
    [TestCase(Person)]
    [TestCase(Employee)]
    [AllureSeverity(SeverityLevel.critical)]
    public void KontakteNavigator_TaskCalendar(TaskContact contact)
    {
        var mainPage = LogInAndOpenNavigator("Kontakte");
        var (node, task, number) = CreateContactWithCalendarTask(mainPage, CustomerCase.Random(), contact);

        var list = mainPage.OpenRecordFolder(node.Name, TasksFolder);
        ShouldListTasks(list, number);
        var calendar = list.OpenTaskCalendar();
        AllureApi.Step("Перевірити календар: Wochenansicht на поточному тижні, смужка завдання контакту, лічильник, кнопки стану", () =>
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
        // Як і в підменю «Neu», далі йдуть шаблони завдань, якщо вони є.
        calendar.AddButtonMenuItems().Take(2).ShouldBe(new[] { "Neue Aufgabe", "Vorlage erstellen/bearbeiten" }, ignoreOrder: false, "Меню кнопки «+»");

        ShouldListTasks(calendar.ShowList(), number);
    }

    /// <summary>
    /// Меню порожньої клітинки на пів години нижче за смужку завдання контакту → «Neu» → «Neue Aufgabe»: вікно нового
    /// завдання контакту з датами клітинки (11:30–12:00). Збережене завдання з'являється в календарі, а в списку
    /// «Aufgaben» контакту — обидва завдання.
    /// </summary>
    [TestCase(Company)]
    [TestCase(Person)]
    [TestCase(Employee)]
    [AllureSeverity(SeverityLevel.critical)]
    public void KontakteNavigator_TaskCalendarNewTaskFromCell(TaskContact contact)
    {
        var mainPage = LogInAndOpenNavigator("Kontakte");
        var customer = CustomerCase.Random();
        var (node, task, number) = CreateContactWithCalendarTask(mainPage, customer, contact);
        var calendar = mainPage.OpenRecordFolder(node.Name, TasksFolder).OpenTaskCalendar();

        var cell = CellBelow(task);
        var dialog = calendar.NewTaskFromCell(cell);
        OnCleanup(dialog.CancelIfOpen);
        var (newTask, newNumber) = SaveContactTask(dialog, customer, node, cell.Start, cell.Start.AddMinutes(30));

        ShouldShowNewTask(calendar, newTask, total: 2);
        ShouldListTasks(ReopenTasksFolder(mainPage, node), number, newNumber);
    }

    /// <summary>
    /// Подвійний клік по порожній клітинці на пів години нижче за смужку завдання контакту: те саме вікно нового завдання
    /// контакту з датами клітинки (11:30–12:00), що й з меню клітинки. Збережене завдання з'являється в календарі й у
    /// списку «Aufgaben» контакту.
    /// </summary>
    [TestCase(Company)]
    [TestCase(Person)]
    [TestCase(Employee)]
    [AllureSeverity(SeverityLevel.critical)]
    public void KontakteNavigator_TaskCalendarNewTaskByDoubleClick(TaskContact contact)
    {
        var mainPage = LogInAndOpenNavigator("Kontakte");
        var customer = CustomerCase.Random();
        var (node, task, number) = CreateContactWithCalendarTask(mainPage, customer, contact);
        var calendar = mainPage.OpenRecordFolder(node.Name, TasksFolder).OpenTaskCalendar();

        var cell = CellBelow(task);
        var dialog = calendar.NewTaskByDoubleClick(cell);
        OnCleanup(dialog.CancelIfOpen);
        var (newTask, newNumber) = SaveContactTask(dialog, customer, node, cell.Start, cell.Start.AddMinutes(30));

        ShouldShowNewTask(calendar, newTask, total: 2);
        ShouldListTasks(ReopenTasksFolder(mainPage, node), number, newNumber);
    }

    /// <summary>
    /// Кнопка «+» → «Neue Aufgabe»: доки в планувальнику не виділено клітинку, ADAICA бере дати з півгодини, у яку
    /// відкрито вікно. Вікно — нового завдання контакту; збережене завдання з'являється в календарі й у списку «Aufgaben»
    /// контакту.
    /// </summary>
    [TestCase(Company)]
    [TestCase(Person)]
    [TestCase(Employee)]
    [AllureSeverity(SeverityLevel.critical)]
    public void KontakteNavigator_TaskCalendarNewTaskFromAddButton(TaskContact contact)
    {
        var mainPage = LogInAndOpenNavigator("Kontakte");
        var customer = CustomerCase.Random();
        var (node, _, number) = CreateContactWithCalendarTask(mainPage, customer, contact);
        var calendar = mainPage.OpenRecordFolder(node.Name, TasksFolder).OpenTaskCalendar();

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
        var (newTask, newNumber) = SaveContactTask(dialog, customer, node, begin, begin.AddMinutes(30));

        ShouldShowNewTask(calendar, newTask, total: 2);
        ShouldListTasks(ReopenTasksFolder(mainPage, node), number, newNumber);
    }

    /// <summary>
    /// Monatsansicht: клітинка — цілий день. Меню клітинки дня завдання контакту (під його смужкою) → «Neu» → «Neue
    /// Aufgabe»: «Beginnt am» — сьогодні 00:00, «Fällig am» — сьогодні 23:59. Збережене завдання з'являється в календарі
    /// й у списку «Aufgaben» контакту.
    /// </summary>
    [TestCase(Company)]
    [TestCase(Person)]
    [TestCase(Employee)]
    [AllureSeverity(SeverityLevel.normal)]
    public void KontakteNavigator_TaskCalendarNewTaskInMonthView(TaskContact contact)
    {
        var mainPage = LogInAndOpenNavigator("Kontakte");
        var customer = CustomerCase.Random();
        var (node, task, number) = CreateContactWithCalendarTask(mainPage, customer, contact);
        var calendar = mainPage.OpenRecordFolder(node.Name, TasksFolder).OpenTaskCalendar();

        calendar.SwitchView(CalendarView.Month);
        var dialog = calendar.NewTaskInDayOf(Row(task));
        OnCleanup(dialog.CancelIfOpen);
        var (newTask, newNumber) = SaveContactTask(dialog, customer, node, DateTime.Today, DateTime.Today.AddHours(23).AddMinutes(59));

        ShouldShowNewTask(calendar, newTask, total: 2);
        ShouldListTasks(ReopenTasksFolder(mainPage, node), number, newNumber);
    }

    /// <summary>
    /// Меню смужки завдання контакту → «Duplizieren», потім меню клітинки на пів години нижче → «Einfügen als Duplikat» і
    /// OK у «Festlegungen für das Duplikat»: у клітинці — копія з тією самою назвою, іншим номером і датами клітинки.
    /// Копія теж належить контакту: у списку «Aufgaben» контакту — обидва завдання.
    /// </summary>
    [TestCase(Company)]
    [TestCase(Person)]
    [TestCase(Employee)]
    [AllureSeverity(SeverityLevel.critical)]
    public void KontakteNavigator_TaskCalendarDuplicate(TaskContact contact)
    {
        var mainPage = LogInAndOpenNavigator("Kontakte");
        var (node, task, number) = CreateContactWithCalendarTask(mainPage, CustomerCase.Random(), contact);
        var calendar = mainPage.OpenRecordFolder(node.Name, TasksFolder).OpenTaskCalendar();
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

        ShouldListTasks(ReopenTasksFolder(mainPage, node), number, copyNumber);
    }

    /// <summary>
    /// Завдання, додане в календарі, має бути й у списку, коли «Listenansicht» повертає його замість календаря. Насправді
    /// список лишається таким, яким був до календаря (лічильник «1», без нового завдання), доки вузол «Aufgaben» не
    /// вибрати знову. Імовірний баг ADAICA, тест падає навмисно (README, «Тести, що падають навмисно»).
    /// </summary>
    [TestCase(Company)]
    [TestCase(Person)]
    [TestCase(Employee)]
    [AllureSeverity(SeverityLevel.normal)]
    public void KontakteNavigator_TaskCalendarListShowsNewTask(TaskContact contact)
    {
        var mainPage = LogInAndOpenNavigator("Kontakte");
        var customer = CustomerCase.Random();
        var (node, task, number) = CreateContactWithCalendarTask(mainPage, customer, contact);
        var calendar = mainPage.OpenRecordFolder(node.Name, TasksFolder).OpenTaskCalendar();

        var cell = CellBelow(task);
        var dialog = calendar.NewTaskFromCell(cell);
        OnCleanup(dialog.CancelIfOpen);
        var (newTask, newNumber) = SaveContactTask(dialog, customer, node, cell.Start, cell.Start.AddMinutes(30));
        ShouldShowNewTask(calendar, newTask, total: 2);

        ShouldListTasks(calendar.ShowList(), number, newNumber);
    }

    /// <summary>
    /// Створює контакт <paramref name="contact"/> — фірму (<see cref="CreateCompany"/>) чи окрему персону з унікальним
    /// e-mail (<see cref="AdaicaTestBase.CreatePersonWithEmail"/>) — а під ним з меню контакту («Neu» → «Aufgabe»)
    /// завдання для календаря: сьогодні 10:00–11:00, з номером у примітці (<see cref="CustomerCase.NewCalendarTask"/>).
    /// Співробітника — у новому відділі нової фірми (<see cref="CreateDepartmentWithEmployee"/>). Повертає вузол контакту
    /// в дереві. Після збереження ADAICA відкриває завдання в області перегляду.
    /// </summary>
    private (ContactNode Node, DashboardTaskData Task, string Number) CreateContactWithCalendarTask(
        MainPage mainPage, CustomerCase customer, TaskContact contact)
    {
        ContactNode node;
        switch (contact)
        {
            case Company:
                var company = customer.NewCompany();
                CreateCompany(mainPage, company);
                node = new([CurrentNode, company.Name], company.Name);
                break;
            case Person:
                var person = customer.NewMailPerson();
                CreatePersonWithEmail(mainPage, person, CustomerCase.NewMailAddress(person));
                node = new([CurrentNode, person.SortName], person.SortName);
                break;
            default:
                var employer = customer.NewCompany();
                var department = customer.NewDepartment();
                var employee = customer.NewEmployee();
                CreateCompany(mainPage, employer);
                CreateDepartmentWithEmployee(mainPage, employer.Name, department, employee);
                // Батьком нового запису ADAICA називає співробітника разом із фірмою: «Nachname, Vorname # Firma».
                node = new([CurrentNode, employer.Name, EmployeesNode, department, employee.NodeName], $"{employee.SortName} # {employer.Name}");
                break;
        }

        var dialog = mainPage.OpenNewChildTaskDialog(node.Name);
        OnCleanup(dialog.CancelIfOpen);
        var number = dialog.Form.Number.Text;
        var task = customer.NewCalendarTask(number);
        dialog.Form.Fill(task);
        dialog.Save();
        mainPage.WaitForOpenedTask(number);
        return (node, task, number);
    }

    /// <summary>
    /// Перевіряє вікно нового завдання з календаря контакту (заголовок називає контакт батьком, дати — з клітинки чи
    /// кнопки), вписує свої назву, Kurzname, Annahmeart і примітку з номером завдання й зберігає завдання. Назву ADAICA
    /// може заповнити назвою контакту (README, «Name des übergeordneten Elements vorausfüllen»), тож тест її перезаписує.
    /// </summary>
    private static (DashboardTaskData Task, string Number) SaveContactTask(
        TaskDialog dialog, CustomerCase customer, ContactNode node, DateTime begin, DateTime due)
    {
        AllureApi.Step($"Перевірити вікно нового завдання контакту «{node.ParentName}»", () =>
            dialog.Title.ShouldBe($"Neue Aufgabe (übergeordnet: {node.ParentName})", "Заголовок вікна"));
        dialog.Form.ShouldHaveDates(begin, due);

        var number = dialog.Form.Number.Text;
        var task = customer.NewCalendarTask(number) with { Begin = begin, Due = due };
        dialog.Form.Fill(task);
        dialog.Save();
        return (task, number);
    }

    /// <summary>
    /// Після збереження календар сам довантажує нове завдання: лічильник показує <paramref name="total"/> завдань
    /// контакту, і смужку нового видно.
    /// </summary>
    private static void ShouldShowNewTask(RecordTaskCalendar calendar, DashboardTaskData task, int total) =>
        AllureApi.Step($"Перевірити, що календар показує нове завдання «{Row(task)}», а завдань контакту — {total}", () =>
        {
            Wait.Until(() => calendar.Count.Total == total, $"{total} завдань контакту в лічильнику календаря");
            ShouldBeFound(calendar, Row(task), true, "нове завдання");
        });

    /// <summary>
    /// Вибирає контакт у «Aktuelle», а потім його вузол «Aufgaben» заново: так ADAICA перечитує список завдань контакту
    /// (після календаря «Listenansicht» показує той список, що був до нього).
    /// </summary>
    private static RecordList ReopenTasksFolder(MainPage mainPage, ContactNode node)
    {
        mainPage.SelectNavigatorNodeAt([.. node.Path]);
        return mainPage.OpenRecordFolder(node.Name, TasksFolder);
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
