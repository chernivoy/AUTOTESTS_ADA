using ADAICA_AUTOTEST.Infrastructure;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;

namespace ADAICA_AUTOTEST.Controls;

/// <summary>Прапорець (CheckBox, TogglePattern).</summary>
public sealed class CheckBoxControl(string name, Func<AutomationElement> find) : Control(name, find)
{
    public bool IsChecked => Element.Patterns.Toggle.Pattern.ToggleState.Value == ToggleState.On;

    /// <summary>Ставить або знімає прапорець і чекає, доки стан зміниться.</summary>
    public void SetChecked(bool isChecked) => Step($"{(isChecked ? "Увімкнути" : "Вимкнути")} «{Name}»", () =>
    {
        if (IsChecked == isChecked)
            return;

        Element.Patterns.Toggle.Pattern.Toggle();
        Wait.Until(() => IsChecked == isChecked, $"стану прапорця «{Name}»");
    });

    public void ShouldBeChecked(bool expected) =>
        Step($"Перевірити «{Name}»: {(expected ? "увімкнено" : "вимкнено")}",
            () => IsChecked.ShouldBe(expected, $"Прапорець «{Name}»"));
}
