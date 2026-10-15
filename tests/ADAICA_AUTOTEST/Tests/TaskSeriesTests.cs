using ADAICA_AUTOTEST.Controls;
using ADAICA_AUTOTEST.Pages;
using Allure.Net.Commons;
using Allure.Net.Commons.Attributes;
using static ADAICA_AUTOTEST.Tests.TaskSeriesTests.SeriesCase;

namespace ADAICA_AUTOTEST.Tests;

/// <summary>
/// Серії завдань: вікно «Aufgabenzyklus» (кнопка «Serientyp» на бланку завдання). Завдання серії ADAICA створює на
/// сервері, коли завдання збережено: усі, чий час уже настав (актуальним завдання стає за добу до свого початку), і ще
/// одне наступне зі статусом «Aufgabe geplant». Решту створює джоб бекенду, коли настане їхній день. Тому серії тут
/// починаються в минулому: їхні завдання з'являються одразу, і тест бачить їх під проєктом у вузлі «Aufgaben» — у
/// дереві й у списку з датами. Очікувані дати тест рахує сам (<see cref="TaskSeriesSchedule"/>). Записи лишаються в базі.
/// </summary>
[AllureSuite("Серії завдань")]
public class TaskSeriesTests : AdaicaTestBase
{
    private const string TasksFolder = "Aufgaben";
    private const string PlannedStatus = "Aufgabe geplant";
    private const string ShortMonthsMessage =
        "Einige Monate haben weniger als 31 Tage. In diesen Monaten fällt das Serienelement auf den letzten Tag des Monats.";

    // Завдання серії сервер створює після збереження, і в списку вони з'являються не одразу.
    private static readonly TimeSpan TasksTimeout = TimeSpan.FromSeconds(30);

    /// <summary>Серії для <see cref="Aufgabenzyklus_SeriesTasks"/>: кожен Serienmuster і кожен варіант кінця серії й завдань.</summary>
    public enum SeriesCase
    {
        /// <summary>Täglich, кожні 2 дні; «Endet nach 5»; «Am Tagesende beenden».</summary>
        EveryOtherDay,

        /// <summary>Täglich, «Jeden Arbeitstag»; «Endet am» — середа наступного тижня; «Über Dauer berechnen» 4 години.</summary>
        EveryWorkday,

        /// <summary>Wöchentlich, понеділок, середа, п'ятниця, починаючи із середи; «Endet nach 6»; «Über Dauer» 2 години.</summary>
        MondayWednesdayFriday,

        /// <summary>Wöchentlich, кожні 2 тижні у вівторок і четвер; «Endet nach 6»; без «Fällig am».</summary>
        TuesdayThursdayEveryOtherWeek,

        /// <summary>Wöchentlich, п'ятниця, починаючи з вівторка; «Endet nach 4»; «Am Wochenende beenden» (неділя).</summary>
        FridayToEndOfWeek,

        /// <summary>Monatlich, 31-ше (у коротших місяцях — останній день); «Endet nach 6»; «Am Monatsende beenden».</summary>
        LastDayOfMonth,

        /// <summary>Monatlich, друга середа; «Endet nach 5»; «Month: Am 21».</summary>
        SecondWednesday,

        /// <summary>Monatlich, остання п'ятниця кожного другого місяця; «Endet nach 4»; «Am Monatsende beenden».</summary>
        LastFridayEveryOtherMonth,

        /// <summary>Jährlich, 14 жовтня; «Endet nach 3»; «Year: Am 31 Dezember».</summary>
        October14,

        /// <summary>Jährlich, четвертий четвер листопада; «Endet nach 3»; «Am Jahresende beenden».</summary>
        FourthThursdayOfNovember,

        /// <summary>Jährlich, останній понеділок лютого кожного другого року; «Endet nach 3»; без «Fällig am».</summary>
        LastMondayOfFebruaryEveryOtherYear,

        /// <summary>
        /// Täglich з позавчора на 10 днів о 00:00: одразу є завдання до завтра включно (завтрашнє актуальне за добу) і
        /// заплановане післязавтрашнє, решту чотири створить джоб.
        /// </summary>
        PlannedAhead,
    }

