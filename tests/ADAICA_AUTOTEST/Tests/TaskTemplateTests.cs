using ADAICA_AUTOTEST.Controls;
using ADAICA_AUTOTEST.Infrastructure;
using ADAICA_AUTOTEST.Pages;
using Allure.Net.Commons;
using Allure.Net.Commons.Attributes;

namespace ADAICA_AUTOTEST.Tests;

/// <summary>
/// Шаблони завдань («Aufgabenvorlagen»): вікно нового завдання, збережене кнопкою «Als Aufgabenvorlage speichern», новий
/// шаблон з нуля, зміни, дублювання, перейменування, обране, швидкий пошук і видалення шаблону, а також завдання з шаблону —
/// у вікні нового завдання («Aufgabenvorlage auswählen»), з пункту шаблону в меню «Neu» і з «Vorlage erstellen/bearbeiten».
/// Шаблони спільні для всіх користувачів і з'являються в меню «Neu» кожного запису, тож тест видаляє свої після себе;
/// завдання з шаблонів, як і всі записи тестів, лишаються в базі. Дати шаблону — пізніше за сьогодні
/// (<see cref="CustomerCase.NewTemplateTask"/>), а «Verantwortlich» — другий користувач тестів.
/// </summary>
[AllureSuite("Шаблони завдань")]
public class TaskTemplateTests : AdaicaTestBase
{
    private static readonly string[] TemplateRowMenu =
        ["Vorlage erstellen", "Duplizieren", "Zu Favoriten hinzufügen", "Anzeigen", "Bearbeiten", "Umbenennen", "Löschen", "Aktualisieren"];

    /// <summary>
    /// «Als Aufgabenvorlage speichern» у вікні нового завдання, де заповнено всі поля, а «Verantwortlich» — другий
    /// користувач. «Neue Aufgabenvorlage» має ті самі поля, відповідального й виконавців, але «Beginnt am» порожнє й
    /// недоступне, номера, дати створення й автора немає, «Erinnerung» і «Serientyp» недоступні, а з вкладок — лише
    /// «Aufgabe» й «Ausführende». Поки «Name der Vorlage» порожнє, OK недоступна. Після OK вікно завдання лишається
    /// відкритим і незміненим, а завдання зберігається як завжди. Шаблон з'являється в меню «Neu» вузла Aufgaben і в
    /// «Aufgabenvorlagen» (створив і змінив поточний користувач щойно, не в обраному), а «Ansehen» показує його поля лише
    /// для читання.
    /// </summary>
    [Test]
    [Category("Smoke")]
    [AllureSeverity(SeverityLevel.critical)]
    public void Aufgabenvorlage_SaveFilledNewTask()
    {
        var mainPage = LogInAndOpenDokumentation();
        var customer = CustomerCase.Random().WithClass();
        var templateName = customer.NewTemplateName();
        DeleteAfterTest(mainPage.OpenTaskTemplates, templateName);

        var dialog = mainPage.OpenNewTaskDialog();
        OnCleanup(dialog.CancelIfOpen);
        var task = customer.NewTemplateTask();
        dialog.Form.Fill(task);
        var responsible = dialog.Form.ChooseResponsible(TestSettings.SecondUserContact);
        var number = dialog.Form.Number.Text;
        number.ShouldNotBeNullOrWhiteSpace();

        var createdFrom = DateTime.Now;
        var templateDialog = dialog.SaveAsTemplate();
        OnCleanup(templateDialog.CancelIfOpen);
        AllureApi.Step($"Перевірити вікно «{TaskTemplateDialog.NewTitle}»", () => templateDialog.ShouldSatisfyAllConditions(
            () => templateDialog.Title.ShouldBe(TaskTemplateDialog.NewTitle, "Заголовок вікна"),
            () => templateDialog.TemplateName.Text.ShouldBeEmpty($"Поле «{templateDialog.TemplateName.Name}»"),
            () => templateDialog.OkButton.IsEnabled.ShouldBeFalse($"OK доступна з порожнім «{templateDialog.TemplateName.Name}»"),
            () => templateDialog.Form.TabNames.ShouldBe(
                ["Aufgabe", TaskForm.ParticipantsTabName(responsible)], ignoreOrder: false, "Вкладки шаблону")));
        templateDialog.Form.ShouldHaveTemplateValues(task);
        templateDialog.Form.ShouldHaveResponsible(responsible);

        templateDialog.TemplateName.Fill(templateName);
        ShouldAllowOk(templateDialog.OkButton, true, "назва шаблону нова");
        templateDialog.Save();
        var createdTo = DateTime.Now;

        AllureApi.Step("Перевірити, що вікно завдання лишилося відкритим і незміненим", () => dialog.ShouldSatisfyAllConditions(
            () => dialog.IsOpen.ShouldBeTrue("Вікно нового завдання закрилося"),
            () => dialog.Form.Number.Text.ShouldBe(number, $"Поле «{dialog.Form.Number.Name}»")));
        dialog.Form.ShouldHaveValues(task);
        dialog.Form.ShouldHaveResponsible(responsible);
        dialog.Save();
        var savedTask = mainPage.WaitForOpenedTask(number);
        savedTask.ShowMainTab();
        savedTask.ShouldHaveValues(task);
        savedTask.ShouldHaveResponsible(responsible);

        ShouldOfferTemplates(mainPage.NewTaskMenuItems(), templateName);

        var templates = mainPage.OpenTaskTemplates();
        OnCleanup(templates.CloseIfOpen);
        ShouldBeCreatedBy(templates.Template(templateName), mainPage.UserDisplayName, createdFrom, createdTo);

        var view = templates.View(templateName);
        OnCleanup(view.CancelIfOpen);
        AllureApi.Step("Перевірити, що «Ansehen» показує шаблон лише для читання", () => view.ShouldSatisfyAllConditions(
            () => view.Title.ShouldBe(TaskTemplateDialog.EditTitle(templateName), "Заголовок вікна"),
            () => view.TemplateName.Text.ShouldBe(templateName, $"Поле «{view.TemplateName.Name}»"),
            () => view.TemplateName.IsReadOnly.ShouldBeTrue($"Поле «{view.TemplateName.Name}» можна змінити"),
            () => view.Form.Name.IsReadOnly.ShouldBeTrue($"Поле «{view.Form.Name.Name}» можна змінити"),
            () => view.OkButton.IsEnabled.ShouldBeFalse("OK доступна")));
        view.Form.ShouldHaveTemplateValues(task);
        view.Form.ShouldHaveResponsible(responsible);
        view.Cancel();
    }

