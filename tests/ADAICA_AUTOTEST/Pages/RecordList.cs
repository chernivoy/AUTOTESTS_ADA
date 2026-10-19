using ADAICA_AUTOTEST.Controls;
using ADAICA_AUTOTEST.Infrastructure;
using Allure.Net.Commons;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;

namespace ADAICA_AUTOTEST.Pages;

/// <summary>
/// Список, який ADAICA показує в області перегляду на вузлі-папці під записом: «Kontakte» («Kontaktart (Funktion)»,
/// «Name», «E-Mail», …, «Historisch: Kommunikationsnummer»), «Notizen» («Name», «Klasse», «Bemerkung», …) тощо.
/// </summary>
public sealed class RecordList(Window window)
{
    /// <summary>Рядки списку: заголовок колонки → текст комірки.</summary>
    public IReadOnlyList<IReadOnlyDictionary<string, string>> Rows
    {
        get
        {
            var list = Wait.For(
                () => window.FindFirstDescendant(cf => cf.ByClassName("InformationView"))?
                    .FindFirstDescendant(cf => cf.ByAutomationId("listView").And(cf.ByControlType(ControlType.DataGrid))),
                "списку записів в області перегляду");
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

    /// <summary>Чекає рядок, у якому колонка <paramref name="column"/> має значення <paramref name="value"/>, і повертає його.</summary>
    public IReadOnlyDictionary<string, string> WaitForRow(string column, string value) =>
        AllureApi.Step($"Знайти в списку рядок «{column}»: {value}", () =>
        {
            var row = Wait.For(
                () => Rows.FirstOrDefault(found => found.GetValueOrDefault(column) == value),
                $"рядка «{column}»: {value}");
            AllureApi.AddAttachment(
                $"Рядок «{value}»",
                "text/plain",
                System.Text.Encoding.UTF8.GetBytes(string.Join(Environment.NewLine, row.Select(cell => $"{cell.Key}: {cell.Value}"))),
                ".txt");
            return row;
        });

    /// <summary>
    /// Кнопка «Kalender» над списком вузла «Aufgaben»: область перегляду замість списку показує планувальник із
    /// завданнями запису (<see cref="RecordTaskCalendar"/>).
    /// </summary>
    public RecordTaskCalendar OpenTaskCalendar() => AllureApi.Step("Показати завдання в календарі кнопкою «Kalender» над списком", () =>
    {
        new ButtonControl("Kalender", () => Wait.For(
                () => window.FindFirstDescendant(cf => cf.ByClassName("InformationView"))?.FindFirstDescendant(cf => cf.ByAutomationId("btnToggleTaskViewList")),
                "кнопки «Kalender» над списком завдань"))
            .Click();
        return new RecordTaskCalendar(window);
    });

    // ADAICA дописує до деяких значень пробіл у кінці: «(An) ».
    private static string CellText(AutomationElement cell) =>
        string.Join(" ", cell.FindAllDescendants(cf => cf.ByControlType(ControlType.Text))
            .Select(text => text.Name.Trim())
            .Where(text => text.Length > 0));
}
