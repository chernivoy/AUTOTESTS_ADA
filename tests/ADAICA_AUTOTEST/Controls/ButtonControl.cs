using FlaUI.Core.AutomationElements;

namespace ADAICA_AUTOTEST.Controls;

/// <summary>Кнопка, яка натискається через InvokePattern.</summary>
public sealed class ButtonControl(string name, Func<AutomationElement> find) : Control(name, find)
{
    public void Click() => Step($"Натиснути «{Name}»", () =>
    {
        var button = Element;
        if (!button.IsEnabled)
            throw new InvalidOperationException($"Кнопка «{Name}» недоступна.");

        button.Patterns.Invoke.Pattern.Invoke();
    });
}
