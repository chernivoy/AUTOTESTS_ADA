using ADAICA_AUTOTEST.Pages;
using Allure.Net.Commons;
using Allure.Net.Commons.Attributes;

namespace ADAICA_AUTOTEST.Tests;

/// <summary>
/// Пошук за номером («Nummer», наприклад TAS0000046) і за ID запису. Номер — одне слово без розбиття:
/// частина номера знаходиться лише з джокером.
/// </summary>
public partial class SearchTests
{
    /// <summary>
    /// Номер повністю, малими літерами, без префікса, кінцем і серединою з джокерами, з пробілами навколо;
    /// ID повністю й початком. Номер і слово з назви в «Alle Wörter» запис не знаходять: вони в різних полях.
    /// </summary>
    [Test]
    [AllureSeverity(SeverityLevel.critical)]
    public void Finden_ByNumber()
    {
        var (search, number) = CreateFixtureAndOpenSearch();
        var id = search.ResultFor(number)!.Id;
        var digits = number[3..];
        var suffix = number[^4..];

        ShouldBehaveAsExpected(search, number,
        [
            new("номер повністю", number, InNumber),
            new("номер малими літерами", number.ToLowerInvariant(), InNumber),
            new("номер з пробілами навколо", $" {number} ", InNumber),
            new("номер з «*» у кінці", number + "*", InNumber),
            new("цифри без префікса не знаходять: номер — одне слово", digits, null),
            new("цифри без префікса з джокером спереду", digits, InNumber, WildcardBefore: true),
            new("кінець номера без джокера не знаходить", suffix, null),
            new("кінець номера з джокером спереду", suffix, InNumber, WildcardBefore: true),
            new("середина номера з обома джокерами", number[2..^2], InNumber, WildcardBefore: true, WildcardAfter: true),
            new("номер і слово з назви — різні поля", $"{number} Wärmepumpe", null),
            new("ID повністю", id, InId),
            new("ID малими літерами", id.ToLowerInvariant(), InId),
            new("початок ID (дата й час створення) з джокером ззаду", id[..12], InId, WildcardAfter: true),
        ]);
    }

    /// <summary>
    /// У режимі «Einzelnes Wort» номер не шукається зовсім: «TAS0000046 Heizkessel» не знаходить завдання
    /// TAS0000046, хоча вистачає будь-якого слова. Так само ігноруються ID і Kurzname: для кількох слів ADAICA
    /// шукає лише в повнотекстових полях. Тест очікує збіг за номером і зараз <b>падає навмисно</b> — імовірний баг
    /// ADAICA, потребує підтвердження розробників.
    /// </summary>
    [Test]
    [AllureSeverity(SeverityLevel.normal)]
    public void Finden_NumberInSingleWordMode()
    {
        var (search, number) = CreateFixtureAndOpenSearch();

        ShouldBehaveAsExpected(search, number,
        [
            new("номер і слово, якого немає", $"{number} Heizkessel", InNumber, SearchMode.SingleWord),
            new("слово, якого немає, і номер", $"Heizkessel {number}", InNumber, SearchMode.SingleWord),
        ]);
    }
}
