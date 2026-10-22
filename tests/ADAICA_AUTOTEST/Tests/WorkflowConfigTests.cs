using ADAICA_AUTOTEST.Controls;
using ADAICA_AUTOTEST.Infrastructure;
using ADAICA_AUTOTEST.Pages;
using Allure.Net.Commons;
using Allure.Net.Commons.Attributes;

namespace ADAICA_AUTOTEST.Tests;

/// <summary>
/// Шаблони workflow у вікні «Workflowkonfiguration» (Konfiguration → Workflowkonfiguration): новий шаблон, зміни,
/// видалення, «Abbrechen» і «Übernehmen» вікна списку, обов'язкові поля, швидкий пошук і «Nur meine», а також дані в
/// колонках списку й в області перегляду. Шаблони спільні для всіх користувачів (з них запускають workflow на
/// документах), тож тест видаляє свої після себе. Prüfer кроків — користувачі тестів.
/// </summary>
[AllureSuite("Workflowkonfiguration")]
public partial class WorkflowConfigTests : AdaicaTestBase
{
    private static readonly string[] Columns = ["Workflow", "Workflowname", "Anwender", "Erstelldatum", "Geändert von", "Änderungsdatum"];

    /// <summary>
    /// Новий шаблон з двома кроками. Порожній бланк: вкладки «Workflow - » і «1 Schritt -  - WF1» зі знаком питання,
    /// «Workflow-Verantwortlicher» — поточний користувач, префікси «Workflow», «Workflowaufgabe», «Schritt», версія
    /// «WF1», «Annahmeart» — «Bestätigung», Prüfer немає, єдиний крок видалити не можна, OK недоступна. Після OK шаблон
    /// з'являється у списку виділеним (створив і змінив поточний користувач щойно), область перегляду показує його лише
    /// для читання, а після OK вікна списку шаблон лишається в базі: у знову відкритому вікні «Bearbeiten» показує ті
    /// самі поля.
    /// </summary>
    [Test]
    [Category("Smoke")]
    [AllureSeverity(SeverityLevel.critical)]
    public void Workflowkonfiguration_NewTemplate()
    {
        var mainPage = LogIn();
        var template = CustomerCase.Random().NewWorkflowTemplate(TestSettings.SecondUserContact, mainPage.UserDisplayName);
        DeleteAfterTest(mainPage, template.Name);

        var config = mainPage.OpenWorkflowConfiguration();
        OnCleanup(config.CancelIfOpen);
        AllureApi.Step("Перевірити колонки списку", () => config.ColumnNames.ShouldBe(Columns, ignoreOrder: false));

        var dialog = config.OpenNew();
        OnCleanup(dialog.CancelIfOpen);
        AllureApi.Step("Перевірити порожній бланк нового шаблону", () => dialog.ShouldSatisfyAllConditions(
            () => dialog.Title.ShouldBe(WorkflowDialog.NewTitle, "Заголовок вікна"),
            () => dialog.Form.TabNames.ShouldBe(["Workflow - ", "1 Schritt -  - WF1"], ignoreOrder: false, "Вкладки"),
            () => dialog.Form.IsTabIncomplete(0).ShouldBeTrue("Вкладка «Workflow» без знака питання"),
            () => dialog.Form.IsTabIncomplete(1).ShouldBeTrue("Вкладка кроку без знака питання"),
            () => dialog.Form.CanAddSteps.ShouldBeTrue("Немає вкладки «+»"),
            () => dialog.OkButton.IsEnabled.ShouldBeFalse("OK доступна з порожнім бланком")));
        dialog.Form.ShouldSatisfyAllConditions(
            () => dialog.Form.Responsible.Text.ShouldBe(mainPage.UserDisplayName, $"Поле «{dialog.Form.Responsible.Name}»"),
            () => dialog.Form.Prefix.Text.ShouldBe(WorkflowTemplateData.DefaultPrefix, $"Поле «{dialog.Form.Prefix.Name}»"),
            () => dialog.Form.Name.Text.ShouldBeEmpty($"Поле «{dialog.Form.Name.Name}»"),
            () => dialog.Form.MainTaskPrefix.Text.ShouldBe(WorkflowTemplateData.DefaultMainTaskPrefix, $"Поле «{dialog.Form.MainTaskPrefix.Name}»"),
            () => dialog.Form.MainTaskName.Text.ShouldBeEmpty($"Поле «{dialog.Form.MainTaskName.Name}»"),
            () => dialog.Form.Version.Text.ShouldBe(WorkflowTemplateData.Version, $"Поле «{dialog.Form.Version.Name}»"),
            () => dialog.Form.ConvertToPdf.IsChecked.ShouldBeFalse($"Прапорець «{dialog.Form.ConvertToPdf.Name}»"),
            () => dialog.Form.Authority.SelectedText.ShouldBe(WorkflowStepData.DefaultAuthority, $"Поле «{dialog.Form.Authority.Name}»"));
        dialog.Form.ShowStep(1);
        dialog.Form.ShouldSatisfyAllConditions(
            () => dialog.Form.StepPrefix.Text.ShouldBe(WorkflowStepData.DefaultPrefix, $"Поле «{dialog.Form.StepPrefix.Name}»"),
            () => dialog.Form.StepName.Text.ShouldBeEmpty($"Поле «{dialog.Form.StepName.Name}»"),
            () => dialog.Form.Reviewers.ShouldBeEmpty("Prüfer"),
            () => dialog.Form.Authority.SelectedText.ShouldBe(WorkflowStepData.DefaultAuthority, $"Поле «{dialog.Form.Authority.Name}»"),
            () => dialog.Form.StepTabMenuItems(1).ShouldBe([("Löschen", false)], "Меню вкладки єдиного кроку"));

        dialog.Form.Fill(template);
        AllureApi.Step("Перевірити, що бланк заповнено: знаків питання немає, OK доступна", () => dialog.ShouldSatisfyAllConditions(
            () => Enumerable.Range(0, template.Steps.Count + 1).Where(dialog.Form.IsTabIncomplete).ShouldBeEmpty("Вкладки зі знаком питання"),
            () => dialog.Form.StepTabMenuItems(1).ShouldBe([("Löschen", true)], "Меню вкладки кроку, коли кроків два"),
            () => dialog.OkButton.IsEnabled.ShouldBeTrue("OK недоступна")));
        dialog.Form.ShouldHaveValues(template, mainPage.UserDisplayName);

        var createdFrom = DateTime.Now;
        dialog.Confirm();
        var createdTo = DateTime.Now;

        AllureApi.Step("Перевірити новий шаблон у списку й області перегляду", () =>
        {
            config.ShouldSatisfyAllConditions(
                () => config.SelectedTemplate.ShouldBe(template.Name, "Виділений шаблон"),
                () => config.OkButton.IsEnabled.ShouldBeTrue("OK вікна списку недоступна"),
                () => config.ApplyButton.IsEnabled.ShouldBeTrue("«Übernehmen» недоступна"));
            ShouldBeCreatedBy(config.Template(template.Name), mainPage.UserDisplayName, createdFrom, createdTo);
            config.Preview.ShouldHaveValues(template, mainPage.UserDisplayName);
            ShouldBeReadOnly(config.Preview);
        });

        config.Save();
        config = mainPage.OpenWorkflowConfiguration();
        ShouldBeCreatedBy(config.Template(template.Name), mainPage.UserDisplayName, createdFrom, createdTo);
        var saved = config.Edit(template.Name);
        AllureApi.Step("Перевірити вікно збереженого шаблону", () => saved.ShouldSatisfyAllConditions(
            () => saved.Title.ShouldBe(WorkflowDialog.EditTitle(template.Name), "Заголовок вікна"),
            () => saved.OkButton.IsEnabled.ShouldBeFalse("OK доступна, хоча нічого не змінено")));
        saved.Form.ShouldHaveValues(template, mainPage.UserDisplayName);
        saved.Cancel();
        config.Cancel();
    }

