using ADAICA_AUTOTEST.Controls;
using ADAICA_AUTOTEST.Infrastructure;
using ADAICA_AUTOTEST.Pages;
using Allure.Net.Commons;
using Allure.Net.Commons.Attributes;

namespace ADAICA_AUTOTEST.Tests;

/// <summary>
/// Workflow на документі під вузлом «DMS – Workflows» навігатора Dokumentation: запуск із шаблону («Workflow starten»),
/// погодження кроків Prüfer по черзі й відхилення документа. Шаблон — два кроки: перший перевіряє поточний користувач,
/// другий — другий користувач тестів, який бачить своє завдання в Dashboard «Meine Aufgaben». Шаблон тест видаляє після
/// себе, а документ, папка workflow і завдання кроків, як і всі записи тестів, лишаються в базі.
/// </summary>
[AllureSuite("Workflow (DMS)")]
public partial class WorkflowTests : AdaicaTestBase
{
    private const string InWorkflowStatus = "Im Workflow-Prozess";
    private const string CurrentStatus = "aktuell";
    private const string AlreadyStartedHint = "Workflow für dieses Dokument ist bereits gestartet";

    /// <summary>
    /// Запуск workflow на документі. Меню документа під «DMS – Workflows» має «Workflow starten» з шаблонами в підменю.
    /// Вікно запуску «Workflowkonfiguration: шаблон» заповнене з шаблону: назви з « - DV1 - WF1», «Dokumentversion» — DV1,
    /// ті самі Prüfer. Після OK під документом з'являється «Ordner» → «Workflow - назва - DV1 - WF1 (OPE…)», і ADAICA її
    /// виділяє; документ — у статусі «Im Workflow-Prozess», а «Workflow starten» недоступний з підказкою «Workflow für
    /// dieses Dokument ist bereits gestartet». У папці — головне завдання («Haupt-Workflow-Aufgabe»), під ним — лише перший
    /// крок («Erste Workflow-Aufgabe», виконавець — Prüfer) і копія документа «назва - DV1 - WF1» з приміткою
    /// «Erzeugt aus der Version 1 des Entwurfes». Завдання кроку має вкладку «Workflow» з кнопками «Dokument akzeptieren»
    /// і «Dokument ablehnen», а в головного завдання її немає.
    /// </summary>
    [Test]
    [Category("Smoke")]
    [AllureSeverity(SeverityLevel.critical)]
    public void DmsWorkflows_StartWorkflowOnDocument()
    {
        var mainPage = LogInAndOpenDokumentation();
        var customer = CustomerCase.Random();
        var template = customer.NewWorkflowTemplate(mainPage.UserDisplayName, TestSettings.SecondUserContact);
        CreateTemplate(mainPage, template);
        var document = CreateDocument(mainPage, customer);

        var menu = mainPage.ReadStartWorkflowMenu(document.FileName);
        AllureApi.Step("Перевірити пункт «Workflow starten» нового документа", () => menu.ShouldSatisfyAllConditions(
            () => menu.IsEnabled.ShouldBeTrue($"«Workflow starten» недоступний: «{menu.Hint}»"),
            () => menu.Items.ShouldContain(template.Name, $"Підменю «Workflow starten»: {string.Join(" | ", menu.Items)}")));

        var dialog = mainPage.OpenStartWorkflow(document.FileName, template.Name);
        OnCleanup(dialog.CancelIfOpen);
        dialog.Title.ShouldBe(WorkflowDialog.EditTitle(template.Name), "Заголовок вікна запуску");
        dialog.Form.ShouldHaveValues(template, mainPage.UserDisplayName, documentVersion: 1);
        dialog.Confirm(StartTimeout);
        var folder = ShouldSelectWorkflowFolder(mainPage, document, template);

        ShouldBeInWorkflow(mainPage, document);
        var mainTask = template.MainTaskDisplayName(1);
        var steps = template.Steps.Select((step, index) => step.DisplayName(index + 1, 1)).ToList();
        AllureApi.Step("Перевірити головне завдання в папці workflow", () =>
        {
            var row = mainPage.OpenRecordFolder(folder, "Aufgaben").WaitForRow("Name", mainTask);
            row.ShouldSatisfyAllConditions(
                () => row["Unterklasse"].ShouldBe("Haupt-Workflow-Aufgabe", "«Unterklasse»"),
                () => row["Ausführende"].ShouldBe(mainPage.UserDisplayName, "«Ausführende»"),
                () => row["Erstellt durch"].ShouldBe(mainPage.UserDisplayName, "«Erstellt durch»"));
        });

        mainPage.OpenRecordInFolder(folder, "Aufgaben", mainTask);
        mainPage.WaitForOpenedTaskNamed(mainTask).HasWorkflowTab.ShouldBeFalse("Головне завдання має вкладку «Workflow»");
        AllureApi.Step("Перевірити, що під головним завданням видно лише перший крок", () =>
        {
            var rows = mainPage.OpenRecordFolder(mainTask, "Aufgaben").Rows;
            rows.Select(row => row["Name"]).ShouldBe([steps[0]], "Кроки під головним завданням");
            rows[0].ShouldSatisfyAllConditions(
                () => rows[0]["Unterklasse"].ShouldBe("Erste Workflow-Aufgabe", "«Unterklasse»"),
                () => rows[0]["Ausführende"].ShouldBe(mainPage.UserDisplayName, "«Ausführende»"),
                () => rows[0]["Erledigt am"].ShouldBeEmpty("«Erledigt am»"));
        });
        AllureApi.Step("Перевірити копію документа для кроків", () =>
        {
            var copy = mainPage.OpenRecordFolder(mainTask, "Dateien").WaitForRow("Dokumentname", $"{document.Name} - DV1 - WF1");
            copy.ShouldSatisfyAllConditions(
                () => copy["Bemerkung"].ShouldBe("Erzeugt aus der Version 1 des Entwurfes", "«Bemerkung»"),
                () => copy["Dateityp"].ShouldBe("docx", "«Dateityp»"));
        });

        mainPage.OpenRecordInFolder(mainTask, "Aufgaben", steps[0]);
        var step = mainPage.WaitForOpenedTaskNamed(steps[0]);
        AllureApi.Step("Перевірити завдання першого кроку", () => step.ShouldSatisfyAllConditions(
            () => step.HasWorkflowTab.ShouldBeTrue("Немає вкладки «Workflow»"),
            () => step.CanReviewDocument.ShouldBeTrue("Prüfer кроку не бачить кнопок погодження"),
            () => step.ApproveDocumentButton.IsEnabled.ShouldBeTrue($"«{step.ApproveDocumentButton.Name}» недоступна"),
            () => step.RefuseDocumentButton.IsEnabled.ShouldBeTrue($"«{step.RefuseDocumentButton.Name}» недоступна")));
    }

