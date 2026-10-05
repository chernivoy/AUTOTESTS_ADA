using ADAICA_AUTOTEST.Pages;
using Allure.Net.Commons;
using Allure.Net.Commons.Attributes;

namespace ADAICA_AUTOTEST.Tests;

/// <summary>
/// Створення записів через навігатор Dokumentation. Збережені записи лишаються в базі.
/// Дані беруться з випадкової справи клієнта (<see cref="CustomerCase"/>), тож назви можуть повторитися
/// в іншому прогоні. Тому свій запис тест упізнає за номером, а інфозапис, у якого номера немає, —
/// за «Bemerkung», де є час запису.
/// </summary>
[AllureSuite("Навігатори")]
public class NavigatorTests : AdaicaTestBase
{
    [Test]
    [Category("Smoke")]
    [AllureSeverity(SeverityLevel.critical)]
    public void DokumentationNavigator_NewProject()
    {
        var mainPage = LogInAndOpenDokumentation();

        var projectDialog = mainPage.OpenNewProjectDialog();
        // Якщо тест упаде до збереження, вікно закриється через «Abbrechen».
        OnCleanup(projectDialog.CancelIfOpen);

        var project = CustomerCase.Random().NewProject();
        projectDialog.Form.Fill(project);

        projectDialog.Form.ShouldHaveValues(project);
        var number = projectDialog.Form.Number.Text;
        number.ShouldNotBeNullOrWhiteSpace();

        projectDialog.Save();

        // Після збереження ADAICA сама переходить до нового проєкту.
        var savedProject = mainPage.WaitForOpenedProject(number);
        mainPage.SelectedNavigatorNodeName.ShouldBe(NodeWithNumber(project.Name, number));
        savedProject.ShouldHaveValues(project);
    }

    [Test]
    [Category("Smoke")]
    [AllureSeverity(SeverityLevel.critical)]
    public void DokumentationNavigator_NewFolder()
    {
        var mainPage = LogInAndOpenDokumentation();

        var folderDialog = mainPage.OpenNewFolderDialog();
        // Якщо тест упаде до збереження, вікно закриється через «Abbrechen».
        OnCleanup(folderDialog.CancelIfOpen);

        var folder = CustomerCase.Random().NewFolder();
        folderDialog.Form.Fill(folder);

        folderDialog.Form.ShouldHaveValues(folder);
        var number = folderDialog.Form.Number.Text;
        // Бланк той самий, що й у проєкту, тож Ordner від проєкту відрізняє лише номер.
        number.ShouldStartWith("OPE");

        folderDialog.Save();

        // Після збереження ADAICA сама переходить до нового Ordner і виділяє його у вузлі Ordner.
        var savedFolder = mainPage.WaitForOpenedFolder(number);
        mainPage.SelectedNavigatorNodePath.ShouldBe(new[] { "Gesamtliste", "Ordner", NodeWithNumber(folder.Name, number) });
        savedFolder.ShouldHaveValues(folder);
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

        var task = CustomerCase.Random().NewTask();
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

        var appointment = CustomerCase.Random().NewAppointment();
        appointmentDialog.Form.Fill(appointment);

        appointmentDialog.Form.ShouldHaveValues(appointment);
        var number = appointmentDialog.Form.Number.Text;
        number.ShouldNotBeNullOrWhiteSpace();

        appointmentDialog.Save();

        // Після збереження ADAICA сама переходить до нового терміну.
        // Вузол терміну в дереві — «ррММдд ГГ:хх/+Nh Назва», без номера, тому номер перевіряємо в бланку.
        var savedAppointment = mainPage.WaitForOpenedAppointment(number);
        mainPage.SelectedNavigatorNodeName.ShouldBe(AppointmentNode(appointment));
        savedAppointment.ShouldHaveValues(appointment);
    }

    [Test]
    [Category("Smoke")]
    [AllureSeverity(SeverityLevel.critical)]
    public void DokumentationNavigator_NewNote()
    {
        var mainPage = LogInAndOpenDokumentation();

        var noteDialog = mainPage.OpenNewNoteDialog();
        // Якщо тест упаде до збереження, вікно закриється через «Abbrechen».
        OnCleanup(noteDialog.CancelIfOpen);

        var note = CustomerCase.Random().NewNote();
        noteDialog.Form.Fill(note);

        noteDialog.Form.ShouldHaveValues(note);

        noteDialog.Save();

        // Після збереження ADAICA сама переходить до нового інфозапису.
        // Номера немає, тому запис шукаємо за «Bemerkung».
        var savedNote = mainPage.WaitForOpenedNote(note.Remark);
        mainPage.SelectedNavigatorNodeName.ShouldBe(NoteNode(note));
        savedNote.ShouldHaveValues(note);
    }