    /// <summary>
    /// Зміни шаблону: «Bearbeiten» відкриває «Workflowkonfiguration: назва», OK доступна лише після зміни. Нова назва,
    /// інша назва головного завдання й першого кроку, інший Prüfer, новий крок у кінці (вкладка «+») і видалений крок
    /// (меню вкладки → «Löschen», кроки перенумеровуються). Після OK список показує нову назву замість старої, а
    /// «Geändert von» / «Änderungsdatum» — поточного користувача й час зміни; після OK вікна списку подвійний клік по
    /// рядку показує змінений шаблон.
    /// </summary>
    [Test]
    [AllureSeverity(SeverityLevel.critical)]
    public void Workflowkonfiguration_EditTemplate()
    {
        var mainPage = LogIn();
        var customer = CustomerCase.Random();
        var template = customer.NewWorkflowTemplate(TestSettings.SecondUserContact, mainPage.UserDisplayName);
        var edited = customer.EditWorkflowTemplate(template, mainPage.UserDisplayName);
        DeleteAfterTest(mainPage, template.Name, edited.Name);
        var createdFrom = DateTime.Now;
        CreateTemplate(mainPage, template);
        var createdTo = DateTime.Now;

        var config = mainPage.OpenWorkflowConfiguration();
        OnCleanup(config.CancelIfOpen);
        var dialog = config.Edit(template.Name);
        OnCleanup(dialog.CancelIfOpen);
        dialog.Title.ShouldBe(WorkflowDialog.EditTitle(template.Name), "Заголовок вікна");
        ShouldAllowOk(dialog.OkButton, false, "нічого не змінено");

        AllureApi.Step("Змінити шаблон: назви, Prüfer першого кроку, новий крок замість кроку затвердження", () =>
        {
            dialog.Form.ShowMainTab();
            dialog.Form.Name.Fill(edited.Name);
            ShouldAllowOk(dialog.OkButton, true, "назву змінено");
            dialog.Form.MainTaskName.Fill(edited.MainTaskName);

            dialog.Form.ShowStep(1);
            dialog.Form.StepName.Fill(edited.Steps[0].Name);
            dialog.Form.AddReviewer(edited.Steps[0].Reviewers[0]);
            dialog.Form.RemoveReviewer(template.Steps[0].Reviewers[0]);

            // Нові кроки додаються лише в кінець: додаємо обидва, а старий крок 2 видаляємо — решта перенумеровується.
            dialog.Form.AddStep();
            dialog.Form.FillStep(edited.Steps[1]);
            dialog.Form.AddStep();
            dialog.Form.FillStep(edited.Steps[2]);
            dialog.Form.DeleteStep(2);
        });
        dialog.Form.ShouldHaveValues(edited, mainPage.UserDisplayName);

        var changedFrom = DateTime.Now;
        dialog.Confirm();
        var changedTo = DateTime.Now;
        AllureApi.Step("Перевірити змінений шаблон у списку", () =>
        {
            config.ShouldSatisfyAllConditions(
                () => config.Contains(template.Name).ShouldBeFalse($"У списку лишилася стара назва «{template.Name}»"),
                () => config.SelectedTemplate.ShouldBe(edited.Name, "Виділений шаблон"));
            ShouldBeChangedBy(config.Template(edited.Name), mainPage.UserDisplayName, createdFrom, createdTo, changedFrom, changedTo);
            config.Preview.ShouldHaveValues(edited, mainPage.UserDisplayName);
        });
        config.Save();

        config = mainPage.OpenWorkflowConfiguration();
        ShouldBeChangedBy(config.Template(edited.Name), mainPage.UserDisplayName, createdFrom, createdTo, changedFrom, changedTo);
        var saved = config.EditByDoubleClick(edited.Name);
        saved.Title.ShouldBe(WorkflowDialog.EditTitle(edited.Name), "Заголовок вікна");
        saved.Form.ShouldHaveValues(edited, mainPage.UserDisplayName);
        saved.Cancel();
        config.Cancel();
    }

