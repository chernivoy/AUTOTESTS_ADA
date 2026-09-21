using System.Globalization;
using System.Text.RegularExpressions;
using ADAICA_AUTOTEST.Infrastructure;
using ADAICA_AUTOTEST.Pages;
using Allure.Net.Commons;
using Allure.Net.Commons.Attributes;

namespace ADAICA_AUTOTEST.Tests;

/// <summary>
/// Повнотекстовий пошук (Elasticsearch) у вікні «Finden» (System → Finden…, вкладка Dokumentation).
/// Тест створює завдання й шукає його; записів зі спільними словами в базі багато, тому своє завдання
/// тест упізнає в результатах за номером. Перед перевірками «не знаходить» тест чекає, доки Elasticsearch
/// проіндексує нове завдання (<see cref="SearchWindow.WaitUntilFound"/>), інакше перевірка пройшла б випадково.
/// </summary>
/// <remarks>
/// Тести розкладено за темами в partial-файли: синтаксис запитів, німецька локаль, символи в тексті, номери,
/// типи записів, фільтри. Кожен випадок таблиці — окремий пошук; очікування перевірено в застосунку.
/// </remarks>
[AllureSuite("Пошук")]
public partial class SearchTests : AdaicaTestBase
{
    private const string InName = "Name (Volltextsuche)";
    private const string InRemark = "Bemerkung (Volltextsuche)";
    private const string InNumber = "Nummer";
    private const string InId = "ID";

    /// <summary>
    /// Завдання для перевірок специфіки Elasticsearch. У назві — складені слова з умлаутами й слово з дефісами,
    /// у примітці — фраза «Ende Monat». Жодне слово не повторюється в обох полях, тож «Fundstelle» однозначна.
    /// </summary>
    private const string FixtureName = "Kick-off-Meeting zur Wärmepumpe im Lagergebäude vorbereiten";

    private const string FixtureRemark = "Offerte für die Montage bis Ende Monat einholen, Elektriker und Sanitär koordinieren.";

    /// <summary>
    /// Завдання з розділовими знаками, числами й запозиченнями: «?» після слова, гендерна зірочка, німецькі лапки,
    /// «é», дата, час, сума у швейцарському форматі, скорочення з крапкою, email.
    /// </summary>
    private const string HandoverName = "Schlüsselübergabe für Mitarbeiter*innen im Café „Sonnenhof“ in Zürich";

    private const string HandoverRemark =
        "Passt der Termin am Montag? Bitte bis 30.09.2026 um 14:30 Uhr bestätigen. Angebot über CHF 1'250.50 inkl. MwSt., "
        + "Rechnung Nr. 4711 an empfang@hotel-alpenrose.ch schicken.";

    /// <summary>
    /// Завдання зі швейцарським і німецьким написанням поруч («Aussentür», «Grösse» — через ss, «Hauptstraße» — через ß),
    /// гендерною двокрапкою й скісною, ієрогліфами, емодзі, телефоном, відсотками, євро й «è».
    /// </summary>
    private const string DoorName = "Aussentür an der Hauptstraße 5a: Grösse für Kund:innen/Besucher prüfen";

    private const string DoorRemark =
        "Muster vom Lieferanten aus Shenzhen (深圳) sind da 👍 Ansprechpartner/in: Frau Keller-Meier, Tel. +41 44 123 45 67, "
        + "Rabatt 5 % bzw. 1.250,50 €. Öffnungszeiten beachten, Crème-Farbe gewünscht.";

    /// <summary>
    /// Завдання, де умлаути записано і як «ü», і як «ue»/«oe» (так пишуть прізвища й назви вулиць),
    /// а ще «ß», Binnen-I, розміри з комою, «×», «&amp;» і скорочення.
    /// </summary>
    private const string CallbackName = "Rückruf bei Herrn Mueller wegen Parkplätzen an der Goethestrasse";

    private const string CallbackRemark =
        "Die Maße des Tors (2,40 m × 3,00 m) vor Ort prüfen – bitte Maßband mitnehmen & Fotos machen. "
        + "MitarbeiterInnen informieren, Termin evtl. am Fr., 02.10.";

