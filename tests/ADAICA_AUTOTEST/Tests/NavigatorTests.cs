using System.Globalization;
using ADAICA_AUTOTEST.Infrastructure;
using ADAICA_AUTOTEST.Pages;
using Allure.Net.Commons;
using Allure.Net.Commons.Attributes;

namespace ADAICA_AUTOTEST.Tests;

/// <summary>
/// Створення записів через навігатор Dokumentation. Збережені записи лишаються в базі;
/// назви між прогонами повторюються, тому свій запис тест упізнає за номером.
/// </summary>
[AllureSuite("Навігатори")]
public class NavigatorTests : AdaicaTestBase
{
    private const string Dokumentation = "Dokumentation";

    [Test]
    [Category("Smoke")]
    [AllureSeverity(SeverityLevel.critical)]
    public void DokumentationNavigator_NewProject()
    {
        var mainPage = LogInAndOpenDokumentation();

        var projectDialog = mainPage.OpenNewProjectDialog();
        // Якщо тест упаде до збереження, вікно закриється через «Abbrechen».
        OnCleanup(projectDialog.CancelIfOpen);

        var today = DateTime.Today;
        var project = new ProjectData(
            Name: "Main_autotest_project",
            ShortName: "MAT",
            BeginDate: today.AddDays(1),
            EndDate: today.AddMonths(1),
            Cost: 1500m,
            CostCurrency: "USD",
            Outlay: 8m,
            OutlayUnit: "Tag(e)",
            Comment: "Created by ADAICA_AUTOTEST");

        projectDialog.Form.Fill(project);

        projectDialog.Form.ShouldHaveValues(project);
        var number = projectDialog.Form.Number.Text;
        number.ShouldNotBeNullOrWhiteSpace();

        projectDialog.Save();

        // Після збереження ADAICA сама переходить до нового проєкту.
        var savedProject = mainPage.WaitForOpenedProject(number);
        mainPage.SelectedNavigatorNodeName.ShouldBe($"{project.Name} ({number})");
        savedProject.ShouldHaveValues(project);
    }

    [Test]
    [Category("Smoke")]
    [AllureSeverity(SeverityLevel.critical)]
    public void DokumentationNavigator_NewTask()
    {
        var mainPage = LogInAndOpenDokumentation();

        var taskDialog = mainPage.OpenNewTaskDialog();
        // Якщо тест упаде до збереження, вікно закриється через «Abbrechen».
        OnCleanup(taskDialog.CancelIfOpen);

        var start = DateTime.Today.AddDays(1);
        var task = new TaskData(
            Name: "Main_autotest_task",
            ShortName: "MAT",
            Begin: start.AddHours(9),
            Due: start.AddDays(7).AddHours(17),
            // Дати виконання й закриття переводять завдання в статус «Aufgabe erledigt».
            Done: start.AddDays(5).AddHours(12),
            ClosedOn: start.AddDays(6),
            AcceptanceType: "Automatisch",
            Priority: "hoch",
            Importance: "mittel",
            EstimatedDuration: 2m,
            EstimatedDurationUnit: "Tag(e)",
            ActualDuration: 6m,
            ActualDurationUnit: "Stunde(n)",
            OrderNumber: "AUF-001",
            PurchaseOrderNumber: "BST-001",
            PercentDone: 50m,
            Location: "Unbekannt",
            LocationText: "Autotest Standort",
            CustomerBenefit: "Unbekannt",
            CustomerBenefitText: "Autotest Kundennutzen",
            Hint: "WF starten",
            HintText: "Autotest Hinweis",
            Remark: "Created by ADAICA_AUTOTEST");

        taskDialog.Form.Fill(task);

        taskDialog.Form.ShouldHaveValues(task);
        var number = taskDialog.Form.Number.Text;
        number.ShouldNotBeNullOrWhiteSpace();

        taskDialog.Save();

        // Після збереження ADAICA сама переходить до нового завдання.
        // Вузол завдання в дереві — лише назва, без номера, тому номер перевіряємо в бланку.
        var savedTask = mainPage.WaitForOpenedTask(number);
        mainPage.SelectedNavigatorNodeName.ShouldBe(task.Name);
        savedTask.ShouldHaveValues(task);
    }

    [Test]
    [Category("Smoke")]
    [AllureSeverity(SeverityLevel.critical)]
    public void DokumentationNavigator_NewAppointment()
    {
        var mainPage = LogInAndOpenDokumentation();

        var appointmentDialog = mainPage.OpenNewAppointmentDialog();
        // Якщо тест упаде до збереження, вікно закриється через «Abbrechen».
        OnCleanup(appointmentDialog.CancelIfOpen);

        var begin = DateTime.Today.AddDays(1).AddHours(10);
        var appointment = new AppointmentData(
            Name: "Main_autotest_appointment",
            ShortName: "MAT",
            Begin: begin,
            End: begin.AddHours(2),
            AllDay: false,
            // Трудовитрати дорівнюють тривалості терміну, тому ADAICA не показує попередження.
            Effort: 120m,
            EffortUnit: "Minute(n)",
            Importance: "hoch",
            Location: "Autotest Ort",
            Remark: "Created by ADAICA_AUTOTEST");

        appointmentDialog.Form.Fill(appointment);

        appointmentDialog.Form.ShouldHaveValues(appointment);
        var number = appointmentDialog.Form.Number.Text;
        number.ShouldNotBeNullOrWhiteSpace();

        appointmentDialog.Save();

        // Після збереження ADAICA сама переходить до нового терміну.
        // Вузол терміну в дереві — «ррММдд ГГ:хх/+Nh Назва», без номера, тому номер перевіряємо в бланку.
        var savedAppointment = mainPage.WaitForOpenedAppointment(number);
        var hours = (int)(appointment.End - appointment.Begin).TotalHours;
        var begins = appointment.Begin.ToString("yyMMdd HH:mm", CultureInfo.InvariantCulture);
        mainPage.SelectedNavigatorNodeName.ShouldBe($"{begins}/+{hours}h {appointment.Name}");
        savedAppointment.ShouldHaveValues(appointment);
    }

    private MainPage LogInAndOpenDokumentation()
    {
        var mainPage = new LoginPage(Session).LoginAs(
            TestSettings.UserName, TestSettings.Password, TestSettings.Database, TestSettings.Schema);
        var tabsBefore = mainPage.NavigatorTabNames;
        OnCleanup(() => mainPage.CloseLastNavigatorTabs(keep: tabsBefore.Count));

        mainPage.OpenNavigator(Dokumentation);

        mainPage.NavigatorTitle.ShouldHaveText(Dokumentation);
        mainPage.NavigatorTabNames.ShouldBe(tabsBefore.Append(Dokumentation));
        return mainPage;
    }
}