    /// <summary>
    /// «Aufgabenvorlage auswählen» у вікні нового завдання відкриває «Aufgabenvorlagen»; «Verwenden» доступна, лише коли
    /// вибрано шаблон. Після «Verwenden» вікно вибору закривається, а в тому самому вікні завдання (номер не змінюється) —
    /// усі поля шаблону, відповідальний і виконавці, «Beginnt am» — сьогодні о 00:00. Завдання зберігається з цими полями.
    /// </summary>
    [Test]
    [AllureSeverity(SeverityLevel.critical)]
    public void Aufgabenvorlage_UseInNewTaskWindow()
    {
        var mainPage = LogInAndOpenDokumentation();
        var customer = CustomerCase.Random().WithClass();
        var templateName = customer.NewTemplateName();
        DeleteAfterTest(mainPage.OpenTaskTemplates, templateName);

        var templateSource = mainPage.OpenNewTaskDialog();
        OnCleanup(templateSource.CancelIfOpen);
        var template = SaveAsTemplateAndCancel(templateSource, customer, templateName);

        var dialog = mainPage.OpenNewTaskDialog();
        OnCleanup(dialog.CancelIfOpen);
        var number = dialog.Form.Number.Text;
        var templates = dialog.ChooseTemplate();
        OnCleanup(templates.CloseIfOpen);
        templates.UseButton.ShouldBeDisabled();
        templates.Use(templateName);

        dialog.Form.Number.ShouldHaveText(number);
        var expected = ShouldBeFilledFromTemplate(dialog, template);
        SaveTaskFromTemplate(mainPage, dialog, expected, template.Responsible);
        mainPage.SelectedNavigatorNodeName.ShouldBe(expected.Name);
    }

