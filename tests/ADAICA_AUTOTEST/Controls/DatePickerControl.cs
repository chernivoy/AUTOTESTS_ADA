using ADAICA_AUTOTEST.Infrastructure;
using FlaUI.Core.AutomationElements;

namespace ADAICA_AUTOTEST.Controls;

/// <summary>
/// Поле дати ADAICA (DatePicker3). Дату приймає через ValuePattern у форматі <see cref="AdaicaFormat.DatePattern"/>,
/// а показане значення лежить у вкладеному PART_TextBox1.
/// </summary>
public sealed class DatePickerControl(string name, Func<AutomationElement> find) : Control(name, find)
{
    /// <summary>Показана дата або null, якщо поле порожнє.</summary>
    public DateTime? Value => AdaicaFormat.ParseDate(TextInput.Read(Element.FindByAutomationId("PART_TextBox1")));

    public void Fill(DateTime date)
    {
        var text = AdaicaFormat.FormatDate(date);
        Step($"Заповнити «{Name}»: {text}", () =>
        {
            TextInput.Write(Element, Name, text);
            Wait.Until(() => Value == date.Date, $"дати {text} у полі «{Name}»");
        });
    }

    public void ShouldHaveValue(DateTime expected) =>
        Step($"Перевірити «{Name}»: {AdaicaFormat.FormatDate(expected)}",
            () => Value.ShouldBe(expected.Date, $"Поле «{Name}»"));
}
