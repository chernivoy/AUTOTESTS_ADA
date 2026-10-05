using ADAICA_AUTOTEST.Infrastructure;
using Allure.Net.Commons;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;

namespace ADAICA_AUTOTEST.Pages;

/// <summary>
/// Контакти запису — список, який ADAICA показує на вузлі «Kontakte» під записом (наприклад, під листом):
/// «Kontaktart (Funktion)», «Name», «Aktuelle Adresse», «E-Mail», «Telefon», «Bemerkung».
/// </summary>
public sealed class RecordContactsList(Window window)
{
    /// <summary>Рядки списку: заголовок колонки → текст комірки.</summary>
    public IReadOnlyList<IReadOnlyDictionary<string, string>> Rows
    {
        get
        {
            var list = Wait.For(
                () => window.FindFirstDescendant(cf => cf.ByClassName("InformationView"))?
                    .FindFirstDescendant(cf => cf.ByAutomationId("listView").And(cf.ByControlType(ControlType.DataGrid))),
                "списку контактів запису");
            var columns = list.FindFirstChild(cf => cf.ByControlType(ControlType.Header))
                .FindAllChildren()
                .Select(header => header.FindFirstChild(cf => cf.ByAutomationId("PART_Content"))?.Name ?? string.Empty)
                .ToList();

            return list.FindAllChildren(cf => cf.ByControlType(ControlType.DataItem))
                .Select(row => (IReadOnlyDictionary<string, string>)row.FindAllChildren()
                    .Select((cell, index) => (Column: columns.ElementAtOrDefault(index) ?? string.Empty, Text: CellText(cell)))
                    .Where(cell => cell.Column.Length > 0)
                    .ToDictionary(cell => cell.Column, cell => cell.Text))
                .ToList();
        }
    }

    /// <summary>Чекає контакт з адресою <paramref name="email"/> і перевіряє його функцію й ім'я.</summary>
    public void ShouldHaveContact(string function, string name, string email) =>
        AllureApi.Step($"Перевірити контакт запису: {function} {name}, {email}", () =>
        {
            var row = Wait.For(
                () => Rows.FirstOrDefault(found => found.GetValueOrDefault("E-Mail") == $"E-Mail: {email}"),
                $"контакту з адресою {email} у списку контактів запису");
            row.ShouldSatisfyAllConditions(
                () => row.GetValueOrDefault("Kontaktart (Funktion)").ShouldBe(function, "Колонка «Kontaktart (Funktion)»"),
                () => row.GetValueOrDefault("Name").ShouldBe(name, "Колонка «Name»"));
        });

    // ADAICA дописує до деяких значень пробіл у кінці: «(An) ».
    private static string CellText(AutomationElement cell) =>
        string.Join(" ", cell.FindAllDescendants(cf => cf.ByControlType(ControlType.Text))
            .Select(text => text.Name.Trim())
            .Where(text => text.Length > 0));
}