    /// <summary>
    /// Шаблон, збережений з вікна нового завдання під проєктом, з'являється в меню проєкту «Neu» → «Aufgabe» після
    /// «Neue Aufgabe» і «Vorlage erstellen/bearbeiten». Пункт шаблону відкриває «Neue Aufgabe (übergeordnet: проєкт)» з
    /// полями шаблону; «Vorlage erstellen/bearbeiten» → шаблон → «Verwenden» — теж. Обидва завдання ADAICA зберігає під
    /// проєктом, у «Aufgaben».
    /// </summary>
    [Test]
    [AllureSeverity(SeverityLevel.critical)]
    public void Aufgabenvorlage_NewChildTaskFromTemplate()
    {
        var mainPage = LogInAndOpenDokumentation();
        var customer = CustomerCase.Random().WithClass();

        var projectDialog = mainPage.OpenNewProjectDialog();
        OnCleanup(projectDialog.CancelIfOpen);
        var project = customer.NewProject();
        projectDialog.Form.Fill(project);
        var projectNumber = projectDialog.Form.Number.Text;
        projectDialog.Save();
        mainPage.WaitForOpenedProject(projectNumber);
        var projectNode = NodeWithNumber(project.Name, projectNumber);

        // Під розгорнутим проєктом є своя папка «Aufgaben», тож шаблони відкриваємо з меню проєкту.
        var templateName = customer.NewTemplateName();
        DeleteAfterTest(() => mainPage.OpenChildTaskTemplates(projectNode), templateName);
        var templateSource = mainPage.OpenNewChildTaskDialog(projectNode);
        OnCleanup(templateSource.CancelIfOpen);
        var template = SaveAsTemplateAndCancel(templateSource, customer, templateName);

        ShouldOfferTemplates(mainPage.NewChildTaskMenuItems(projectNode), templateName);

        var fromMenu = mainPage.OpenNewChildTaskFromTemplate(projectNode, templateName);
        OnCleanup(fromMenu.CancelIfOpen);
        fromMenu.Title.ShouldBe($"Neue Aufgabe (übergeordnet: {projectNode})");
        var expected = ShouldBeFilledFromTemplate(fromMenu, template);
        SaveTaskFromTemplate(mainPage, fromMenu, expected, template.Responsible);
        ShouldBeSelectedUnder(mainPage, projectNode, "Aufgaben", expected.Name);

        var templates = mainPage.OpenChildTaskTemplates(projectNode);
        OnCleanup(templates.CloseIfOpen);
        templates.Use(templateName);
        var fromTemplates = new TaskDialog(mainPage.Window);
        OnCleanup(fromTemplates.CancelIfOpen);
        fromTemplates.Title.ShouldBe($"Neue Aufgabe (übergeordnet: {projectNode})");
        ShouldBeFilledFromTemplate(fromTemplates, template);
        SaveTaskFromTemplate(mainPage, fromTemplates, expected, template.Responsible);
        ShouldBeSelectedUnder(mainPage, projectNode, "Aufgaben", expected.Name);
    }

