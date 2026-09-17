using ADAICA_AUTOTEST.Infrastructure;
using FlaUI.Core.AutomationElements;

namespace ADAICA_AUTOTEST.Controls;

/// <summary>
/// Дата з часом: у контейнері ADAICA лежать поле дати DatePicker3 («dpDate») і редагований список часу («cmbTime»).
/// Час вводиться з точністю до хвилини.
/// </summary>
public sealed class DateTimePickerControl(string name, Func<AutomationElement> find) : Control(name, find)
{
    /// <summary>Показані дата й час або null, якщо дата порожня. Порожній час вважається північчю.</summary>
    public DateTime? Value
    {
        get
        {
            var container = Element;
            var date = AdaicaFormat.ParseDate(TextInput.Read(DatePart(container).FindByAutomationId("PART_TextBox1")));
            if (date is null)
                return null;

            return date.Value + (AdaicaFormat.ParseTime(TextInput.Read(TimePart(container))) ?? TimeSpan.Zero);
        }
    }

    public void Fill(DateTime value)
    {
        var expected = ToMinutes(value);
        Step($"Заповнити «{Name}»: {AdaicaFormat.FormatDateTime(expected)}", () =>
        {
            var container = Element;
            TextInput.Write(DatePart(container), Name, AdaicaFormat.FormatDate(expected));
            TextInput.Write(TimePart(container), Name, AdaicaFormat.FormatTime(expected));
            Wait.Until(() => Value == expected, $"значення {AdaicaFormat.FormatDateTime(expected)} у полі «{Name}»");
        });
    }

    public void ShouldHaveValue(DateTime expected) =>
        Step($"Перевірити «{Name}»: {AdaicaFormat.FormatDateTime(ToMinutes(expected))}",
            () => Value.ShouldBe(ToMinutes(expected), $"Поле «{Name}»"));

    private static AutomationElement DatePart(AutomationElement container) => container.FindByAutomationId("dpDate");

    private static AutomationElement TimePart(AutomationElement container) => container.FindByAutomationId("cmbTime");

    private static DateTime ToMinutes(DateTime value) =>
        new(value.Year, value.Month, value.Day, value.Hour, value.Minute, 0, value.Kind);
}