    /// <summary>
    /// Повний прохід: Prüfer погоджують кроки по черзі. Перший крок погоджує поточний користувач («Dokument akzeptieren» →
    /// OK): крок стає «Aufgabe erledigt», а під головним завданням з'являється другий («Letzte Workflow-Aufgabe»,
    /// виконавець — другий користувач). Поточний користувач (адміністратор workflow, але не Prüfer) кнопок погодження в
    /// другому кроці не бачить. Другий користувач знаходить крок у «Meine Aufgaben», відкриває й погоджує його. Після
    /// цього workflow завершено: обидва кроки виконано, документ знову «aktuell», а «Workflow starten» знову доступний і
    /// першим у підменю пропонує щойно завершений workflow.
    /// </summary>
    [Test]
    [AllureSeverity(SeverityLevel.critical)]
    public void DmsWorkflows_ApproveAllSteps()
    {
        var mainPage = LogInAndOpenDokumentation();
        var customer = CustomerCase.Random();
        var template = customer.NewWorkflowTemplate(mainPage.UserDisplayName, TestSettings.SecondUserContact);
        CreateTemplate(mainPage, template);
        var document = CreateDocument(mainPage, customer);
        var folder = StartWorkflow(mainPage, document, template);
        var mainTask = template.MainTaskDisplayName(1);
        var steps = template.Steps.Select((step, index) => step.DisplayName(index + 1, 1)).ToList();

        mainPage.OpenRecordInFolder(folder, "Aufgaben", mainTask);
        mainPage.OpenRecordInFolder(mainTask, "Aufgaben", steps[0]);
        mainPage.WaitForOpenedTaskNamed(steps[0]).ApproveDocument();
        AllureApi.Step("Перевірити кроки після погодження першого", () =>
        {
            var rows = WaitForSteps(mainPage, mainTask, 2);
            rows.ShouldSatisfyAllConditions(
                () => rows[0]["Name"].ShouldBe(steps[0], "Перший крок"),
                () => rows[0]["Status"].ShouldBe("Aufgabe erledigt", "«Status» першого кроку"),
                () => rows[0]["Erledigt am"].ShouldNotBeEmpty("«Erledigt am» першого кроку"),
                () => rows[1]["Name"].ShouldBe(steps[1], "Другий крок"),
                () => rows[1]["Unterklasse"].ShouldBe("Letzte Workflow-Aufgabe", "«Unterklasse» другого кроку"),
                () => rows[1]["Ausführende"].ShouldStartWith(SecondUserTaskName, Case.Sensitive, "«Ausführende» другого кроку"),
                () => rows[1]["Erledigt am"].ShouldBeEmpty("«Erledigt am» другого кроку"));
        });
        mainPage.OpenRecordInFolder(mainTask, "Aufgaben", steps[1]);
        mainPage.WaitForOpenedTaskNamed(steps[1]).CanReviewDocument
            .ShouldBeFalse("Кнопки погодження бачить адміністратор workflow, який не є Prüfer кроку");

        RestartForAnotherUser();
        ApproveAsSecondUser(steps[1]);

        RestartForAnotherUser();
        mainPage = LogInAndOpenDokumentation();
        mainPage.SelectWorkflowFolder(document.FileName, folder);
        mainPage.OpenRecordInFolder(folder, "Aufgaben", mainTask);
        AllureApi.Step("Перевірити, що обидва кроки виконано", () =>
        {
            var rows = WaitForSteps(mainPage, mainTask, 2);
            rows.ShouldAllBe(row => row["Status"] == "Aufgabe erledigt", $"Кроки: {string.Join(" | ", rows.Select(row => $"{row["Name"]}: {row["Status"]}"))}");
        });
        ShouldBeAvailableForWorkflow(mainPage, document, template);
    }