    /// <summary>
    /// «Neu» у «Aufgabenvorlagen» відкриває порожній шаблон: без назви шаблону й завдання, «Verantwortlich» і єдиний
    /// виконавець — поточний користувач, OK недоступна. До вибору шаблону в списку доступна лише «Neu». Заповнений шаблон
    /// (усі поля, «Verantwortlich» — другий користувач) з'являється в списку, а пункт шаблону в меню «Neu» вузла Aufgaben
    /// відкриває вікно нового завдання з його полями.
    /// </summary>
    [Test]
    [AllureSeverity(SeverityLevel.critical)]
    public void Aufgabenvorlage_NewInTemplatesWindow()
    {
        var mainPage = LogInAndOpenDokumentation();
        var customer = CustomerCase.Random().WithClass();
        var templateName = customer.NewTemplateName();
        DeleteAfterTest(mainPage.OpenTaskTemplates, templateName);

        var templates = mainPage.OpenTaskTemplates();
        OnCleanup(templates.CloseIfOpen);
        AllureApi.Step("Перевірити кнопки, поки шаблон не вибрано", () => templates.ShouldSatisfyAllConditions(
            () => templates.NewButton.IsEnabled.ShouldBeTrue($"«{templates.NewButton.Name}» недоступна"),
            () => templates.ViewButton.IsEnabled.ShouldBeFalse($"«{templates.ViewButton.Name}» доступна"),
            () => templates.EditButton.IsEnabled.ShouldBeFalse($"«{templates.EditButton.Name}» доступна"),
            () => templates.DeleteButton.IsEnabled.ShouldBeFalse($"«{templates.DeleteButton.Name}» доступна"),
            () => templates.UseButton.IsEnabled.ShouldBeFalse($"«{templates.UseButton.Name}» доступна")));

        var createdFrom = DateTime.Now;
        var dialog = templates.OpenNew();
        OnCleanup(dialog.CancelIfOpen);
        AllureApi.Step($"Перевірити порожній шаблон «{TaskTemplateDialog.NewTitle}»", () => dialog.ShouldSatisfyAllConditions(
            () => dialog.Title.ShouldBe(TaskTemplateDialog.NewTitle, "Заголовок вікна"),
            () => dialog.TemplateName.Text.ShouldBeEmpty($"Поле «{dialog.TemplateName.Name}»"),
            () => dialog.Form.Name.Text.ShouldBeEmpty($"Поле «{dialog.Form.Name.Name}»"),
            () => dialog.Form.Begin.IsEnabled.ShouldBeFalse($"Поле «{dialog.Form.Begin.Name}» доступне"),
            () => dialog.Form.SeriesTypeButton.IsEnabled.ShouldBeFalse($"Кнопка «{dialog.Form.SeriesTypeButton.Name}» доступна"),
            () => dialog.OkButton.IsEnabled.ShouldBeFalse("OK доступна в порожньому шаблоні")));
        dialog.Form.ShouldHaveResponsible(mainPage.UserDisplayName);

        var task = customer.NewTemplateTask();
        dialog.Form.FillTemplate(task);
        var responsible = dialog.Form.ChooseResponsible(TestSettings.SecondUserContact);
        dialog.TemplateName.Fill(templateName);
        dialog.Save();
        var template = new TaskTemplateData(templateName, task, responsible);
        ShouldBeCreatedBy(templates.Template(templateName), mainPage.UserDisplayName, createdFrom, DateTime.Now);
        templates.Close();

        var taskDialog = mainPage.OpenNewTaskFromTemplate(templateName);
        OnCleanup(taskDialog.CancelIfOpen);
        taskDialog.Title.ShouldBe("Neue Aufgabe (übergeordnet: Aufgaben)");
        var expected = ShouldBeFilledFromTemplate(taskDialog, template);
        SaveTaskFromTemplate(mainPage, taskDialog, expected, template.Responsible);
        mainPage.SelectedNavigatorNodeName.ShouldBe(expected.Name);
    }

