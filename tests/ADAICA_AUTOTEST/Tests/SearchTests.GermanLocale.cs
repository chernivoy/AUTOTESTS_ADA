using ADAICA_AUTOTEST.Pages;
using Allure.Net.Commons;
using Allure.Net.Commons.Attributes;

namespace ADAICA_AUTOTEST.Tests;

/// <summary>
/// Німецька локаль: умлаути й їхнє написання через «ae/oe/ue», «ß» і швейцарське «ss», велика «ẞ», акценти в запозиченнях,
/// Binnen-I. Elasticsearch в ADAICA зберігає «ä» як «ae» і «ß» як «ss», а «é» — як «e»; стемінгу немає.
/// </summary>
public partial class SearchTests
{
    // Фікстура CallbackName / CallbackRemark: «ü» і «ue» в тексті, «ß», Binnen-I.
    private static readonly SearchCase[] UmlautCases =
    [
        new("«ue» у записі, запит з «ue»", "Mueller", InName),
        new("«ue» у записі, запит з «ü»", "Müller", InName),
        new("«ue» у записі, запит великими з «Ü»", "MÜLLER", InName),
        new("«u» замість «ü» не знаходить", "Muller", null),
        new("«oe» і «ss» у записі, запит з «ö» і «ß»", "Göthestraße", InName),
        new("«oe» і «ss» у записі, запит з «ß»", "Goethestraße", InName),
        new("«oe» і «ss» у записі, запит як у записі", "Goethestrasse", InName),
        new("«ä» у записі, запит з «ä»", "Parkplätzen", InName),
        new("«ä» у записі, запит з «ae»", "Parkplaetzen", InName),
        new("«a» замість «ä» не знаходить", "Parkplatzen", null),
        new("«ü» на початку слова в записі, запит з «ü»", "Rückruf", InName),
        new("«ü» у записі, запит з «ue»", "Rueckruf", InName),
        new("«u» замість «ü» не знаходить", "Ruckruf", null),
        new("однина не знаходить множину з умлаутом: стемінгу немає", "Parkplatz", null),
        new("початок слова з умлаутом і джокером ззаду", "Parkpl", InName, WildcardAfter: true),
        new("«ß» у записі, запит з «ß»", "Maße", InRemark),
        new("«ß» у записі, запит з «ss» (і «Masse» — інше слово, але пишеться так само)", "Masse", InRemark),
        new("«ß» у складеному слові, запит з «ß»", "Maßband", InRemark),
        new("«ß» у складеному слові, запит з «ss»", "Massband", InRemark),
        new("Binnen-I як у записі", "MitarbeiterInnen", InRemark),
        new("Binnen-I малими літерами", "Mitarbeiterinnen", InRemark),
        new("слово без кінцевої «s» не знаходить: стемінгу немає", "Tor", null),
        new("слово як у записі", "Tors", InRemark),
    ];

    // Фікстура CallbackName / CallbackRemark: розміри, «×», «&», скорочення з крапкою.
    private static readonly SearchCase[] AbbreviationAndMeasureCases =
    [
        new("число з десятковою комою", "2,40", InRemark),
        new("число з одиницею як фраза", "3,00 m", InRemark, SearchMode.ExactPhrase),
        new("знак множення не індексується", "×", null),
        new("скорочення з крапкою «Fr.»", "Fr.", InRemark),
        new("скорочення з крапкою «evtl.»", "evtl.", InRemark),
        new("дата без року з крапкою в кінці", "02.10.", InRemark),
        new("дата без року без крапки в кінці", "02.10", InRemark),
        new("фраза", "Fotos machen", InRemark, SearchMode.ExactPhrase),
        new("«&» у фразі пропускається", "mitnehmen & Fotos", InRemark, SearchMode.ExactPhrase),
    ];

    // Фікстура DoorName / DoorRemark: «ß» у записі поруч зі швейцарським «ss», «Ö» на початку слова.
    private static readonly SearchCase[] SharpSCases =
    [
        new("«ß» у записі, запит як у записі", "Hauptstraße", InName),
        new("«ß» у записі, запит з «ss»", "Hauptstrasse", InName),
        new("«ß» у записі, запит великими з «SS»", "HAUPTSTRASSE", InName),
        new("«ß» у записі, запит з великою «ẞ» (U+1E9E)", "HAUPTSTRA\u1E9EE", InName),
        new("одна «s» замість «ß» не знаходить", "Hauptstrase", null),
        new("початок слова перед «ß» з джокером ззаду", "Hauptstra", InName, WildcardAfter: true),
        new("кінець складеного слова з «ß» і джокером спереду", "straße", InName, WildcardBefore: true),
        new("кінець складеного слова з «ss» і джокером спереду", "strasse", InName, WildcardBefore: true),
        new("швейцарське «ss» у записі, запит як у записі", "Aussentür", InName),
        new("швейцарське «ss» у записі, запит з «ß»", "Außentür", InName),
        new("«ss» і «ü» у записі, запит з «ss» і «ue»", "Aussentuer", InName),
        new("«u» замість «ü» не знаходить", "Aussentur", null),
        new("великі літери з «Ü»", "AUSSENTÜR", InName),
        new("«ö» і «ss» у записі, запит як у записі", "Grösse", InName),
        new("«ö» і «ss» у записі, запит з «ß»", "Größe", InName),
        new("«ö» і «ss» у записі, запит з «oe»", "Groesse", InName),
        new("«o» замість «ö» не знаходить", "Grosse", null),
        new("«Ö» на початку слова, запит як у записі", "Öffnungszeiten", InRemark),
        new("«Ö» на початку слова, запит з «Oe»", "Oeffnungszeiten", InRemark),
        new("«Ö» на початку слова, запит малими", "öffnungszeiten", InRemark),
        new("«O» замість «Ö» не знаходить", "Offnungszeiten", null),
        new("початок слова з «ö» різним регістром і джокером ззаду", "öFFNUNG", InRemark, WildcardAfter: true),
        new("початок слова з «Oe» і джокером ззаду", "Oeffnung", InRemark, WildcardAfter: true),
    ];

