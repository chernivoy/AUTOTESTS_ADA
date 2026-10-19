using ADAICA_AUTOTEST.Controls;
using ADAICA_AUTOTEST.Infrastructure;
using ADAICA_AUTOTEST.Pages;
using Allure.Net.Commons;
using Allure.Net.Commons.Attributes;
using FlaUI.Core.Tools;
using static ADAICA_AUTOTEST.Pages.TaskListFilter;

namespace ADAICA_AUTOTEST.Tests;

/// <summary>
/// Вікно «Kalender» — кнопка над списком «Meine Aufgaben» навігатора Dashboard (<see cref="TaskCalendarWindow"/>):
/// фільтри стану, швидкий пошук і його поля, «Kontakt», контекстні меню клітинки й смужки, кнопка «+». Тест створює свої
/// завдання (вони лишаються в базі) і звужує вікно швидким пошуком за номером завдання: тоді в планувальнику лише
/// смужки цього тесту, а решта клітинок порожня.
/// </summary>
public partial class DashboardTests
{
    /// <summary>
    /// Фільтри стану у вікні «Kalender» поєднуються за тими самими правилами, що й над списком «Meine Aufgaben», тож
    /// тест створює те саме завдання, що й <see cref="DashboardNavigator_TaskListFilter"/>, відкриває вікно (усі фільтри
    /// вимкнено, завдання видно), звужує його пошуком за номером і вмикає фільтри по одному.
    /// </summary>
    [TestCase(Unread)]
    [TestCase(Waiting)]
    [TestCase(Accepted)]
    [TestCase(Refused)]
    [TestCase(Done)]
    [TestCase(Undone)]
    [TestCase(Open)]
    [TestCase(Closed)]
    [TestCase(Overdue)]
    [TestCase(Actual)]
    [TestCase(Planned)]
    [AllureSeverity(SeverityLevel.critical)]
    public void DashboardNavigator_CalendarFilter(TaskListFilter filter)
    {
        var mainPage = LogInAndShowSingleNavigator("Dashboard");
        var taskList = mainPage.OpenUserTaskList();
        var created = CreateTaskForFilter(taskList, filter);

        var calendar = OpenCalendar(taskList);
        AllureApi.Step("Перевірити, що у вікні «Kalender» усі фільтри стану вимкнено", () =>
            calendar.Filters.Where(pair => pair.Value.IsOn).Select(pair => pair.Key.Label()).ShouldBeEmpty("Увімкнені фільтри"));
        calendar.Search(created.SearchText);
        calendar.IsShown(created.Task).ShouldBeTrue($"«{created.Task}» не видно у вікні «Kalender» без фільтрів");

        calendar.FiltersShowing(created.Task).ShouldBe(
            created.Expected,
            ignoreOrder: true,
            $"Фільтри, під якими видно «{created.Task}» (очікували: {string.Join(", ", created.Expected.Select(item => item.Label()))})");
    }