    /// <summary>
    /// Видалення шаблону: «Löschen» питає «Soll die ausgewählte Vorlage gelöscht werden?»; «Nein» лишає шаблон, «Ja»
    /// прибирає його зі списку, а після OK вікна списку шаблону немає й у знову відкритому вікні. Контекстне меню рядка
    /// має ті самі дії: «Hinzufügen», «Bearbeiten», «Löschen».
    /// </summary>
    [Test]
    [AllureSeverity(SeverityLevel.critical)]
    public void Workflowkonfiguration_DeleteTemplate()
    {
        var mainPage = LogIn();
        var template = CustomerCase.Random().NewWorkflowTemplate(TestSettings.SecondUserContact, mainPage.UserDisplayName);
        DeleteAfterTest(mainPage, template.Name);
        CreateTemplate(mainPage, template);

        var config = mainPage.OpenWorkflowConfiguration();
        OnCleanup(config.CancelIfOpen);
        config.RowMenuItems(template.Name).ShouldBe(["Hinzufügen", "Bearbeiten", "Löschen"], ignoreOrder: false, "Меню рядка шаблону");

        var question = config.Delete(template.Name);
        AllureApi.Step("Перевірити питання про видалення", () => question.ShouldSatisfyAllConditions(
            () => question.Title.ShouldBe("Löschen", "Заголовок"),
            () => question.Message.Text.ShouldBe("Soll die ausgewählte Vorlage gelöscht werden?", "Текст")));
        question.Button("Nein").Click();
        AllureApi.Step("Перевірити, що після «Nein» шаблон лишився", () => config.ShouldSatisfyAllConditions(
            () => config.Contains(template.Name).ShouldBeTrue("Шаблон зник зі списку"),
            () => config.OkButton.IsEnabled.ShouldBeFalse("OK доступна, хоча нічого не змінено")));

        config.Delete(template.Name).Button("Ja").Click();
        Wait.Until(() => !config.Contains(template.Name), $"зникнення шаблону «{template.Name}» зі списку");
        ShouldAllowOk(config.OkButton, true, "шаблон видалено");
        config.Save();

        config = mainPage.OpenWorkflowConfiguration();
        config.Contains(template.Name).ShouldBeFalse($"Шаблон «{template.Name}» є у списку після видалення");
        config.Cancel();
    }

