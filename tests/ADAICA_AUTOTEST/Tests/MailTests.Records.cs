using ADAICA_AUTOTEST.Infrastructure;
using ADAICA_AUTOTEST.Pages;
using Allure.Net.Commons;
using Allure.Net.Commons.Attributes;

namespace ADAICA_AUTOTEST.Tests;

/// <summary>
/// Вкладка «Dokumentation» вікна збереження листа: нові записи для листа («+» і «Neu» з меню рядка), фільтр
/// «Suchbegriff eingeben…», кнопка ієрархії й пошук «Finden». Записи, створені у вікні, мають номери одразу, але
/// лишаються в базі, лише якщо зберегти лист: після «Abbrechen» пошук їх не знаходить.
/// </summary>
public partial class MailTests
{
    // Рядки вкладки «Dokumentation»: спершу вузол, з якого створено лист («ROOT EMAIL»), далі додані записи.
    private const int RootEmailRow = 0;
    private const int ParentRecordRow = 1;

    // Типи записів у меню «+» і «Neu» вкладки «Dokumentation» — у тому порядку, як їх показує ADAICA.
    private static readonly RecordKind[] RecordKinds =
        [RecordKind.Project, RecordKind.Folder, RecordKind.Task, RecordKind.Appointment, RecordKind.Note];

    /// <summary>
    /// Вікно збереження листа: на вкладці «Dokumentation» «+» → «Projekt», а з меню рядка проєкту «Neu» — підпроєкт,
    /// Ordner, завдання, термін і інфозапис під ним (заголовок кожного вікна — «… (übergeordnet: проєкт)»). Дерево
    /// показує проєкт і п'ять записів під ним. Збережений лист у «Zugriffspfade» лежить у вузлі «E-Mails», у проєкті
    /// й у кожному дочірньому записі — ланцюжком «запис ← проєкт».
    /// </summary>
    [Test]
    [AllureSeverity(SeverityLevel.normal)]
    public void DokumentationNavigator_MailToNewProjectWithChildRecords() => MailToNewRecordWithChildRecords(RecordKind.Project);

    /// <summary>
    /// Як <see cref="DokumentationNavigator_MailToNewProjectWithChildRecords"/>, але батьківський запис — Ordner
    /// («+» → «Ordner»), а під ним проєкт, вкладений Ordner, завдання, термін і інфозапис.
    /// </summary>
    [Test]
    [AllureSeverity(SeverityLevel.normal)]
    public void DokumentationNavigator_MailToNewFolderWithChildRecords() => MailToNewRecordWithChildRecords(RecordKind.Folder);

    /// <summary>
    /// Як <see cref="DokumentationNavigator_MailToNewProjectWithChildRecords"/>, але батьківський запис — завдання
    /// («+» → «Aufgabe» → «Neue Aufgabe»), а під ним проєкт, Ordner, вкладене завдання, термін і інфозапис.
    /// </summary>
    [Test]
    [AllureSeverity(SeverityLevel.normal)]
    public void DokumentationNavigator_MailToNewTaskWithChildRecords() => MailToNewRecordWithChildRecords(RecordKind.Task);

    /// <summary>
    /// Як <see cref="DokumentationNavigator_MailToNewProjectWithChildRecords"/>, але батьківський запис — термін
    /// («+» → «Termin»), а під ним проєкт, Ordner, завдання, вкладений термін і інфозапис.
    /// </summary>
    [Test]
    [AllureSeverity(SeverityLevel.normal)]
    public void DokumentationNavigator_MailToNewAppointmentWithChildRecords() => MailToNewRecordWithChildRecords(RecordKind.Appointment);

    /// <summary>
    /// Як <see cref="DokumentationNavigator_MailToNewProjectWithChildRecords"/>, але батьківський запис — інфозапис
    /// («+» → «Notiz»), а під ним проєкт, Ordner, завдання, термін і вкладений інфозапис.
    /// </summary>
    [Test]
    [AllureSeverity(SeverityLevel.normal)]
    public void DokumentationNavigator_MailToNewNoteWithChildRecords() => MailToNewRecordWithChildRecords(RecordKind.Note);