    /// <summary>
    /// Швидкий пошук у вікні «Kalender»: номер; два слова з назви в зворотному порядку — «Alle Wörter» знаходить,
    /// «Exakte Suche» — ні, а в порядку назви — знаходить; слово з назви разом зі словом, якого в завданні немає, —
    /// знаходить лише «Einzelnes Wort». Поля «Suchen in»: без «Nummer» номер ще знаходиться через примітку («Bezug: …»),
    /// без «Nummer» і «Bemerkung» — ні; без «Name» не знаходяться слова з назви. Після тесту поля повертаються.
    /// </summary>
    [Test]
    [AllureSeverity(SeverityLevel.critical)]
    public void DashboardNavigator_CalendarQuickSearch()
    {
        var mainPage = LogInAndShowSingleNavigator("Dashboard");
        var taskList = mainPage.OpenUserTaskList();
        var (task, number) = CreateCalendarTask(taskList, CustomerCase.Random());
        var row = Row(task);
        var calendar = OpenCalendar(taskList);

        // Слова назви, яких немає ні в примітці, ні в Kurzname: за ними завдання знаходиться лише через поле «Name».
        var words = task.Name.Split(' ')
            .Where(word => word.Length > 3 && !Contains(task.Remark, word) && !Contains(task.ShortName, word))
            .ToList();
        words.Count.ShouldBeGreaterThanOrEqualTo(2, $"У назві «{task.Name}» замало слів, яких немає в примітці");
        var (first, last) = (words[0], words[^1]);
        // Слово, якого немає в завданні.
        const string missing = "Wohnwagen";

        calendar.Search(number);
        ShouldBeFound(calendar, row, true, $"номер {number}");
        calendar.Count.Total.ShouldBe(1, "Завдань із цим номером");

        calendar.Search($"{last} {first}", SearchMode.AllWords);
        ShouldBeFound(calendar, row, true, $"«{last} {first}», {SearchMode.AllWords.Label()}");
        calendar.Search($"{last} {first}", SearchMode.ExactPhrase);
        ShouldBeFound(calendar, row, false, $"«{last} {first}», {SearchMode.ExactPhrase.Label()}");
        calendar.Search($"{first} {last}", SearchMode.ExactPhrase);
        ShouldBeFound(calendar, row, true, $"«{first} {last}», {SearchMode.ExactPhrase.Label()}");
        calendar.Search($"{first} {missing}", SearchMode.SingleWord);
        ShouldBeFound(calendar, row, true, $"«{first} {missing}», {SearchMode.SingleWord.Label()}");
        calendar.Search($"{first} {missing}", SearchMode.AllWords);
        ShouldBeFound(calendar, row, false, $"«{first} {missing}», {SearchMode.AllWords.Label()}");

        var fields = calendar.OpenSearchFields();
        var fieldsBefore = fields.Items;
        OnCleanup(() => RestoreSearchFields(calendar, fieldsBefore));
        fieldsBefore.Select(field => field.Name).ShouldBe(
            new[] { "Bemerkung", "Kommentar", "Kurzname", "Name", "Nummer", "Text (Volltextsuche)" }, ignoreOrder: false, "Поля «Suchen in»");
        fieldsBefore.Where(field => field.IsChecked == true).Select(field => field.Name).ShouldBe(
            ["Bemerkung", "Kurzname", "Name", "Nummer"], ignoreOrder: true, "Позначені поля «Suchen in»");
        fieldsBefore.Where(field => !field.IsEnabled).Select(field => field.Name).ShouldBe(
            ["Kommentar", "Text (Volltextsuche)"], ignoreOrder: true, "Недоступні поля «Suchen in»");
        fields.Set("Nummer", false);
        fields.Close();
        calendar.Search(number);
        ShouldBeFound(calendar, row, true, $"номер {number} без поля «Nummer» (номер є в примітці)");

        fields = calendar.OpenSearchFields();
        fields.Set("Bemerkung", false);
        fields.Close();
        calendar.Search(number);
        ShouldBeFound(calendar, row, false, $"номер {number} без полів «Nummer» і «Bemerkung»");

        fields = calendar.OpenSearchFields();
        fields.Set("Nummer", true);
        fields.Set("Bemerkung", true);
        fields.Set("Name", false);
        fields.Close();
        calendar.Search($"{first} {last}", SearchMode.AllWords);
        ShouldBeFound(calendar, row, false, $"«{first} {last}» без поля «Name»");
    }

    /// <summary>
    /// «Kontakt» вибирає не завдання, а стовпці учасників: спершу видно лише стовпець поточного користувача. Без
    /// позначки біля нього смужки завдання немає, з позначкою — знову є, «Alle auswählen» позначає всіх.
    /// </summary>
    [Test]
    [AllureSeverity(SeverityLevel.normal)]
    public void DashboardNavigator_CalendarContactFilter()
    {
        var mainPage = LogInAndShowSingleNavigator("Dashboard");
        var taskList = mainPage.OpenUserTaskList();
        var (task, number) = CreateCalendarTask(taskList, CustomerCase.Random());
        var row = Row(task);
        var calendar = OpenCalendar(taskList);
        calendar.Search(number);

        var contacts = calendar.OpenContacts();
        var user = taskList.UserName;
        AllureApi.Step($"Перевірити, що в «Kontakt» позначено лише «{user}»", () =>
            contacts.Items.Where(item => item.IsChecked == true).Select(item => item.Name).ShouldBe(new[] { user }, ignoreOrder: false, "Позначені учасники"));

        contacts.Set(user, false);
        ShouldBeFound(calendar, row, false, $"без стовпця «{user}»");
        contacts.Set(user, true);
        ShouldBeFound(calendar, row, true, $"зі стовпцем «{user}»");
        contacts.Set(TreeFilterPopup.SelectAll, true);
        AllureApi.Step("Перевірити, що «Alle auswählen» позначило всіх учасників", () => contacts.ShouldSatisfyAllConditions(
            () => contacts.AllSelected.ShouldBe(true, TreeFilterPopup.SelectAll),
            () => contacts.Items.Where(item => item.IsChecked != true).Select(item => item.Name).ShouldBeEmpty("Непозначені учасники")));
        ShouldBeFound(calendar, row, true, "з усіма стовпцями");
        contacts.Close();
    }