    // Кожен випадок — окремий пошук. InField == null: завдання знаходитися не має.
    private static readonly SearchCase[] WildcardCases =
    [
        new("ціле слово без джокерів", "Wärmepumpe", InName),
        new("початок слова без джокера не знаходить", "Wärme", null),
        new("початок слова з джокером ззаду («Txt*»)", "Wärme", InName, WildcardAfter: true),
        new("кінець слова без джокера не знаходить: складені слова не розбиваються", "pumpe", null),
        new("кінець слова з джокером ззаду не знаходить", "pumpe", null, WildcardAfter: true),
        new("кінець слова з джокером спереду («*Txt»)", "pumpe", InName, WildcardBefore: true),
        new("середина слова з обома джокерами", "rmepump", InName, WildcardBefore: true, WildcardAfter: true),
        new("середина слова з одним джокером не знаходить", "rmepump", null, WildcardBefore: true),
        new("середина слова з джокером лише ззаду не знаходить", "rmepump", null, WildcardAfter: true),
        new("ціле слово з обома джокерами", "Wärmepumpe", InName, WildcardBefore: true, WildcardAfter: true),
        new("початок слова в примітці з джокером ззаду", "Sanit", InRemark, WildcardAfter: true),
        new("дві літери з джокером ззаду", "Wä", InName, WildcardAfter: true),
        new("великі літери з джокером ззаду", "WÄRME", InName, WildcardAfter: true),
        new("«ae» замість «ä» з джокером ззаду", "Waerme", InName, WildcardAfter: true),
        new("великі літери з джокером спереду", "PUMPE", InName, WildcardBefore: true),
        new("кінець складеного слова з «Ä» і джокером спереду", "GEBÄUDE", InName, WildcardBefore: true),
        new("кінець складеного слова з «ae» і джокером спереду", "gebaeude", InName, WildcardBefore: true),
    ];

    private static readonly SearchCase[] ModeCases =
    [
        new("обидва слова в назві", "Wärmepumpe Lagergebäude", InName),
        new("порядок слів не важливий", "Lagergebäude Wärmepumpe", InName),
        new("одного слова в завданні немає", "Wärmepumpe Heizkessel", null),
        new("слова в різних полях (назва й примітка) не знаходять", "Wärmepumpe Elektriker", null),
        new("три слова в назві", "Wärmepumpe Lagergebäude vorbereiten", InName),
        new("три слова, одного з них немає", "Wärmepumpe Lagergebäude Heizkessel", null),
        new("три слова в примітці", "Montage Elektriker Sanitär", InRemark),
        new("вистачає одного слова", "Wärmepumpe Heizkessel", InName, SearchMode.SingleWord),
        new("вистачає одного слова з примітки", "Heizkessel Elektriker", InRemark, SearchMode.SingleWord),
        new("вистачає одного з трьох слів", "Heizkessel Kupferrohr Sanitär", InRemark, SearchMode.SingleWord),
        new("жодного з трьох слів немає", "Heizkessel Kupferrohr Tankstelle", null, SearchMode.SingleWord),
        new("слова з обох полів: «Fundstelle» — назва", "Wärmepumpe Elektriker Heizkessel", InName, SearchMode.SingleWord),
        new("фраза в примітці", "Ende Monat", InRemark, SearchMode.ExactPhrase),
        new("інший порядок слів не знаходить", "Monat Ende", null, SearchMode.ExactPhrase),
        new("слова не поруч не знаходять", "Montage Monat", null, SearchMode.ExactPhrase),
        new("ті самі слова не поруч знаходять", "Montage Monat", InRemark),
        new("фраза з трьох слів у назві", "Wärmepumpe im Lagergebäude", InName, SearchMode.ExactPhrase),
        new("фраза зі службовими словами", "zur Wärmepumpe im", InName, SearchMode.ExactPhrase),
        new("фраза з дефісами", "Kick-off-Meeting zur Wärmepumpe", InName, SearchMode.ExactPhrase),
        new("фраза з трьох слів у примітці", "Ende Monat einholen", InRemark, SearchMode.ExactPhrase),
        new("фраза малими літерами", "ende monat", InRemark, SearchMode.ExactPhrase),
        new("кома в тексті фразу не розриває", "einholen Elektriker", InRemark, SearchMode.ExactPhrase),
        new("фраза з комою в запиті", "Monat einholen, Elektriker", InRemark, SearchMode.ExactPhrase),
        new("службове слово у фразі", "Elektriker und Sanitär", InRemark, SearchMode.ExactPhrase),
        new("пропущене службове слово розриває фразу", "Elektriker Sanitär", null, SearchMode.ExactPhrase),
        new("кінець і початок примітки — не фраза", "koordinieren Offerte", null, SearchMode.ExactPhrase),
    ];