    /// <summary>
    /// Кнопки й поле над деревом «Dokumentation» у вікні збереження листа.
    /// <list type="bullet">
    /// <item>«+» — меню Projekt, Ordner, Aufgabe, Termin, Notiz; тест створює проєкт, а під ним з меню рядка — Ordner.</item>
    /// <item>«Suchbegriff eingeben…»: фільтр за назвою Ordner (лупа біля поля) лишає Ordner разом із проєктом над ним;
    /// хрестик повертає весь список; фільтр за номером Ordner (Enter) — те саме; текст, якого немає в жодному записі, —
    /// порожній список, навіть без «ROOT EMAIL».</item>
    /// <item>Лупа «Finden» у другому листі (перший збережено — інакше записів з нього не було б): Ordner знаходиться пошуком за номером і стає в список
    /// сам, без проєкту; кнопка ієрархії показує над ним проєкт, повторне натискання — ховає.</item>
    /// </list>
    /// </summary>
    [Test]
    [AllureSeverity(SeverityLevel.normal)]
    public void DokumentationNavigator_MailSaveDialogRecordsToolbar()
    {
        TurnOffSalutationForSending();
        var mainPage = LogInAndOpenDokumentation();
        EnsureOutlookAccount(mainPage, OutlookApplication.DefaultAccountAddress());
        var customer = CustomerCase.Random();

        var mail = customer.NewMail();
        var (saveDialog, _) = SendMail(mainPage, mail);
        saveDialog.ShouldHaveRecordRows(1);
        AllureApi.Step("Перевірити меню «+»: Projekt, Ordner, Aufgabe, Termin, Notiz", () =>
            saveDialog.NewRecordMenu().ShouldBe(
                [new("Projekt", true), new("Ordner", true), new("Aufgabe", true), new("Termin", true), new("Notiz", true)],
                "Меню «+» на вкладці «Dokumentation»"));

        var project = CreateRecord(saveDialog, customer, RecordKind.Project, parent: null);
        var folder = CreateRecord(saveDialog, customer, RecordKind.Folder, parent: project);
        saveDialog.ShouldHaveRecordRows(3);

        saveDialog.FilterRecords(folder.Name);
        saveDialog.ShouldHaveRecordRows(2);
        RecordRowShouldBe(saveDialog, ParentRecordRow, folder.Label);
        saveDialog.ClearRecordFilter();
        saveDialog.ShouldHaveRecordRows(3);

        saveDialog.FilterRecords(folder.Number!, withEnter: true);
        saveDialog.ShouldHaveRecordRows(2);
        RecordRowShouldBe(saveDialog, ParentRecordRow, folder.Label);
        saveDialog.ClearRecordFilter();

        saveDialog.FilterRecords(CustomerCase.NewMailAddress(customer.NewMailPerson()));
        saveDialog.ShouldHaveRecordRows(0);
        saveDialog.ClearRecordFilter();
        saveDialog.ShouldHaveRecordRows(3);
        CreateRecipient(saveDialog, mail);
        saveDialog.Save();

        (saveDialog, _) = SendMail(mainPage, customer.NewMail());
        saveDialog.LinkRecord(folder.Number!, folder.Number!);
        saveDialog.ShouldHaveRecordRows(2);
        RecordRowShouldBe(saveDialog, ParentRecordRow, folder.Label);

        saveDialog.ToggleHierarchy();
        OnCleanup(() =>
        {
            if (saveDialog.IsOpen && saveDialog.HasRecordRow(2))
                saveDialog.ToggleHierarchy();
        });
        saveDialog.ShouldHaveRecordRows(3);
        RecordRowShouldBe(saveDialog, ParentRecordRow, project.Label);
        RecordRowShouldBe(saveDialog, ParentRecordRow + 1, folder.Label);

        saveDialog.ToggleHierarchy();
        saveDialog.ShouldHaveRecordRows(2);
        saveDialog.Cancel();
    }

    /// <summary>
    /// Лист новій людині; у вікні збереження — запис <paramref name="parentKind"/> через «+» і під ним з меню рядка
    /// «Neu» записи всіх п'яти типів. Для отримувача — нова персона, лист зберігається, а «Zugriffspfade» листа
    /// показує вузол «E-Mails», батьківський запис і кожен дочірній під ним.
    /// </summary>
    private void MailToNewRecordWithChildRecords(RecordKind parentKind)
    {
        TurnOffSalutationForSending();
        var mainPage = LogInAndOpenDokumentation();
        EnsureOutlookAccount(mainPage, OutlookApplication.DefaultAccountAddress());
        var customer = CustomerCase.Random();
        var mail = customer.NewMail();

        var (saveDialog, _) = SendMail(mainPage, mail);
        var parent = CreateRecord(saveDialog, customer, parentKind, parent: null);
        saveDialog.ShouldHaveRecordRows(2);
        RecordRowShouldBe(saveDialog, ParentRecordRow, parent.Label);

        var children = RecordKinds.Select(kind => CreateRecord(saveDialog, customer, kind, parent)).ToList();
        saveDialog.ShouldHaveRecordRows(2 + children.Count);
        RecordRowShouldBe(saveDialog, ParentRecordRow, parent.Label);

        CreateRecipient(saveDialog, mail);
        saveDialog.ShouldBeSavable(true);
        saveDialog.Save();

        var paths = mainPage.WaitForOpenedMail(mail.To).ReadAccessPaths();
        AllureApi.Step("Перевірити «Zugriffspfade»: «ROOT EMAIL», батьківський запис і кожен дочірній під ним", () =>
            paths.Records.ShouldBe(
                [["ROOT EMAIL"], [parent.Label], .. children.Select(child => (IReadOnlyList<string>)[child.Label, parent.Label])],
                ignoreOrder: true,
                "Записи в «Zugriffspfade»"));
    }