    /// <summary>
    /// Склад контекстних меню: меню клітинки («Einfügen als Duplikat» недоступне, поки нічого не скопійовано; «Zeitskala»
    /// у Wochenansicht приховано), його підменю «Neu» й «Ansichtwechsel» (позначено поточний вид), меню смужки й підменю
    /// «Öffnen» («In Aktuelle» і «In Gesamtliste» для Dashboard приховано), меню кнопки «+».
    /// </summary>
    [Test]
    [AllureSeverity(SeverityLevel.normal)]
    public void DashboardNavigator_CalendarMenus()
    {
        var mainPage = LogInAndShowSingleNavigator("Dashboard");
        var taskList = mainPage.OpenUserTaskList();
        var (task, number) = CreateCalendarTask(taskList, CustomerCase.Random());
        var row = Row(task);
        var calendar = OpenCalendar(taskList);
        calendar.Search(number);

        var menu = calendar.OpenCellMenu();
        menu.Items.ShouldBe(
            new CalendarMenuItem[] { new("Neu", true), new("Einfügen als Duplikat", false), new("Heute", true), new("Gehe zu Datum...", true), new("Ansichtwechsel", true) },
            ignoreOrder: false,
            "Меню клітинки");
        // Після «Neue Aufgabe» і «Vorlage erstellen/bearbeiten» ADAICA додає шаблони завдань, якщо вони є.
        menu.Submenu("Neu").Take(2).ShouldBe(
            new CalendarMenuItem[] { new("Neue Aufgabe", true), new("Vorlage erstellen/bearbeiten", true) },
            ignoreOrder: false,
            "Підменю «Neu»");
        menu.Close();

        menu = calendar.OpenCellMenu();
        menu.Submenu("Ansichtwechsel").ShouldBe(
            new CalendarMenuItem[] { new("Tagesansicht", true, false), new("Wochenansicht", true, true), new("Arbeitswochenansicht", true, false), new("Monatsansicht", true, false) },
            ignoreOrder: false,
            "Підменю «Ansichtwechsel»");
        menu.Close();

        menu = calendar.OpenTaskMenu(row);
        menu.Items.ShouldBe(
            new CalendarMenuItem[] { new("Öffnen", true), new("Duplizieren", true), new("Löschen", true), new("Eigenschaften", true) },
            ignoreOrder: false,
            "Меню смужки");
        menu.Submenu("Öffnen").ShouldBe(
            new CalendarMenuItem[] { new("Im Dokumentation-Navigator öffnen", true), new("In einem neuen Fenster öffnen", true) },
            ignoreOrder: false,
            "Підменю «Öffnen»");
        menu.Close();

        calendar.AddButtonMenuItems().ShouldBe(new[] { "Neue Aufgabe", "Vorlage erstellen/bearbeiten" }, ignoreOrder: false, "Меню кнопки «+»");
    }