    /// <summary>
    /// «Bearbeiten» відкриває «Aufgabenvorlage bearbeiten: назва» з полями шаблону; OK доступна, лише коли щось змінено.
    /// Змінені назва завдання, «Fällig am», «Priorität», «Bemerkung» і «Verantwortlich» (замість поточного користувача,
    /// яким ADAICA заповнює новий шаблон, — другий) зберігаються: їх показує «Ansehen»,
    /// а в списку оновлюються «Änderungsdatum» і «Geändert durch» («Erstelldatum» не змінюється). «Duplizieren» в меню
    /// рядка відкриває «Neue Aufgabenvorlage» з уже зміненими полями й порожньою назвою шаблону; копія зберігається як
    /// окремий шаблон з тими самими полями.
    /// </summary>
    [Test]
    [AllureSeverity(SeverityLevel.normal)]
    public void Aufgabenvorlage_EditAndDuplicate()
    {
        var mainPage = LogInAndOpenDokumentation();
        var customer = CustomerCase.Random().WithClass();
        var templateName = customer.NewTemplateName();
        var copyName = customer.NewTemplateName();
        DeleteAfterTest(mainPage.OpenTaskTemplates, templateName, copyName);

        var templates = mainPage.OpenTaskTemplates();
        OnCleanup(templates.CloseIfOpen);
        var template = CreateInTemplatesWindow(templates, customer, templateName);
        var created = templates.Template(templateName);

        var edit = templates.Edit(templateName);
        OnCleanup(edit.CancelIfOpen);
        AllureApi.Step("Перевірити вікно «Bearbeiten»", () => edit.ShouldSatisfyAllConditions(
            () => edit.Title.ShouldBe(TaskTemplateDialog.EditTitle(templateName), "Заголовок вікна"),
            () => edit.TemplateName.Text.ShouldBe(templateName, $"Поле «{edit.TemplateName.Name}»"),
            () => edit.OkButton.IsEnabled.ShouldBeFalse("OK доступна без змін")));
        edit.Form.ShouldHaveTemplateValues(template.Task);
        edit.Form.ShouldHaveResponsible(template.Responsible);

        var other = customer.NewTemplateTask();
        var changed = template.Task with { Name = other.Name, Due = other.Due, Priority = "niedrig", Remark = other.Remark };
        AllureApi.Step("Змінити назву завдання, «Fällig am», «Priorität» і «Bemerkung»", () =>
        {
            edit.Form.Name.Fill(changed.Name);
            edit.Form.Due.Fill(changed.Due);
            edit.Form.Priority.Select(changed.Priority);
            edit.Form.SimpleRemarkTab.Select();
            edit.Form.Remark.Fill(changed.Remark);
        });
        var responsible = edit.Form.ChooseResponsible(TestSettings.SecondUserContact);
        var changedFrom = DateTime.Now;
        ShouldAllowOk(edit.OkButton, true, "шаблон змінено");
        edit.Save();
        var changedTo = DateTime.Now;

        var row = templates.Template(templateName);
        AllureApi.Step("Перевірити рядок зміненого шаблону", () => row.ShouldSatisfyAllConditions(
            () => row.Created.ShouldBe(created.Created, "«Erstelldatum»"),
            () => row.Changed.ShouldBeInRange(Minute(changedFrom), changedTo, "«Änderungsdatum»"),
            () => row.ChangedBy.ShouldBe(mainPage.UserDisplayName, "«Geändert durch»")));
        var view = templates.View(templateName);
        OnCleanup(view.CancelIfOpen);
        view.Form.ShouldHaveTemplateValues(changed);
        view.Form.ShouldHaveResponsible(responsible);
        view.Cancel();

        var copy = templates.Duplicate(templateName);
        OnCleanup(copy.CancelIfOpen);
        AllureApi.Step("Перевірити копію шаблону", () => copy.ShouldSatisfyAllConditions(
            () => copy.Title.ShouldBe(TaskTemplateDialog.NewTitle, "Заголовок вікна"),
            () => copy.TemplateName.Text.ShouldBeEmpty($"Поле «{copy.TemplateName.Name}»"),
            () => copy.OkButton.IsEnabled.ShouldBeFalse($"OK доступна з порожнім «{copy.TemplateName.Name}»")));
        copy.Form.ShouldHaveTemplateValues(changed);
        copy.Form.ShouldHaveResponsible(responsible);
        copy.TemplateName.Fill(copyName);
        copy.Save();

        templates.TemplateNames.ShouldContain(templateName, "Шаблон після «Duplizieren»");
        var copyView = templates.View(copyName);
        OnCleanup(copyView.CancelIfOpen);
        copyView.Form.ShouldHaveTemplateValues(changed);
        copyView.Form.ShouldHaveResponsible(responsible);
        copyView.Cancel();
    }

