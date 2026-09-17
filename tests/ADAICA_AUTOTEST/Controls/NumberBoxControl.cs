using ADAICA_AUTOTEST.Infrastructure;
using FlaUI.Core.AutomationElements;

namespace ADAICA_AUTOTEST.Controls;

/// <summary>
/// Числове поле. Значення порівнюються як числа: після збереження ADAICA показує «1500» як «1500,00».
/// </summary>
public sealed class NumberBoxControl(string name, Func<AutomationElement> find) : Control(name, find)
{
    /// <summary>Показане число або null, якщо поле порожнє.</summary>
    public decimal? Value => AdaicaFormat.ParseNumber(TextInput.Read(Element));

    public void Fill(decimal value)
    {
        var text = AdaicaFormat.FormatNumber(value);
        Step($"Заповнити «{Name}»: {text}", () =>
        {
            TextInput.Write(Element, Name, text);
            Wait.Until(() => Value == value, $"значення {text} у полі «{Name}»");
        });
    }

    public void ShouldHaveValue(decimal expected) =>
        Step($"Перевірити «{Name}»: {AdaicaFormat.FormatNumber(expected)}",
            () => Value.ShouldBe(expected, $"Поле «{Name}»"));
}
