using ADAICA_AUTOTEST.Controls;
using ADAICA_AUTOTEST.Pages;
using Allure.Net.Commons;
using Allure.Net.Commons.Attributes;

namespace ADAICA_AUTOTEST.Tests;

/// <summary>Фільтри «Suchen in (Suchkriterien)» і «Suchen nach (Klassentypen)» у розділі «Filtern».</summary>
public partial class SearchTests
{
    /// <summary>
    /// Без поля в «Suchen in» запис не знаходиться за словами з цього поля, але знаходиться за іншими; без типу
    /// запису в «Suchen nach» — не знаходиться зовсім. Кожен прапорець тест повертає одразу після своїх пошуків;
    /// між відкриттями вікна ADAICA фільтри не запам'ятовує.
    /// </summary>
    [Test]
    [AllureSeverity(SeverityLevel.critical)]
    public void Finden_Filters()
    {
        var (search, number) = CreateFixtureAndOpenSearch();
        var filters = search.Filters;

        (CheckBoxControl Filter, SearchCase[] Cases)[] groups =
        [
            (filters.Field(SearchFilters.Remark),
            [
                new("без поля Bemerkung слово з примітки не знаходить", "Sanitär", null),
                new("без поля Bemerkung слово з назви знаходить", "Wärmepumpe", InName),
            ]),
            (filters.Field(SearchFilters.Name),
            [
                new("без поля Name слово з назви не знаходить", "Wärmepumpe", null),
                new("без поля Name слово з примітки знаходить", "Sanitär", InRemark),
            ]),
            (filters.Field(SearchFilters.Number),
            [
                new("без поля Nummer номер не знаходить", number, null),
                new("без поля Nummer слово з назви знаходить", "Wärmepumpe", InName),
            ]),
            (filters.ClassType(SearchFilters.Tasks),
            [
                new("без типу Aufgaben завдання не знаходиться за назвою", "Wärmepumpe", null),
                new("без типу Aufgaben завдання не знаходиться за номером", number, null),
            ]),
            (filters.ClassType(SearchFilters.Projects),
            [
                new("без типу Projekte завдання знаходиться", "Wärmepumpe", InName),
            ]),
        ];

        search.ShouldSatisfyAllConditions(groups.Select(group => (Action)(() =>
        {
            group.Filter.SetChecked(false);
            try
            {
                ShouldBehaveAsExpected(search, number, group.Cases);
            }
            finally
            {
                group.Filter.SetChecked(true);
            }
        })).ToArray());

        AllureApi.Step("Перевірити, що всі фільтри знову ввімкнено", () =>
            filters.Unchecked.ShouldBeEmpty("Фільтри, у яких знято прапорець"));
    }
}
