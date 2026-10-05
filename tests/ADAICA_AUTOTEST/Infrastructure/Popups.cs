using FlaUI.Core.AutomationElements;
using FlaUI.Core.Conditions;

namespace ADAICA_AUTOTEST.Infrastructure;

/// <summary>
/// Вікна WPF Popup застосунку: випадні списки й контекстні меню. Поки ADAICA на передньому плані, WPF робить Popup
/// дочірнім вікном головного вікна, а коли користувач працює в іншому застосунку — окремим вікном робочого столу
/// без власника (Popup.ConnectedToForegroundWindow у вихідному коді WPF). Тому шукаємо в обох місцях.
/// </summary>
public static class Popups
{
    public static IEnumerable<AutomationElement> Of(AutomationElement owner)
    {
        var processId = owner.Properties.ProcessId.Value;
        return owner.FindAllChildren(cf => cf.ByClassName("Popup"))
            .Concat(owner.Automation.GetDesktop()
                .FindAllChildren(cf => cf.ByClassName("Popup").And(cf.ByProcessId(processId))));
    }

    /// <summary>Перший елемент, що відповідає умові, у будь-якому Popup застосунку, або null.</summary>
    public static AutomationElement? FindDescendant(AutomationElement owner, Func<ConditionFactory, ConditionBase> condition) =>
        Of(owner)
            .Select(popup => popup.FindFirstDescendant(condition))
            .FirstOrDefault(element => element is not null);
}