    /// <summary>
    /// Відхилення: Prüfer першого кроку натискає «Dokument ablehnen» і OK. Посеред збереження ADAICA питає причину
    /// (`wndRefuseRemark`): поле порожнє, OK недоступна, доки причини немає. Причина стає «Bemerkung» кроку, workflow
    /// зупиняється — другий крок не з'являється, документ знову «aktuell», а «Workflow starten» знову доступний.
    /// </summary>
    [Test]
    [AllureSeverity(SeverityLevel.critical)]
    public void DmsWorkflows_RefuseStep()
    {
        var mainPage = LogInAndOpenDokumentation();
        var customer = CustomerCase.Random();
        var template = customer.NewWorkflowTemplate(mainPage.UserDisplayName, TestSettings.SecondUserContact);
        var remark = customer.NewRefuseRemark();
        CreateTemplate(mainPage, template);
        var document = CreateDocument(mainPage, customer);
        var folder = StartWorkflow(mainPage, document, template);
        var mainTask = template.MainTaskDisplayName(1);
        var firstStep = template.Steps[0].DisplayName(1, 1);

        mainPage.OpenRecordInFolder(folder, "Aufgaben", mainTask);
        mainPage.OpenRecordInFolder(mainTask, "Aufgaben", firstStep);
        var step = mainPage.WaitForOpenedTaskNamed(firstStep);
        OnCleanup(step.CancelIfEditing);
        var refuse = step.RefuseDocument();
        AllureApi.Step("Перевірити вікно причини відмови", () => refuse.ShouldSatisfyAllConditions(
            () => refuse.Remark.Text.ShouldBeEmpty($"Поле «{refuse.Remark.Name}»"),
            () => refuse.OkButton.IsEnabled.ShouldBeFalse("OK доступна без причини")));
        AllureApi.AddAttachment("Вікно причини відмови", "text/plain", System.Text.Encoding.UTF8.GetBytes($"{refuse.Title}{Environment.NewLine}{refuse.Caption.Text}"), ".txt");
        refuse.Confirm(remark);
        CloseMessageIfShown(mainPage);

        AllureApi.Step("Перевірити, що workflow зупинився на першому кроці", () =>
        {
            var rows = mainPage.OpenRecordFolder(mainTask, "Aufgaben").Rows;
            rows.Select(row => row["Name"]).ShouldBe([firstStep], "Кроки під головним завданням");
            rows[0].ShouldSatisfyAllConditions(
                () => rows[0]["Bemerkung"].ShouldBe(remark, "«Bemerkung» кроку"),
                () => rows[0]["Erledigt am"].ShouldBeEmpty("«Erledigt am» кроку"));
        });
        ShouldBeAvailableForWorkflow(mainPage, document, template);
    }