    /// <summary>
    /// «Ansichtwechsel» у меню клітинки перемикає вид: Tagesansicht — один день, Arbeitswochenansicht — п'ять,
    /// Wochenansicht — сім, Monatsansicht — кілька тижнів; поточний вид позначено в підменю. Правий клік, що відкриває
    /// меню, виділяє клітинку, і новий вид показує її день, тож після Monatsansicht (клітинка в нижньому ряду місяця)
    /// Wochenansicht показує інший тиждень, а «Heute» повертає на тиждень із сьогоднішнім днем і смужкою завдання.
    /// </summary>
    [Test]
    [AllureSeverity(SeverityLevel.normal)]
    public void DashboardNavigator_CalendarViews()
    {
        var mainPage = LogInAndShowSingleNavigator("Dashboard");
        var taskList = mainPage.OpenUserTaskList();
        var (task, number) = CreateCalendarTask(taskList, CustomerCase.Random());
        var row = Row(task);
        var calendar = OpenCalendar(taskList);
        calendar.Search(number);

        AllureApi.Step("Перевірити, що вікно відкрилося у Wochenansicht на тижні із сьогоднішнім днем", () =>
        {
            calendar.View.ShouldBe(CalendarView.Week, "Вид");
            ShouldSpan(calendar.Period, 7, DateTime.Today);
        });

        foreach (var (view, days) in new[] { (CalendarView.Day, 1), (CalendarView.WorkWeek, 5), (CalendarView.Month, 28), (CalendarView.Week, 7) })
        {
            calendar.SwitchView(view);
            AllureApi.Step($"Перевірити вид «{view.Label()}» і його період", () =>
            {
                calendar.View.ShouldBe(view, "Вид");
                var (from, to) = calendar.Period;
                if (view == CalendarView.Month)
                    (to - from).TotalDays.ShouldBeGreaterThanOrEqualTo(days, $"Днів у періоді {from:dd.MM.yyyy}–{to:dd.MM.yyyy}");
                else
                    (to - from).TotalDays.ShouldBe(days, $"Днів у періоді {from:dd.MM.yyyy}–{to:dd.MM.yyyy}");
            });

            var menu = calendar.OpenCellMenu();
            menu.Submenu("Ansichtwechsel").Where(entry => entry.IsChecked == true).Select(entry => entry.Name)
                .ShouldBe(new[] { view.Label() }, ignoreOrder: false, "Позначений вид в «Ansichtwechsel»");
            menu.Close();
        }

        calendar.GoToToday();
        AllureApi.Step("Перевірити, що «Heute» показує тиждень із сьогоднішнім днем і смужкою завдання", () =>
        {
            calendar.View.ShouldBe(CalendarView.Week, "Вид");
            ShouldSpan(calendar.Period, 7, DateTime.Today);
            calendar.Bars.ShouldContain(row, "Смужки тижня");
        });
    }

    /// <summary>
    /// «Gehe zu Datum...» у меню клітинки: вікно показує день виділеної клітинки й поточний вид, а після OK планувальник
    /// має показати вписаний день у вибраному виді. Перехід не відбувається ні після OK, ні після Enter: планувальник
    /// лишається на тому самому тижні у Wochenansicht, хоча вікно показує нові дату й вид. Імовірний баг ADAICA, тест
    /// падає навмисно (README, «Тести, що падають навмисно»).
    /// </summary>
    [Test]
    [AllureSeverity(SeverityLevel.normal)]
    public void DashboardNavigator_CalendarGoToDate()
    {
        var mainPage = LogInAndShowSingleNavigator("Dashboard");
        var taskList = mainPage.OpenUserTaskList();
        var (task, number) = CreateCalendarTask(taskList, CustomerCase.Random());
        var row = Row(task);
        var calendar = OpenCalendar(taskList);
        calendar.Search(number);

        var dialog = calendar.OpenGoToDate();
        AllureApi.Step("Перевірити, що «Gehe zu Datum» показує день виділеної клітинки й поточний вид", () =>
        {
            // Правий клік, що відкрив меню, виділив клітинку в показаному тижні.
            var (from, to) = calendar.Period;
            var shown = AdaicaFormat.ParseDate(dialog.DateText);
            shown.ShouldNotBeNull("Datum");
            shown.Value.ShouldBeInRange(from, to.AddDays(-1), "Datum");
            dialog.ViewText.ShouldBe(CalendarView.Week.Label(), "Zeige In");
        });

        var target = DateTime.Today.AddDays(45);
        dialog.GoTo(target, CalendarView.Day);
        AllureApi.Step($"Перевірити, що показано {target:dd.MM.yyyy} у Tagesansicht без смужки сьогоднішнього завдання", () =>
        {
            // Планувальник переходить уже після закриття вікна.
            Retry.WhileFalse(() => calendar.View == CalendarView.Day && calendar.Period == (target, target.AddDays(1)), TestSettings.DefaultTimeout, ignoreException: true);
            calendar.ShouldSatisfyAllConditions(
                () => calendar.View.ShouldBe(CalendarView.Day, "Вид після «Gehe zu Datum»"),
                () => calendar.Period.ShouldBe((target, target.AddDays(1)), "Період після «Gehe zu Datum»"),
                () => calendar.Bars.ShouldNotContain(row, "Смужки показаного дня"));
        });
    }