    /// <summary>
    /// Створює проєкт і під ним завдання, задає йому серію <paramref name="seriesCase"/> з початком у минулому й запускає
    /// її. Після OK у «Aufgabenzyklus» завдання має бути першим у серії: «Beginnt am» — перший день серії о тій самій
    /// годині, «Fällig am» — за «Enddatum für einzelne Zyklus-Aufgaben» (для 31-го числа ADAICA ще попереджає про короткі
    /// місяці). Після збереження під проєктом у «Aufgaben» — саме ті завдання, які дає серія: у дереві стільки ж вузлів з
    /// назвою завдання, а в списку вузла — їхні «Beginnt am» і «Fällig am»; незавершена серія має ще одне наступне
    /// завдання «Aufgabe geplant».
    /// </summary>
    [TestCase(EveryOtherDay)]
    [TestCase(EveryWorkday)]
    [TestCase(MondayWednesdayFriday)]
    [TestCase(TuesdayThursdayEveryOtherWeek)]
    [TestCase(FridayToEndOfWeek)]
    [TestCase(LastDayOfMonth)]
    [TestCase(SecondWednesday)]
    [TestCase(LastFridayEveryOtherMonth)]
    [TestCase(October14)]
    [TestCase(FourthThursdayOfNovember)]
    [TestCase(LastMondayOfFebruaryEveryOtherYear)]
    [TestCase(PlannedAhead)]
    [AllureSeverity(SeverityLevel.critical)]
    public void Aufgabenzyklus_SeriesTasks(SeriesCase seriesCase)
    {
        var series = Series(seriesCase, DateTime.Today);
        var mainPage = LogInAndOpenDokumentation();
        var customer = CustomerCase.Random();
        var projectNode = CreateProject(mainPage, customer);

        var task = customer.NewSeriesTask(series.Pattern);
        // О 00:00 заплановане саме післязавтрашнє завдання, о котрій би годині не йшов прогін.
        if (seriesCase == PlannedAhead)
            task = task with { Begin = DateTime.Today };

        var (number, savedAt) = CreateSeriesTask(
            mainPage, projectNode, task, series, seriesCase == LastDayOfMonth ? ShortMonthsMessage : null);

        ShouldShowSeriesTasks(mainPage, projectNode, task.Name, number, TaskSeriesSchedule.CreatedAt(series, task.Begin.TimeOfDay, savedAt));
    }

    /// <summary>
    /// Змінює запущену серію: «Bearbeiten» на вкладці «Aufgabenzyklus», «Endet nach» 3 → 4, OK і збереження. Під проєктом
    /// має бути 4 завдання: три наявні й одне нове. Падає через баг ADAICA: після збереження серія ділиться, і ADAICA
    /// заново створює всі завдання від початку; перше лишається шаблоном нової серії, а решта наявних дублюються (6 завдань
    /// замість 4).
    /// </summary>
    [Test]
    [AllureSeverity(SeverityLevel.critical)]
    public void Aufgabenzyklus_EditKeepsCreatedTasks()
    {
        // П'ятниці, починаючи з вівторка шість тижнів тому: і три, і чотири завдання — у минулому.
        var series = new TaskSeriesData(
            new SeriesPattern.Weekly(1, [DayOfWeek.Friday]), Monday(DateTime.Today).AddDays(-41), new SeriesEnd.AfterCount(3), new SeriesTaskDue.EndOfPeriod());
        var mainPage = LogInAndOpenDokumentation();
        var customer = CustomerCase.Random();
        var projectNode = CreateProject(mainPage, customer);
        var task = customer.NewSeriesTask(series.Pattern);
        var (number, _) = CreateSeriesTask(mainPage, projectNode, task, series);

        var form = mainPage.WaitForOpenedTask(number);
        OnCleanup(form.CancelIfEditing);
        var seriesDialog = form.EditSeries();
        OnCleanup(seriesDialog.CancelIfOpen);
        seriesDialog.Count.Fill(4);
        seriesDialog.Confirm();
        form.SaveChanges();

        var longer = series with { End = new SeriesEnd.AfterCount(4) };
        ShouldShowSeriesTasks(mainPage, projectNode, task.Name, number, TaskSeriesSchedule.CreatedAt(longer, task.Begin.TimeOfDay, DateTime.Now));
    }

