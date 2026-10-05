using ADAICA_AUTOTEST.Infrastructure;
using Allure.Net.Commons;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;

namespace ADAICA_AUTOTEST.Pages;

/// <summary>
/// Вікно з деревом пунктів із прапорцями й кнопками OK / Abbrechen («Kategorie» контакту, «Titel» персони).
/// Прапорці й пункти не мають AutomationId, тому прапорець шукаємо поруч із підписом пункту.
/// </summary>
public abstract class CheckTreeDialog(Window owner, string windowId, string title)
    : ChildDialog(owner, windowId, title, "btnOKWndCategories", "btnCancelWndCategories")
{
    /// <summary>
    /// Ставить прапорець пункту. Лише клік: після TogglePattern прапорець вмикається,
    /// але вікно не бачить зміни й кнопка OK лишається недоступною.
    /// </summary>
    public void Check(string item) => AllureApi.Step($"Позначити «{item}»", () =>
    {
        AutomationElement CheckBox() => Wait.For(
            () => Root.FindAllDescendants(cf => cf.ByControlType(ControlType.TreeItem))
                .FirstOrDefault(treeItem => treeItem.FindFirstChild(cf => cf.ByControlType(ControlType.Text).And(cf.ByName(item))) is not null)
                ?.FindFirstChild(cf => cf.ByControlType(ControlType.CheckBox)),
            $"пункту «{item}» у вікні «{Root.Name}»");
        bool IsChecked() => CheckBox().Patterns.Toggle.Pattern.ToggleState.Value == ToggleState.On;

        if (IsChecked())
            return;

        UiInput.LeftClick(CheckBox, () => IsChecked() && OkButton.IsEnabled, $"вибору «{item}»");
    });
}