    /// <summary>
    /// Проєкт, а під ним з його контекстного меню — підпроєкт, Ordner, завдання, термін, інфозапис і документ із Word-файлу.
    /// Усі записи стосуються однієї справи клієнта, як у реальному проєкті.
    /// </summary>
    [Test]
    [AllureSeverity(SeverityLevel.critical)]
    public void DokumentationNavigator_NewProjectWithChildRecords()
    {
        var mainPage = LogInAndOpenDokumentation();
        var customer = CustomerCase.Random();

        var projectDialog = mainPage.OpenNewProjectDialog();
        OnCleanup(projectDialog.CancelIfOpen);
        var project = customer.NewProject();
        projectDialog.Form.Fill(project);
        var projectNumber = projectDialog.Form.Number.Text;
        projectDialog.Save();
        mainPage.WaitForOpenedProject(projectNumber);

        CreateChildRecords(mainPage, customer, NodeWithNumber(project.Name, projectNumber));
    }

    /// <summary>
    /// Ordner, а під ним з його контекстного меню — проєкт, вкладений Ordner, завдання, термін, інфозапис і документ із Word-файлу.
    /// Усі записи стосуються однієї справи клієнта.
    /// </summary>
    [Test]
    [AllureSeverity(SeverityLevel.critical)]
    public void DokumentationNavigator_NewFolderWithChildRecords()
    {
        var mainPage = LogInAndOpenDokumentation();
        var customer = CustomerCase.Random();

        var folderDialog = mainPage.OpenNewFolderDialog();
        OnCleanup(folderDialog.CancelIfOpen);
        var folder = customer.NewFolder();
        folderDialog.Form.Fill(folder);
        var folderNumber = folderDialog.Form.Number.Text;
        folderDialog.Save();
        mainPage.WaitForOpenedFolder(folderNumber);

        CreateChildRecords(mainPage, customer, NodeWithNumber(folder.Name, folderNumber));
    }

    /// <summary>
    /// Завдання, а під ним з його контекстного меню — проєкт, Ordner, вкладене завдання, термін, інфозапис і документ із Word-файлу.
    /// </summary>
    [Test]
    [AllureSeverity(SeverityLevel.critical)]
    public void DokumentationNavigator_NewTaskWithChildRecords()
    {
        var mainPage = LogInAndOpenDokumentation();
        var customer = CustomerCase.Random();

        var taskDialog = mainPage.OpenNewTaskDialog();
        OnCleanup(taskDialog.CancelIfOpen);
        var task = customer.NewTask();
        taskDialog.Form.Fill(task);
        var taskNumber = taskDialog.Form.Number.Text;
        taskDialog.Save();
        mainPage.WaitForOpenedTask(taskNumber);

        CreateChildRecords(mainPage, customer, task.Name);
    }

    /// <summary>
    /// Термін, а під ним з його контекстного меню — проєкт, Ordner, завдання, вкладений термін, інфозапис і документ із Word-файлу.
    /// </summary>
    [Test]
    [AllureSeverity(SeverityLevel.critical)]
    public void DokumentationNavigator_NewAppointmentWithChildRecords()
    {
        var mainPage = LogInAndOpenDokumentation();
        var customer = CustomerCase.Random();

        var appointmentDialog = mainPage.OpenNewAppointmentDialog();
        OnCleanup(appointmentDialog.CancelIfOpen);
        var appointment = customer.NewAppointment();
        appointmentDialog.Form.Fill(appointment);
        var appointmentNumber = appointmentDialog.Form.Number.Text;
        appointmentDialog.Save();
        mainPage.WaitForOpenedAppointment(appointmentNumber);

        CreateChildRecords(mainPage, customer, AppointmentNode(appointment));
    }

    /// <summary>
    /// Інфозапис, а під ним з його контекстного меню — проєкт, Ordner, завдання, термін, вкладений інфозапис і документ із Word-файлу.
    /// </summary>
    [Test]
    [AllureSeverity(SeverityLevel.critical)]
    public void DokumentationNavigator_NewNoteWithChildRecords()
    {
        var mainPage = LogInAndOpenDokumentation();
        var customer = CustomerCase.Random();

        var noteDialog = mainPage.OpenNewNoteDialog();
        OnCleanup(noteDialog.CancelIfOpen);
        var note = customer.NewNote();
        noteDialog.Form.Fill(note);
        noteDialog.Save();
        mainPage.WaitForOpenedNote(note.Remark);

        CreateChildRecords(mainPage, customer, NoteNode(note));
    }
}