    /// <summary>
    /// Новий запис у вікні збереження листа: через «+» або, якщо задано <paramref name="parent"/>, з меню його рядка
    /// «Neu» (тоді заголовок вікна має назвати батька). Дані — з тієї самої справи клієнта.
    /// </summary>
    private MailRecord CreateRecord(MailSaveDialog saveDialog, CustomerCase customer, RecordKind kind, MailRecord? parent)
    {
        string? number = null;
        string name;
        string label;
        RecordDialog dialog;
        switch (kind)
        {
            case RecordKind.Project:
            {
                var projectDialog = parent is null ? saveDialog.OpenNewProject() : saveDialog.OpenNewChildProject(ParentRecordRow);
                OnCleanup(projectDialog.CancelIfOpen);
                var project = parent is null ? customer.NewProject() : customer.NewSubproject();
                projectDialog.Form.Fill(project);
                number = projectDialog.Form.Number.Text;
                (dialog, name, label) = (projectDialog, project.Name, NodeWithNumber(project.Name, number));
                break;
            }
            case RecordKind.Folder:
            {
                var folderDialog = parent is null ? saveDialog.OpenNewFolder() : saveDialog.OpenNewChildFolder(ParentRecordRow);
                OnCleanup(folderDialog.CancelIfOpen);
                var folder = customer.NewFolder();
                folderDialog.Form.Fill(folder);
                number = folderDialog.Form.Number.Text;
                (dialog, name, label) = (folderDialog, folder.Name, NodeWithNumber(folder.Name, number));
                break;
            }
            case RecordKind.Task:
            {
                var taskDialog = parent is null ? saveDialog.OpenNewTask() : saveDialog.OpenNewChildTask(ParentRecordRow);
                OnCleanup(taskDialog.CancelIfOpen);
                var task = customer.NewTask();
                taskDialog.Form.Fill(task);
                number = taskDialog.Form.Number.Text;
                (dialog, name, label) = (taskDialog, task.Name, task.Name);
                break;
            }
            case RecordKind.Appointment:
            {
                var appointmentDialog = parent is null ? saveDialog.OpenNewAppointment() : saveDialog.OpenNewChildAppointment(ParentRecordRow);
                OnCleanup(appointmentDialog.CancelIfOpen);
                var appointment = customer.NewAppointment();
                appointmentDialog.Form.Fill(appointment);
                number = appointmentDialog.Form.Number.Text;
                (dialog, name, label) = (appointmentDialog, appointment.Name, AppointmentNode(appointment));
                break;
            }
            case RecordKind.Note:
            {
                var noteDialog = parent is null ? saveDialog.OpenNewNote() : saveDialog.OpenNewChildNote(ParentRecordRow);
                OnCleanup(noteDialog.CancelIfOpen);
                var note = customer.NewNote();
                noteDialog.Form.Fill(note);
                (dialog, name, label) = (noteDialog, note.Name, NoteNode(note));
                break;
            }
            default:
                throw new ArgumentOutOfRangeException(nameof(kind), kind, null);
        }

        if (parent is not null)
        {
            var title = dialog.Title;
            AllureApi.Step($"Перевірити, що вікно нового запису називає батька: «{parent.Label}»", () =>
                title.ShouldEndWith($"(übergeordnet: {parent.Label})", Case.Sensitive, "Заголовок вікна нового запису"));
        }

        dialog.Save();
        return new MailRecord(kind, name, number, label);
    }

    private static void RecordRowShouldBe(MailSaveDialog saveDialog, int recordRow, string expected) =>
        AllureApi.Step($"Перевірити, що в рядку {recordRow + 1} вкладки «Dokumentation» — «{expected}»", () =>
            saveDialog.RecordRowLabel(recordRow).ShouldBe(expected, $"Запис у рядку {recordRow + 1} вкладки «Dokumentation»"));

    private enum RecordKind
    {
        Project,
        Folder,
        Task,
        Appointment,
        Note,
    }

    /// <summary>Запис, створений у вікні збереження листа: назва, номер (в інфозапису його немає) і підпис у списках ADAICA.</summary>
    private sealed record MailRecord(RecordKind Kind, string Name, string? Number, string Label);
}