    /// <summary>
    /// «Name der Vorlage» обов'язкове й унікальне без урахування регістру: у новому шаблоні OK недоступна з порожньою назвою
    /// й з назвою іншого шаблону великими літерами, а з новою назвою — доступна; «Abbrechen» нічого не зберігає.
    /// «Umbenennen» відкриває «Vorlage umbenennen» з поточною назвою: OK недоступна, поки назва та сама чи збігається з
    /// назвою іншого шаблону (і в іншому регістрі). Нова назва замінює стару і в списку (зі зміненим «Änderungsdatum»),
    /// і в меню «Neu» вузла Aufgaben.
    /// </summary>
    [Test]
    [AllureSeverity(SeverityLevel.normal)]
    public void Aufgabenvorlage_NameAndRename()
    {
        var mainPage = LogInAndOpenDokumentation();
        var customer = CustomerCase.Random();
        var first = customer.NewTemplateName();
        var second = customer.NewTemplateName();
        var renamed = customer.NewTemplateName();
        var unsaved = customer.NewTemplateName();
        DeleteAfterTest(mainPage.OpenTaskTemplates, first, second, renamed, unsaved);

        var templates = mainPage.OpenTaskTemplates();
        OnCleanup(templates.CloseIfOpen);
        CreateInTemplatesWindow(templates, customer, first);
        CreateInTemplatesWindow(templates, customer, second);

        var dialog = templates.OpenNew();
        OnCleanup(dialog.CancelIfOpen);
        dialog.Form.Name.Fill(customer.NewTemplateTask().Name);
        ShouldAllowOk(dialog.OkButton, false, "«Name der Vorlage» порожнє");
        dialog.TemplateName.Fill(unsaved);
        ShouldAllowOk(dialog.OkButton, true, "назва шаблону нова");
        dialog.TemplateName.Fill(first.ToUpperInvariant());
        ShouldAllowOk(dialog.OkButton, false, "така назва вже є (в іншому регістрі)");
        dialog.Cancel();
        templates.TemplateNames.ShouldNotContain(unsaved, "Шаблон з вікна, закритого «Abbrechen»");

        var rename = templates.Rename(first);
        OnCleanup(rename.CancelIfOpen);
        rename.TemplateName.ShouldHaveText(first);
        ShouldAllowOk(rename.OkButton, false, "назва та сама");
        rename.TemplateName.Fill(renamed);
        ShouldAllowOk(rename.OkButton, true, "назва нова");
        rename.TemplateName.Fill(second);
        ShouldAllowOk(rename.OkButton, false, "така назва вже є");
        rename.TemplateName.Fill(renamed);
        ShouldAllowOk(rename.OkButton, true, "назва нова");
        rename.TemplateName.Fill(second.ToLowerInvariant());
        ShouldAllowOk(rename.OkButton, false, "така назва вже є (в іншому регістрі)");
        var renamedFrom = DateTime.Now;
        rename.Rename(renamed);
        var renamedTo = DateTime.Now;

        AllureApi.Step("Перевірити перейменований шаблон у списку", () => templates.ShouldSatisfyAllConditions(
            () => templates.TemplateNames.ShouldNotContain(first, "Шаблон зі старою назвою"),
            () => templates.Template(renamed).Changed.ShouldBeInRange(Minute(renamedFrom), renamedTo, "«Änderungsdatum»"),
            () => templates.Template(renamed).ChangedBy.ShouldBe(mainPage.UserDisplayName, "«Geändert durch»")));
        templates.Close();

        var menu = mainPage.NewTaskMenuItems();
        AllureApi.Step("Перевірити, що меню «Neu» показує нову назву", () => menu.ShouldSatisfyAllConditions(
            () => menu.ShouldContain(renamed, "Підменю «Neu» вузла Aufgaben"),
            () => menu.ShouldNotContain(first, "Підменю «Neu» вузла Aufgaben")));
    }

    /// <summary>
    /// Меню рядка в «Aufgabenvorlagen»: «Vorlage erstellen», «Duplizieren», «Zu Favoriten hinzufügen», «Anzeigen»,
    /// «Bearbeiten», «Umbenennen», «Löschen», «Aktualisieren». Обраний шаблон отримує зірку, а в списку й у меню «Neu»
    /// стоїть перед необраними, хоч за назвою йде пізніше; у меню рядка тоді «Aus Favoriten entfernen», і воно повертає
    /// порядок за назвою. Швидкий пошук за частиною назви лишає в списку лише цей шаблон, хрестик повертає всі. «Löschen»
    /// питає «Soll die ausgewählte Vorlage gelöscht werden?»: «Nein» лишає шаблон, «Ja» прибирає його зі списку й меню.
    /// </summary>
    [Test]
    [AllureSeverity(SeverityLevel.normal)]
    public void Aufgabenvorlage_FavoritesSearchAndDelete()
    {
        var mainPage = LogInAndOpenDokumentation();
        var customer = CustomerCase.Random();
        string[] names = [customer.NewTemplateName(), customer.NewTemplateName()];
        // ADAICA сортує назви за абеткою; вони відрізняються латинським словом на початку й номером у кінці.
        var (earlier, later) = string.CompareOrdinal(names[0], names[1]) < 0 ? (names[0], names[1]) : (names[1], names[0]);
        DeleteAfterTest(mainPage.OpenTaskTemplates, earlier, later);

        var templates = mainPage.OpenTaskTemplates();
        OnCleanup(templates.CloseIfOpen);
        CreateInTemplatesWindow(templates, customer, earlier);
        CreateInTemplatesWindow(templates, customer, later);
        templates.RowMenuItems(later).ShouldBe(TemplateRowMenu, ignoreOrder: false, "Меню рядка шаблону");
        ShouldBeListedBefore(templates.TemplateNames, earlier, later, "у списку «Aufgabenvorlagen»");

        templates.AddToFavorites(later);
        ShouldBeListedBefore(templates.TemplateNames, later, earlier, "у списку «Aufgabenvorlagen»");
        var favoriteMenu = templates.RowMenuItems(later);
        AllureApi.Step("Перевірити меню рядка обраного шаблону", () => favoriteMenu.ShouldSatisfyAllConditions(
            () => favoriteMenu.ShouldContain("Aus Favoriten entfernen", "Меню рядка"),
            () => favoriteMenu.ShouldNotContain("Zu Favoriten hinzufügen", "Меню рядка")));

        var all = templates.TemplateNames;
        // Номер у кінці назви є лише в одного шаблону.
        templates.Search(earlier.Split(' ')[^1], [earlier]);
        templates.ClearSearch(all);
        templates.Close();
        ShouldBeListedBefore(mainPage.NewTaskMenuItems(), later, earlier, "у меню «Neu» вузла Aufgaben");

        templates = mainPage.OpenTaskTemplates();
        OnCleanup(templates.CloseIfOpen);
        templates.RemoveFromFavorites(later);
        ShouldBeListedBefore(templates.TemplateNames, earlier, later, "у списку «Aufgabenvorlagen»");

        var question = templates.Delete(earlier);
        question.Message.ShouldHaveText("Soll die ausgewählte Vorlage gelöscht werden?");
        question.Button("Nein").Click();
        templates.TemplateNames.ShouldContain(earlier, "Шаблон після «Nein»");
        templates.Delete(earlier).Button("Ja").Click();
        Wait.Until(() => !templates.Contains(earlier), $"зникнення шаблону «{earlier}» зі списку");
        templates.Close();

        var menu = mainPage.NewTaskMenuItems();
        AllureApi.Step("Перевірити меню «Neu» після видалення шаблону", () => menu.ShouldSatisfyAllConditions(
            () => menu.ShouldNotContain(earlier, "Підменю «Neu» вузла Aufgaben"),
            () => menu.ShouldContain(later, "Підменю «Neu» вузла Aufgaben")));
    }

