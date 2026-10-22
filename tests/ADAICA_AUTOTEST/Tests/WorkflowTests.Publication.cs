using System.Runtime.InteropServices;
using ADAICA_AUTOTEST.Infrastructure;
using ADAICA_AUTOTEST.Pages;
using Allure.Net.Commons;
using Allure.Net.Commons.Attributes;

namespace ADAICA_AUTOTEST.Tests;

/// <summary>
/// Що прапорці шаблону дають на документі: «Nach dem Start in PDF umwandeln» (копія для кроків — PDF, лише для
/// Word-документа), публікація після кроку в Ordner під «DMS – Veröffentlichungen», «Veröffentlichen als aktuelle Version
/// einer früheren Veröffentlichung» і публікація на документ (нова версія цього документа), а також версія документа
/// («DV2» після редагування) і перемикачі «Name des übergeordneten Elements vorausfüllen» у вікні запуску. Шаблон —
/// один крок, який погоджує поточний користувач.
/// </summary>
public partial class WorkflowTests
{
    private const string PublishedStatus = "Veröffentlicht";
    private const string ApprovedStatus = "Genehmigt";

    /// <summary>
    /// PDF, публікація в Ordner і розсилка в чат. Вікно запуску показує прапорці шаблону; «als aktuelle Version» у ньому
    /// немає, бо документ ще не публікували. Копія документа для кроку — «назва - DV1 - WF1» типу «pdf». Коли Prüfer
    /// погодив крок, копія стає «Genehmigt», а в Ordner публікацій з'являється документ «назва - DV1 - WF1» (pdf, підклас
    /// «Workflow», статус «Veröffentlicht»). Сам документ знову «aktuell», і його можна запускати знову. Другий
    /// користувач, контакт «In Chat verteilen», бачить у «Meine Chats» чат із назвою workflow.
    /// </summary>
    [Test]
    [AllureSeverity(SeverityLevel.critical)]
    public void DmsWorkflows_PublishPdfToFolder()
    {
        var mainPage = LogInAndOpenDokumentation();
        var customer = CustomerCase.Random();
        var folder = CreatePublicationFolder(mainPage, customer);
        var publication = new WorkflowPublicationData([folder], Chat: true) { ChatContacts = [TestSettings.SecondUserContact] };
        var template = PublishingTemplate(mainPage, customer, publication) with { ConvertToPdf = true };
        CreateTemplate(mainPage, template);
        var document = CreateDocument(mainPage, customer);

        var dialog = mainPage.OpenStartWorkflow(document.FileName, template.Name);
        OnCleanup(dialog.CancelIfOpen);
        dialog.Form.ShouldHaveValues(template, mainPage.UserDisplayName, documentVersion: 1);
        AllureApi.Step("Перевірити, що «als aktuelle Version» немає: документ ще не публікували", () =>
            dialog.Form.HasAsActualVersion.ShouldBeFalse($"Є прапорець «{dialog.Form.AsActualVersion.Name}»"));
        dialog.Confirm(StartTimeout);
        var workflowFolder = ShouldSelectWorkflowFolder(mainPage, document, template);

        var mainTask = template.MainTaskDisplayName(1);
        var copy = $"{document.Name} - DV1 - WF1";
        mainPage.OpenRecordInFolder(workflowFolder, "Aufgaben", mainTask);
        ShouldHaveDocument(mainPage, mainTask, "Dateien", copy, "pdf", CurrentStatus);
        ApproveStep(mainPage, mainTask, template.Steps[0].DisplayName(1, 1));
        ShouldHaveDocument(mainPage, mainTask, "Dateien", copy, "pdf", ApprovedStatus);

        mainPage.SelectPublicationFolder(folder);
        var published = ShouldHaveDocument(mainPage, folder, "Dateien", copy, "pdf", PublishedStatus);
        published["Unterklasse"].ShouldBe("Workflow", "«Unterklasse» опублікованого документа");
        ShouldBeAvailableForWorkflow(mainPage, document, template);

        RestartForAnotherUser();
        AllureApi.Step("Перевірити, що другий користувач (контакт «In Chat verteilen») має чат публікації", () =>
        {
            var dashboard = LogInAndShowSingleNavigator("Dashboard", TestSettings.SecondUserName);
            var tabsBefore = dashboard.NavigatorTabNames.Count;
            OnCleanup(() => dashboard.CloseLastNavigatorTabs(keep: tabsBefore));
            dashboard.OpenUserChats().ShouldShow(template.Name, "Aktiv");
        });
    }