    /// <summary>
    /// Усе, що зроблено у вікні списку, ADAICA зберігає лише після OK чи «Übernehmen». Новий шаблон після «Abbrechen»
    /// зникає; після «Übernehmen» (вона стає недоступною, а OK — ні) лишається й після «Abbrechen». Видалений шаблон після
    /// «Abbrechen» повертається.
    /// </summary>
    [Test]
    public void Workflowkonfiguration_CancelAndApply()
    {
        var mainPage = LogIn();
        var customer = CustomerCase.Random();
        var discarded = customer.NewWorkflowTemplate(TestSettings.SecondUserContact, mainPage.UserDisplayName);
        var applied = customer.NewWorkflowTemplate(mainPage.UserDisplayName, TestSettings.SecondUserContact);
        DeleteAfterTest(mainPage, discarded.Name, applied.Name);

        var config = mainPage.OpenWorkflowConfiguration();
        OnCleanup(config.CancelIfOpen);
        var dialog = config.OpenNew();
        dialog.Form.Fill(discarded);
        dialog.Confirm();
        config.Contains(discarded.Name).ShouldBeTrue($"Шаблону «{discarded.Name}» немає у списку");
        config.Cancel();
        config = mainPage.OpenWorkflowConfiguration();
        config.Contains(discarded.Name).ShouldBeFalse($"Шаблон «{discarded.Name}» збережено, хоча вікно закрито «Abbrechen»");

        dialog = config.OpenNew();
        dialog.Form.Fill(applied);
        dialog.Confirm();
        config.Apply();
        AllureApi.Step("Перевірити кнопки після «Übernehmen»", () => config.ShouldSatisfyAllConditions(
            () => config.ApplyButton.IsEnabled.ShouldBeFalse("«Übernehmen» доступна"),
            () => config.OkButton.IsEnabled.ShouldBeTrue("OK недоступна")));
        config.Cancel();
        config = mainPage.OpenWorkflowConfiguration();
        config.Contains(applied.Name).ShouldBeTrue($"Шаблону «{applied.Name}» немає, хоча його «übernommen»");

        config.Delete(applied.Name).Button("Ja").Click();
        Wait.Until(() => !config.Contains(applied.Name), $"зникнення шаблону «{applied.Name}» зі списку");
        config.Cancel();
        config = mainPage.OpenWorkflowConfiguration();
        config.Contains(applied.Name).ShouldBeTrue($"Шаблон «{applied.Name}» видалено, хоча вікно закрито «Abbrechen»");
        config.Cancel();
    }

