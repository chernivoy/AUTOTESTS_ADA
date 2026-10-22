using ADAICA_AUTOTEST.Infrastructure;
using ADAICA_AUTOTEST.Pages;
using Allure.Net.Commons;
using Allure.Net.Commons.Attributes;

namespace ADAICA_AUTOTEST.Tests;

/// <summary>
/// Прапорці шаблону workflow: «Nach dem Start in PDF umwandeln» на вкладці «Workflow», «Nach diesem Schritt
/// veröffentlichen und verteilen» і «Veröffentlichen als aktuelle Version einer früheren Veröffentlichung» на вкладці
/// кроку, а також панель публікації під вкладками — шлях публікації («Verknüpft» у дереві «DMS – Veröffentlichungen»)
/// і розсилка («In Chat verteilen», «Per Mail verteilen»).
/// </summary>
public partial class WorkflowConfigTests
{
    private const string FolderAlreadySelectedMessage =
        "Nach der Auswahl eines Dokumentationselementes (z.B. eines Ordners, eines Projektes) als Ziel für die Veröffentlichung "
        + "ist die gleichzeitige Veröffentlichung auf ein Dokument nicht möglich. Bitte ändern Sie Ihre Auswahl.";

    private const string OnlyOneDocumentMessage = "Es kann nur ein Dokument als Ziel für die Veröffentlichung ausgewählt werden.";

    private const string DocumentAlreadySelectedMessage =
        "Nach der Auswahl eines Dokumentes ist die gleichzeitige Veröffentlichung in einen anderen Dokumentationselement "
        + "(z.B. in einen Ordner, ein Projekt) nicht möglich. Bitte ändern Sie Ihre Auswahl.";

