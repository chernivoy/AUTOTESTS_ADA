using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Conditions;
using FlaUI.Core.Definitions;

namespace ADAICA_AUTOTEST.Infrastructure;

/// <summary>
/// Пошук у raw-дереві UI Automation. Звичайний пошук FlaUI бачить лише control view, куди WPF
/// не включає елементи з шаблонів (наприклад, TextBlock із заголовком вкладки).
/// </summary>
public static class RawTree
{
    public static AutomationElement? FindFirstDescendantRaw(
        this AutomationElement parent, Func<ConditionFactory, ConditionBase> condition)
    {
        using (CreateRequest().Activate())
            return parent.FindFirstDescendant(condition);
    }

    // Full: властивості знайдених елементів і далі читаються наживо, а не лише з кешу.
    private static CacheRequest CreateRequest() => new()
    {
        TreeFilter = TrueCondition.Default,
        TreeScope = TreeScope.Element,
        AutomationElementMode = AutomationElementMode.Full,
    };
}