    /// <summary>
    /// Стирає «Beginnt am» нового завдання й задає серію: щопонеділка з початком два тижні тому, «Am Wochenende beenden».
    /// Після OK завдання має стати першим у серії: «Beginnt am» — перший понеділок о 00:00, «Fällig am» — неділя того
    /// тижня о 23:59. Падає через баг ADAICA: обидва поля лишаються порожніми, і завдання зберігається без дат, хоча решта
    /// завдань серії дати має. Завдання тест не зберігає.
    /// </summary>
    [Test]
    [AllureSeverity(SeverityLevel.normal)]
    public void Aufgabenzyklus_EmptyBeginDate()
    {
        var series = new TaskSeriesData(
            new SeriesPattern.Weekly(1, [DayOfWeek.Monday]), Monday(DateTime.Today).AddDays(-14), new SeriesEnd.AfterCount(3), new SeriesTaskDue.EndOfPeriod());
        var mainPage = LogInAndOpenDokumentation();
        var dialog = mainPage.OpenNewTaskDialog();
        OnCleanup(dialog.CancelIfOpen);
        dialog.Form.Fill(CustomerCase.Random().NewSeriesTask(series.Pattern));
        dialog.Form.Begin.Clear();

        var seriesDialog = dialog.Form.OpenSeriesType();
        OnCleanup(seriesDialog.CancelIfOpen);
        seriesDialog.Fill(series);
        seriesDialog.Confirm();

        var first = TaskSeriesSchedule.Tasks(series, TimeSpan.Zero).First();
        dialog.Form.ShouldHaveDates(first.Begin, first.Due);
    }

    /// <summary>
    /// Ставить «Beginnt am» нового завдання на день через два тижні й відкриває «Aufgabenzyklus»: «Beginn» серії — цей
    /// день, і в Monatlich «Am N. Tag», і в Jährlich «Am N. [Monat]» за замовчуванням теж мають бути його число й місяць.
    /// Падає через баг ADAICA: число підставляється сьогоднішнє (місяць — правильний), і після OK завдання непомітно
    /// переноситься на інший день. Завдання тест не зберігає.
    /// </summary>
    [Test]
    [AllureSeverity(SeverityLevel.normal)]
    public void Aufgabenzyklus_DefaultDayOfMonth()
    {
        var mainPage = LogInAndOpenDokumentation();
        var dialog = mainPage.OpenNewTaskDialog();
        OnCleanup(dialog.CancelIfOpen);
        var task = CustomerCase.Random().NewSeriesTask(new SeriesPattern.MonthlyOnDay(1, 1));
        // Через 15 днів число завжди інше, ніж сьогодні.
        var begin = task.Begin.AddDays(15);
        dialog.Form.Fill(task with { Begin = begin });

        var seriesDialog = dialog.Form.OpenSeriesType();
        OnCleanup(seriesDialog.CancelIfOpen);
        var start = seriesDialog.Start.Value;
        seriesDialog.Monthly.Select();
        var monthlyDay = seriesDialog.MonthlyDay.Value;
        seriesDialog.Yearly.Select();
        var yearlyDay = seriesDialog.YearlyDay.Value;
        var yearlyMonth = seriesDialog.YearlyDateMonth.SelectedText;
        seriesDialog.Cancel();

        AllureApi.Step($"Перевірити значення за замовчуванням для «Beginnt am» {AdaicaFormat.FormatDate(begin)}", () =>
            this.ShouldSatisfyAllConditions(
                () => start.ShouldBe(begin.Date, "«Beginn» серії"),
                () => monthlyDay.ShouldBe(begin.Day, "Monatlich: «Am … Tag»"),
                () => yearlyDay.ShouldBe(begin.Day, "Jährlich: «Am … [Monat]»"),
                () => yearlyMonth.ShouldBe(AdaicaFormat.FormatMonth(begin.Month), "Jährlich: місяць")));
    }