    /// <summary>
    /// Меню порожньої клітинки → «Neu» → «Neue Aufgabe»: «Beginnt am» і «Fällig am» — межі клітинки (пів години), бо
    /// клітинку тест вибирає на пів години нижче за смужку свого завдання (10:00–11:00), тобто 11:30–12:00. Збережене
    /// завдання з'являється у вікні.
    /// </summary>
    [Test]
    [AllureSeverity(SeverityLevel.critical)]
    public void DashboardNavigator_CalendarNewTaskFromCell()
    {
        var mainPage = LogInAndShowSingleNavigator("Dashboard");
        var taskList = mainPage.OpenUserTaskList();
        var customer = CustomerCase.Random();
        var (task, number) = CreateCalendarTask(taskList, customer);
        var calendar = OpenCalendar(taskList);
        calendar.Search(number);

        var cell = CellBelow(task);
        var dialog = calendar.NewTaskFromCell(cell);
        OnCleanup(dialog.CancelIfOpen);
        dialog.Form.ShouldHaveDates(cell.Start, cell.Start.AddMinutes(30));

        var newNumber = dialog.Form.Number.Text;
        var newTask = customer.NewCalendarTask(newNumber) with { Begin = cell.Start, Due = cell.Start.AddMinutes(30) };
        dialog.Form.Fill(newTask);
        dialog.Save();

        calendar.Search(newNumber);
        ShouldBeFound(calendar, Row(newTask), true, $"нове завдання {newNumber}");
    }

    /// <summary>
    /// Кнопка «+» → «Neue Aufgabe»: доки в планувальнику не виділено клітинку, ADAICA бере дати з півгодини, у яку
    /// відкрито вікно («Beginnt am» 17:30, «Fällig am» 18:00, якщо зараз 17:31). Збережене завдання з'являється у вікні.
    /// </summary>
    [Test]
    [AllureSeverity(SeverityLevel.critical)]
    public void DashboardNavigator_CalendarNewTaskFromAddButton()
    {
        var mainPage = LogInAndShowSingleNavigator("Dashboard");
        var taskList = mainPage.OpenUserTaskList();
        var customer = CustomerCase.Random();
        var (task, number) = CreateCalendarTask(taskList, customer);
        var calendar = OpenCalendar(taskList);
        calendar.Search(number);

        var openedFrom = DateTime.Now;
        var dialog = calendar.NewTaskFromAddButton();
        OnCleanup(dialog.CancelIfOpen);
        var openedTo = DateTime.Now;
        AllureApi.Step("Перевірити, що «Beginnt am» і «Fällig am» — межі півгодини, у яку відкрито вікно", () =>
        {
            var begin = dialog.Form.Begin.Value;
            begin.ShouldNotBeNull("Beginnt am");
            begin.Value.ShouldBeOneOf(HalfHourStart(openedFrom), HalfHourStart(openedTo));
            dialog.Form.Due.Value.ShouldBe(begin.Value.AddMinutes(30), "Fällig am");
        });

        var newNumber = dialog.Form.Number.Text;
        var newTask = customer.NewCalendarTask(newNumber);
        dialog.Form.Fill(newTask);
        dialog.Save();

        calendar.Search(newNumber);
        ShouldBeFound(calendar, Row(newTask), true, $"нове завдання {newNumber}");
    }

