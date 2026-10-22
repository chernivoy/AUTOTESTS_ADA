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
    // Запис зі списку ADAICA відкриває за 10–15 с: розгортає дерево до нього й виділяє.
    private static readonly TimeSpan OpenTimeout = TimeSpan.FromSeconds(40);

    /// <summary>Рядки списку: заголовок колонки → текст комірки.</summary>
    public IReadOnlyList<IReadOnlyDictionary<string, string>> Rows
    {
        get
        {
            var list = List;
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
    /// Подвійний клік по рядку, у якого назва («Name» чи «Dokumentname») — <paramref name="name"/>: ADAICA відкриває
    /// запис: розгортає до нього дерево навігатора й виділяє його. Коли запис відкрито, каже <paramref name="opened"/>.
    /// </summary>
    public void Open(string name, Func<bool> opened, TimeSpan? timeout = null) => AllureApi.Step($"Відкрити «{name}» подвійним кліком по рядку списку", () =>
        UiInput.DoubleClick(
            () => Wait.For(
                () => List.FindAllChildren(cf => cf.ByControlType(ControlType.DataItem))
                    .FirstOrDefault(row => row.FindAllDescendants(cf => cf.ByControlType(ControlType.Text)).Any(text => text.Name.Trim() == name))?
                    .FindAllDescendants(cf => cf.ByControlType(ControlType.Text))
                    .First(text => text.Name.Trim() == name),
                $"рядка «{name}» у списку"),
            opened,
            $"відкриття «{name}»",
            timeout ?? OpenTimeout));

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

    // Область перегляду є в кожної відкритої вкладки навігатора; список беремо з видимої. Список вузла під щойно
    // розгорнутим записом (наприклад, кроки під головним завданням workflow) ADAICA показує й за 10 с і довше.
    private AutomationElement List =>
        Wait.For(
            () => window.FindAllDescendants(cf => cf.ByClassName("InformationView"))
                .Select(view => view.FindFirstDescendant(cf => cf.ByAutomationId("listView").And(cf.ByControlType(ControlType.DataGrid))))
                .FirstOrDefault(list => list is { IsOffscreen: false }),
            "списку записів в області перегляду",
            TimeSpan.FromSeconds(30));

    // ADAICA дописує до деяких значень пробіл у кінці: «(An) ».
    private static string CellText(AutomationElement cell) =>
        string.Join(" ", cell.FindAllDescendants(cf => cf.ByControlType(ControlType.Text))
            .Select(text => text.Name.Trim())
            .Where(text => text.Length > 0));
}