    /// <summary>
    /// «als aktuelle Version»: документ публікують двічі в той самий Ordner. Перший workflow дає опублікований документ
    /// «назва - DV1 - WF1» (docx, версія 1 з 1). Повторний запуск з того самого шаблону вже показує «als aktuelle
    /// Version» (вимкнений, як у шаблоні); тест його вмикає, і після погодження в Ordner так само один документ, але з
    /// двома версіями.
    /// </summary>
    [Test]
    [AllureSeverity(SeverityLevel.critical)]
    public void DmsWorkflows_PublishAsActualVersion()
    {
        var mainPage = LogInAndOpenDokumentation();
        var customer = CustomerCase.Random();
        var folder = CreatePublicationFolder(mainPage, customer);
        var template = PublishingTemplate(mainPage, customer, new WorkflowPublicationData([folder]));
        CreateTemplate(mainPage, template);
        var document = CreateDocument(mainPage, customer);
        var mainTask = template.MainTaskDisplayName(1);
        var step = template.Steps[0].DisplayName(1, 1);
        var published = $"{document.Name} - DV1 - WF1";

        var workflowFolder = StartWorkflow(mainPage, document, template);
        mainPage.OpenRecordInFolder(workflowFolder, "Aufgaben", mainTask);
        ApproveStep(mainPage, mainTask, step);
        mainPage.SelectPublicationFolder(folder);
        ShouldHaveDocument(mainPage, folder, "Dateien", published, "docx", PublishedStatus);
        mainPage.OpenDocumentInFolder(folder, "Dateien", published).Version.ShouldBe("1 / 1", "Версія опублікованого документа");

        var dialog = mainPage.OpenStartWorkflow(document.FileName, template.Name);
        OnCleanup(dialog.CancelIfOpen);
        dialog.Form.ShowStep(1);
        AllureApi.Step("Перевірити «als aktuelle Version» у вікні запуску документа, який уже публікували", () => dialog.Form.ShouldSatisfyAllConditions(
            () => dialog.Form.HasAsActualVersion.ShouldBeTrue($"Немає прапорця «{dialog.Form.AsActualVersion.Name}»"),
            () => dialog.Form.AsActualVersion.IsChecked.ShouldBeFalse($"Прапорець «{dialog.Form.AsActualVersion.Name}»"),
            () => dialog.Form.AsActualVersion.IsEnabled.ShouldBeTrue($"Прапорець «{dialog.Form.AsActualVersion.Name}» недоступний")));
        dialog.Form.AsActualVersion.SetChecked(true);
        dialog.Confirm(StartTimeout);
        workflowFolder = ShouldSelectWorkflowFolder(mainPage, document, template);
        mainPage.OpenRecordInFolder(workflowFolder, "Aufgaben", mainTask);
        ApproveStep(mainPage, mainTask, step);

        mainPage.SelectPublicationFolder(folder);
        AllureApi.Step("Перевірити, що в Ordner публікацій той самий документ, тепер з двома версіями", () =>
        {
            var rows = WaitForDocumentRows(mainPage, folder, "Dateien", rows => rows.Count > 0);
            rows.Select(row => row["Dokumentname"]).ShouldBe([published], "Документи в Ordner публікацій");
            mainPage.OpenDocumentInFolder(folder, "Dateien", published).Version.ShouldBe("2 / 2", "Версія опублікованого документа");
        });
    }