    /// <summary>
    /// Обов'язкові поля й перевірки бланка: OK доступна, лише коли на всіх вкладках заповнено «Workflowname», назви
    /// головного завдання й кроків і в кожного кроку є Prüfer. Назва наявного шаблону дає біля «Workflowname» помилку
    /// «Eine gleichnamige Vorlage ist bereits vorhanden». «Beginnt am» пізніше за «Fällig am» не дає зберегти шаблон.
    /// </summary>
    [Test]
    public void Workflowkonfiguration_RequiredFields()
    {
        var mainPage = LogIn();
        var customer = CustomerCase.Random();
        var existing = customer.NewWorkflowTemplate(TestSettings.SecondUserContact, mainPage.UserDisplayName);
        var template = customer.NewWorkflowTemplate(mainPage.UserDisplayName, TestSettings.SecondUserContact);
        DeleteAfterTest(mainPage, existing.Name, template.Name);
        CreateTemplate(mainPage, existing);

        var config = mainPage.OpenWorkflowConfiguration();
        OnCleanup(config.CancelIfOpen);
        var dialog = config.OpenNew();
        OnCleanup(dialog.CancelIfOpen);
        var form = dialog.Form;
        var step = template.Steps[0];

        form.ShowMainTab();
        form.Name.Fill(template.Name);
        form.MainTaskName.Fill(template.MainTaskName);
        AllureApi.Step("Перевірити: вкладка «Workflow» заповнена, крок — ні", () => form.ShouldSatisfyAllConditions(
            () => form.IsTabIncomplete(0).ShouldBeFalse("Знак питання на вкладці «Workflow»"),
            () => form.IsTabIncomplete(1).ShouldBeTrue("Немає знака питання на вкладці кроку"),
            () => dialog.OkButton.IsEnabled.ShouldBeFalse("OK доступна з незаповненим кроком")));

        form.ShowStep(1);
        form.StepName.Fill(step.Name);
        AllureApi.Step("Перевірити: у кроку немає Prüfer", () => form.ShouldSatisfyAllConditions(
            () => form.IsTabIncomplete(1).ShouldBeTrue("Немає знака питання на вкладці кроку без Prüfer"),
            () => dialog.OkButton.IsEnabled.ShouldBeFalse("OK доступна без Prüfer")));
        form.AddReviewer(step.Reviewers[0]);
        ShouldAllowOk(dialog.OkButton, true, "заповнено все обов'язкове");

        form.AddStep();
        AllureApi.Step("Перевірити: новий крок порожній", () => form.ShouldSatisfyAllConditions(
            () => form.IsTabIncomplete(2).ShouldBeTrue("Немає знака питання на вкладці нового кроку"),
            () => dialog.OkButton.IsEnabled.ShouldBeFalse("OK доступна з порожнім кроком")));
        form.DeleteStep(2);
        ShouldAllowOk(dialog.OkButton, true, "порожній крок видалено");

        form.ShowMainTab();
        form.MainTaskName.Fill(string.Empty);
        ShouldAllowOk(dialog.OkButton, false, "назву головного завдання стерто");
        form.MainTaskName.Fill(template.MainTaskName);

        form.Name.Fill(existing.Name);
        AllureApi.Step("Перевірити помилку для назви наявного шаблону", () => form.ShouldSatisfyAllConditions(
            () => Wait.Until(() => form.FieldErrors.Contains("Eine gleichnamige Vorlage ist bereits vorhanden"), "помилки біля «Workflowname»"),
            () => form.IsTabIncomplete(0).ShouldBeTrue("Немає знака питання на вкладці «Workflow»"),
            () => dialog.OkButton.IsEnabled.ShouldBeFalse("OK доступна з назвою наявного шаблону")));
        form.Name.Fill(template.Name);
        ShouldAllowOk(dialog.OkButton, true, "назва знову нова");

        form.ShowStep(1);
        var begin = DateTime.Today.AddDays(10).AddHours(9);
        form.Due.Fill(begin.AddDays(-3));
        form.Begin.Fill(begin);
        AllureApi.Step("Перевірити, що «Beginnt am» пізніше за «Fällig am» не дає зберегти шаблон", () => form.ShouldSatisfyAllConditions(
            () => form.IsTabIncomplete(1).ShouldBeTrue("Немає знака питання на вкладці кроку"),
            () => dialog.OkButton.IsEnabled.ShouldBeFalse("OK доступна, хоча початок пізніше за термін")));
        form.Due.Fill(begin.AddDays(3));
        ShouldAllowOk(dialog.OkButton, true, "термін пізніше за початок");
        dialog.Cancel();
        config.Contains(template.Name).ShouldBeFalse($"Шаблон «{template.Name}» з'явився після «Abbrechen»");
        config.Cancel();
    }

