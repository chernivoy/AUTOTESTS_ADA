using FlaUI.Core.AutomationElements;

namespace ADAICA_AUTOTEST.Controls;

/// <summary>Поле пароля. Значення не потрапляє у звіт, а UI Automation не дає його прочитати.</summary>
public sealed class PasswordBoxControl(string name, Func<AutomationElement> find) : Control(name, find)
{
    public void Fill(string password) =>
        Step($"Ввести пароль у «{Name}»", () => TextInput.Write(Element, Name, password));
}
