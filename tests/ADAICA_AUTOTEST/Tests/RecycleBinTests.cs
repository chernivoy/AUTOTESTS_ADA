using System.Globalization;
using ADAICA_AUTOTEST.Controls;
using ADAICA_AUTOTEST.Infrastructure;
using ADAICA_AUTOTEST.Pages;
using Allure.Net.Commons;
using Allure.Net.Commons.Attributes;

namespace ADAICA_AUTOTEST.Tests;

/// <summary>
/// Papierkorb навігатора Dokumentation: запис видаляється пунктом «Löschen» контекстного меню, з'являється у вузлі
/// «Papierkorb» (тест перевіряє всі колонки рядка), відновлюється пунктом «Wiederherstellen», видаляється знову й
/// остаточно видаляється з Papierkorb. Номера запису Papierkorb не показує, тож свій рядок тест упізнає за внутрішнім
/// ID з вікна «Eigenschaften». Після тесту запису в базі немає.
/// </summary>
[AllureSuite("Papierkorb")]
public class RecycleBinTests : AdaicaTestBase
{
    private const string CurrentNode = "Aktuelle";
    private const string TasksNode = "Aufgaben";

    // Питання «Löschen» і в дереві, і в Papierkorb: «Folgende Elemente werden gelöscht: \nНазва \n\n1.  Das Löschen
    // erfolgt endgültig!\n\n2.  Untergeordnete Elemente …\n\nBestätigen Sie das Löschen?». Довгу назву ADAICA обрізає «...».
    private const string DeleteQuestionStart = "Folgende Elemente werden gelöscht:";
    private const string DeleteQuestionEnd = "Bestätigen Sie das Löschen?";

    [Test]
    [AllureSeverity(SeverityLevel.critical)]
    public void DokumentationNavigator_ProjectToRecycleBinAndBack()
    {
        var mainPage = LogInAndOpenDokumentation();
        var dialog = mainPage.OpenNewProjectDialog();
        OnCleanup(dialog.CancelIfOpen);
        var project = CustomerCase.Random().WithClass().NewProject();
        dialog.Form.Fill(project);
        var number = dialog.Form.Number.Text;
        dialog.Save();
        mainPage.WaitForOpenedProject(number);

        DeleteRestoreAndPurge(
            mainPage,
            new DeletedRecord(NodeWithNumber(project.Name, number), project.Name, "PROJECT", RootOf("PROJECT")),
            () => mainPage.WaitForOpenedProject(number).ShouldHaveValues(project));
    }

    [Test]
    [AllureSeverity(SeverityLevel.critical)]
    public void DokumentationNavigator_FolderToRecycleBinAndBack()
    {
        var mainPage = LogInAndOpenDokumentation();
        var dialog = mainPage.OpenNewFolderDialog();
        OnCleanup(dialog.CancelIfOpen);
        var folder = CustomerCase.Random().WithClass().NewFolder();
        dialog.Form.Fill(folder);
        var number = dialog.Form.Number.Text;
        dialog.Save();
        mainPage.WaitForOpenedFolder(number);

        DeleteRestoreAndPurge(
            mainPage,
            new DeletedRecord(NodeWithNumber(folder.Name, number), folder.Name, "OPERATION", RootOf("OPERATION")),
            () => mainPage.WaitForOpenedFolder(number).ShouldHaveValues(folder));
    }

    [Test]
    [AllureSeverity(SeverityLevel.critical)]
    public void DokumentationNavigator_TaskToRecycleBinAndBack()
    {
        var mainPage = LogInAndOpenDokumentation();
        var dialog = mainPage.OpenNewTaskDialog();
        OnCleanup(dialog.CancelIfOpen);
        var task = CustomerCase.Random().WithClass().NewTask();
        dialog.Form.Fill(task);
        var number = dialog.Form.Number.Text;
        dialog.Save();
        mainPage.WaitForOpenedTask(number);

        DeleteRestoreAndPurge(
            mainPage,
            new DeletedRecord(task.Name, task.Name, "TASK", RootOf("TASK")),
            () => mainPage.WaitForOpenedTask(number).ShouldHaveValues(task));
    }

