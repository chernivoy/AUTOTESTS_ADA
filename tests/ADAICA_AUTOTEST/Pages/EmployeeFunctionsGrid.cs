using ADAICA_AUTOTEST.Controls;
using ADAICA_AUTOTEST.Infrastructure;
using Allure.Net.Commons;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;

namespace ADAICA_AUTOTEST.Pages;

/// <summary>
/// Таблиця функцій співробітника у вікнах «Mitarbeiter umziehen» і «Mitarbeiter bearbeiten» (DevExpress GridControl
/// `dgFunctionsCtrEmployeeFunctions`): Funktion, Gültig vom, Gültig bis, Status. Над нею кнопки «Neu», «Bearbeiten»,
/// «Löschen». Комірки — елементи з AutomationId «RowData.Row.…» і значенням у ValuePattern; нову, ще не збережену
/// функцію ADAICA позначає «*» («Neue Funktion») біля назви.
/// </summary>
/// <param name="window">Вікно з таблицею: вікно «Funktion hinzufügen» — його дочірнє вікно.</param>
public sealed class EmployeeFunctionsGrid(Window window)
{
    public ButtonControl AddButton { get; } = new("Neu", Locate.ById(window, "btnAddFunctionCtrEmployeeFunctions"));

    /// <summary>Рядки таблиці у форматі <see cref="EmployeeFunctionData.Row"/>, у порядку показу.</summary>
    public IReadOnlyList<string> Rows =>
        Wait.FindByAutomationId(window, "dataPresenter")
            .FindAllChildren(cf => cf.ByControlType(ControlType.DataItem))
            .Select(row => EmployeeFunctionData.FormatRow(
                Cell(row, "FunctionDescription"),
                DateIn(Cell(row, "ValidFrom")),
                DateIn(Cell(row, "ValidTo")),
                Cell(row, "StatusDescription"),
                isNew: row.FindFirstDescendant(cf => cf.ByControlType(ControlType.Text).And(cf.ByName("*"))) is not null))
            .ToList();

    /// <summary>«Neu» → «Funktion hinzufügen»: функція й дати, перевірка статусу, який ADAICA вивела з дат, OK.</summary>
    public void Add(EmployeeFunctionData function) => AllureApi.Step($"Додати функцію {function}", () =>
    {
        AddButton.Click();
        var dialog = new EmployeeFunctionDialog(window);
        dialog.Fill(function);
        dialog.Status.WaitForText(function.Status);
        dialog.Confirm();
        Wait.Until(() => Rows.Contains(function.Row(isNew: true)), $"функції «{function}» у таблиці");
    });

    /// <summary>Перевіряє рядки таблиці (порядок не важливий). Таблиця завантажує рядки не одразу після відкриття вікна.</summary>
    /// <param name="isNew">Чи функції ще не збережено (позначка «*»).</param>
    public void ShouldHaveFunctions(IReadOnlyList<EmployeeFunctionData> expected, bool isNew = false) =>
        AllureApi.Step($"Перевірити функції: {string.Join("; ", expected)}", () =>
        {
            Wait.Until(() => Rows.Count > 0, "рядків у таблиці функцій");
            Rows.ShouldBe(expected.Select(function => function.Row(isNew)), ignoreOrder: true, "Таблиця функцій");
        });

    private static string Cell(AutomationElement row, string column) =>
        row.FindFirstChild(cf => cf.ByAutomationId($"RowData.Row.{column}"))?.Patterns.Value.PatternOrDefault?.Value.ValueOrDefault ?? string.Empty;

    // Дату таблиця показує то з часом, то без: «01.10.2026 00:00:00» чи «01.10.2026».
    private static DateTime? DateIn(string text) => AdaicaFormat.ParseDate(text.Split(' ')[0]);
}