    /// <summary>
    /// Швидкий пошук і «Nur meine». Другий користувач створює шаблон, поточний — два. Пошук за спільним номером справи
    /// показує всі три, без урахування регістру — теж; пошук за іменем другого користувача («Anwender») — його шаблон,
    /// але не шаблони поточного; пошук за датою й часом створення («дд.ММ.рррр гг:хх») знаходить шаблон, створений саме
    /// тоді. «Nur meine» ховає шаблон другого користувача й діє разом із пошуком; хрестик у полі пошуку повертає весь список.
    /// </summary>
    [Test]
    public void Workflowkonfiguration_SearchAndOnlyMine()
    {
        var customer = CustomerCase.Random();
        var mainPage = new LoginPage(Session).LoginAs(TestSettings.SecondUserName, TestSettings.Password, TestSettings.Database, TestSettings.Schema);
        // Ім'я із заголовка вікна («Anna van de Goldun») не перетворити на «Прізвище, Ім'я»: прізвище з кількох слів.
        var secondUser = TestSettings.SecondUserContact;
        var foreign = customer.NewWorkflowTemplate(TestSettings.SecondUserContact, TestSettings.SecondUserContact);
        // Шаблон другого користувача видаляє вже основний користувач: прибирання, зареєстроване досі, виконується до виходу.
        CreateTemplate(mainPage, foreign);
        RestartForAnotherUser();

        mainPage = LogIn();
        var mine = customer.NewWorkflowTemplate(TestSettings.SecondUserContact, mainPage.UserDisplayName);
        var mineToo = customer.NewWorkflowTemplate(mainPage.UserDisplayName, mainPage.UserDisplayName);
        DeleteAfterTest(mainPage, foreign.Name, mine.Name, mineToo.Name);
        CreateTemplate(mainPage, mine);
        CreateTemplate(mainPage, mineToo);

        var config = mainPage.OpenWorkflowConfiguration();
        OnCleanup(config.CancelIfOpen);
        // Рядки всього списку: з них рахуємо, що має лишитися після кожного пошуку.
        var rows = config.Templates;
        var all = rows.Select(row => row.Name).ToList();
        all.ShouldContain(foreign.Name, "Шаблон другого користувача");
        ShouldBeCreatedBy(config.Template(foreign.Name), secondUser);
        ShouldBeCreatedBy(config.Template(mine.Name), mainPage.UserDisplayName);

        // Назви всіх трьох закінчуються номером із часу; спільна в них — дата й година створення.
        var stamp = CommonStamp(foreign.Name, mine.Name, mineToo.Name);
        config.Search(stamp, [foreign.Name, mine.Name, mineToo.Name]);
        config.Search(mine.Name.ToUpperInvariant(), [mine.Name]);
        config.Search(mine.Name.ToLowerInvariant(), [mine.Name]);

        config.Search(secondUser, NamesOf(rows, row => row.CreatedBy == secondUser || row.ChangedBy == secondUser));
        config.TemplateNames.ShouldContain(foreign.Name, $"Пошук за «{secondUser}»");

        var createdText = AdaicaFormat.FormatDateTime(rows.Single(row => row.Name == mineToo.Name).Created);
        config.Search(createdText, NamesOf(rows, row => AdaicaFormat.FormatDateTime(row.Created) == createdText || AdaicaFormat.FormatDateTime(row.Changed) == createdText));

        config.Search(stamp, [foreign.Name, mine.Name, mineToo.Name]);
        config.ShowOnlyMine(true, [mine.Name, mineToo.Name]);
        config.ClearSearch(NamesOf(rows, row => row.CreatedBy == mainPage.UserDisplayName));
        config.ShowOnlyMine(false, all);
        config.Cancel();
    }

    private MainPage LogIn() =>
        new LoginPage(Session).LoginAs(TestSettings.UserName, TestSettings.Password, TestSettings.Database, TestSettings.Schema);

