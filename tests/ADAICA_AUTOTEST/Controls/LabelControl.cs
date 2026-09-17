using FlaUI.Core.AutomationElements;

namespace ADAICA_AUTOTEST.Controls;

/// <summary>Текст лише для читання (TextBlock): заголовки, повідомлення.</summary>
public sealed class LabelControl(string name, Func<AutomationElement> find) : Control(name, find)
{
    public string Text => Element.Name;

    public void ShouldHaveText(string expected) =>
        Step($"Перевірити «{Name}»: {expected}", () => Text.ShouldBe(expected, $"Текст «{Name}»"));
}