    private static readonly TimeSpan StartTimeout = TimeSpan.FromSeconds(60);

    // Так завдання показує другого користувача у «Ausführende»: «Goldun, Anna # Hotel Seeblick …».
    private static string SecondUserTaskName => "Goldun, Anna";

    /// <summary>Створює шаблон у «Workflowkonfiguration» і видаляє його після тесту.</summary>
    private void CreateTemplate(MainPage mainPage, WorkflowTemplateData template) =>
        AllureApi.Step($"Створити шаблон workflow {template}", () =>
        {
            OnCleanup(() => mainPage.OpenWorkflowConfiguration().DeleteIfPresentAndClose(template.Name));
            var config = mainPage.OpenWorkflowConfiguration();
            OnCleanup(config.CancelIfOpen);
            var dialog = config.OpenNew();
            dialog.Form.Fill(template);
            dialog.Confirm();
            config.Save();
        });

    /// <summary>Документ справи з Word-файлу під «DMS – Workflows» у статусі «aktuell».</summary>
    private TestWordFile CreateDocument(MainPage mainPage, CustomerCase customer)
    {
        var file = CreateDmsDocument(mainPage, customer, MainPage.WorkflowsNodeName);
        mainPage.WaitForOpenedDocument(file.Name).Status.ShouldHaveSelected(CurrentStatus);
        return file;
    }

    /// <summary>Запускає workflow з шаблону без змін у бланку й повертає вузол папки workflow.</summary>
    private static string StartWorkflow(MainPage mainPage, TestWordFile document, WorkflowTemplateData template)
    {
        mainPage.OpenStartWorkflow(document.FileName, template.Name).Confirm(StartTimeout);
        return ShouldSelectWorkflowFolder(mainPage, document, template);
    }

    // Після запуску ADAICA виділяє папку workflow: «документ → Ordner → Workflow - назва - DV1 - WF1 (OPE…)».
    private static string ShouldSelectWorkflowFolder(
        MainPage mainPage, TestWordFile document, WorkflowTemplateData template, int documentVersion = 1) =>
        AllureApi.Step("Перевірити, що ADAICA виділила папку workflow під документом", () =>
        {
            var path = WaitForNavigatorPath(mainPage).TakeLast(3).ToList();
            path.ShouldSatisfyAllConditions(
                () => path[0].ShouldBe(document.FileName, "Документ"),
                () => path[1].ShouldBe("Ordner", "Папка під документом"),
                () => path[2].ShouldStartWith($"{template.DisplayName(documentVersion)} (OPE", Case.Sensitive, "Папка workflow"));
            return path[2];
        });

