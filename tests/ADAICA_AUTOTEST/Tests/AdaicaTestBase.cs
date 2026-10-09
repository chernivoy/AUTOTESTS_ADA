using System.Globalization;
using ADAICA_AUTOTEST.Controls;
using ADAICA_AUTOTEST.Infrastructure;
using ADAICA_AUTOTEST.Pages;
using Allure.Net.Commons;

namespace ADAICA_AUTOTEST.Tests;

/// <summary>
/// Базовий клас тестів ADAICA: після тесту виконує зареєстроване прибирання і завершує застосунок
/// з підтвердженням виходу, щоб коректно закрити сесію, а не зупиняти процес примусово.
/// </summary>
public abstract partial class AdaicaTestBase : UiTestBase
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
    /// Входить в ADAICA, ставить опції пошти, які повертає <paramref name="change"/> з поточних, і перезапускає ADAICA:
    /// опції діють лише після перезапуску. Після тесту попередні опції повертаються (без перезапуску: тест на той час
    /// уже закінчено). Далі тест входить сам, наприклад <see cref="LogInAndOpenDokumentation"/>.
    /// </summary>
    protected void SetMailOptionsAndRestart(Func<MailOptions, MailOptions> change) =>
        AllureApi.Step("Змінити опції пошти й перезапустити ADAICA", () =>
        {
            var mainPage = new LoginPage(Session).LoginAs(
                TestSettings.UserName, TestSettings.Password, TestSettings.Database, TestSettings.Schema);
            var window = mainPage.OpenOptions().OpenMailOptions();
            var before = window.ReadMailOptions();
            OnCleanup(() => RestoreMailOptions(before));
            window.SetMailOptions(change(before));
            RestartApplication(() => new AdaicaWindow(Session).Exit());
        });

    /// <summary>
    /// Повертає опції пошти. Частина з них системна, тож лишати їх зміненими не можна: якщо ADAICA після падіння
    /// тесту не відповідає, тест перезапускає її, входить і пробує ще раз.
    /// </summary>
    private void RestoreMailOptions(MailOptions options) =>
        AllureApi.Step($"Повернути опції пошти: {options}", () =>
        {
            try
            {
                SetMailOptions(new MainPage(Session), options);
            }
            catch (Exception ex)
            {
                TestContext.Out.WriteLine($"Опції пошти не повернуто з першої спроби, ADAICA буде перезапущено: {ex.Message}");
                RestartApplication(() =>
                {
                    try
                    {
                        new AdaicaWindow(Session).Exit();
                    }
                    catch (Exception exitError)
                    {
                        TestContext.Out.WriteLine($"ADAICA не закрилася коректно, процес буде зупинено: {exitError.Message}");
                    }
                });
                SetMailOptions(
                    new LoginPage(Session).LoginAs(TestSettings.UserName, TestSettings.Password, TestSettings.Database, TestSettings.Schema),
                    options);
            }
        });

    private static void SetMailOptions(MainPage mainPage, MailOptions options)
    {
        var window = mainPage.OpenOptions().OpenMailOptions();
        if (window.ReadMailOptions() == options)
            window.Close();
        else
            window.SetMailOptions(options);
    }

    /// <summary>
    /// Входить в ADAICA й показує навігатор <paramref name="navigatorName"/>, який може бути відкритий лише в одній
    /// вкладці (як Dashboard: у списку «+» відкритий Dashboard недоступний). Якщо вкладка вже є, перемикається на неї,
    /// а після тесту повертає вкладку, яка була активною; інакше відкриває нову й після тесту закриває її.
    /// </summary>
    protected MainPage LogInAndShowSingleNavigator(string navigatorName)
    {
        var mainPage = new LoginPage(Session).LoginAs(
            TestSettings.UserName, TestSettings.Password, TestSettings.Database, TestSettings.Schema);
        if (!mainPage.NavigatorTabNames.Contains(navigatorName))
        {
            var tabsBefore = mainPage.NavigatorTabNames;
            OnCleanup(() => mainPage.CloseLastNavigatorTabs(keep: tabsBefore.Count));
            mainPage.OpenNavigator(navigatorName);
            return mainPage;
        }

        var activeBefore = mainPage.NavigatorTitle.Text;
        if (activeBefore != navigatorName && mainPage.NavigatorTabNames.Contains(activeBefore))
            OnCleanup(() => mainPage.SwitchToNavigatorTab(activeBefore));

        mainPage.SwitchToNavigatorTab(navigatorName);
        return mainPage;
    }

    /// <summary>
    /// Створює з контекстного меню вузла <paramref name="parentNode"/> (будь-якого запису) проєкт, Ordner, завдання,
    /// термін, інфозапис і документ із Word-файлу. Кожен збережений запис ADAICA має виділити в дереві під батьківським
    /// вузлом, у папці свого типу: «Батько → Aufgaben → Завдання», документ — «Батько → Dateien → файл.docx».
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

        CreateDocumentFromWordFile(mainPage, parentNode, customer.NewWordFile(), customer.Class);
    }

    /// <summary>
    /// Створює під вузлом <paramref name="parentNode"/> документ із Word-файлу <paramref name="content"/>, який тест
    /// складає сам, і перевіряє картку документа (назва — ім'я файлу без розширення, тип «docx», розмір як у файлу,
    /// версія 1 з 1, статус «aktuell», автор — поточний користувач, дата створення — час тесту), а в дереві — що
    /// документ виділено під батьківським вузлом у «Dateien». Файл прибирається після тесту. Документ ADAICA зберігає
    /// одразу, без вікна, тож клас <paramref name="recordClass"/> (якщо задано) тест вибирає вже в огляді й зберігає
    /// кнопкою OK разом із Kurzname «MAT».
    /// </summary>
    protected CreatedDocument CreateDocumentFromWordFile(
        MainPage mainPage, string parentNode, WordFileContent content, RecordClass? recordClass = null)
    {
        var file = TestWordFile.CreateDocx(content);
        OnCleanup(file.Dispose);
        AllureApi.AddAttachment(file.FileName, "application/vnd.openxmlformats-officedocument.wordprocessingml.document", file.Path);

        var createdFrom = DateTime.Now;
        mainPage.NewChildDocumentFromFile(parentNode, file.Path);
        var document = mainPage.WaitForOpenedDocument(file.Name);
        var createdTo = DateTime.Now;

        document.ShouldHaveValues(new DocumentData(
            Name: file.Name,
            ShortName: string.Empty,
            FileType: "docx",
            FileSize: AdaicaFormat.FormatFileSize(file.Length),
            Creator: mainPage.UserDisplayName,
            Version: "1 / 1",
            Status: "aktuell",
            Remark: string.Empty));
        document.ShouldBeCreatedBetween(createdFrom, createdTo);
        ShouldBeSelectedUnder(mainPage, parentNode, "Dateien", file.FileName);

        if (recordClass is not null)
        {
            OnCleanup(document.CancelIfEditing);
            document.SetClass(recordClass, CustomerCase.ShortName);
            document.ShouldHaveValues(new DocumentData(
                Name: file.Name,
                ShortName: CustomerCase.ShortName,
                FileType: "docx",
                FileSize: AdaicaFormat.FormatFileSize(file.Length),
                Creator: mainPage.UserDisplayName,
                Version: "1 / 1",
                Status: "aktuell",
                Remark: string.Empty,
                Class: recordClass));
            ShouldBeSelectedUnder(mainPage, parentNode, "Dateien", file.FileName);
        }

        return new CreatedDocument(file, document, createdFrom, createdTo);
    }

    /// <summary>Документ, створений <see cref="CreateDocumentFromWordFile"/>, і проміжок часу, коли його створено.</summary>
    protected sealed record CreatedDocument(TestWordFile File, DocumentForm Form, DateTime CreatedFrom, DateTime CreatedTo);

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

    // «дд.ММ.рр Назва», а з класом — ще «(Клас - Підклас)» у кінці.
    protected static string NoteNode(NoteData note) =>
        $"{note.Begin.ToString("dd.MM.yy", CultureInfo.InvariantCulture)} {note.Name}"
        + (note.Class is { } recordClass ? $" ({recordClass.Class} - {recordClass.SubClass})" : string.Empty);

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
