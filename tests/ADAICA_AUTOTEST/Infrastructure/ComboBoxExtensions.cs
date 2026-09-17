using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;

namespace ADAICA_AUTOTEST.Infrastructure;

public static class ComboBoxExtensions
{
    /// <summary>
    /// Вибирає пункт за текстом і чекає, доки вибір застосується. Список після цього завжди згорнутий.
    /// </summary>
    public static void SelectByText(this ComboBox comboBox, string text)
    {
        if (comboBox.SelectedItem?.Text == text)
            return;

        try
        {
            // Пункти можуть з'явитися із затримкою (наприклад, схеми після вибору бази).
            Wait.Until(() => comboBox.Items.Any(item => item.Text == text), $"пункту «{text}» у списку");
            comboBox.Select(text);
        }
        finally
        {
            // Пошук пунктів розгортає список, а поки він розгорнутий, ADAICA ігнорує закриття вікна.
            if (comboBox.ExpandCollapseState == ExpandCollapseState.Expanded)
                comboBox.Collapse();
        }

        Wait.Until(() => comboBox.SelectedItem?.Text == text, $"вибору «{text}»");
    }
}