    private static void ShouldBeInWorkflow(MainPage mainPage, TestWordFile document) =>
        AllureApi.Step($"Перевірити документ у workflow: статус «{InWorkflowStatus}», «Workflow starten» недоступний", () =>
        {
            mainPage.SelectNavigatorNode(document.FileName);
            mainPage.WaitForOpenedDocument(document.Name).Status.ShouldHaveSelected(InWorkflowStatus);
            var menu = mainPage.ReadStartWorkflowMenu(document.FileName);
            menu.ShouldSatisfyAllConditions(
                () => menu.IsEnabled.ShouldBeFalse("«Workflow starten» доступний"),
                () => menu.Hint.ShouldBe(AlreadyStartedHint, "Підказка «Workflow starten»"));
        });

    // Після завершення чи відхилення: документ знову «aktuell», запустити можна знову — першим у підменю попередній workflow.
    private static void ShouldBeAvailableForWorkflow(MainPage mainPage, TestWordFile document, WorkflowTemplateData template) =>
        AllureApi.Step($"Перевірити документ після workflow: статус «{CurrentStatus}», «Workflow starten» знову доступний", () =>
        {
            mainPage.SelectNavigatorNode(document.FileName);
            mainPage.WaitForOpenedDocument(document.Name).Status.ShouldHaveSelected(CurrentStatus);
            var menu = mainPage.ReadStartWorkflowMenu(document.FileName);
            menu.ShouldSatisfyAllConditions(
                () => menu.IsEnabled.ShouldBeTrue($"«Workflow starten» недоступний: «{menu.Hint}»"),
                () => menu.Items.FirstOrDefault().ShouldBe(template.Name, $"Перший пункт підменю «Workflow starten»: {string.Join(" | ", menu.Items)}"));
        });

    // Нові кроки з'являються в списку не одразу: ADAICA розкриває їх під час збереження попереднього.
    private static IReadOnlyList<IReadOnlyDictionary<string, string>> WaitForSteps(MainPage mainPage, string mainTask, int count) =>
        Wait.For(
            () => mainPage.OpenRecordFolder(mainTask, "Aufgaben").Rows is { } rows && rows.Count == count ? rows : null,
            $"{count} кроків під головним завданням",
            TimeSpan.FromSeconds(30));

    /// <summary>
    /// Другий користувач входить у Dashboard, знаходить крок <paramref name="step"/> у «Meine Aufgaben» (крок почався
    /// сьогодні), відкриває його подвійним кліком і погоджує документ.
    /// </summary>
    private void ApproveAsSecondUser(string step) => AllureApi.Step($"Погодити крок «{step}» другим користувачем", () =>
    {
        var mainPage = LogInAndShowSingleNavigator("Dashboard", TestSettings.SecondUserName);
        var tabsBefore = mainPage.NavigatorTabNames.Count;
        OnCleanup(() => mainPage.CloseLastNavigatorTabs(keep: tabsBefore));
        var tasks = mainPage.OpenUserTaskList();
        tasks.ShowAll();
        var row = new TaskListRow(step, DateTime.Today);
        tasks.Search(step, [row]);
        tasks.OpenTask(row);
        var form = mainPage.WaitForOpenedTaskNamed(step);
        OnCleanup(form.CancelIfEditing);
        form.CanReviewDocument.ShouldBeTrue("Prüfer кроку не бачить кнопок погодження");
        form.ApproveDocument();
    });

    // Після відмови ADAICA відкриває лист адміністратору workflow, а без поштового клієнта показує повідомлення.
    private static void CloseMessageIfShown(MainPage mainPage) =>
        AllureApi.Step("Закрити повідомлення ADAICA після відмови, якщо воно є", () =>
        {
            var message = FlaUI.Core.Tools.Retry.WhileNull(() => mainPage.FindMessageBox(), TimeSpan.FromSeconds(5), ignoreException: true).Result;
            if (message is null)
                return;

            AllureApi.AddAttachment("Повідомлення після відмови", "text/plain", System.Text.Encoding.UTF8.GetBytes($"{message.Title}: {message.Message.Text}"), ".txt");
            message.Button("OK").Click();
        });
}