    /// <summary>
    /// Публікація на документ: шляхом публікації шаблону вибрано документ під «DMS – Veröffentlichungen» (вкладених
    /// записів Ordner дерево шляхів не показує), тож «als aktuelle Version» увімкнено й заблоковано. Після погодження
    /// кроку новий документ не з'являється: погоджений файл стає версією 2 вибраного документа, а той — «Veröffentlicht».
    /// Ім'я документ бере з нового файлу: «назва документа workflow - DV1 - WF1» (21.10.2026; так само, як версія з
    /// файлу, — питання до розробників, тож тест приймає обидві назви й показує фактичну у звіті).
    /// </summary>
    [Test]
    public void DmsWorkflows_PublishAsVersionOfDocument()
    {
        var mainPage = LogInAndOpenDokumentation();
        var customer = CustomerCase.Random();
        const string root = MainPage.PublicationsNodeName;
        var target = CreateDmsDocument(mainPage, customer, root);
        var template = PublishingTemplate(mainPage, customer, new WorkflowPublicationData([target.FileName], AsActualVersion: true));
        CreateTemplate(mainPage, template);
        var document = CreateDocument(mainPage, customer);
        var mainTask = template.MainTaskDisplayName(1);

        var dialog = mainPage.OpenStartWorkflow(document.FileName, template.Name);
        OnCleanup(dialog.CancelIfOpen);
        dialog.Form.ShowStep(1);
        AllureApi.Step("Перевірити крок: шлях — документ, «als aktuelle Version» заблоковано", () => dialog.Form.ShouldSatisfyAllConditions(
            () => dialog.Form.Publication.LinkedPaths.ShouldBe([target.FileName], "Шлях публікації"),
            () => dialog.Form.HasAsActualVersion.ShouldBeFalse($"Прапорець «{dialog.Form.AsActualVersion.Name}» видно, хоча документ не публікували")));
        dialog.Confirm(StartTimeout);
        var workflowFolder = ShouldSelectWorkflowFolder(mainPage, document, template);
        mainPage.OpenRecordInFolder(workflowFolder, "Aufgaben", mainTask);
        ApproveStep(mainPage, mainTask, template.Steps[0].DisplayName(1, 1));

        mainPage.SelectNavigatorNode(root);
        AllureApi.Step("Перевірити, що погоджений файл став новою версією вибраного документа", () =>
        {
            // Нова версія з файлу дає документу ім'я цього файлу, тож вибраний документ може називатися вже як копія.
            var published = $"{document.Name} - DV1 - WF1";
            bool IsTarget(IReadOnlyDictionary<string, string> row) => row["Dokumentname"] == target.Name || row["Dokumentname"] == published;
            var rows = WaitForDocumentRows(mainPage, root, "Dateien", rows => rows.Any(row => IsTarget(row) && row["Status"] == PublishedStatus));
            var targets = rows.Where(IsTarget).Select(row => row["Dokumentname"]).ToList();
            AllureApi.AddAttachment("Документ публікації", "text/plain", System.Text.Encoding.UTF8.GetBytes(string.Join(Environment.NewLine, targets)), ".txt");
            targets.Count.ShouldBe(1, $"Вибраний документ і окремий опублікований: {string.Join(" | ", targets)}");
            mainPage.OpenDocumentInFolder(root, "Dateien", targets[0]).Version.ShouldBe("2 / 2", "Версія вибраного документа");
        });
    }

    /// <summary>
    /// «Nach dem Start in PDF umwandeln» — лише для Word-документа: на текстовому файлі вікно запуску показує прапорець
    /// знятим і недоступним, хоча в шаблоні його ввімкнено.
    /// </summary>
    [Test]
    public void DmsWorkflows_PdfOnlyForWordDocuments()
    {
        var mainPage = LogInAndOpenDokumentation();
        var customer = CustomerCase.Random();
        var plain = customer.NewWorkflowTemplate(mainPage.UserDisplayName, TestSettings.SecondUserContact);
        var template = plain with { ConvertToPdf = true, Steps = [plain.Steps[0]] };
        CreateTemplate(mainPage, template);
        var text = CreateTextDocument(mainPage, customer);

        var dialog = mainPage.OpenStartWorkflow(Path.GetFileName(text), template.Name);
        OnCleanup(dialog.CancelIfOpen);
        dialog.Form.ShowMainTab();
        AllureApi.Step("Перевірити «PDF» для текстового документа", () => dialog.Form.ShouldSatisfyAllConditions(
            () => dialog.Form.ConvertToPdf.IsChecked.ShouldBeFalse($"Прапорець «{dialog.Form.ConvertToPdf.Name}» увімкнено"),
            () => dialog.Form.ConvertToPdf.IsEnabled.ShouldBeFalse($"Прапорець «{dialog.Form.ConvertToPdf.Name}» доступний")));
        dialog.Cancel();
    }