    /// <summary>
    /// Після тесту видаляє шаблони <paramref name="names"/>, які є в «Aufgabenvorlagen». Реєструйте до вікон завдань і
    /// шаблонів: прибирання йде у зворотному порядку, тож вони закриються раніше.
    /// </summary>
    private void DeleteAfterTest(Func<TaskTemplatesWindow> openTemplates, params string[] names) =>
        OnCleanup(() => AllureApi.Step($"Видалити шаблони тесту: {string.Join(", ", names)}", () =>
        {
            var templates = openTemplates();
            try
            {
                templates.DeleteIfPresent(names);
            }
            finally
            {
                templates.CloseIfOpen();
            }
        }));

    /// <summary>
    /// Заповнює вікно нового завдання всіма полями справи, робить відповідальним другого користувача, зберігає вікно як
    /// шаблон <paramref name="templateName"/> і закриває вікно завдання без збереження.
    /// </summary>
    private TaskTemplateData SaveAsTemplateAndCancel(TaskDialog dialog, CustomerCase customer, string templateName) =>
        AllureApi.Step($"Зберегти нове завдання як шаблон «{templateName}»", () =>
        {
            var task = customer.NewTemplateTask();
            dialog.Form.Fill(task);
            var responsible = dialog.Form.ChooseResponsible(TestSettings.SecondUserContact);
            var templateDialog = dialog.SaveAsTemplate();
            OnCleanup(templateDialog.CancelIfOpen);
            templateDialog.TemplateName.Fill(templateName);
            templateDialog.Save();
            dialog.Cancel();
            return new TaskTemplateData(templateName, task, responsible);
        });

    /// <summary>
    /// Створює шаблон кнопкою «Neu» у «Aufgabenvorlagen»: усі поля справи, «Verantwortlich» — поточний користувач
    /// (так ADAICA заповнює новий шаблон).
    /// </summary>
    private TaskTemplateData CreateInTemplatesWindow(TaskTemplatesWindow templates, CustomerCase customer, string templateName) =>
        AllureApi.Step($"Створити шаблон «{templateName}» у «{TaskTemplatesWindow.Title}»", () =>
        {
            var dialog = templates.OpenNew();
            OnCleanup(dialog.CancelIfOpen);
            var task = customer.NewTemplateTask();
            dialog.Form.FillTemplate(task);
            var responsible = dialog.Form.Responsible.Text;
            dialog.TemplateName.Fill(templateName);
            dialog.Save();
            Wait.Until(() => templates.Contains(templateName), $"шаблону «{templateName}» у списку");
            return new TaskTemplateData(templateName, task, responsible);
        });