    [Test]
    [AllureSeverity(SeverityLevel.critical)]
    public void DokumentationNavigator_AppointmentToRecycleBinAndBack()
    {
        var mainPage = LogInAndOpenDokumentation();
        var dialog = mainPage.OpenNewAppointmentDialog();
        OnCleanup(dialog.CancelIfOpen);
        var appointment = CustomerCase.Random().WithClass().NewAppointment();
        dialog.Form.Fill(appointment);
        var number = dialog.Form.Number.Text;
        dialog.Save();
        mainPage.WaitForOpenedAppointment(number);

        DeleteRestoreAndPurge(
            mainPage,
            new DeletedRecord(AppointmentNode(appointment), appointment.Name, "APPOINT", RootOf("APPOINT")),
            () => mainPage.WaitForOpenedAppointment(number).ShouldHaveValues(appointment));
    }

    [Test]
    [AllureSeverity(SeverityLevel.critical)]
    public void DokumentationNavigator_NoteToRecycleBinAndBack()
    {
        var mainPage = LogInAndOpenDokumentation();
        var dialog = mainPage.OpenNewNoteDialog();
        OnCleanup(dialog.CancelIfOpen);
        var note = CustomerCase.Random().WithClass().NewNote();
        dialog.Form.Fill(note);
        dialog.Save();
        mainPage.WaitForOpenedNote(note.Remark);

        DeleteRestoreAndPurge(
            mainPage,
            new DeletedRecord(NoteNode(note), note.Name, "HISTORY", RootOf("HISTORY")),
            () => mainPage.WaitForOpenedNote(note.Remark).ShouldHaveValues(note));
    }

    /// <summary>
    /// Документ із Word-файлу під новим проєктом (окремо документ не створюється). У Papierkorb батько документа —
    /// цей проєкт, а контакту в документа немає, тож колонки контакту порожні.
    /// </summary>
    [Test]
    [AllureSeverity(SeverityLevel.critical)]
    public void DokumentationNavigator_DocumentToRecycleBinAndBack()
    {
        var mainPage = LogInAndOpenDokumentation();
        var customer = CustomerCase.Random();
        var (projectNode, projectId) = CreateProject(mainPage, customer);

        var document = CreateDocumentFromWordFile(mainPage, projectNode, customer.NewWordFile());
        var file = document.File;
        var data = new DocumentData(
            Name: file.Name,
            ShortName: string.Empty,
            FileType: "docx",
            FileSize: AdaicaFormat.FormatFileSize(file.Length),
            Creator: mainPage.UserDisplayName,
            Version: "1 / 1",
            Status: "aktuell",
            Remark: string.Empty);

        DeleteRestoreAndPurge(
            mainPage,
            new DeletedRecord(file.FileName, file.Name, "BLOB", new ParentRecord("PROJECT", projectId, projectNode), HasContact: false),
            () => mainPage.WaitForOpenedDocument(file.Name).ShouldHaveValues(data));
    }