    /// <summary>
    /// Як прапорці кроку залежать один від одного. Без публікації «als aktuelle Version» недоступний, панелі публікації
    /// немає. «veröffentlichen» на кроці 1 показує панель (шлях — корінь «DMS – Veröffentlichungen», позначений за
    /// замовчуванням) і відкриває «als aktuelle Version»; на кроці 2 «veröffentlichen» тоді недоступний, а підказка
    /// називає крок 1. На «Verteilung» чат і e-mail вимкнені, їхні «...» і текст повідомлення недоступні; кожен
    /// прапорець відкриває свою «...» і текст. Зняте «veröffentlichen» ховає панель і знову відкриває прапорець кроку 2,
    /// який забирає публікацію собі.
    /// </summary>
    [Test]
    public void Workflowkonfiguration_PublishCheckboxes()
    {
        var mainPage = LogIn();
        var template = CustomerCase.Random().NewWorkflowTemplate(mainPage.UserDisplayName, TestSettings.SecondUserContact);
        var config = mainPage.OpenWorkflowConfiguration();
        OnCleanup(config.CancelIfOpen);
        var dialog = config.OpenNew();
        OnCleanup(dialog.CancelIfOpen);
        var form = dialog.Form;
        var publication = form.Publication;
        form.Fill(template);

        form.ShowStep(1);
        AllureApi.Step("Перевірити прапорці кроку без публікації", () => form.ShouldSatisfyAllConditions(
            () => form.Publish.IsChecked.ShouldBeFalse($"Прапорець «{form.Publish.Name}»"),
            () => form.Publish.IsEnabled.ShouldBeTrue($"Прапорець «{form.Publish.Name}» недоступний"),
            () => form.AsActualVersion.IsChecked.ShouldBeFalse($"Прапорець «{form.AsActualVersion.Name}»"),
            () => form.AsActualVersion.IsEnabled.ShouldBeFalse($"Прапорець «{form.AsActualVersion.Name}» доступний без публікації"),
            () => publication.IsShown.ShouldBeFalse("Панель публікації без «veröffentlichen»")));

        form.Publish.SetChecked(true);
        AllureApi.Step("Перевірити крок 1 з публікацією: панель, шлях за замовчуванням, «als aktuelle Version»", () => form.ShouldSatisfyAllConditions(
            () => publication.IsShown.ShouldBeTrue("Панелі публікації немає"),
            () => Wait.Until(() => publication.Paths.Count > 0, "дерева «Pfad der Veröffentlichung»"),
            () => publication.LinkedPaths.ShouldBe([MainPage.PublicationsNodeName], "Шлях публікації нового шаблону"),
            () => form.AsActualVersion.IsEnabled.ShouldBeTrue($"Прапорець «{form.AsActualVersion.Name}» недоступний"),
            () => form.AsActualVersion.IsChecked.ShouldBeFalse($"Прапорець «{form.AsActualVersion.Name}»")));
        var firstStep = form.StepTabCaption(1);

        form.ShowStep(2);
        AllureApi.Step("Перевірити крок 2: публікацію вже ввімкнено на кроці 1", () => form.ShouldSatisfyAllConditions(
            () => form.Publish.IsChecked.ShouldBeFalse($"Прапорець «{form.Publish.Name}»"),
            () => form.Publish.IsEnabled.ShouldBeFalse($"Прапорець «{form.Publish.Name}» доступний на другому кроці"),
            () => form.PublishHint.ShouldBe(
                $"Die Veröffentlichung wurde in Schritt {firstStep} aktiviert. Um die Veröffentlichung in diesem Schritt zu aktivieren, "
                + $"entfernen Sie das Häkchen in Schritt {firstStep}.",
                $"Підказка «{form.Publish.Name}»"),
            () => form.AsActualVersion.IsEnabled.ShouldBeFalse($"Прапорець «{form.AsActualVersion.Name}» доступний"),
            () => publication.IsShown.ShouldBeFalse("Панель публікації на кроці без «veröffentlichen»")));

        form.ShowStep(1);
        publication.ShowDistribution();
        ShouldHaveDistribution(publication, chat: false, mail: false);
        publication.Chat.SetChecked(true);
        ShouldHaveDistribution(publication, chat: true, mail: false);
        publication.Mail.SetChecked(true);
        ShouldHaveDistribution(publication, chat: true, mail: true);
        publication.Chat.SetChecked(false);
        ShouldHaveDistribution(publication, chat: false, mail: true);
        publication.Mail.SetChecked(false);
        ShouldHaveDistribution(publication, chat: false, mail: false);

        form.AsActualVersion.SetChecked(true);
        form.Publish.SetChecked(false);
        AllureApi.Step("Перевірити крок 1 після того, як публікацію вимкнено", () => form.ShouldSatisfyAllConditions(
            () => publication.IsShown.ShouldBeFalse("Панель публікації лишилася"),
            () => form.AsActualVersion.IsEnabled.ShouldBeFalse($"Прапорець «{form.AsActualVersion.Name}» доступний без публікації")));
        form.ShowStep(2);
        AllureApi.Step("Перевірити крок 2: публікацію можна ввімкнути", () => form.ShouldSatisfyAllConditions(
            () => form.Publish.IsEnabled.ShouldBeTrue($"Прапорець «{form.Publish.Name}» недоступний"),
            () => form.PublishHint.ShouldBeEmpty($"Підказка «{form.Publish.Name}»")));
        form.Publish.SetChecked(true);
        publication.IsShown.ShouldBeTrue("Панелі публікації на кроці 2 немає");
        form.ShowStep(1);
        form.Publish.IsEnabled.ShouldBeFalse($"Прапорець «{form.Publish.Name}» кроку 1 доступний, хоча публікацію ввімкнено на кроці 2");

        dialog.Cancel();
        config.Cancel();
    }

