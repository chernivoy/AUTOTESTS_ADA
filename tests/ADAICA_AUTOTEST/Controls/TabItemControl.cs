using ADAICA_AUTOTEST.Infrastructure;
using FlaUI.Core.AutomationElements;

namespace ADAICA_AUTOTEST.Controls;

/// <summary>Вкладка (TabItem). Вміст невибраної вкладки WPF не показує в дереві UI Automation.</summary>
public sealed class TabItemControl(string name, Func<AutomationElement> find) : Control(name, find)
{
    public bool IsSelected => Element.Patterns.SelectionItem.Pattern.IsSelected.Value;

    public void Select() => Step($"Відкрити вкладку «{Name}»", () =>
    {
        if (IsSelected)
            return;

        Element.Patterns.SelectionItem.Pattern.Select();
        Wait.Until(() => IsSelected, $"вибору вкладки «{Name}»");
    });
}