    // Джокери-перемикачі діють на кожне слово запиту.
    private static readonly SearchCase[] SeveralWordsWildcardCases =
    [
        new("початки двох слів", "Wärme Lager", InName, WildcardAfter: true),
        new("кінці двох слів", "pumpe gebäude", InName, WildcardBefore: true),
        new("початки двох слів не поруч у фразі не знаходять", "Wärme Lager", null, SearchMode.ExactPhrase, WildcardAfter: true),
        new("початки слів фрази", "Wärmep im Lagerg", InName, SearchMode.ExactPhrase, WildcardAfter: true),
        new("початок останнього слова фрази", "Ende Mon", InRemark, SearchMode.ExactPhrase, WildcardAfter: true),
        new("середини слів фрази", "rmepump im agergeb", InName, SearchMode.ExactPhrase, WildcardBefore: true, WildcardAfter: true),
        new("початок одного зі слів", "Wärme Heiz", InName, SearchMode.SingleWord, WildcardAfter: true),
        new("кінець одного зі слів", "pumpe kessel", InName, SearchMode.SingleWord, WildcardBefore: true),
    ];

    private static readonly SearchCase[] WordFormCases =
    [
        new("малі літери", "wärmepumpe", InName),
        new("великі літери", "WÄRMEPUMPE", InName),
        new("«ae» замість «ä» в назві", "Waermepumpe", InName),
        new("«ae» замість «ä» в примітці", "Sanitaer", InRemark),
        new("«ä» в середині складеного слова", "Lagergebäude", InName),
        new("«ae» в середині складеного слова", "Lagergebaeude", InName),
        new("частина слова з дефісами", "Meeting", InName),
        new("слово з дефісами цілком", "Kick-off-Meeting", InName),
        new("«a» замість «ä» не знаходить", "Warmepumpe", null),
        new("множина не знаходить однину: основи слів Elasticsearch не виділяє", "Wärmepumpen", null),
    ];

    [Test]
    [AllureSeverity(SeverityLevel.critical)]
    public void Finden_TaskByNameWords()
    {
        var mainPage = LogInAndOpenDokumentation();
        var task = CustomerCase.Random().NewTask();
        var (number, _) = CreateTask(mainPage, task);
        var search = OpenSearch(mainPage);

        var result = search.WaitUntilFound(SearchWords(task.Name, notIn: task.Remark), number);

        ShouldBeTask(result, number, task, InName);
    }

    [Test]
    [AllureSeverity(SeverityLevel.critical)]
    public void Finden_TaskByRemarkWords()
    {
        var mainPage = LogInAndOpenDokumentation();
        var task = CustomerCase.Random().NewTask();
        var (number, _) = CreateTask(mainPage, task);
        var search = OpenSearch(mainPage);

        var result = search.WaitUntilFound(SearchWords(task.Remark, notIn: task.Name), number);

        ShouldBeTask(result, number, task, InRemark);
    }

    /// <summary>Джокери «*Txt» і «Txt*» перед списком режимів: частини слів знаходяться лише з ними.</summary>
    [Test]
    [AllureSeverity(SeverityLevel.critical)]
    public void Finden_Wildcards()
    {
        var (search, number) = CreateFixtureAndOpenSearch();
        ShouldBehaveAsExpected(search, number, WildcardCases);
    }