    /// <summary>
    /// Шлях публікації: куди ADAICA покладе опублікований документ. Тест створює під «DMS – Veröffentlichungen» свій
    /// Ordner і два документи. Позначити можна кілька папок (корінь і Ordner), але не документ разом з ними — «Nach der
    /// Auswahl eines Dokumentationselementes … nicht möglich». Без папок документ позначається, і тоді «als aktuelle
    /// Version» ADAICA вмикає сама й блокує (файл стане новою версією цього документа). Другий документ — «Es kann nur ein
    /// Dokument … ausgewählt werden», папка до документа — «Nach der Auswahl eines Dokumentes … nicht möglich». Знятий
    /// документ знову відкриває «als aktuelle Version».
    /// </summary>
    [Test]
    public void Workflowkonfiguration_PublicationPathRules()
    {
        var mainPage = LogInAndOpenDokumentation();
        var customer = CustomerCase.Random();
        var folder = CreatePublicationFolder(mainPage, customer);
        // Документи — просто під коренем: вкладених записів Ordner дерево шляхів не показує (див. README).
        var document = CreateDmsDocument(mainPage, customer, MainPage.PublicationsNodeName).FileName;
        var otherDocument = CreateDmsDocument(mainPage, customer, MainPage.PublicationsNodeName).FileName;
        var template = customer.NewWorkflowTemplate(mainPage.UserDisplayName, TestSettings.SecondUserContact);

        var config = mainPage.OpenWorkflowConfiguration();
        OnCleanup(config.CancelIfOpen);
        var dialog = config.OpenNew();
        OnCleanup(dialog.CancelIfOpen);
        var form = dialog.Form;
        var publication = form.Publication;
        form.Fill(template);
        form.ShowStep(1);
        form.Publish.SetChecked(true);
        Wait.Until(() => publication.Paths.Any(path => path.Name == otherDocument), "своїх записів у дереві «Pfad der Veröffentlichung»");
        AllureApi.Step("Перевірити дерево шляхів: свої Ordner і документи під коренем, позначено лише корінь", () =>
        {
            var paths = publication.Paths;
            paths.ShouldSatisfyAllConditions(
                () => paths[0].Name.ShouldBe(MainPage.PublicationsNodeName, "Перший рядок"),
                () => paths.Select(path => path.Name).ShouldContain(folder, "Свій Ordner"),
                () => paths.Select(path => path.Name).ShouldContain(document, "Свій документ"),
                () => paths.Select(path => path.Name).ShouldContain(otherDocument, "Другий документ"),
                () => paths.Where(path => path.IsLinked).Select(path => path.Name).ShouldBe([MainPage.PublicationsNodeName], "Позначені шляхи"),
                () => paths.ShouldAllBe(path => path.IsEnabled, "Недоступні прапорці «Verknüpft»"));
        });

        publication.SetLinked(folder, true);
        publication.LinkedPaths.ShouldBe([MainPage.PublicationsNodeName, folder], ignoreOrder: true, "Корінь і Ordner разом");
        ShouldRefuse(publication, document, FolderAlreadySelectedMessage, [MainPage.PublicationsNodeName, folder]);

        publication.SetLinked(MainPage.PublicationsNodeName, false);
        publication.SetLinked(folder, false);
        publication.SetLinked(document, true);
        AllureApi.Step("Перевірити «als aktuelle Version», коли шляхом вибрано документ", () => form.ShouldSatisfyAllConditions(
            () => form.AsActualVersion.IsChecked.ShouldBeTrue($"Прапорець «{form.AsActualVersion.Name}» не ввімкнено"),
            () => form.AsActualVersion.IsEnabled.ShouldBeFalse($"Прапорець «{form.AsActualVersion.Name}» можна зняти")));
        ShouldRefuse(publication, otherDocument, OnlyOneDocumentMessage, [document]);
        ShouldRefuse(publication, folder, DocumentAlreadySelectedMessage, [document]);

        publication.SetLinked(document, false);
        AllureApi.Step("Перевірити «als aktuelle Version», коли документ знято", () =>
            form.AsActualVersion.IsEnabled.ShouldBeTrue($"Прапорець «{form.AsActualVersion.Name}» лишився заблокованим"));
        dialog.Cancel();
        config.Cancel();
    }