    /// <summary>
    /// «Vorlage erstellen/bearbeiten» (у підменю «Neu» меню клітинки й у меню кнопки «+») відкриває вікно
    /// «Aufgabenvorlagen» з кнопками й колонками списку шаблонів; «Schließen» його закриває.
    /// </summary>
    [Test]
    [AllureSeverity(SeverityLevel.normal)]
    public void DashboardNavigator_CalendarTaskTemplates()
    {
        var mainPage = LogInAndShowSingleNavigator("Dashboard");
        var taskList = mainPage.OpenUserTaskList();
        var (_, number) = CreateCalendarTask(taskList, CustomerCase.Random());
        var calendar = OpenCalendar(taskList);
        calendar.Search(number);

        foreach (var open in new Func<TaskTemplatesWindow>[] { calendar.OpenTemplatesFromCell, calendar.OpenTemplatesFromAddButton })
        {
            var templates = open();
            OnCleanup(templates.CloseIfOpen);
            AllureApi.Step($"Перевірити вікно «{TaskTemplatesWindow.Title}»", () => templates.ShouldSatisfyAllConditions(
                () => templates.ColumnNames.ShouldBe(new[] { "Name", "Erstelldatum", "Erstellt durch", "Änderungsdatum", "Geändert durch" }, ignoreOrder: false, "Колонки шаблонів"),
                () => templates.NewButton.IsEnabled.ShouldBeTrue("«Neu» недоступна"),
                () => templates.CloseButton.IsEnabled.ShouldBeTrue("«Schließen» недоступна")));
            templates.Close();
        }
    }

    /// <summary>
    /// «Duplizieren» у меню смужки показує вікно «Aus dem Zwischenspeicher einfügen» і робить доступним «Einfügen als
    /// Duplikat» у меню клітинки. Вставка питає «Festlegungen für das Duplikat»: дати копії бере клітинка (11:30–12:00),
    /// тож перенести дати не можна, а відповідального й виконавців ADAICA пропонує перенести. Після OK копія з тією самою
    /// назвою й приміткою, але з іншим номером з'являється в клітинці, а вікно вставки закривається. Бланк копії тест читає
    /// в панелі під планувальником, вибравши її смужку.
    /// </summary>
    [Test]
    [AllureSeverity(SeverityLevel.critical)]
    public void DashboardNavigator_CalendarDuplicate()
    {
        var mainPage = LogInAndShowSingleNavigator("Dashboard");
        var taskList = mainPage.OpenUserTaskList();
        var (task, number) = CreateCalendarTask(taskList, CustomerCase.Random());
        var row = Row(task);
        var calendar = OpenCalendar(taskList);
        calendar.Search(number);

        OnCleanup(calendar.CancelPendingPaste);
        calendar.Duplicate(row);
        var menu = calendar.OpenCellMenu();
        menu.Items.ShouldContain(new CalendarMenuItem("Einfügen als Duplikat", true), "«Einfügen als Duplikat» після «Duplizieren»");
        menu.Close();

        var cell = CellBelow(task);
        var options = calendar.PasteDuplicate(cell);
        OnCleanup(options.CancelIfOpen);
        AllureApi.Step("Перевірити «Festlegungen für das Duplikat»: дати бере клітинка, відповідальний і виконавці переносяться", () => options.ShouldSatisfyAllConditions(
            () => options.BeginEndDate.IsEnabled.ShouldBeFalse("«Beginn- und Enddatum übernehmen» доступний"),
            () => options.BeginEndDate.IsChecked.ShouldBeFalse("«Beginn- und Enddatum übernehmen» увімкнено"),
            () => options.DoneDate.IsEnabled.ShouldBeFalse("«Datum \"Erledigt am\" übernehmen» доступний"),
            () => options.ClosedDate.IsEnabled.ShouldBeFalse("«Datum \"Geschlossen am\" übernehmen» доступний"),
            () => options.Administrator.IsChecked.ShouldBeTrue("«Verantwortlichen übernehmen» вимкнено"),
            () => options.Participants.IsChecked.ShouldBeTrue("«Ausführende(n) übernehmen» вимкнено")));
        options.Confirm();
        AllureApi.Step("Дочекатися копії у вікні: швидкий пошук за номером знаходить її через примітку", () =>
            Wait.Until(() => calendar.Bars.Count(bar => bar == row) == 2, "двох смужок завдання"));

        // Смужки впорядковано згори вниз: оригінал о 10:00, копія — о 11:30.
        calendar.SelectTask(row, index: 1);
        var copy = calendar.Details;
        AllureApi.Step("Перевірити бланк копії", () => copy.ShouldSatisfyAllConditions(
            () => copy.Name.ShouldHaveText(task.Name),
            () => copy.Number.Text.ShouldNotBe(number, "Номер копії"),
            () => copy.Begin.Value.ShouldBe(cell.Start, "Beginnt am"),
            () => copy.Due.Value.ShouldBe(cell.Start.AddMinutes(30), "Fällig am"),
            () => copy.Remark.ShouldHaveText(task.Remark)));
        AllureApi.Step("Перевірити, що після вставки вікно «Aus dem Zwischenspeicher einfügen» закрилося (без «Mehrfachverwendung» копія вставляється один раз)", () =>
            calendar.HasPendingPaste.ShouldBeFalse("Вікно «Aus dem Zwischenspeicher einfügen» відкрите"));
    }

