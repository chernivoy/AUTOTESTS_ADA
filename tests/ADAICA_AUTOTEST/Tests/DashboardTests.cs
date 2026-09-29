using ADAICA_AUTOTEST.Controls;
using ADAICA_AUTOTEST.Pages;
using Allure.Net.Commons;
using Allure.Net.Commons.Attributes;
using static ADAICA_AUTOTEST.Pages.TaskListFilter;

namespace ADAICA_AUTOTEST.Tests;

/// <summary>
/// Список «Meine Aufgaben» на вузлі користувача в навігаторі Dashboard. Завдання створюються кнопкою «+» над списком
/// і лишаються в базі. Назви можуть повторитися в іншому прогоні, тож своє завдання тест знаходить швидким пошуком
/// за номером.
/// </summary>
[AllureSuite("Навігатори")]
public class DashboardTests : AdaicaTestBase
{
    // Під якими фільтрами видно завдання в кожному стані. Кожне завдання потрапляє рівно під один фільтр з кожної
    // групи: прийняття, виконання, закриття, «Aktuell/Geplant»; «Überfällig» — лише прострочене.
    private static readonly TaskListFilter[] AcceptedTask = [Accepted, Undone, Open, Actual];
    private static readonly TaskListFilter[] WaitingTask = [Waiting, Undone, Open, Actual];
    private static readonly TaskListFilter[] RefusedTask = [Refused, Undone, Open, Actual];
    private static readonly TaskListFilter[] DoneTask = [Accepted, Done, Open, Actual];
    private static readonly TaskListFilter[] ClosedTask = [Accepted, Undone, Closed, Actual];
    private static readonly TaskListFilter[] OverdueTask = [Accepted, Undone, Open, Overdue, Actual];

    // Решту завдань серії ADAICA створює непрочитаними, тож фільтри прийняття їх не показують.
    private static readonly TaskListFilter[] UnreadSeriesTask = [Unread, Undone, Open, Actual];
    private static readonly TaskListFilter[] PlannedSeriesTask = [Unread, Undone, Open, Planned];

    /// <summary>
    /// Створює в списку «Meine Aufgaben» (кнопка «+») завдання, яке має потрапити під фільтр <paramref name="filter"/>,
    /// і одразу перевіряє фільтри: вмикає їх по одному й дивиться, під якими видно завдання. Непрочитане й заплановане
    /// завдання — це друге й третє завдання щоденної серії, прострочене — з «Fällig am» учора, відхилене — через
    /// «Ablehnen» у контекстному меню, неприйняте — з Annahmeart «Erster».
    /// </summary>
    [TestCase(Unread)]
    [TestCase(Waiting)]
    [TestCase(Accepted)]
    [TestCase(Refused)]
    [TestCase(Done)]
    [TestCase(Undone)]
    [TestCase(Open)]
    [TestCase(Closed)]
    [TestCase(Overdue)]
    [TestCase(Actual)]
    [TestCase(Planned)]
    [AllureSeverity(SeverityLevel.critical)]
    public void DashboardNavigator_TaskListFilter(TaskListFilter filter)
    {
        var mainPage = LogInAndShowSingleNavigator("Dashboard");
        var taskList = mainPage.OpenUserTaskList();
        var customer = CustomerCase.Random();
        var yesterday = DateTime.Today.AddDays(-1);

        var (task, expected) = filter switch
        {
            Unread => (CreateDailySeries(taskList, customer, day: 1), UnreadSeriesTask),
            Planned => (CreateDailySeries(taskList, customer, day: 2), PlannedSeriesTask),
            Waiting => (CreateTask(taskList, customer.NewDashboardTask() with { AcceptanceType = "Erster" }), WaitingTask),
            Refused => (CreateRefusedTask(taskList, customer.NewDashboardTask()), RefusedTask),
            Done => (CreateTask(taskList, customer.NewDashboardTask() with { Done = yesterday.AddHours(16).AddMinutes(30) }), DoneTask),
            Closed => (CreateTask(taskList, customer.NewDashboardTask() with { ClosedOn = DateTime.Today }), ClosedTask),
            Overdue => (CreateTask(taskList, customer.NewDashboardTask() with { Due = yesterday.AddHours(17) }), OverdueTask),
            _ => (CreateTask(taskList, customer.NewDashboardTask()), AcceptedTask),
        };

        taskList.FiltersShowing(task).ShouldBe(
            expected,
            ignoreOrder: true,
            $"Фільтри, під якими видно «{task}» (очікували: {string.Join(", ", expected.Select(item => item.Label()))})");
    }

