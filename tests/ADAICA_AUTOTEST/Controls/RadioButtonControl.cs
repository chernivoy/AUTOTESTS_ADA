using ADAICA_AUTOTEST.Infrastructure;
using FlaUI.Core.AutomationElements;

namespace ADAICA_AUTOTEST.Controls;

/// <summary>Перемикач (RadioButton, SelectionItemPattern).</summary>
public sealed class RadioButtonControl(string name, Func<AutomationElement> find) : Control(name, find)
{
    public bool IsSelected => Element.Patterns.SelectionItem.Pattern.IsSelected.Value;

    /// <summary>Вибирає перемикач і чекає, доки вибір застосується.</summary>
    public void Select() => Step($"Вибрати «{Name}»", () =>
    {
        if (IsSelected)
            return;

        Element.Patterns.SelectionItem.Pattern.Select();
        Wait.Until(() => IsSelected, $"вибору «{Name}»");
    });
}