    /// <summary>
    /// Меню смужки → «Öffnen» → «Im Dokumentation-Navigator öffnen»: головне вікно переходить у навігатор Dokumentation,
    /// виділяє завдання в «Aktuelle» й показує його бланк. Після тесту навігатор повертається на Dashboard.
    /// </summary>
    [Test]
    [AllureSeverity(SeverityLevel.normal)]
    public void DashboardNavigator_CalendarOpenInDokumentation()
    {
        var mainPage = LogInAndShowSingleNavigator("Dashboard");
        var taskList = mainPage.OpenUserTaskList();
        var (task, number) = CreateCalendarTask(taskList, CustomerCase.Random());
        var calendar = OpenCalendar(taskList);
        calendar.Search(number);

        var tabsBefore = mainPage.NavigatorTabNames.Count;
        OnCleanup(() => mainPage.CloseLastNavigatorTabs(keep: tabsBefore));
        OnCleanup(() => mainPage.SwitchToNavigatorTab("Dashboard"));
        calendar.OpenInDokumentation(Row(task));
        // Вікно «Kalender» показує бланк вибраного завдання з тими самими полями, тож для перевірки головного вікна його закриваємо.
        calendar.Close();

        mainPage.NavigatorTitle.ShouldHaveText("Dokumentation");
        mainPage.WaitForOpenedTask(number).Name.ShouldHaveText(task.Name);
        AllureApi.Step($"Перевірити, що в дереві виділено «Aktuelle → {task.Name}»", () =>
            mainPage.SelectedNavigatorNodePath.TakeLast(2).ShouldBe(new[] { "Aktuelle", task.Name }));
    }

    /// <summary>
    /// Меню смужки → «Öffnen» → «In einem neuen Fenster öffnen»: окреме вікно із заголовком — назвою завдання й бланком
    /// цього завдання; «Abbrechen» його закриває.
    /// </summary>
    [Test]
    [AllureSeverity(SeverityLevel.normal)]
    public void DashboardNavigator_CalendarOpenInNewWindow()
    {
        var mainPage = LogInAndShowSingleNavigator("Dashboard");
        var taskList = mainPage.OpenUserTaskList();
        var (task, number) = CreateCalendarTask(taskList, CustomerCase.Random());
        var calendar = OpenCalendar(taskList);
        calendar.Search(number);

        var window = calendar.OpenInNewWindow(Row(task), number);
        OnCleanup(window.CloseIfOpen);
        AllureApi.Step("Перевірити вікно завдання", () => window.ShouldSatisfyAllConditions(
            () => window.Title.ShouldBe(task.Name, "Заголовок вікна"),
            () => window.Form.Name.ShouldHaveText(task.Name),
            () => window.Form.ShortName.ShouldHaveText(task.ShortName),
            () => window.Form.Begin.Value.ShouldBe(task.Begin, "Beginnt am"),
            () => window.Form.Due.Value.ShouldBe(task.Due, "Fällig am"),
            () => window.Form.Remark.ShouldHaveText(task.Remark)));
        window.Close();
    }