    /// <summary>
    /// Вмикає в «Spaltenkonfiguration» (контекстне меню заголовка колонки) всі колонки списку «Meine Aufgaben»,
    /// створює кнопкою «+» завдання, де заповнено все, що можна заповнити, і перевіряє його текст у кожній колонці.
    /// Klasse й Unterklasse лишаються порожніми: у BLANK_IGOR є лише «Blanko-Eintrag», і вибрати її не можна.
    /// Після тесту конфігурацію колонок повертає як була.
    /// </summary>
    [Test]
    [AllureSeverity(SeverityLevel.critical)]
    public void DashboardNavigator_TaskListAllColumns()
    {
        var mainPage = LogInAndShowSingleNavigator("Dashboard");
        var taskList = mainPage.OpenUserTaskList();

        var columnsDialog = taskList.OpenColumnsConfig();
        var columns = columnsDialog.Columns.Select(column => column.Name).ToList();
        var shownBefore = columnsDialog.Columns.Where(column => column.IsChecked).Select(column => column.Name).ToList();
        // Конфігурацію колонок ADAICA запам'ятовує для користувача.
        OnCleanup(() => taskList.OpenColumnsConfig().ShowOnly(shownBefore));
        columns.ShouldBe(DashboardTaskList.ColumnIds.Keys, ignoreOrder: true, "Колонки в «Spaltenkonfiguration»");
        columnsDialog.ShowAll();
        taskList.ColumnNames.ShouldBe(columns, ignoreOrder: true, "Колонки таблиці після «Spaltenkonfiguration»");

        var task = CustomerCase.Random().NewCurrentTask();
        var now = DateTime.Now;
        var openedAt = new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute, 0);
        var dialog = taskList.OpenNewTaskDialog();
        OnCleanup(dialog.CancelIfOpen);
        dialog.Form.Fill(task);
        var number = dialog.Form.Number.Text;
        dialog.Save();
        var savedAt = DateTime.Now;

        var row = new TaskListRow(task.Name, task.Begin.Date);
        taskList.ShowAll();
        taskList.Search(number, [row]);
        var cells = taskList.CellTexts(row);

        var user = taskList.UserName;
        var expected = new Dictionary<string, string>
        {
            // Прапорець і картинку стану UI Automation називає словами: «Angewählt» — виконано, «Done» — стан «erledigt».
            ["Erledigt"] = "Angewählt",
            ["Aktion"] = "Done",
            ["Name"] = task.Name,
            ["Beginnt am"] = AdaicaFormat.FormatDateTime(task.Begin),
            ["Erstellt durch"] = user,
            ["Kurzname"] = task.ShortName,
            // Довгу примітку список обрізає: перші 100 символів і «...».
            ["Bemerkung"] = task.Remark.Length > 100 ? $"{task.Remark[..100]}..." : task.Remark,
            ["% erledigt"] = AdaicaFormat.FormatNumber(task.PercentDone),
            ["Ablehnungsdatum"] = string.Empty,
            ["Annahmeart"] = task.AcceptanceType,
            ["Auftragsnummer"] = task.OrderNumber,
            ["Ausführende"] = user,
            ["Bestell-Nr."] = task.PurchaseOrderNumber,
            ["Dauer (gesch.)"] = $"{AdaicaFormat.FormatNumber(task.EstimatedDuration)} {task.EstimatedDurationUnit}",
            ["Dauer (real)"] = $"{AdaicaFormat.FormatNumber(task.ActualDuration)} {task.ActualDurationUnit}",
            ["Erledigt am"] = AdaicaFormat.FormatDateTime(task.Done),
            ["Fällig am"] = AdaicaFormat.FormatDateTime(task.Due),
            ["Geändert durch"] = user,
            // «Geschlossen am» — лише дата, але колонка показує її з часом.
            ["Geschlossen am"] = AdaicaFormat.FormatDateTime(task.ClosedOn.Date),
            ["Hinweis"] = task.Hint,
            ["Hinweis (Wert)"] = task.HintText,
            ["Klasse"] = string.Empty,
            ["Klassentyp"] = "TASK",
            ["Kundennutzen"] = task.CustomerBenefit,
            ["Kundennutzen (Wert)"] = task.CustomerBenefitText,
            ["Nummer"] = number,
            ["Priorität"] = task.Priority,
            ["Standort"] = task.Location,
            ["Standort (Wert)"] = task.LocationText,
            // Заповнені «Erledigt am» і «Geschlossen am» дають статус «erledigt».
            ["Status"] = "Aufgabe erledigt",
            ["Systembemerkung"] = string.Empty,
            ["Systemnotiz"] = string.Empty,
            ["Unterklasse"] = string.Empty,
            ["Verantwortlich"] = user,
            ["Wichtigkeit"] = task.Importance,
        };