    /// <summary>Створює шаблон <paramref name="template"/> і зберігає його кнопкою OK вікна списку.</summary>
    private void CreateTemplate(MainPage mainPage, WorkflowTemplateData template) =>
        AllureApi.Step($"Створити шаблон workflow {template}", () =>
        {
            var config = mainPage.OpenWorkflowConfiguration();
            OnCleanup(config.CancelIfOpen);
            var dialog = config.OpenNew();
            dialog.Form.Fill(template);
            dialog.Confirm();
            config.Save();
        });

    private void DeleteAfterTest(MainPage mainPage, params string[] names) =>
        OnCleanup(() => mainPage.OpenWorkflowConfiguration().DeleteIfPresentAndClose(names));

    private static IReadOnlyCollection<string> NamesOf(IEnumerable<WorkflowTemplateRow> rows, Func<WorkflowTemplateRow, bool> condition) =>
        rows.Where(condition).Select(row => row.Name).ToList();

    // Найдовший спільний кінець номерів у назвах: «2610201432» для «… 261020143207» і «… 261020143215».
    private static string CommonStamp(params string[] names)
    {
        var numbers = names.Select(name => name[(name.LastIndexOf(' ') + 1)..]).ToList();
        var length = Enumerable.Range(0, numbers.Min(number => number.Length))
            .TakeWhile(index => numbers.All(number => number[index] == numbers[0][index]))
            .Count();
        return numbers[0][..length];
    }

    private static void ShouldBeReadOnly(WorkflowForm form) =>
        AllureApi.Step("Перевірити, що область перегляду лише для читання", () =>
        {
            form.ShowMainTab();
            form.ShouldSatisfyAllConditions(
                () => form.CanAddSteps.ShouldBeFalse("Є вкладка «+»"),
                () => form.Name.IsReadOnly.ShouldBeTrue($"Поле «{form.Name.Name}» можна змінити"),
                () => form.MainTaskName.IsReadOnly.ShouldBeTrue($"Поле «{form.MainTaskName.Name}» можна змінити"),
                () => form.ConvertToPdf.IsEnabled.ShouldBeFalse($"Прапорець «{form.ConvertToPdf.Name}» доступний"),
                () => form.Begin.IsEnabled.ShouldBeFalse($"Поле «{form.Begin.Name}» доступне"));
        });

    private static void ShouldBeCreatedBy(WorkflowTemplateRow row, string user, DateTime? from = null, DateTime? to = null) =>
        AllureApi.Step($"Перевірити рядок шаблону «{row.Name}»: створив і змінив {user}", () => row.ShouldSatisfyAllConditions(
            () => row.CreatedBy.ShouldBe(user, "«Anwender»"),
            () => row.ChangedBy.ShouldBe(user, "«Geändert von»"),
            () => row.Changed.ShouldBe(row.Created, "«Änderungsdatum» нового шаблону"),
            () =>
            {
                if (from is { } start && to is { } end)
                    row.Created.ShouldBeInRange(Minute(start), end, "«Erstelldatum»");
            }));

    private static void ShouldBeChangedBy(
        WorkflowTemplateRow row, string user, DateTime createdFrom, DateTime createdTo, DateTime changedFrom, DateTime changedTo) =>
        AllureApi.Step($"Перевірити рядок шаблону «{row.Name}»: змінив {user} щойно", () => row.ShouldSatisfyAllConditions(
            () => row.CreatedBy.ShouldBe(user, "«Anwender»"),
            () => row.Created.ShouldBeInRange(Minute(createdFrom), createdTo, "«Erstelldatum»"),
            () => row.ChangedBy.ShouldBe(user, "«Geändert von»"),
            () => row.Changed.ShouldBeInRange(Minute(changedFrom), changedTo, "«Änderungsdatum»")));

    private static void ShouldAllowOk(ButtonControl ok, bool allowed, string because) =>
        AllureApi.Step($"Перевірити, що OK {(allowed ? "доступна" : "недоступна")}: {because}", () =>
            Wait.Until(() => ok.IsEnabled == allowed, $"{(allowed ? "доступної" : "недоступної")} кнопки OK ({because})"));

    // Дати в списку — з точністю до хвилини.
    private static DateTime Minute(DateTime value) => new(value.Ticks - value.Ticks % TimeSpan.TicksPerMinute, value.Kind);
}
