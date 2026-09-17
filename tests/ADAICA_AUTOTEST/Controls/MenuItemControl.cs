using FlaUI.Core.AutomationElements;

namespace ADAICA_AUTOTEST.Controls;

/// <summary>Пункт меню, зокрема контекстного.</summary>
public sealed class MenuItemControl(string name, Func<AutomationElement> find) : Control(name, find)
{
    public void Click() =>
        Step($"Вибрати пункт меню «{Name}»", () => Element.Patterns.Invoke.Pattern.Invoke());
}