    /// <summary>
    /// Шаблон зберігає всі прапорці: «PDF», публікацію після кроку 2 у свій Ordner (корінь знято), «als aktuelle Version»,
    /// чат із другим користувачем і e-mail. Область перегляду й «Bearbeiten» показують те саме. Далі публікацію перенесено
    /// на крок 1 у корінь «DMS – Veröffentlichungen», e-mail вимкнено: після OK обох вікон «Bearbeiten» показує саме це.
    /// Шлях і розсилка — спільні для шаблону, а «als aktuelle Version» — свій у кожного кроку.
    /// </summary>
    [Test]
    [AllureSeverity(SeverityLevel.critical)]
    public void Workflowkonfiguration_PublishSettingsSaved()
    {
        var mainPage = LogInAndOpenDokumentation();
        var customer = CustomerCase.Random();
        var folder = CreatePublicationFolder(mainPage, customer);
        var plain = customer.NewWorkflowTemplate(mainPage.UserDisplayName, TestSettings.SecondUserContact);
        var publishing = new WorkflowPublicationData([folder], AsActualVersion: true, Chat: true, Mail: true)
        {
            ChatContacts = [TestSettings.SecondUserContact],
        };
        var template = plain with
        {
            ConvertToPdf = true,
            Steps = [plain.Steps[0], plain.Steps[1] with { Publication = publishing }],
        };
        var moved = template with
        {
            Steps =
            [
                template.Steps[0] with { Publication = publishing with { Paths = [MainPage.PublicationsNodeName], AsActualVersion = false, Mail = false } },
                plain.Steps[1],
            ],
        };
        DeleteAfterTest(mainPage, template.Name);

        var config = mainPage.OpenWorkflowConfiguration();
        OnCleanup(config.CancelIfOpen);
        var dialog = config.OpenNew();
        OnCleanup(dialog.CancelIfOpen);
        dialog.Form.Fill(template);
        dialog.Form.ShouldHaveValues(template, mainPage.UserDisplayName);
        dialog.Confirm();
        config.Preview.ShouldHaveValues(template, mainPage.UserDisplayName);
        config.Save();

        config = mainPage.OpenWorkflowConfiguration();
        var saved = config.Edit(template.Name);
        saved.Form.ShouldHaveValues(template, mainPage.UserDisplayName);
        AllureApi.Step("Перенести публікацію на крок 1 у корінь «DMS – Veröffentlichungen» і вимкнути e-mail", () =>
        {
            saved.Form.ShowStep(2);
            saved.Form.Publish.SetChecked(false);
            saved.Form.ShowStep(1);
            saved.Form.FillStep(moved.Steps[0]);
        });
        saved.Form.ShouldHaveValues(moved, mainPage.UserDisplayName);
        saved.Confirm();
        config.Save();

        config = mainPage.OpenWorkflowConfiguration();
        saved = config.Edit(template.Name);
        saved.Form.ShouldHaveValues(moved, mainPage.UserDisplayName);
        saved.Cancel();
        config.Cancel();
    }

    private static void ShouldHaveDistribution(WorkflowPublicationPanel publication, bool chat, bool mail) =>
        AllureApi.Step($"Перевірити «Verteilung»: чат {(chat ? "увімкнено" : "вимкнено")}, e-mail {(mail ? "увімкнено" : "вимкнено")}", () =>
            publication.ShouldSatisfyAllConditions(
                () => publication.Chat.IsChecked.ShouldBe(chat, $"Прапорець «{publication.Chat.Name}»"),
                () => publication.Mail.IsChecked.ShouldBe(mail, $"Прапорець «{publication.Mail.Name}»"),
                () => publication.ChatContactsButton.IsEnabled.ShouldBe(chat, $"Доступність «{publication.ChatContactsButton.Name}»"),
                () => publication.MailContactsButton.IsEnabled.ShouldBe(mail, $"Доступність «{publication.MailContactsButton.Name}»"),
                () => publication.IsMessageEditable.ShouldBe(chat || mail, "Чи можна редагувати текст повідомлення")));

    private static void ShouldRefuse(WorkflowPublicationPanel publication, string path, string message, IReadOnlyList<string> linked) =>
        AllureApi.Step($"Перевірити, що «{path}» не можна позначити разом з {string.Join(", ", linked)}", () =>
        {
            var refused = publication.TryLink(path);
            if (refused is null)
                throw new ShouldAssertException($"ADAICA позначила «{path}» без повідомлення");
            refused.ShouldSatisfyAllConditions(
                () => OneLine(refused).ShouldBe(message, "Повідомлення ADAICA"),
                () => publication.LinkedPaths.ShouldBe(linked, ignoreOrder: true, "Позначені шляхи після повідомлення"));
        });

    // Повідомлення ADAICA переносить друге речення на новий рядок.
    private static string OneLine(string text) =>
        string.Join(" ", text.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
}
