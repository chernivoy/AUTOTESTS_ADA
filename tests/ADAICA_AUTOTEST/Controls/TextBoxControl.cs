using ADAICA_AUTOTEST.Infrastructure;
using FlaUI.Core.AutomationElements;

namespace ADAICA_AUTOTEST.Controls;

/// <summary>Текстове поле (TextBox).</summary>
public sealed class TextBoxControl(string name, Func<AutomationElement> find) : Control(name, find)
{
    public string Text => TextInput.Read(Element);

    public bool IsReadOnly => Element.Patterns.Value.Pattern.IsReadOnly.Value;

    /// <summary>Вводить текст і чекає, доки поле його покаже.</summary>
    public void Fill(string text) => Step($"Заповнити «{Name}»: {text}", () =>
    {
        TextInput.Write(Element, Name, text);
        Wait.Until(() => Text == text, $"значення «{text}» у полі «{Name}»");
    });

    public void ShouldHaveText(string expected) =>
        Step($"Перевірити «{Name}»: {expected}", () => Text.ShouldBe(expected, $"Поле «{Name}»"));
}