    // Фікстура HandoverName / HandoverRemark: «é» в «Café», «ü» в назві міста.
    private static readonly SearchCase[] AccentCases =
    [
        new("слово з «é» як у записі", "Café", InName),
        new("«e» замість «é»", "Cafe", InName),
        new("великі літери з «É»", "CAFÉ", InName),
        new("«é», набране як «e» + U+0301", "Cafe\u0301", InName),
        new("«?» замість «é»: «é» займає одну літеру", "Caf?", InName),
        new("початок слова перед «é» з джокером ззаду", "Caf", InName, WildcardAfter: true),
        new("назва міста з «ü»", "Zürich", InName),
        new("назва міста з «ue»", "Zuerich", InName),
        new("назва міста великими з «Ü»", "ZÜRICH", InName),
        new("«u» замість «ü» у назві міста не знаходить", "Zurich", null),
    ];

    /// <summary>
    /// Умлаути в обидва боки: запит з «ü» знаходить «ue» у записі, запит з «ue» — «ü»; «ß» ≡ «ss»; Binnen-I;
    /// відсутність стемінгу («Parkplatz» не знаходить «Parkplätzen»).
    /// </summary>
    [Test]
    [AllureSeverity(SeverityLevel.critical)]
    public void Finden_Umlauts()
    {
        var (search, number) = CreateFixtureAndOpenSearch(CallbackName, CallbackRemark);
        ShouldBehaveAsExpected(search, number, UmlautCases);
    }

    /// <summary>«ß», швейцарське «ss» і велика «ẞ» у будь-якому поєднанні, «Ö» на початку слова.</summary>
    [Test]
    [AllureSeverity(SeverityLevel.critical)]
    public void Finden_SharpSAndSwissSpelling()
    {
        var (search, number) = CreateFixtureAndOpenSearch(DoorName, DoorRemark);
        ShouldBehaveAsExpected(search, number, SharpSCases);
    }

    /// <summary>Акценти в запозиченнях: «é» ≡ «e» (на відміну від умлаутів) і назва міста з «ü».</summary>
    [Test]
    [AllureSeverity(SeverityLevel.normal)]
    public void Finden_Accents()
    {
        var (search, number) = CreateFixtureAndOpenSearch(HandoverName, HandoverRemark);
        ShouldBehaveAsExpected(search, number, AccentCases);
    }

    /// <summary>Числа з комою, «×», «&amp;» і німецькі скорочення з крапкою.</summary>
    [Test]
    [AllureSeverity(SeverityLevel.normal)]
    public void Finden_AbbreviationsAndMeasures()
    {
        var (search, number) = CreateFixtureAndOpenSearch(CallbackName, CallbackRemark);
        ShouldBehaveAsExpected(search, number, AbbreviationAndMeasureCases);
    }

    /// <summary>
    /// «?» замінює одну літеру, але не умлаут і не «ß»: «Hauptstra?e» не знаходить «Hauptstraße», а «Hauptstra??e» —
    /// знаходить. Elasticsearch зберігає «ß» як «ss», а «ü», «ö», «ä» — як «ue», «oe», «ae», і «?» порівнює вже цей текст
    /// (так само «W?rmepumpe» не знаходить «Wärmepumpe»). Для користувача це одна літера, тому тест очікує збіг
    /// і зараз <b>падає навмисно</b> — імовірний баг ADAICA, потребує підтвердження розробників.
    /// </summary>
    [Test]
    [AllureSeverity(SeverityLevel.normal)]
    public void Finden_QuestionMarkForUmlautOrSharpS()
    {
        var (search, number) = CreateFixtureAndOpenSearch(DoorName, DoorRemark);
        ShouldBehaveAsExpected(search, number,
        [
            new("«?» замість «ß»", "Hauptstra?e", InName),
            new("«?» замість «ü»", "Aussent?r", InName),
            new("«?» замість «ö»", "Gr?sse", InName),
        ]);
    }

    /// <summary>
    /// Умлаут, набраний двома символами («a» + U+0308, так буває після копіювання з macOS чи PDF), виглядає так само,
    /// але не знаходить «Wärmepumpe», хоча «e» + U+0301 знаходить «Café» (<see cref="Finden_Accents"/>).
    /// Запит варто нормалізувати (NFC), тому тест очікує збіг і зараз <b>падає навмисно</b> — імовірний баг ADAICA.
    /// </summary>
    [Test]
    [AllureSeverity(SeverityLevel.minor)]
    public void Finden_DecomposedUmlaut()
    {
        var (search, number) = CreateFixtureAndOpenSearch();
        ShouldBehaveAsExpected(search, number,
        [
            new("«ä», набране як «a» + U+0308", "Wa\u0308rmepumpe", InName),
        ]);
    }
}
