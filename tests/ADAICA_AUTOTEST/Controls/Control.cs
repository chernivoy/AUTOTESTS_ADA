using Allure.Net.Commons;
using FlaUI.Core.AutomationElements;

namespace ADAICA_AUTOTEST.Controls;

/// <summary>
/// Контрол сторінки (компонент POM): одне поле чи кнопка, яку Page Object оголошує як властивість.
/// Елемент UI Automation шукається заново при кожному зверненні: WPF може перебудувати дерево,
/// і збережене посилання застаріє.
/// </summary>
/// <remarks>
/// Кроки звіту контроли створюють через <see cref="AllureApi.Step(string, Action)"/>, а не атрибутом
/// [AllureStep]: так у назві кроку є назва поля, а винятки не загортаються в TargetInvocationException.
/// </remarks>
public abstract class Control(string name, Func<AutomationElement> find)
{
    /// <summary>Назва для звіту й повідомлень про помилки — як підпис поля в ADAICA.</summary>
    public string Name { get; } = name;

    public bool IsEnabled => Element.IsEnabled;

    protected AutomationElement Element => find();

    public void ShouldBeEnabled() =>
        Step($"Перевірити, що «{Name}» доступне", () => IsEnabled.ShouldBeTrue($"«{Name}» недоступне"));

    public void ShouldBeDisabled() =>
        Step($"Перевірити, що «{Name}» недоступне", () => IsEnabled.ShouldBeFalse($"«{Name}» доступне"));

    public override string ToString() => Name;

    protected static void Step(string name, Action action) => AllureApi.Step(name, action);
}
