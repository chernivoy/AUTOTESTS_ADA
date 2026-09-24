using System.Globalization;
using ADAICA_AUTOTEST.Infrastructure;
using ADAICA_AUTOTEST.Pages;
using Allure.Net.Commons;

namespace ADAICA_AUTOTEST.Tests;

/// <summary>
/// Базовий клас тестів ADAICA: після тесту виконує зареєстроване прибирання і завершує застосунок
/// з підтвердженням виходу, щоб коректно закрити сесію, а не зупиняти процес примусово.
/// </summary>
public abstract class AdaicaTestBase : UiTestBase
{
    private readonly List<Action> _cleanups = [];

    /// <summary>
    /// Реєструє дію, яка поверне застосунок у попередній стан (наприклад, закриє відкриту вкладку).
    /// Виконується після скріншота падіння, тож він показує стан у момент помилки.
    /// </summary>
    protected void OnCleanup(Action cleanup) => _cleanups.Add(cleanup);

    /// <summary>
    /// Входить в ADAICA й відкриває навігатор Dokumentation у новій вкладці. Після тесту вкладку буде закрито,
    /// бо відкриті вкладки ADAICA запам'ятовує для користувача.
    /// </summary>
    protected MainPage LogInAndOpenDokumentation() => LogInAndOpenNavigator("Dokumentation");

    /// <summary>
    /// Входить в ADAICA й відкриває навігатор <paramref name="navigatorName"/> у новій вкладці. Після тесту вкладку
    /// буде закрито, бо відкриті вкладки ADAICA запам'ятовує для користувача.
    /// </summary>
    protected MainPage LogInAndOpenNavigator(string navigatorName)
    {
        var mainPage = new LoginPage(Session).LoginAs(
            TestSettings.UserName, TestSettings.Password, TestSettings.Database, TestSettings.Schema);
        var tabsBefore = mainPage.NavigatorTabNames;
        OnCleanup(() => mainPage.CloseLastNavigatorTabs(keep: tabsBefore.Count));

        mainPage.OpenNavigator(navigatorName);

        mainPage.NavigatorTitle.ShouldHaveText(navigatorName);
        mainPage.NavigatorTabNames.ShouldBe(tabsBefore.Append(navigatorName));
        return mainPage;
    }

    /// <summary>
    /// Створює з контекстного меню вузла <paramref name="parentNode"/> (будь-якого запису) проєкт, Ordner, завдання,
    /// термін та інфозапис. Кожен збережений запис ADAICA має виділити в дереві під батьківським вузлом,
    /// у папці свого типу: «Батько → Aufgaben → Завдання».
    /// </summary>
    protected void CreateChildRecords(MainPage mainPage, CustomerCase customer, string parentNode)
    {
        var projectDialog = mainPage.OpenNewChildProjectDialog(parentNode);
        OnCleanup(projectDialog.CancelIfOpen);
        var project = customer.NewSubproject();
        projectDialog.Form.Fill(project);
        var projectNumber = projectDialog.Form.Number.Text;
        projectDialog.Save();
        mainPage.WaitForOpenedProject(projectNumber).ShouldHaveValues(project);
        ShouldBeSelectedUnder(mainPage, parentNode, "Projekte", NodeWithNumber(project.Name, projectNumber));

        var folderDialog = mainPage.OpenNewChildFolderDialog(parentNode);
        OnCleanup(folderDialog.CancelIfOpen);
        var folder = customer.NewFolder();
        folderDialog.Form.Fill(folder);
        var folderNumber = folderDialog.Form.Number.Text;
        folderDialog.Save();
        mainPage.WaitForOpenedFolder(folderNumber).ShouldHaveValues(folder);
        ShouldBeSelectedUnder(mainPage, parentNode, "Ordner", NodeWithNumber(folder.Name, folderNumber));

        var taskDialog = mainPage.OpenNewChildTaskDialog(parentNode);
        OnCleanup(taskDialog.CancelIfOpen);
        var task = customer.NewTask();
        taskDialog.Form.Fill(task);
        var taskNumber = taskDialog.Form.Number.Text;
        taskDialog.Save();
        mainPage.WaitForOpenedTask(taskNumber).ShouldHaveValues(task);
        ShouldBeSelectedUnder(mainPage, parentNode, "Aufgaben", task.Name);

        var appointmentDialog = mainPage.OpenNewChildAppointmentDialog(parentNode);
        OnCleanup(appointmentDialog.CancelIfOpen);
        var appointment = customer.NewAppointment();
        appointmentDialog.Form.Fill(appointment);
        var appointmentNumber = appointmentDialog.Form.Number.Text;
        appointmentDialog.Save();
        mainPage.WaitForOpenedAppointment(appointmentNumber).ShouldHaveValues(appointment);
        ShouldBeSelectedUnder(mainPage, parentNode, "Termine", AppointmentNode(appointment));

        var noteDialog = mainPage.OpenNewChildNoteDialog(parentNode);
        OnCleanup(noteDialog.CancelIfOpen);
        var note = customer.NewNote();
        noteDialog.Form.Fill(note);
        noteDialog.Save();
        mainPage.WaitForOpenedNote(note.Remark).ShouldHaveValues(note);
        ShouldBeSelectedUnder(mainPage, parentNode, "Notizen", NoteNode(note));
    }

    private static void ShouldBeSelectedUnder(MainPage mainPage, string parentNode, string folder, string recordNode) =>
        AllureApi.Step(
            $"Перевірити, що в дереві виділено «{recordNode}» у «{parentNode} → {folder}»",
            () => mainPage.SelectedNavigatorNodePath.TakeLast(3).ShouldBe(new[] { parentNode, folder, recordNode }));

    // Як ADAICA підписує вузли записів у дереві навігатора. Проєкт і Ordner — «Назва (Номер)».
    protected static string NodeWithNumber(string name, string number) => $"{name} ({number})";

    // «ррММдд ГГ:хх/+Nh Назва», наприклад «260918 10:00/+2h Besprechung mit Frau Keller».
    protected static string AppointmentNode(AppointmentData appointment)
    {
        var begins = appointment.Begin.ToString("yyMMdd HH:mm", CultureInfo.InvariantCulture);
        var hours = (int)(appointment.End - appointment.Begin).TotalHours;
        return $"{begins}/+{hours}h {appointment.Name}";
    }

    // «дд.ММ.рр Назва».
    protected static string NoteNode(NoteData note) =>
        $"{note.Begin.ToString("dd.MM.yy", CultureInfo.InvariantCulture)} {note.Name}";

    protected override void CloseApplication()
    {
        // Один екземпляр класу обслуговує всі тести, тому список очищаємо щоразу.
        var cleanups = _cleanups.AsEnumerable().Reverse().ToList();
        _cleanups.Clear();

        foreach (var cleanup in cleanups)
        {
            try
            {
                cleanup();
            }
            catch (Exception ex)
            {
                TestContext.Out.WriteLine($"Прибирання після тесту не вдалося: {ex}");
            }
        }

        new AdaicaWindow(Session).Exit();
    }
}
