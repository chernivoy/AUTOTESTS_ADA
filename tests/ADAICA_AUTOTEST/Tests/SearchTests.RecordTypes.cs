using ADAICA_AUTOTEST.Pages;
using Allure.Net.Commons;
using Allure.Net.Commons.Attributes;

namespace ADAICA_AUTOTEST.Tests;

/// <summary>
/// Пошук кожного типу запису, який уміють створювати тести: проєкт, Ordner, термін, інфозапис (завдання —
/// у решті тестів класу). Дані — з випадкової справи клієнта (<see cref="CustomerCase"/>).
/// </summary>
public partial class SearchTests
{
    public enum RecordType
    {
        Project,
        Folder,
        Appointment,
        Note,
    }

    /// <summary>
    /// Запис знаходиться за приміткою й назвою, скопійованими повністю («Exakte Suche»), і за номером; у рядку
    /// результату — правильні «Klassentyp», «Fundstelle», назва й примітка. В інфозапису номера немає, тож його
    /// впізнаємо за «Bemerkung».
    /// </summary>
    [TestCase(RecordType.Project, "PROJECT")]
    [TestCase(RecordType.Folder, "OPERATION")]
    [TestCase(RecordType.Appointment, "APPOINT")]
    [TestCase(RecordType.Note, "HISTORY")]
    [AllureSeverity(SeverityLevel.critical)]
    public void Finden_RecordTypes(RecordType type, string classType)
    {
        var mainPage = LogInAndOpenDokumentation();
        var (number, name, remark) = CreateRecord(mainPage, type);
        var search = OpenSearch(mainPage);

        // Пошук за приміткою чекає, доки Elasticsearch проіндексує новий запис.
        var byRemark = number is null
            ? search.WaitUntilFoundWithRemark(remark, remark, SearchMode.ExactPhrase, wildcardAfter: false)
            : search.WaitUntilFound(remark, number, SearchMode.ExactPhrase, wildcardAfter: false);
        var byName = AllureApi.Step($"Знайти запис за назвою «{name}»", () =>
        {
            search.FindRecords(name, SearchMode.ExactPhrase, wildcardAfter: false);
            return number is null ? search.ResultWithRemark(remark) : search.ResultFor(number);
        });
        var byNumber = number is null
            ? null
            : AllureApi.Step($"Знайти запис за номером {number}", () =>
            {
                search.FindRecords(number, wildcardAfter: false);
                return search.ResultFor(number);
            });

        AllureApi.Step("Перевірити знайдені рядки", () => byRemark.ShouldSatisfyAllConditions(
            () => byRemark.ClassType.ShouldBe(classType, "Klassentyp"),
            () => byRemark.Name.ShouldBe(name, "Betreff/Name"),
            () => byRemark.Remark.ShouldBe(remark, "Bemerkung"),
            () => byRemark.FoundIn.ShouldBe(InRemark, "Fundstelle для пошуку за приміткою"),
            () => byName.ShouldNotBeNull("Пошук за назвою не знайшов запис").FoundIn.ShouldBe(InName, "Fundstelle для пошуку за назвою"),
            () =>
            {
                if (number is not null)
                    byNumber.ShouldNotBeNull("Пошук за номером не знайшов запис").FoundIn.ShouldBe(InNumber, "Fundstelle для пошуку за номером");
            }));
    }

    /// <summary>Створює запис і повертає його номер (в інфозапису — null), назву й примітку.</summary>
    private (string? Number, string Name, string Remark) CreateRecord(MainPage mainPage, RecordType type)
    {
        var customer = CustomerCase.Random();
        switch (type)
        {
            case RecordType.Project:
            {
                var project = customer.NewProject();
                var dialog = mainPage.OpenNewProjectDialog();
                OnCleanup(dialog.CancelIfOpen);
                dialog.Form.Fill(project);
                var number = dialog.Form.Number.Text;
                dialog.Save();
                mainPage.WaitForOpenedProject(number);
                return (number, project.Name, project.Comment);
            }

            case RecordType.Folder:
            {
                var folder = customer.NewFolder();
                var dialog = mainPage.OpenNewFolderDialog();
                OnCleanup(dialog.CancelIfOpen);
                dialog.Form.Fill(folder);
                var number = dialog.Form.Number.Text;
                dialog.Save();
                mainPage.WaitForOpenedFolder(number);
                return (number, folder.Name, folder.Comment);
            }

            case RecordType.Appointment:
            {
                var appointment = customer.NewAppointment();
                var dialog = mainPage.OpenNewAppointmentDialog();
                OnCleanup(dialog.CancelIfOpen);
                dialog.Form.Fill(appointment);
                var number = dialog.Form.Number.Text;
                dialog.Save();
                mainPage.WaitForOpenedAppointment(number);
                return (number, appointment.Name, appointment.Remark);
            }

            case RecordType.Note:
            {
                var note = customer.NewNote();
                var dialog = mainPage.OpenNewNoteDialog();
                OnCleanup(dialog.CancelIfOpen);
                dialog.Form.Fill(note);
                dialog.Save();
                mainPage.WaitForOpenedNote(note.Remark);
                return (null, note.Name, note.Remark);
            }

            default:
                throw new ArgumentOutOfRangeException(nameof(type), type, null);
        }
    }
}
