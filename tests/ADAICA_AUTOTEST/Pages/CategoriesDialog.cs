using ADAICA_AUTOTEST.Infrastructure;
using Allure.Net.Commons;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;

namespace ADAICA_AUTOTEST.Pages;

/// <summary>
/// Вікно «Kategorie» (кнопка «...» під списком категорій контакту): дерево категорій із прапорцями.
/// Прапорці й пункти не мають AutomationId, тому прапорець шукаємо поруч із підписом категорії.
/// </summary>
public sealed class CategoriesDialog(Window owner)
    : ChildDialog(owner, "wndCategories", "Kategorie", "btnOKWndCategories", "btnCancelWndCategories")
{
    /// <summary>
    /// Ставить прапорець категорії. Лише клік мишею: після TogglePattern прапорець вмикається,
    /// але вікно не бачить зміни й кнопка OK лишається недоступною.
    /// </summary>
    public void Check(string category) => AllureApi.Step($"Позначити категорію «{category}»", () =>
    {
        var checkBox = Wait.For(
            () => Root.FindAllDescendants(cf => cf.ByControlType(ControlType.TreeItem))
                .FirstOrDefault(item => item.FindFirstChild(cf => cf.ByControlType(ControlType.Text).And(cf.ByName(category))) is not null)
                ?.FindFirstChild(cf => cf.ByControlType(ControlType.CheckBox)),
            $"категорії «{category}» у вікні «Kategorie»");

        if (checkBox.Patterns.Toggle.Pattern.ToggleState.Value == ToggleState.On)
            return;

        AtomicMouse.LeftClick(checkBox);
        Wait.Until(() => OkButton.IsEnabled, $"вибору категорії «{category}»");
    });
}
