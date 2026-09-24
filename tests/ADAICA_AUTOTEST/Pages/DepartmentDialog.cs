using ADAICA_AUTOTEST.Controls;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;

namespace ADAICA_AUTOTEST.Pages;

/// <summary>
/// Вікно «Neue Organisationseinheit hinzufügen» (вузол фірми «Mitarbeiter» → контекстне меню). У вікна немає
/// AutomationId, тому впізнаємо його за кнопкою OK. Кнопка доступна, лише коли назву введено.
/// Після OK ADAICA додає відділ під «Mitarbeiter», виділяє його в дереві й відкриває його картку.
/// </summary>
public sealed class DepartmentDialog : ChildDialog
{
    private const string OkButtonId = "btnOkWndDepartment";

    public DepartmentDialog(Window owner)
        : base(owner, FindWindow, "Neue Organisationseinheit hinzufügen", OkButtonId, "btnCancelWndDepartment")
    {
        Name = new TextBoxControl("Name", Locate.ById(Root, "txtName"));
    }

    public TextBoxControl Name { get; }

    private static AutomationElement? FindWindow(Window owner) =>
        owner.FindAllChildren(cf => cf.ByControlType(ControlType.Window))
            .FirstOrDefault(window => window.FindFirstChild(cf => cf.ByAutomationId(OkButtonId)) is not null);
}