    /// <summary>
    /// Перевіряє вікно нового завдання з полями шаблону: усе — як у шаблоні, «Beginnt am» — сьогодні о 00:00. Назву
    /// завдання з шаблону видно, лише коли вимкнено «Name des übergeordneten Elements vorausfüllen» (інакше в полі —
    /// назва батька), тож після перевірки тест вписує її сам, як користувач. Повертає очікувані поля завдання.
    /// </summary>
    private static TaskData ShouldBeFilledFromTemplate(TaskDialog dialog, TaskTemplateData template) =>
        AllureApi.Step($"Перевірити поля завдання з шаблону «{template.Name}»", () =>
        {
            var expected = template.Task with { Begin = DateTime.Today };
            dialog.Form.ShouldHaveOwnName(expected.Name);
            dialog.Form.Name.Fill(expected.Name);
            dialog.Form.ShouldHaveValues(expected);
            dialog.Form.ShouldHaveResponsible(template.Responsible);
            return expected;
        });

    private static void SaveTaskFromTemplate(MainPage mainPage, TaskDialog dialog, TaskData expected, string responsible)
    {
        var number = dialog.Form.Number.Text;
        dialog.Save();
        var savedTask = mainPage.WaitForOpenedTask(number);
        savedTask.ShowMainTab();
        savedTask.ShouldHaveValues(expected);
        savedTask.ShouldHaveResponsible(responsible);
    }

    // Підменю нового завдання: спершу «Neue Aufgabe» й «Vorlage erstellen/bearbeiten», далі шаблони.
    private static void ShouldOfferTemplates(IReadOnlyList<string> menu, string templateName) =>
        AllureApi.Step($"Перевірити, що меню нового завдання пропонує шаблон «{templateName}»", () => menu.ShouldSatisfyAllConditions(
            () => menu.Take(2).ShouldBe(["Neue Aufgabe", "Vorlage erstellen/bearbeiten"], ignoreOrder: false, "Перші пункти меню"),
            () => menu.Skip(2).ShouldContain(templateName, "Шаблони в меню")));

    private static void ShouldBeCreatedBy(TaskTemplateRow row, string user, DateTime from, DateTime to) =>
        AllureApi.Step($"Перевірити рядок шаблону «{row.Name}»: створив і змінив {user} щойно, не в обраному", () => row.ShouldSatisfyAllConditions(
            () => row.Created.ShouldBeInRange(Minute(from), to, "«Erstelldatum»"),
            () => row.CreatedBy.ShouldBe(user, "«Erstellt durch»"),
            () => row.Changed.ShouldBeInRange(Minute(from), to, "«Änderungsdatum»"),
            () => row.ChangedBy.ShouldBe(user, "«Geändert durch»"),
            () => row.IsFavorite.ShouldBeFalse("Шаблон в обраному")));

    private static void ShouldBeListedBefore(IReadOnlyList<string> names, string first, string second, string where) =>
        AllureApi.Step($"Перевірити, що «{first}» стоїть перед «{second}» {where}", () =>
        {
            var firstIndex = names.ToList().IndexOf(first);
            var secondIndex = names.ToList().IndexOf(second);
            firstIndex.ShouldBeGreaterThanOrEqualTo(0, $"«{first}» {where}");
            secondIndex.ShouldBeGreaterThan(firstIndex, $"Позиція «{second}» {where}: {string.Join(" | ", names)}");
        });

    // OK стає доступною чи недоступною, щойно ADAICA перевірить назву; чекаємо очікуваного стану.
    private static void ShouldAllowOk(ButtonControl ok, bool allowed, string because) =>
        AllureApi.Step($"Перевірити, що OK {(allowed ? "доступна" : "недоступна")}: {because}", () =>
            Wait.Until(() => ok.IsEnabled == allowed, $"{(allowed ? "доступної" : "недоступної")} кнопки OK ({because})"));

    // Дати в списку — з точністю до хвилини.
    private static DateTime Minute(DateTime value) => new(value.Ticks - value.Ticks % TimeSpan.TicksPerMinute, value.Kind);
}