    /// <summary>
    /// «Eigenschaften» у меню смужки показує Klassentyp «TASK» і внутрішній ID завдання. «Löschen» питає (питання
    /// «Löschen» з назвою завдання), після «Ja» смужка зникає, а завдання з цим ID з'являється в Papierkorb навігатора
    /// Dokumentation. Звідти тест видаляє його остаточно.
    /// </summary>
    [Test]
    [AllureSeverity(SeverityLevel.critical)]
    public void DashboardNavigator_CalendarPropertiesAndDelete()
    {
        var mainPage = LogInAndShowSingleNavigator("Dashboard");
        var taskList = mainPage.OpenUserTaskList();
        var (task, number) = CreateCalendarTask(taskList, CustomerCase.Random());
        var row = Row(task);
        var calendar = OpenCalendar(taskList);
        calendar.Search(number);

        var properties = calendar.OpenProperties(row);
        OnCleanup(() =>
        {
            if (properties.IsOpen)
                properties.Close();
        });
        var id = properties.Id;
        AllureApi.Step("Перевірити вікно «Eigenschaften»", () => properties.ShouldSatisfyAllConditions(
            () => properties.ClassType.ShouldBe("TASK", "Klassentyp"),
            () => id.ShouldMatch(@"^\d{12}TASK\d{6}$", "ID")));
        properties.Close();

        var question = calendar.Delete(row);
        AllureApi.Step($"Підтвердити видалення «{task.Name}»", () =>
        {
            question.ShouldSatisfyAllConditions(
                () => question.Title.ShouldBe("Löschen", "Заголовок питання"),
                () => question.Message.Text.ShouldStartWith("Folgende Elemente werden gelöscht:", customMessage: "Питання"));
            question.Button("Ja").Click();
        });
        AllureApi.Step("Перевірити, що смужки видаленого завдання у вікні немає", () =>
        {
            Wait.Until(() => calendar.Count.Total == 0, "порожнього результату пошуку після видалення");
            calendar.Bars.ShouldNotContain(row);
        });
        calendar.Close();

        var tabsBefore = mainPage.NavigatorTabNames.Count;
        OnCleanup(() => mainPage.CloseLastNavigatorTabs(keep: tabsBefore));
        mainPage.OpenNavigator("Dokumentation");
        var bin = mainPage.OpenRecycleBin();
        var entry = bin.WaitForEntry(id);
        AllureApi.Step("Перевірити рядок Papierkorb", () => entry.ShouldSatisfyAllConditions(
            () => entry.Name.ShouldBe(task.Name, "Name"),
            () => entry.ClassType.ShouldBe("TASK", "Klassentyp")));

        bin.DeleteForever(id).Button("Ja").Click();
        bin.ShouldNotContain(id);
    }

    private TaskCalendarWindow OpenCalendar(DashboardTaskList taskList)
    {
        var calendar = taskList.OpenCalendar();
        OnCleanup(calendar.CloseIfOpen);
        return calendar;
    }

    /// <summary>
    /// Створює кнопкою «+» над списком завдання для вікна «Kalender» (<see cref="CustomerCase.NewCalendarTask"/>):
    /// сьогодні з 10:00 до 11:00, з номером завдання в примітці.
    /// </summary>
    private (DashboardTaskData Task, string Number) CreateCalendarTask(DashboardTaskList taskList, CustomerCase customer)
    {
        var dialog = taskList.OpenNewTaskDialog();
        OnCleanup(dialog.CancelIfOpen);
        var number = dialog.Form.Number.Text;
        var task = customer.NewCalendarTask(number);
        dialog.Form.Fill(task);
        dialog.Save();
        return (task, number);
    }

    /// <summary>Повертає позначки полів «Suchen in», як вони були до тесту.</summary>
    private static void RestoreSearchFields(TaskCalendarWindow calendar, IReadOnlyList<TreeFilterItem> fields)
    {
        if (!calendar.IsOpen)
            return;

        var popup = calendar.OpenSearchFields();
        foreach (var field in fields.Where(field => field.IsEnabled && field.IsChecked is { } isChecked && popup.Items.First(item => item.Name == field.Name).IsChecked != isChecked))
            popup.Set(field.Name, field.IsChecked!.Value);
        popup.Close();
    }

    private static bool Contains(string text, string word) => text.Contains(word, StringComparison.OrdinalIgnoreCase);
}
