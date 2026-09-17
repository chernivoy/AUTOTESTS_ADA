using ADAICA_AUTOTEST.Infrastructure;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;

namespace ADAICA_AUTOTEST.Controls;

/// <summary>Випадний список (ComboBox), зокрема редагований.</summary>
public sealed class ComboBoxControl(string name, Func<AutomationElement> find) : Control(name, find)
{
    public string? SelectedText => ComboBox.SelectedItem is { } item ? ItemText(item) : null;

    /// <summary>Текст у полі редагованого списку.</summary>
    public string Text => TextInput.Read(Element);

    public IReadOnlyList<string> Items
    {
        get
        {
            var comboBox = ComboBox;
            try
            {
                return comboBox.Items.Select(ItemText).ToList();
            }
            finally
            {
                Collapse(comboBox);
            }
        }
    }

    private ComboBox ComboBox => Element.AsComboBox();

    /// <summary>Вибирає пункт за текстом і чекає, доки вибір застосується.</summary>
    public void Select(string text) => Step($"Вибрати в «{Name}»: {text}", () =>
    {
        if (SelectedText == text)
            return;

        var comboBox = ComboBox;
        try
        {
            // Пункти можуть з'явитися із затримкою (наприклад, схеми після вибору бази).
            Wait.For(() => comboBox.Items.FirstOrDefault(item => ItemText(item) == text), $"пункту «{text}» у списку «{Name}»")
                .Select();
        }
        finally
        {
            Collapse(comboBox);
        }

        Wait.Until(() => SelectedText == text, $"вибору «{text}» у списку «{Name}»");
    });

    /// <summary>Вводить текст у редагований список — значення може й не бути серед пунктів.</summary>
    public void EnterText(string text) => Step($"Ввести в «{Name}»: {text}", () =>
    {
        if (!ComboBox.IsEditable)
            throw new InvalidOperationException($"Список «{Name}» не редагований, використайте Select.");

        TextInput.Write(Element, Name, text);
        Wait.Until(() => Text == text, $"значення «{text}» у списку «{Name}»");
    });

    public void ShouldHaveSelected(string expected) =>
        Step($"Перевірити «{Name}»: {expected}", () => SelectedText.ShouldBe(expected, $"Список «{Name}»"));

    // FlaUI бере текст пункту WPF з першого вкладеного елемента. У пунктах з картинкою (Priorität)
    // це картинка без тексту, а сам текст лежить у Name.
    private static string ItemText(ComboBoxItem item) => item.Text is { Length: > 0 } text ? text : item.Name;

    // Пошук пунктів розгортає список, а поки він розгорнутий, ADAICA ігнорує закриття вікна.
    private static void Collapse(ComboBox comboBox)
    {
        if (comboBox.ExpandCollapseState == ExpandCollapseState.Expanded)
            comboBox.Collapse();
    }
}