        // Час створення, зміни й прийняття ADAICA ставить сама під час збереження.
        string[] savedTimeColumns = ["Erstelldatum", "Änderungsdatum", "Annahmedatum"];

        AllureApi.Step("Перевірити текст завдання в кожній колонці", () => cells.ShouldSatisfyAllConditions(
            expected.Select(pair => (Action)(() => cells.GetValueOrDefault(pair.Key).ShouldBe(pair.Value, $"Колонка «{pair.Key}»")))
                .Concat(savedTimeColumns.Select(column => (Action)(() => ShouldBeBetween(cells.GetValueOrDefault(column), column, openedAt, savedAt))))
                .Append(() => cells.GetValueOrDefault("ID").ShouldMatch(@"^\d{12}TASK\d{6}$", "Колонка «ID»"))
                .Append(() => cells.Keys.ShouldBe(columns, ignoreOrder: true, "Колонки, прочитані з рядка"))
                .ToArray()));
    }

    private static void ShouldBeBetween(string? text, string column, DateTime from, DateTime to)
    {
        var value = text is { Length: > 0 }
            ? DateTime.ParseExact(text, $"{AdaicaFormat.DatePattern} {AdaicaFormat.TimePattern}", System.Globalization.CultureInfo.InvariantCulture)
            : (DateTime?)null;
        value.ShouldNotBeNull($"Колонка «{column}» порожня");
        value.Value.ShouldBeInRange(from, to, $"Колонка «{column}»: {text}");
    }

    /// <summary>Створює завдання й звужує список швидким пошуком за його номером до цього завдання.</summary>
    private TaskListRow CreateTask(DashboardTaskList taskList, DashboardTaskData task)
    {
        var dialog = taskList.OpenNewTaskDialog();
        OnCleanup(dialog.CancelIfOpen);
        dialog.Form.Fill(task);
        var number = dialog.Form.Number.Text;
        dialog.Save();

        var row = new TaskListRow(task.Name, task.Begin.Date);
        taskList.ShowAll();
        taskList.Search(number, [row]);
        return row;
    }

    private TaskListRow CreateRefusedTask(DashboardTaskList taskList, DashboardTaskData task)
    {
        var row = CreateTask(taskList, task);
        taskList.Refuse(row);
        return row;
    }

    /// <summary>
    /// Створює щоденну серію з трьох завдань (сьогодні, завтра, післязавтра) і повертає завдання дня <paramref name="day"/>
    /// (0 — перше). Список звужує швидким пошуком за номером першого завдання, який є в примітці кожного завдання серії.
    /// </summary>
    private TaskListRow CreateDailySeries(DashboardTaskList taskList, CustomerCase customer, int day)
    {
        var dialog = taskList.OpenNewTaskDialog();
        OnCleanup(dialog.CancelIfOpen);
        var number = dialog.Form.Number.Text;
        var task = customer.NewDailyTask(number);
        dialog.Form.Fill(task);
        dialog.StartDailySeries(3);
        dialog.Save();

        var rows = Enumerable.Range(0, 3).Select(offset => new TaskListRow(task.Name, task.Begin.Date.AddDays(offset))).ToList();
        taskList.ShowAll();
        taskList.Search(number, rows);
        AllureApi.Step($"Перевіряти завдання серії за {rows[day].Begin:dd.MM.yyyy}");
        return rows[day];
    }
}