    /// <summary>
    /// Ставить «Beginnt am» нового завдання на найближче 7-ме, 14-те й 21-ше число й щоразу перевіряє, який тиждень місяця
    /// вікно «Aufgabenzyklus» пропонує в Monatlich «Am [Woche] [Wochentag]» і в Jährlich «Am [Woche] [Wochentag] im
    /// [Monat]»: 7-ме число — завжди перший такий день тижня в місяці, 14-те — другий, 21-ше — третій. День тижня й місяць
    /// мають бути від цієї ж дати. Падає через баг ADAICA: тиждень більший на один (ADAICA рахує «число / 7 + 1»).
    /// Завдання тест не зберігає.
    /// </summary>
    [Test]
    [AllureSeverity(SeverityLevel.normal)]
    public void Aufgabenzyklus_DefaultWeekOfMonth()
    {
        var mainPage = LogInAndOpenDokumentation();
        var dialog = mainPage.OpenNewTaskDialog();
        OnCleanup(dialog.CancelIfOpen);
        var task = CustomerCase.Random().NewSeriesTask(new SeriesPattern.MonthlyOnWeekday(WeekOfMonth.First, DayOfWeek.Monday, 1));
        dialog.Form.Fill(task);

        var checks = new List<Action>();
        foreach (var day in new[] { 7, 14, 21 })
        {
            var begin = NextDayOfMonth(day) + task.Begin.TimeOfDay;
            var week = SeriesPattern.WeekLabel((WeekOfMonth)((day - 1) / 7 + 1));
            var weekday = AdaicaFormat.FormatDayOfWeek(begin.DayOfWeek);
            var month = AdaicaFormat.FormatMonth(begin.Month);
            dialog.Form.Begin.Fill(begin);

            var seriesDialog = dialog.Form.OpenSeriesType();
            OnCleanup(seriesDialog.CancelIfOpen);
            seriesDialog.Monthly.Select();
            seriesDialog.MonthlyOnWeekday.Select();
            var monthly = (Week: seriesDialog.MonthlyWeek.SelectedText, Weekday: seriesDialog.MonthlyWeekday.SelectedText);
            seriesDialog.Yearly.Select();
            seriesDialog.YearlyOnWeekday.Select();
            var yearly = (Week: seriesDialog.YearlyWeek.SelectedText, Weekday: seriesDialog.YearlyWeekday.SelectedText,
                Month: seriesDialog.YearlyWeekdayMonth.SelectedText);
            seriesDialog.Cancel();

            var date = AdaicaFormat.FormatDate(begin);
            checks.Add(() => monthly.ShouldBe((week, weekday), $"Monatlich для {date}: «Am [Woche] [Wochentag]»"));
            checks.Add(() => yearly.ShouldBe((week, weekday, month), $"Jährlich для {date}: «Am [Woche] [Wochentag] im [Monat]»"));
        }

        AllureApi.Step("Перевірити запропонований тиждень місяця", () => this.ShouldSatisfyAllConditions(checks.ToArray()));
    }

