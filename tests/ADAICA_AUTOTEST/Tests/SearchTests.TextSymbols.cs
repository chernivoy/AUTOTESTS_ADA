using ADAICA_AUTOTEST.Pages;
using Allure.Net.Commons;
using Allure.Net.Commons.Attributes;

namespace ADAICA_AUTOTEST.Tests;

/// <summary>
/// Символи в тексті записів: розділові знаки, гендерні форми, лапки, email, дати, час і суми в німецькому
/// та швейцарському форматі, інші алфавіти й емодзі. Користувач шукає так, як бачить текст у записі.
/// </summary>
public partial class SearchTests
{
    // Фікстура HandoverName / HandoverRemark.
    private static readonly SearchCase[] PunctuationCases =
    [
        new("слово, після якого в тексті стоїть «?»", "Montag", InRemark),
        new("«Montag?» як у тексті: «?» — джокер, тож потрібне слово з 7 літер", "Montag?", null),
        new("«Montag?» як у тексті з «Txt*» (так відкривається вікно)", "Montag?", InRemark, WildcardAfter: true),
        new("гендерна зірочка як у записі", "Mitarbeiter*innen", InName),
        new("основа перед гендерною зірочкою", "Mitarbeiter", InName),
        new("закінчення після гендерної зірочки", "innen", InName),
        new("гендерна форма разом — інше слово", "Mitarbeiterinnen", null),
        new("гендерна двокрапка замість зірочки", "Mitarbeiter:innen", InName),
        new("слово в німецьких лапках", "Sonnenhof", InName),
        new("запит у німецьких лапках „…“", "„Sonnenhof“", InName),
        new("запит у прямих лапках", "\"Sonnenhof\"", InName),
        new("запит у швейцарських лапках «…»", "«Sonnenhof»", InName),
        new("фраза з лапками", "Café „Sonnenhof“", InName, SearchMode.ExactPhrase),
        new("фраза без лапок", "Café Sonnenhof", InName, SearchMode.ExactPhrase),
        new("email цілком", "empfang@hotel-alpenrose.ch", InRemark),
        new("ім'я скриньки з email", "empfang", InRemark),
        new("частина домену з email", "alpenrose", InRemark),
        new("домен з email", "alpenrose.ch", InRemark),
        new("домен із дефісом з email", "hotel-alpenrose", InRemark),
        new("частина email після «@»", "@hotel", InRemark),
        new("скорочення з крапкою «MwSt.»", "MwSt.", InRemark),
        new("скорочення без крапки", "MwSt", InRemark),
        new("скорочення «inkl.»", "inkl.", InRemark),
        new("скорочення «Nr.»", "Nr.", InRemark),
    ];

    // Фікстура HandoverName / HandoverRemark.
    private static readonly SearchCase[] NumberInTextCases =
    [
        new("дата як у тексті", "30.09.2026", InRemark),
        new("дата без року не знаходить: дата — одне слово", "30.09", null),
        new("дата без року з джокером ззаду", "30.09", InRemark, WildcardAfter: true),
        new("рік з дати окремо не знаходить", "2026", null),
        new("час як у тексті", "14:30", InRemark),
        new("година з часу: двокрапка ділить час", "14", InRemark),
        new("сума у швейцарському форматі", "1'250.50", InRemark),
        new("сума без копійок не знаходить: сума — одне слово", "1'250", null),
        new("сума без апострофа не знаходить", "1250.50", null),
        new("номер рахунку", "4711", InRemark),
        new("номер рахунку з «Nr.» як фраза", "Nr. 4711", InRemark, SearchMode.ExactPhrase),
        new("код валюти", "CHF", InRemark),
    ];

    // Фікстура DoorName / DoorRemark.
    private static readonly SearchCase[] SymbolCases =
    [
        new("гендерна двокрапка як у записі", "Kund:innen", InName),
        new("основа перед гендерною двокрапкою", "Kund", InName),
        new("гендерна форма разом — інше слово", "Kundinnen", null),
        new("гендерна зірочка замість двокрапки", "Kund*innen", InName),
        new("слова через скісну риску", "Kund:innen/Besucher", InName),
        new("слово після скісної риски", "Besucher", InName),
        new("форма зі скісною «/in»", "Ansprechpartner/in", InRemark),
        new("основа перед «/in»", "Ansprechpartner", InRemark),
        new("форма зі скісною й двокрапкою, як у тексті", "Ansprechpartner/in:", InRemark),
        new("подвійне прізвище", "Keller-Meier", InRemark),
        new("подвійне прізвище як фраза", "Keller-Meier", InRemark, SearchMode.ExactPhrase),
        new("номер будинку з літерою", "5a", InName),
        new("телефон як фраза", "+41 44 123 45 67", InRemark, SearchMode.ExactPhrase),
        new("код країни з «+»", "+41", InRemark),
        new("відсотки як фраза", "5 %", InRemark, SearchMode.ExactPhrase),
        new("сума в німецькому форматі", "1.250,50", InRemark),
        new("сума зі знаком євро як фраза", "1.250,50 €", InRemark, SearchMode.ExactPhrase),
        new("знак євро не індексується", "€", null),
        new("слово з «è»", "Crème", InRemark),
        new("«e» замість «è»", "Creme", InRemark),
        new("слово з «è» і дефісом", "Crème-Farbe", InRemark),
        new("частина слова з «è» після дефіса", "Farbe", InRemark),
    ];