    /// <summary>
    /// Режими «Alle Wörter», «Exakte Suche», «Einzelnes Wort» для двох і трьох слів. Службові слова у фразі
    /// займають своє місце: без них слова вже не стоять поруч.
    /// </summary>
    [Test]
    [AllureSeverity(SeverityLevel.critical)]
    public void Finden_SearchModes()
    {
        var (search, number) = CreateFixtureAndOpenSearch();
        ShouldBehaveAsExpected(search, number, ModeCases);
    }

    /// <summary>Джокери «*Txt» і «Txt*» для кількох слів у кожному режимі: джокер додається до кожного слова.</summary>
    [Test]
    [AllureSeverity(SeverityLevel.normal)]
    public void Finden_WildcardsForSeveralWords()
    {
        var (search, number) = CreateFixtureAndOpenSearch();
        ShouldBehaveAsExpected(search, number, SeveralWordsWildcardCases);
    }

    /// <summary>
    /// Як Elasticsearch нормалізує слова: регістр, «ä» ≡ «ae», дефіси. Без джокерів, щоб частина слова не знайшлася випадково.
    /// Останні два випадки фіксують поточну поведінку: «a» ≠ «ä» і відсутність стемінгу.
    /// </summary>
    [Test]
    [AllureSeverity(SeverityLevel.normal)]
    public void Finden_WordForms()
    {
        var (search, number) = CreateFixtureAndOpenSearch();
        ShouldBehaveAsExpected(search, number, WordFormCases);
    }

    /// <summary>
    /// Значення в колонках рядка результату: пошук за номером знаходить рівно одне завдання, і кожна колонка
    /// має збігатися з тим, що збережено в завданні. Підсумок для одного запису — в однині.
    /// </summary>
    [Test]
    [AllureSeverity(SeverityLevel.critical)]
    public void Finden_ResultColumns()
    {
        var mainPage = LogInAndOpenDokumentation();
        var task = FixtureTask();
        var (number, created) = CreateTask(mainPage, task);
        var search = OpenSearch(mainPage);

        var result = search.WaitUntilFound(number, number, wildcardAfter: false);
        var summary = search.ResultSummary;
        var suffix = number[^4..];
        var bySuffix = AllureApi.Step($"Знайти завдання за кінцем номера «{suffix}» з джокером спереду", () =>
        {
            search.FindRecords(suffix, wildcardBefore: true, wildcardAfter: false);
            return search.ResultFor(number);
        });

        var createdAt = DateTime.ParseExact(created, "dd.MM.yyyy HH:mm", CultureInfo.InvariantCulture);
        AllureApi.Step("Перевірити колонки рядка результату", () =>
            result.ShouldSatisfyAllConditions(
                () => result.Number.ShouldBe(number, "Nummer / Version"),
                () => result.ClassType.ShouldBe("TASK", "Klassentyp"),
                () => result.Name.ShouldBe(task.Name, "Betreff/Name"),
                () => result.FoundIn.ShouldBe(InNumber, "Fundstelle"),
                () => result.Remark.ShouldBe(task.Remark, "Bemerkung"),
                () => result.Class.ShouldBeEmpty("Klasse"),
                () => result.Subclass.ShouldBeEmpty("Unterklasse"),
                () => result.Created.ShouldBe(created, "Erstelldatum"),
                () => result.Id.ShouldMatch(@"^\d{12}TASK\d{6}$", "ID"),
                () => result.Id[..6].ShouldBe(createdAt.ToString("yyMMdd", CultureInfo.InvariantCulture), "ID починається з дати створення")));

        AllureApi.Step($"Перевірити, що «*{suffix}» знаходить завдання за номером", () =>
            bySuffix.ShouldNotBeNull($"«*{suffix}» не знайшов завдання {number}").FoundIn.ShouldBe(InNumber, "Fundstelle"));

        AllureApi.Step("Перевірити підсумок для одного запису", () =>
            ShouldBeSentence(summary, "Es wurde 1 Datensatz gefunden.", "Підсумок пошуку для одного запису"));
    }

