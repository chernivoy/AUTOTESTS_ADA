using ADAICA_AUTOTEST.Infrastructure;
using FlaUI.Core.AutomationElements;

namespace ADAICA_AUTOTEST.Controls;

/// <summary>Текст лише для читання (TextBlock): заголовки, повідомлення.</summary>
public sealed class LabelControl(string name, Func<AutomationElement> find) : Control(name, find)
{
    public string Text => Element.Name;

    public void ShouldHaveText(string expected) =>
        Step($"Перевірити «{Name}»: {expected}", () => Text.ShouldBe(expected, $"Текст «{Name}»"));

    /// <summary>Для тексту, який ADAICA оновлює не одразу, а із затримкою після дії користувача.</summary>
    public void WaitForText(string expected) =>
        Step($"Дочекатися «{Name}»: {expected}", () => Wait.Until(() => Text == expected, $"тексту «{expected}» у «{Name}»"));
}
