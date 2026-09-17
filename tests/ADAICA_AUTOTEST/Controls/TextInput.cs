using FlaUI.Core.AutomationElements;

namespace ADAICA_AUTOTEST.Controls;

/// <summary>Читання й запис тексту через ValuePattern — спільне для текстових, числових полів і полів дати.</summary>
internal static class TextInput
{
    public static string Read(AutomationElement element) => element.Patterns.Value.Pattern.Value.Value;

    public static void Write(AutomationElement element, string controlName, string text)
    {
        var value = element.Patterns.Value.PatternOrDefault
            ?? throw new InvalidOperationException($"«{controlName}» не приймає введення тексту.");

        if (value.IsReadOnly.Value)
            throw new InvalidOperationException($"Поле «{controlName}» доступне лише для читання.");

        value.SetValue(text);
    }
}