    /// <summary>
    /// Версія документа в назвах і перемикачі назв. Документ отримав другу актуальну версію з файлу (меню документа →
    /// версії → «als aktuelle Version» → з файлової системи; документ при цьому бере ім'я нового файлу), тож вікно запуску показує
    /// «Dokumentversion» DV2 і назви з « - DV2 - WF1». Перемикач «Name des übergeordneten Elements vorausfüllen» біля
    /// «Workflowname», назви головного завдання й назви кроку підставляє назву документа, вимкнений — повертає назву з
    /// шаблону. Після запуску папка workflow — «… - DV2 - WF1», а копія документа — «назва - DV2 - WF1» з приміткою
    /// «Erzeugt aus der Version 2 des Entwurfes».
    /// </summary>
    [Test]
    public void DmsWorkflows_DocumentVersionInNames()
    {
        var mainPage = LogInAndOpenDokumentation();
        var customer = CustomerCase.Random();
        var plain = customer.NewWorkflowTemplate(mainPage.UserDisplayName, TestSettings.SecondUserContact);
        var template = plain with { Steps = [plain.Steps[0]] };
        CreateTemplate(mainPage, template);
        var original = CreateDocument(mainPage, customer);

        // Версія 2 — з іншого файлу: «Bearbeiten» потребує Word, якого на тестовій машині може не бути. Документ після
        // цього називається, як новий файл.
        var document = TestWordFile.CreateDocx(customer.NewWordFile());
        OnCleanup(document.Dispose);
        mainPage.AddActualVersionFromFile(original.FileName, document.Path);
        mainPage.WaitForOpenedDocument(document.Name).WaitForVersionCount(2);

        var dialog = mainPage.OpenStartWorkflow(document.FileName, template.Name);
        OnCleanup(dialog.CancelIfOpen);
        dialog.Form.ShouldHaveValues(template, mainPage.UserDisplayName, documentVersion: 2);

        var fromDocument = template with
        {
            Name = document.Name,
            MainTaskName = document.Name,
            Steps = [template.Steps[0] with { Name = document.Name }],
        };
        dialog.Form.ShowMainTab();
        dialog.Form.UseDocumentName(DocumentNameField.Workflow, true);
        dialog.Form.UseDocumentName(DocumentNameField.MainTask, true);
        dialog.Form.ShowStep(1);
        dialog.Form.UseDocumentName(DocumentNameField.Step, true);
        dialog.Form.ShouldHaveValues(fromDocument, mainPage.UserDisplayName, documentVersion: 2);

        dialog.Form.ShowMainTab();
        dialog.Form.UseDocumentName(DocumentNameField.Workflow, false);
        dialog.Form.UseDocumentName(DocumentNameField.MainTask, false);
        dialog.Form.ShowStep(1);
        dialog.Form.UseDocumentName(DocumentNameField.Step, false);
        dialog.Form.ShouldHaveValues(template, mainPage.UserDisplayName, documentVersion: 2);

        dialog.Confirm(StartTimeout);
        var workflowFolder = ShouldSelectWorkflowFolder(mainPage, document, template, documentVersion: 2);
        var mainTask = template.MainTaskDisplayName(2);
        mainPage.OpenRecordInFolder(workflowFolder, "Aufgaben", mainTask);
        var copy = ShouldHaveDocument(mainPage, mainTask, "Dateien", $"{document.Name} - DV2 - WF1", "docx", CurrentStatus);
        copy["Bemerkung"].ShouldBe("Erzeugt aus der Version 2 des Entwurfes", "«Bemerkung» копії документа");
    }

