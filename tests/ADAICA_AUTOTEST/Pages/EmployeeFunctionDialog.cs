using ADAICA_AUTOTEST.Controls;
using ADAICA_AUTOTEST.Infrastructure;
using Allure.Net.Commons;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;

namespace ADAICA_AUTOTEST.Pages;

/// <summary>
/// Вікно «Funktion hinzufügen» (кнопка «Neu» над таблицею функцій співробітника): функція з довідника Mitarbeiterfunktion
/// і дві необов'язкові дати; статус ADAICA виводить із дат. У списку немає функцій, які вже є в таблиці. У вікна немає
/// AutomationId, тому впізнаємо його за списком функцій. OK доступна, коли функцію вибрано.
/// </summary>
/// <param name="owner">Вікно «Mitarbeiter umziehen» чи «Mitarbeiter bearbeiten».</param>
public sealed class EmployeeFunctionDialog(Window owner)
    : ChildDialog(owner, FindWindow, "Funktion hinzufügen", "btnOkWndEmployeeFunctionEdit", "btnCancelWndEmployeeFunctionEdit")
{
    private const string FunctionId = "cmbFunctionWndEmployeeFunctionEdit";

    public ComboBoxControl Function => new("Funktion", Locate.ById(Root, FunctionId));

    public DatePickerControl ValidFrom => new("Gültig vom", Locate.ById(Root, "dpValidFromWndEmployeeFunctionEdit"));

    public DatePickerControl ValidTo => new("Gültig bis", Locate.ById(Root, "dpValidToWndEmployeeFunctionEdit"));

    /// <summary>Статус — TextBlock без AutomationId праворуч від підпису «Status».</summary>
    public LabelControl Status => new("Status", () => Wait.For(
        () => Root.FindAllChildren(cf => cf.ByControlType(ControlType.Text))
            .SkipWhile(text => text.Name != "Status")
            .ElementAtOrDefault(1),
        "статусу у вікні «Funktion hinzufügen»"));

    public void Fill(EmployeeFunctionData function) => AllureApi.Step($"Заповнити функцію {function}", () =>
    {
        Function.Select(function.Function);
        if (function.ValidFrom is { } validFrom)
            ValidFrom.Fill(validFrom);
        if (function.ValidTo is { } validTo)
            ValidTo.Fill(validTo);
    });

    private static AutomationElement? FindWindow(Window owner) =>
        owner.FindAllChildren(cf => cf.ByControlType(ControlType.Window))
            .FirstOrDefault(window => window.FindFirstChild(cf => cf.ByAutomationId(FunctionId)) is not null);
}
