using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;

namespace ADAICA_AUTOTEST.Controls;

/// <summary>Кнопка-перемикач (TogglePattern), наприклад «+» для списку навігаторів.</summary>
public sealed class ToggleButtonControl(string name, Func<AutomationElement> find) : Control(name, find)
{
    public bool IsOn => Element.Patterns.Toggle.Pattern.ToggleState.Value == ToggleState.On;

    public void SwitchOn() => Step($"Увімкнути «{Name}»", () =>
    {
        if (!IsOn)
            Element.Patterns.Toggle.Pattern.Toggle();
    });
}
