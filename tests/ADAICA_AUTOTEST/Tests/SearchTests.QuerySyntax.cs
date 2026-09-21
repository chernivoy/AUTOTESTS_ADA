using ADAICA_AUTOTEST.Pages;
using Allure.Net.Commons;
using Allure.Net.Commons.Attributes;

namespace ADAICA_AUTOTEST.Tests;

/// <summary>
/// Що користувач може ввести в «Suchbegriff», крім звичайних слів: джокери, оператори Elasticsearch, дужки й інші
/// спецсимволи, дефіси й тире, зайві пробіли, службові слова. Очікування — ні повідомлення про помилку, ні зайвих записів,
/// а спецсимвол не змінює змісту запиту непомітно для користувача.
/// </summary>
public partial class SearchTests
{
    // Джокери, введені в тексті запиту (перемикачі «*Txt» і «Txt*» вимкнено).
    private static readonly SearchCase[] TypedWildcardCases =
    [
        new("«*» у кінці слова", "Wärme*", InName),
        new("«*» на початку слова", "*pumpe", InName),
        new("«*» з обох боків", "*rmepump*", InName),
        new("«*» у середині слова", "Wär*pumpe", InName),
        new("«*» замість кількох літер у середині", "Wärm*pe", InName),
        new("«*» замість умлаута", "W*rmepumpe", InName),
        new("«?» замість однієї літери в середині", "Wärmep?mpe", InName),
        new("«?» замість першої літери", "?ärmepumpe", InName),
        new("«?» замість останньої літери", "Wärmepump?", InName),
        new("«?» після цілого слова вимагає ще однієї літери", "Wärmepumpe?", null),
        new("«?» — джокер, тож «Montag?» знаходить «Montage»", "Montag?", InRemark),
        new("«%» діє як «*» у кінці", "Wärme%", InName),
        new("«%» діє як «*» з обох боків", "%Wärme%", InName),
        new("«_» — не джокер", "_ärmepumpe", null),
    ];

    // Оператори й синтаксис запитів Elasticsearch ADAICA не підтримує: слова беруться буквально.
    private static readonly SearchCase[] OperatorCases =
    [
        new("лапки в «Alle Wörter» не роблять фрази", "\"Ende Monat\"", InRemark),
        new("слова в лапках в іншому порядку теж знаходять", "\"Monat Ende\"", InRemark),
        new("OR — не оператор: «Heizkessel» у завданні немає", "Wärmepumpe OR Heizkessel", null),
        new("|| — не оператор", "Wärmepumpe || Heizkessel", null),
        new("AND між словами нічого не змінює", "Wärmepumpe AND Lagergebäude", InName),
        new("&& між словами нічого не змінює", "Wärmepumpe && Lagergebäude", InName),
        new("AND без другого слова", "Wärmepumpe AND", InName),
        new("NOT не виключає слова", "Wärmepumpe NOT Lagergebäude", InName),
        new("мінус перед словом не виключає його", "Wärmepumpe -Lagergebäude", InName),
        new("плюс перед словом", "+Wärmepumpe", InName),
        new("«~» після слова ігнорується", "Wärmepumpe~", InName),
        new("нечіткого пошуку немає: слово з помилкою й «~» не знаходить", "Wärmepunpe~", null),
        new("«^» ділить слово, а «2» у назві немає", "Wärmepumpe^2", null),
        new("двокрапка не задає поле: «name» — ще одне слово", "name:Wärmepumpe", null),
        new("регулярних виразів немає", "/W.rmepumpe/", null),
        new("SQL-подібний рядок не знаходить зайвого", "Wärmepumpe' OR '1'='1", null),
    ];

    // Розділові знаки й дужки навколо слова: слово знаходиться, помилки немає.
    private static readonly SearchCase[] SpecialCharacterCases =
    [
        new("круглі дужки", "(Wärmepumpe)", InName),
        new("квадратні дужки", "[Wärmepumpe]", InName),
        new("фігурні дужки", "{Wärmepumpe}", InName),
        new("кутові дужки", "<Wärmepumpe>", InName),
        new("скісні риски з обох боків", "/Wärmepumpe/", InName),
        new("скісна риска в кінці", "Wärmepumpe/", InName),
        new("зворотна скісна риска", "Wärmepumpe\\", InName),
        new("непарна лапка", "\"Wärmepumpe", InName),
        new("апостроф", "Wärmepumpe'", InName),
        new("знак оклику", "Wärmepumpe!", InName),
        new("крапка", "Wärmepumpe.", InName),
        new("крапка з комою", "Wärmepumpe;", InName),
        new("знак рівності", "Wärmepumpe=", InName),
        new("«&» ділить слово на «Wärme» і «pumpe»", "Wärme&pumpe", null),
        new("кома без пробілу — як пробіл", "Wärmepumpe,Lagergebäude", InName),
        new("крапка з комою без пробілу — як пробіл", "Wärmepumpe;Lagergebäude", InName),
        new("кома між словами не робить «або»", "Wärmepumpe, Heizkessel", null),
        new("довгий запит: те саме слово 60 разів", string.Join(' ', Enumerable.Repeat("Wärmepumpe", 60)), InName),
        new("слово з 300 літер", new string('W', 300), null),
    ];