    /// <summary>Шаблон з одним кроком (Prüfer — поточний користувач), після якого документ публікується.</summary>
    private static WorkflowTemplateData PublishingTemplate(MainPage mainPage, CustomerCase customer, WorkflowPublicationData publication)
    {
        var plain = customer.NewWorkflowTemplate(mainPage.UserDisplayName, TestSettings.SecondUserContact);
        return plain with { Steps = [plain.Steps[0] with { Publication = publication }] };
    }

    /// <summary>Prüfer (поточний користувач) відкриває крок <paramref name="step"/> під головним завданням і погоджує документ.</summary>
    private static void ApproveStep(MainPage mainPage, string mainTask, string step)
    {
        mainPage.OpenRecordInFolder(mainTask, "Aufgaben", step);
        mainPage.WaitForOpenedTaskNamed(step).ApproveDocument();
    }

    /// <summary>
    /// Чекає документ <paramref name="name"/> зі статусом <paramref name="status"/> у списку вузла
    /// <paramref name="folderName"/> під <paramref name="recordNode"/> (статус ADAICA оновлює після збереження кроку, тож
    /// список відкривається знову) і перевіряє його тип.
    /// </summary>
    private static IReadOnlyDictionary<string, string> ShouldHaveDocument(
        MainPage mainPage, string recordNode, string folderName, string name, string fileType, string status) =>
        AllureApi.Step($"Перевірити документ «{name}» у {folderName} під «{recordNode}»: {fileType}, «{status}»", () =>
        {
            var row = WaitForDocumentRows(mainPage, recordNode, folderName, rows => rows.Any(row => row["Dokumentname"] == name && row["Status"] == status))
                .Single(row => row["Dokumentname"] == name);
            row["Dateityp"].ShouldBe(fileType, $"«Dateityp» документа «{name}»");
            return row;
        });

    private static IReadOnlyList<IReadOnlyDictionary<string, string>> WaitForDocumentRows(
        MainPage mainPage, string recordNode, string folderName, Func<IReadOnlyList<IReadOnlyDictionary<string, string>>, bool> condition)
    {
        IReadOnlyList<IReadOnlyDictionary<string, string>> rows = [];
        Wait.Until(
            () => condition(rows = mainPage.OpenRecordFolder(recordNode, folderName).Rows),
            $"документів у {folderName} під «{recordNode}»",
            TimeSpan.FromSeconds(40));
        return rows;
    }

    /// <summary>Текстовий документ справи під «DMS – Workflows»: повертає шлях до файлу (файл прибирається після тесту).</summary>
    private string CreateTextDocument(MainPage mainPage, CustomerCase customer) =>
        AllureApi.Step($"Створити текстовий документ під «{MainPage.WorkflowsNodeName}»", () =>
        {
            var content = customer.NewWordFile();
            var path = Path.Combine(Path.GetTempPath(), "ADAICA_AUTOTEST", $"{content.FileName}.txt");
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllLines(path, content.Paragraphs.Prepend(content.Title));
            OnCleanup(() =>
            {
                try { File.Delete(path); } catch (IOException) { }
            });
            mainPage.NewChildDocumentFromFile(MainPage.WorkflowsNodeName, path);
            mainPage.WaitForOpenedDocument(Path.GetFileNameWithoutExtension(path));
            return path;
        });

    // Поки ADAICA перетворює документ на PDF, головне вікно не відповідає UI Automation.
    private static IReadOnlyList<string> WaitForNavigatorPath(MainPage mainPage) =>
        Wait.For(
            () =>
            {
                try
                {
                    return mainPage.SelectedNavigatorNodePath;
                }
                catch (Exception ex) when (ex is TimeoutException or COMException)
                {
                    return null;
                }
            },
            "дерева навігатора після запуску workflow",
            StartTimeout);
}