    /// <summary>
    /// Відновлений запис має повернутися туди, звідки його видалено. Завдання під проєктом видаляється й відновлюється;
    /// у Papierkorb батьком указано проєкт, але відновлене завдання ADAICA показує лише в «Aktuelle», а під проєктом
    /// його немає й після перезапуску. Так само записи з Gesamtliste не повертаються у свій вузол (Projekte, Ordner, …).
    /// Імовірний баг, ще не підтверджений розробником (README, «Тести, що падають навмисно»).
    /// </summary>
    [Test]
    [AllureSeverity(SeverityLevel.critical)]
    public void DokumentationNavigator_RestoredRecordReturnsToParent()
    {
        var mainPage = LogInAndOpenDokumentation();
        var customer = CustomerCase.Random();
        var (projectNode, projectId) = CreateProject(mainPage, customer);

        var taskDialog = mainPage.OpenNewChildTaskDialog(projectNode);
        OnCleanup(taskDialog.CancelIfOpen);
        var task = customer.NewTask();
        taskDialog.Form.Fill(task);
        var taskNumber = taskDialog.Form.Number.Text;
        taskDialog.Save();
        mainPage.WaitForOpenedTask(taskNumber);
        var taskId = mainPage.ReadRecordId(task.Name);

        ConfirmDeletion(mainPage, mainPage.DeleteRecord(task.Name), task.Name);
        var bin = mainPage.OpenRecycleBin();
        bin.WaitForEntry(taskId).ParentId.ShouldBe(projectId);
        bin.Restore(taskId);
        mainPage.WaitForOpenedTask(taskNumber);

        mainPage.RefreshNavigatorNode(projectNode);
        AllureApi.Step(
            $"Перевірити, що відновлене завдання знову під «{projectNode} → {TasksNode}»",
            () =>
            {
                mainPage.ChildNodeNames(projectNode).ShouldContain(TasksNode, $"Під проєктом «{projectNode}» немає вузла «{TasksNode}»");
                mainPage.ChildNodeNames(projectNode, TasksNode).ShouldContain(task.Name);
            });
    }

    /// <summary>
    /// Спільний сценарій: «Löschen» → рядок у Papierkorb з усіма колонками → «Wiederherstellen» (запис відкривається
    /// в «Aktuelle» з тими самими полями, рядок зникає з Papierkorb) → знову «Löschen» → остаточне «Löschen» у Papierkorb.
    /// </summary>
    private void DeleteRestoreAndPurge(MainPage mainPage, DeletedRecord record, Action shouldBeRestored)
    {
        var id = mainPage.ReadRecordId(record.Node);

        var deletedFrom = DateTime.Now;
        ConfirmDeletion(mainPage, mainPage.DeleteRecord(record.Node), record.Name);
        var deletedTo = DateTime.Now;

        var bin = mainPage.OpenRecycleBin();
        ShouldBeEntry(mainPage, bin.WaitForEntry(id), record, id, deletedFrom, deletedTo);

        bin.Restore(id);
        shouldBeRestored();
        AllureApi.Step(
            $"Перевірити, що відновлений запис виділено в «{CurrentNode} → {record.Node}»",
            () => mainPage.SelectedNavigatorNodePath.ShouldBe(new[] { CurrentNode, record.Node }));
        mainPage.OpenRecycleBin().ShouldNotContain(id);

        mainPage.SelectNavigatorNodeAt(CurrentNode, record.Node);
        ConfirmDeletion(mainPage, mainPage.DeleteRecord(record.Node), record.Name);

        bin = mainPage.OpenRecycleBin();
        bin.WaitForEntry(id);
        ConfirmDeletion(mainPage, bin.DeleteForever(id), record.Name);
        bin.ShouldNotContain(id);
    }

    /// <summary>Перевіряє питання «Löschen» (назву запису ADAICA може обрізати «...») і відповідає «Ja».</summary>
    private static void ConfirmDeletion(MainPage mainPage, MessageBoxDialog question, string recordName) =>
        AllureApi.Step($"Підтвердити видалення «{recordName}»", () =>
        {
            var lines = question.Message.Text.Split('\n').Select(line => line.Trim()).ToList();
            question.ShouldSatisfyAllConditions(
                () => question.Title.ShouldBe("Löschen"),
                () => lines[0].ShouldBe(DeleteQuestionStart),
                () => lines[^1].ShouldBe(DeleteQuestionEnd),
                () =>
                {
                    if (lines[1].EndsWith("...", StringComparison.Ordinal))
                        recordName.ShouldStartWith(lines[1][..^3]);
                    else
                        lines[1].ShouldBe(recordName);
                });

            question.Button("Ja").Click();
            Wait.Until(() => mainPage.FindMessageBox() is null, "закриття питання «Löschen»");
        });