    // Фікстура DoorName / DoorRemark: «Shenzhen (深圳)» і емодзі в примітці.
    private static readonly SearchCase[] ForeignScriptCases =
    [
        new("ієрогліфи як у тексті", "深圳", InRemark),
        new("один ієрогліф — окреме слово", "深", InRemark),
        new("один ієрогліф з джокером ззаду", "深", InRemark, WildcardAfter: true),
        new("ієрогліфи в дужках, як у тексті", "(深圳)", InRemark),
        new("ієрогліфа, якого в записі немає", "海", null),
        new("японські слова, яких у записі немає", "日本語", null),
        new("кирилиця, якої в записі немає", "Москва", null),
        new("латинська назва перед ієрогліфами як фраза", "aus Shenzhen", InRemark, SearchMode.ExactPhrase),
        new("емодзі окремо не індексується", "👍", null),
        new("емодзі у фразі пропускається", "da 👍", InRemark, SearchMode.ExactPhrase),
    ];

    /// <summary>
    /// «?» після слова, гендерні зірочка й двокрапка, німецькі, швейцарські й прямі лапки, email частинами,
    /// скорочення з крапкою.
    /// </summary>
    [Test]
    [AllureSeverity(SeverityLevel.normal)]
    public void Finden_PunctuationInText()
    {
        var (search, number) = CreateFixtureAndOpenSearch(HandoverName, HandoverRemark);
        ShouldBehaveAsExpected(search, number, PunctuationCases);
    }

    /// <summary>
    /// Дата, час, сума й номер рахунку: дата й сума — одне слово (частина знаходиться лише з джокером),
    /// а час ділиться двокрапкою.
    /// </summary>
    [Test]
    [AllureSeverity(SeverityLevel.normal)]
    public void Finden_NumbersDatesAndAmountsInText()
    {
        var (search, number) = CreateFixtureAndOpenSearch(HandoverName, HandoverRemark);
        ShouldBehaveAsExpected(search, number, NumberInTextCases);
    }

    /// <summary>Гендерна двокрапка, скісна риска, подвійне прізвище, телефон, відсотки, суми, «€», «è».</summary>
    [Test]
    [AllureSeverity(SeverityLevel.normal)]
    public void Finden_SymbolsInText()
    {
        var (search, number) = CreateFixtureAndOpenSearch(DoorName, DoorRemark);
        ShouldBehaveAsExpected(search, number, SymbolCases);
    }

    /// <summary>Ієрогліфи, кирилиця, японські слова й емодзі: знаходиться те, що є в записі, і без повідомлень про помилку.</summary>
    [Test]
    [AllureSeverity(SeverityLevel.normal)]
    public void Finden_ForeignScripts()
    {
        var (search, number) = CreateFixtureAndOpenSearch(DoorName, DoorRemark);
        ShouldBehaveAsExpected(search, number, ForeignScriptCases);
    }

    /// <summary>
    /// Ієрогліфи в запиті поводяться не так, як написано: фраза «Shenzhen 深圳», скопійована з примітки, запис
    /// не знаходить, а слово «深海», якого в записі немає, знаходить його — вистачає одного збіглого ієрогліфа «深».
    /// Тест очікує пошук як для латинських слів і зараз <b>падає навмисно</b> — імовірний баг ADAICA, потребує
    /// підтвердження розробників.
    /// </summary>
    [Test]
    [AllureSeverity(SeverityLevel.minor)]
    public void Finden_ChineseTextAsWritten()
    {
        var (search, number) = CreateFixtureAndOpenSearch(DoorName, DoorRemark);
        ShouldBehaveAsExpected(search, number,
        [
            new("латинська назва й ієрогліфи як фраза", "Shenzhen 深圳", InRemark, SearchMode.ExactPhrase),
            new("ієрогліфи й наступне слово як фраза", "深圳 sind", InRemark, SearchMode.ExactPhrase),
            new("слово з ієрогліфів, якого в записі немає", "深海", null),
        ]);
    }
}
