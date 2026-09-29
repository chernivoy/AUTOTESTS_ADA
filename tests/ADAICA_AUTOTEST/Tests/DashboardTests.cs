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