    /// <summary>
    /// Усі колонки рядка Papierkorb. ID користувача й персони тест знати не може, тож перевіряє їхній формат; контакт
    /// запису — відповідальний, тобто той самий користувач, що видалив запис.
    /// </summary>
    private static void ShouldBeEntry(
        MainPage mainPage, RecycleBinEntry entry, DeletedRecord record, string id, DateTime deletedFrom, DateTime deletedTo) =>
        AllureApi.Step("Перевірити колонки рядка Papierkorb", () =>
        {
            var user = mainPage.UserDisplayName;
            entry.ShouldSatisfyAllConditions(
                () => entry.Name.ShouldBe(record.Name),
                () => entry.Id.ShouldBe(id),
                () => entry.ClassType.ShouldBe(record.ClassType),
                // «Datum» — час видалення з точністю до хвилини.
                () => ParseDateTime(entry.Date).ShouldBeInRange(
                    new DateTime(deletedFrom.Ticks - deletedFrom.Ticks % TimeSpan.TicksPerMinute), deletedTo),
                () => entry.User.ShouldBe($"Account: {TestSettings.UserName}, Person: {user}"),
                () => entry.RealUserId.ShouldMatch(@"^\d{12}USER\d{6}$"),
                () => entry.ParentClassType.ShouldBe(record.Parent.ClassType),
                () => entry.ParentId.ShouldBe(record.Parent.Id),
                () => entry.ParentName.ShouldBe(record.Parent.Name),
                () => entry.Person.ShouldMatch(@"^\d{12}PERS\d{6}$"),
                () => entry.ContactClassType.ShouldBe(record.HasContact ? "CONTACT" : string.Empty),
                () => entry.ContactId.ShouldBe(record.HasContact ? entry.Person : string.Empty),
                () => entry.ContactName.ShouldBe(record.HasContact ? user : string.Empty),
                () => entry.ObjectClassType.ShouldBeEmpty(),
                () => entry.ObjectId.ShouldBeEmpty(),
                () => entry.ObjectName.ShouldBeEmpty());
        });

    private (string Node, string Id) CreateProject(MainPage mainPage, CustomerCase customer)
    {
        var dialog = mainPage.OpenNewProjectDialog();
        OnCleanup(dialog.CancelIfOpen);
        var project = customer.NewProject();
        dialog.Form.Fill(project);
        var number = dialog.Form.Number.Text;
        dialog.Save();
        mainPage.WaitForOpenedProject(number);
        var node = NodeWithNumber(project.Name, number);
        return (node, mainPage.ReadRecordId(node));
    }

    // Записи з Gesamtliste ADAICA підв'язує до кореневого запису свого типу: «ROOT_PROJECT_000000000» (22 символи), «ROOT PROJECT».
    private static ParentRecord RootOf(string classType) =>
        new(classType, $"ROOT_{classType}_".PadRight(22, '0'), $"ROOT {classType}");

    private static DateTime ParseDateTime(string text) =>
        DateTime.ParseExact(text, $"{AdaicaFormat.DatePattern} {AdaicaFormat.TimePattern}", CultureInfo.InvariantCulture);

    /// <param name="Node">Вузол запису в дереві.</param>
    /// <param name="Name">Назва запису, як у Papierkorb і в питанні «Löschen».</param>
    /// <param name="HasContact">Чи пов'язано запис із контактом (відповідальним); у документа контакту немає.</param>
    private sealed record DeletedRecord(string Node, string Name, string ClassType, ParentRecord Parent, bool HasContact = true);

    /// <summary>Батьківський запис у колонках Papierkorb: Klassentyp, ID і назва.</summary>
    private sealed record ParentRecord(string ClassType, string Id, string Name);
}