    private static readonly SearchCase[] HyphenAndSpaceCases =
    [
        new("частина слова з дефісами", "Kick-off", InName),
        new("перша частина слова з дефісами", "Kick", InName),
        new("коротка частина слова з дефісами — теж слово", "off", InName),
        new("без дефіса разом — інше слово", "Kickoff", null),
        new("частини через пробіл", "Kick off", InName),
        new("частини через пробіл як фраза", "Kick off", InName, SearchMode.ExactPhrase),
        new("усі частини як фраза", "Kick off Meeting", InName, SearchMode.ExactPhrase),
        new("коротке тире (–) замість дефіса", "Kick\u2013off\u2013Meeting", InName),
        new("довге тире (—) замість дефіса", "Kick\u2014off", InName),
        new("дефіс перед словом", "-Meeting", InName),
        new("дефіс після слова", "Meeting-", InName),
        new("дефіс після першої частини", "Kick-", InName),
        new("частина слова з дефісами з джокером ззаду", "Kick-off", InName, WildcardAfter: true),
        new("окремий дефіс між словами", "Wärmepumpe - Lagergebäude", InName),
        new("пробіл перед словом", " Wärmepumpe", InName),
        new("пробіл після слова", "Wärmepumpe ", InName),
        new("по два пробіли з обох боків", "  Wärmepumpe  ", InName),
        new("два пробіли між словами", "Wärmepumpe  Lagergebäude", InName),
        new("два пробіли у фразі", "Ende  Monat", InRemark, SearchMode.ExactPhrase),
        new("табуляція між словами", "Wärmepumpe\tLagergebäude", InName),
        new("нерозривний пробіл між словами", "Wärmepumpe\u00A0Lagergebäude", InName),
        new("складене слово, розбите пробілом, не знаходить", "Wärme pumpe", null),
        new("частини складеного слова не знаходять навіть окремо", "Wärme pumpe", null, SearchMode.SingleWord),
    ];

    // Службові слова (німецькі й англійські) Elasticsearch не індексує: окремо вони нічого не знаходять.
    private static readonly SearchCase[] StopWordCases =
    [
        new("«und» окремо", "und", null),
        new("«für» окремо", "für", null),
        new("«die» окремо", "die", null),
        new("«im» окремо", "im", null),
        new("«zur» окремо", "zur", null),
        new("лише службові слова в «Einzelnes Wort»", "und für", null, SearchMode.SingleWord),
        new("англійське «the»", "the", null),
        new("англійське «AND» окремо", "AND", null),
        new("службове слово після слова пропускається", "Wärmepumpe und", InName),
        new("службове слово перед словом пропускається", "und Wärmepumpe", InName),
        new("фраза, що починається службовим словом", "zur Wärmepumpe", InName, SearchMode.ExactPhrase),
        new("фраза зі службовим словом на початку в примітці", "bis Ende", InRemark, SearchMode.ExactPhrase),
    ];

    /// <summary>
    /// Джокери, введені в самому запиті: «*» — будь-скільки літер, «?» — одна літера, «%» працює як «*», «_» — ні.
    /// «?» — завжди джокер, навіть якщо користувач скопіював «Montag?» з тексту.
    /// </summary>
    [Test]
    [AllureSeverity(SeverityLevel.normal)]
    public void Finden_TypedWildcards()
    {
        var (search, number) = CreateFixtureAndOpenSearch();
        ShouldBehaveAsExpected(search, number, TypedWildcardCases);
    }

    /// <summary>Оператори Elasticsearch (OR, NOT, «-», «~», «поле:», регулярні вирази) не діють: ADAICA шукає слова буквально.</summary>
    [Test]
    [AllureSeverity(SeverityLevel.normal)]
    public void Finden_QueryOperators()
    {
        var (search, number) = CreateFixtureAndOpenSearch();
        ShouldBehaveAsExpected(search, number, OperatorCases);
    }

    /// <summary>Розділові знаки, дужки й інші спецсимволи в запиті, а також дуже довгий запит: без повідомлень про помилку.</summary>
    [Test]
    [AllureSeverity(SeverityLevel.normal)]
    public void Finden_SpecialCharactersInQuery()
    {
        var (search, number) = CreateFixtureAndOpenSearch();
        ShouldBehaveAsExpected(search, number, SpecialCharacterCases);
    }

    /// <summary>Дефіси, тире й зайві пробіли (зокрема табуляція й нерозривний пробіл).</summary>
    [Test]
    [AllureSeverity(SeverityLevel.normal)]
    public void Finden_HyphensAndSpaces()
    {
        var (search, number) = CreateFixtureAndOpenSearch();
        ShouldBehaveAsExpected(search, number, HyphenAndSpaceCases);
    }

    /// <summary>
    /// Службові слова («und», «für», «die», «the», «and»): окремо не знаходять нічого, поруч зі звичайним словом
    /// пропускаються, а у фразі займають своє місце.
    /// </summary>
    [Test]
    [AllureSeverity(SeverityLevel.minor)]
    public void Finden_StopWords()
    {
        var (search, number) = CreateFixtureAndOpenSearch();
        ShouldBehaveAsExpected(search, number, StopWordCases);
    }
}
