using ADAICA_AUTOTEST.Controls;
using FlaUI.Core.AutomationElements;

namespace ADAICA_AUTOTEST.Pages;

/// <summary>Картка відділу фірми (Organisationseinheit) в області перегляду.</summary>
/// <param name="root">Область перегляду з карткою.</param>
public sealed class DepartmentForm(AutomationElement root)
{
    public const string NameId = "txtFullNameDepartment";

    public TextBoxControl Name { get; } = new("Name", Locate.ById(root, NameId));
}