    /// <summary>Назви режимів пошуку: мають бути німецькими словами латиницею.</summary>
    [Test]
    [AllureSeverity(SeverityLevel.minor)]
    public void Finden_SearchModeNames()
    {
        var mainPage = new LoginPage(Session).LoginAs(
            TestSettings.UserName, TestSettings.Password, TestSettings.Database, TestSettings.Schema);
        var search = OpenSearch(mainPage);

        // Список режимів доступний, лише коли слів кілька.
        search.SearchTerm.Fill("Wärmepumpe Montage");
        Wait.Until(() => search.Mode.IsEnabled, "доступності режиму пошуку");
        var names = search.Mode.Items;

        names.ShouldBe(
            new[] { "Alle Wörter", "Exakte Suche", "Einzelnes Wort" },
            $"Режими пошуку. Символи не з латиниці: {string.Join("; ", names.Select(NonLatinLetters).Where(text => text.Length > 0))}");
    }

    private SearchWindow OpenSearch(MainPage mainPage)
    {
        var search = mainPage.OpenSearchWindow();
        OnCleanup(search.CloseIfOpen);
        return search;
    }

    private (SearchWindow Search, string Number) CreateFixtureAndOpenSearch(string name = FixtureName, string remark = FixtureRemark)
    {
        var mainPage = LogInAndOpenDokumentation();
        var (number, _) = CreateTask(mainPage, FixtureTask(name, remark));
        var search = OpenSearch(mainPage);

        // Пошук за номером чекає, доки Elasticsearch проіндексує нове завдання.
        search.WaitUntilFound(number, number, wildcardAfter: false);
        return (search, number);
    }

    // Дати й решта полів — з випадкової справи, назва й примітка — сталі.
    private static TaskData FixtureTask(string name = FixtureName, string remark = FixtureRemark) =>
        CustomerCase.Random().NewTask() with { Name = name, Remark = remark };

    /// <summary>Створює завдання й повертає його номер і дату створення, як їх показує збережене завдання.</summary>
    private (string Number, string Created) CreateTask(MainPage mainPage, TaskData task)
    {
        var taskDialog = mainPage.OpenNewTaskDialog();
        OnCleanup(taskDialog.CancelIfOpen);
        taskDialog.Form.Fill(task);
        var number = taskDialog.Form.Number.Text;
        taskDialog.Save();

        var savedTask = mainPage.WaitForOpenedTask(number);
        return (number, savedTask.Created.Text);
    }

    private static void ShouldBeTask(SearchResult result, string number, TaskData task, string foundIn) =>
        result.ShouldSatisfyAllConditions(
            () => result.Number.ShouldBe(number, "Nummer / Version"),
            () => result.ClassType.ShouldBe("TASK", "Klassentyp"),
            () => result.Name.ShouldBe(task.Name, "Betreff/Name"),
            () => result.FoundIn.ShouldBe(foundIn, "Fundstelle"),
            () => result.Remark.ShouldBe(task.Remark, "Bemerkung"));

    /// <summary>
    /// Порівнює текст для людей (речення) і повідомляє про розбіжність одним рядком. <c>ShouldBe</c> для рядків
    /// додає таблицю розбіжностей, яка порівнює символи за позицією: одна вставлена літера («wurde» → «wurden»)
    /// зсуває решту рядка, і таблиця показує розбіжність майже в кожному символі.
    /// </summary>
    private static void ShouldBeSentence(string? actual, string expected, string what)
    {
        if (actual != expected)
            throw new ShouldAssertException($"{what}: «{actual}», а має бути «{expected}».");
    }

    /// <summary>Виконує всі випадки й повідомляє про всі розбіжності разом, а не лише про першу.</summary>
    private static void ShouldBehaveAsExpected(SearchWindow search, string number, SearchCase[] cases) =>
        search.ShouldSatisfyAllConditions(cases.Select(searchCase => (Action)(() => Check(search, number, searchCase))).ToArray());

