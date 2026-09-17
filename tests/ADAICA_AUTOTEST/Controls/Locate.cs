using ADAICA_AUTOTEST.Infrastructure;
using FlaUI.Core.AutomationElements;

namespace ADAICA_AUTOTEST.Controls;

/// <summary>Способи знайти елемент для контрола.</summary>
public static class Locate
{
    /// <summary>
    /// Ланцюжок AutomationId від <paramref name="scope"/>: кожен наступний шукається всередині попереднього,
    /// кожен крок чекає появи елемента. Ланцюжок потрібен, коли AutomationId неунікальний
    /// (наприклад, ComboBox «combo» всередині різних ImageComboBox).
    /// </summary>
    public static Func<AutomationElement> ById(AutomationElement scope, params string[] automationIds) =>
        () => automationIds.Aggregate(scope, (parent, automationId) => parent.FindByAutomationId(automationId));
}