    /// <summary>
    /// Задає новому завданню річну серію «Am 29. Februar» з початком 1 січня найближчого високосного року. Поле дня
    /// приймає 29 без жодної помилки, тож OK має бути доступна. Падає через баг ADAICA: OK недоступна без пояснення — для
    /// лютого ADAICA завжди дозволяє лише 28 днів, навіть у високосний рік. Завдання тест не зберігає.
    /// </summary>
    [Test]
    [AllureSeverity(SeverityLevel.normal)]
    public void Aufgabenzyklus_YearlyOnLeapDay()
    {
        var leapYear = Enumerable.Range(DateTime.Today.Year + 1, 4).First(DateTime.IsLeapYear);
        var series = new TaskSeriesData(
            new SeriesPattern.YearlyOnDate(29, 2, 1), new DateTime(leapYear, 1, 1), new SeriesEnd.AfterCount(3), new SeriesTaskDue.None());
        var mainPage = LogInAndOpenDokumentation();
        var dialog = mainPage.OpenNewTaskDialog();
        OnCleanup(dialog.CancelIfOpen);
        var task = CustomerCase.Random().NewSeriesTask(series.Pattern);
        dialog.Form.Fill(task with { Begin = series.Start + task.Begin.TimeOfDay });

        var seriesDialog = dialog.Form.OpenSeriesType();
        OnCleanup(seriesDialog.CancelIfOpen);
        seriesDialog.Fill(series);

        seriesDialog.OkButton.ShouldBeEnabled();
    }

    /// <summary>
    /// Відкриває «Aufgabenzyklus» нового завдання й перевіряє підписи варіанта «Am …» в «Enddatum für einzelne
    /// Zyklus-Aufgaben» у Monatlich і Jährlich. Падає через баг ADAICA: підписи «Month» і «Year» англійською (у XAML вони
    /// записані текстом, без перекладу). Завдання тест не зберігає.
    /// </summary>
    [Test]
    [AllureSeverity(SeverityLevel.minor)]
    public void Aufgabenzyklus_EndDateCaptionsInGerman()
    {
        var mainPage = LogInAndOpenDokumentation();
        var dialog = mainPage.OpenNewTaskDialog();
        OnCleanup(dialog.CancelIfOpen);
        dialog.Form.Fill(CustomerCase.Random().NewSeriesTask(new SeriesPattern.MonthlyOnDay(1, 1)));

        var seriesDialog = dialog.Form.OpenSeriesType();
        OnCleanup(seriesDialog.CancelIfOpen);
        seriesDialog.Monthly.Select();
        var monthly = seriesDialog.TaskDueOnDayOfMonth.Caption;
        seriesDialog.Yearly.Select();
        var yearly = seriesDialog.TaskDueOnDayOfYear.Caption;
        seriesDialog.Cancel();

        AllureApi.Step($"Перевірити підписи: Monatlich «{monthly}», Jährlich «{yearly}»", () =>
            this.ShouldSatisfyAllConditions(
                () => monthly.ShouldNotBe("Month", "Monatlich: підпис варіанта «Am N»"),
                () => yearly.ShouldNotBe("Year", "Jährlich: підпис варіанта «Am N [Monat]»")));
    }