    private static void Check(SearchWindow search, string number, SearchCase searchCase) =>
        AllureApi.Step(searchCase.ToString(), () =>
        {
            search.FindRecords(searchCase.Words, searchCase.Mode, searchCase.WildcardBefore, searchCase.WildcardAfter);
            var result = search.ResultFor(number);

            if (searchCase.InField is null)
            {
                result.ShouldBeNull($"{searchCase}: знайшов завдання {number}");
                return;
            }

            result.ShouldNotBeNull($"{searchCase}: не знайшов завдання {number}");
            result.FoundIn.ShouldBe(searchCase.InField, $"{searchCase}: Fundstelle");
        });

    /// <summary>
    /// Два найдовші слова тексту (від 5 літер, тож без службових «für», «mit»), яких немає в іншому полі запису:
    /// так запис може знайтися лише за потрібним полем.
    /// </summary>
    private static string SearchWords(string text, string notIn)
    {
        var otherWords = Words(notIn).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var words = Words(text)
            .Where(word => !otherWords.Contains(word))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderByDescending(word => word.Length)
            .Take(2)
            .ToList();
        return words.Count > 0
            ? string.Join(' ', words)
            : throw new InvalidOperationException($"У тексті «{text}» немає слів, яких нема в «{notIn}».");
    }

    private static IEnumerable<string> Words(string text) => Word().Matches(text).Select(match => match.Value);

    // Літери поза Latin-1 (німецькі там усі) з кодами: кирилична «Е» (U+0415) на вигляд не відрізняється від латинської «E».
    private static string NonLatinLetters(string text)
    {
        var letters = text.Where(letter => char.IsLetter(letter) && letter > '\u00FF').ToList();
        return letters.Count == 0
            ? string.Empty
            : $"«{text}»: {string.Join(", ", letters.Select(letter => $"«{letter}» U+{(int)letter:X4}"))}";
    }

    [GeneratedRegex(@"\p{L}{5,}")]
    private static partial Regex Word();

    /// <summary>Один пошук і очікуваний результат для нашого завдання.</summary>
    /// <param name="Why">Що перевіряє випадок — для кроку звіту.</param>
    /// <param name="Words">Що ввести в «Suchbegriff».</param>
    /// <param name="InField">Очікувана «Fundstelle» або null, якщо завдання знаходитися не має.</param>
    private sealed record SearchCase(
        string Why,
        string Words,
        string? InField,
        SearchMode Mode = SearchMode.AllWords,
        bool WildcardBefore = false,
        bool WildcardAfter = false)
    {
        public override string ToString()
        {
            // Режим має значення лише для кількох слів; назви — як у списку ADAICA.
            var mode = Words.Contains(' ')
                ? Mode switch
                {
                    SearchMode.AllWords => "Alle Wörter, ",
                    SearchMode.ExactPhrase => "Exakte Suche, ",
                    _ => "Einzelnes Wort, ",
                }
                : string.Empty;
            var wildcards = (WildcardBefore ? "*" : "") + "слово" + (WildcardAfter ? "*" : "");
            var expected = InField is null ? "не знаходить" : $"знаходить у «{InField}»";
            return $"{Why}: «{Visible(Words)}» ({mode}{wildcards}) → {expected}";
        }

        /// <summary>
        /// Запит так, щоб у звіті було видно невидимі символи (табуляцію, нерозривний пробіл, комбіновані діакритики),
        /// а довгий запит не займав пів екрана.
        /// </summary>
        private static string Visible(string words)
        {
            var text = string.Concat(words.Select(symbol => symbol switch
            {
                '\t' => "\\t",
                '\u00A0' => "\\u00A0",
                _ when CharUnicodeInfo.GetUnicodeCategory(symbol) == UnicodeCategory.NonSpacingMark => $"\\u{(int)symbol:X4}",
                _ => symbol.ToString(),
            }));
            return text.Length > 60 ? $"{text[..40]}… ({words.Length} символів)" : text;
        }
    }
}