    /// <summary>
    /// Серія для випадку <paramref name="seriesCase"/>, яка починається в минулому. Усі завдання, крім випадку
    /// <see cref="SeriesCase.PlannedAhead"/>, теж у минулому, тож ADAICA створює їх одразу й жодного не планує наперед.
    /// </summary>
    private static TaskSeriesData Series(SeriesCase seriesCase, DateTime today)
    {
        var monday = Monday(today);
        var firstOfMonth = new DateTime(today.Year, today.Month, 1);
        var yearsAgo = (int years) => new DateTime(today.Year - years, 1, 1);
        return seriesCase switch
        {
            EveryOtherDay => new(new SeriesPattern.Daily(2), today.AddDays(-14), new SeriesEnd.AfterCount(5), new SeriesTaskDue.EndOfPeriod()),
            // З понеділка три тижні тому до середи наступного тижня: вісім робочих днів, «Endet am» — теж робочий день.
            EveryWorkday => new(new SeriesPattern.Workdays(), monday.AddDays(-21), new SeriesEnd.OnDate(monday.AddDays(-12)), new SeriesTaskDue.AfterHours(4)),
            MondayWednesdayFriday => new(
                new SeriesPattern.Weekly(1, [DayOfWeek.Monday, DayOfWeek.Wednesday, DayOfWeek.Friday]), monday.AddDays(-26), new SeriesEnd.AfterCount(6), new SeriesTaskDue.AfterHours(2)),
            TuesdayThursdayEveryOtherWeek => new(
                new SeriesPattern.Weekly(2, [DayOfWeek.Tuesday, DayOfWeek.Thursday]), monday.AddDays(-55), new SeriesEnd.AfterCount(6), new SeriesTaskDue.None()),
            FridayToEndOfWeek => new(new SeriesPattern.Weekly(1, [DayOfWeek.Friday]), monday.AddDays(-41), new SeriesEnd.AfterCount(4), new SeriesTaskDue.EndOfPeriod()),
            LastDayOfMonth => new(new SeriesPattern.MonthlyOnDay(31, 1), firstOfMonth.AddMonths(-7), new SeriesEnd.AfterCount(6), new SeriesTaskDue.EndOfPeriod()),
            SecondWednesday => new(
                new SeriesPattern.MonthlyOnWeekday(WeekOfMonth.Second, DayOfWeek.Wednesday, 1), firstOfMonth.AddMonths(-6), new SeriesEnd.AfterCount(5), new SeriesTaskDue.OnDayOfMonth(21)),
            LastFridayEveryOtherMonth => new(
                new SeriesPattern.MonthlyOnWeekday(WeekOfMonth.Last, DayOfWeek.Friday, 2), firstOfMonth.AddMonths(-9), new SeriesEnd.AfterCount(4), new SeriesTaskDue.EndOfPeriod()),
            October14 => new(new SeriesPattern.YearlyOnDate(14, 10, 1), yearsAgo(3), new SeriesEnd.AfterCount(3), new SeriesTaskDue.OnDayOfYear(31, 12)),
            FourthThursdayOfNovember => new(
                new SeriesPattern.YearlyOnWeekday(WeekOfMonth.Fourth, DayOfWeek.Thursday, 11, 1), yearsAgo(3), new SeriesEnd.AfterCount(3), new SeriesTaskDue.EndOfPeriod()),
            LastMondayOfFebruaryEveryOtherYear => new(
                new SeriesPattern.YearlyOnWeekday(WeekOfMonth.Last, DayOfWeek.Monday, 2, 2), yearsAgo(6), new SeriesEnd.AfterCount(3), new SeriesTaskDue.None()),
            PlannedAhead => new(new SeriesPattern.Daily(1), today.AddDays(-3), new SeriesEnd.AfterCount(10), new SeriesTaskDue.EndOfPeriod()),
            _ => throw new ArgumentOutOfRangeException(nameof(seriesCase), seriesCase, null),
        };
    }

    /// <summary>Створює проєкт, під яким будуть завдання серії, і повертає його вузол у дереві.</summary>
    private string CreateProject(MainPage mainPage, CustomerCase customer)
    {
        var dialog = mainPage.OpenNewProjectDialog();
        OnCleanup(dialog.CancelIfOpen);
        var project = customer.NewProject();
        dialog.Form.Fill(project);
        var number = dialog.Form.Number.Text;
        dialog.Save();
        mainPage.WaitForOpenedProject(number);
        return NodeWithNumber(project.Name, number);
    }

    /// <summary>
    /// Створює під проєктом завдання <paramref name="task"/>, задає серію й перевіряє, що після OK завдання стало першим
    /// у серії, запускає серію й зберігає завдання. Повертає номер завдання й час збереження.
    /// </summary>
    /// <param name="expectedMessage">Повідомлення, яке ADAICA має показати після OK у «Aufgabenzyklus», або null.</param>
    private (string Number, DateTime SavedAt) CreateSeriesTask(
        MainPage mainPage, string projectNode, DashboardTaskData task, TaskSeriesData series, string? expectedMessage = null)
    {
        var dialog = mainPage.OpenNewChildTaskDialog(projectNode);
        OnCleanup(dialog.CancelIfOpen);
        dialog.Form.Fill(task);
        var number = dialog.Form.Number.Text;

        var seriesDialog = dialog.Form.OpenSeriesType();
        OnCleanup(seriesDialog.CancelIfOpen);
        seriesDialog.Fill(series);
        seriesDialog.Confirm().ShouldBe(expectedMessage, "Повідомлення ADAICA після OK у вікні «Aufgabenzyklus»");

        var first = TaskSeriesSchedule.Tasks(series, task.Begin.TimeOfDay).First();
        dialog.Form.ShouldHaveDates(first.Begin, first.Due);

        dialog.Form.StartSeries();
        dialog.Save();
        return (number, DateTime.Now);
    }

    /// <summary>
    /// Перевіряє завдання серії під проєктом у вузлі «Aufgaben»: у списку вузла — їхні «Beginnt am», «Fällig am» і статус
    /// «Aufgabe geplant» у запланованого, назва в усіх — назва завдання <paramref name="number"/>, і воно саме — перше в
    /// серії; у дереві — стільки ж вузлів з цією назвою.
    /// </summary>
    private static void ShouldShowSeriesTasks(
        MainPage mainPage, string projectNode, string taskName, string number, IReadOnlyList<SeriesTask> expected) =>
        AllureApi.Step($"Перевірити {expected.Count} завдань серії під «{projectNode}» → {TasksFolder}", () =>
        {
            var rows = SeriesTaskRows(mainPage, projectNode, expected.Count);
            var listed = rows
                .Select(row => new ListedTask(row.GetValueOrDefault("Beginnt am"), row.GetValueOrDefault("Fällig am"), row.GetValueOrDefault("Status") == PlannedStatus))
                .OrderBy(task => AdaicaFormat.ParseDateTime(task.Begin) ?? DateTime.MaxValue)
                .ThenBy(task => task.Due)
                .ToList();
            var first = rows.FirstOrDefault(row => row.GetValueOrDefault("Nummer") == number);

            mainPage.ShouldSatisfyAllConditions(
                () => listed.ShouldBe(expected.Select(Listed).ToList(), "Завдання серії в списку «Aufgaben», за «Beginnt am»"),
                () => rows.Select(row => row.GetValueOrDefault("Name")).ShouldAllBe(name => name == taskName, "Назви завдань серії"),
                () => first?.GetValueOrDefault("Beginnt am").ShouldBe(Listed(expected[0]).Begin, $"«Beginnt am» завдання {number}"),
                () => mainPage.ChildNodeNames(projectNode, TasksFolder).ShouldBe(Enumerable.Repeat(taskName, expected.Count), "Вузли завдань під проєктом у дереві"));
        });

    /// <summary>
    /// Рядки списку вузла «Aufgaben» під проєктом. Список сам не оновлюється, тож, доки в ньому менше
    /// <paramref name="expectedCount"/> завдань, вузол перевідкриваємо, але не довше <see cref="TasksTimeout"/>: що знайшлося,
    /// порівнює вже тест.
    /// </summary>
    private static IReadOnlyList<IReadOnlyDictionary<string, string>> SeriesTaskRows(MainPage mainPage, string projectNode, int expectedCount)
    {
        var deadline = DateTime.Now + TasksTimeout;
        while (true)
        {
            var rows = mainPage.OpenRecordFolder(projectNode, TasksFolder).Rows;
            if (rows.Count >= expectedCount || DateTime.Now > deadline)
                return rows;

            mainPage.SelectNavigatorNode(projectNode);
        }
    }

    private static ListedTask Listed(SeriesTask task) => new(
        AdaicaFormat.FormatDateTime(task.Begin),
        task.Due is { } due ? AdaicaFormat.FormatDateTime(due) : string.Empty,
        task.IsPlanned);

    private static DateTime Monday(DateTime day) => day.Date.AddDays(-(((int)day.DayOfWeek + 6) % 7));

    // Найближчий після сьогодні день з числом day.
    private static DateTime NextDayOfMonth(int day)
    {
        var date = DateTime.Today.AddDays(1);
        while (date.Day != day)
            date = date.AddDays(1);
        return date;
    }

    /// <summary>Завдання в списку вузла «Aufgaben»: «Beginnt am» і «Fällig am», як їх показує список, і чи заплановане воно.</summary>
    private sealed record ListedTask(string? Begin, string? Due, bool IsPlanned);
}
