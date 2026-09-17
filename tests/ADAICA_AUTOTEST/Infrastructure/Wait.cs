using FlaUI.Core.AutomationElements;
using FlaUI.Core.Tools;

namespace ADAICA_AUTOTEST.Infrastructure;

/// <summary>
/// Очікування замість Thread.Sleep: умова перевіряється, доки не виконається або не мине таймаут.
/// </summary>
public static class Wait
{
    public static void Until(Func<bool> condition, string description, TimeSpan? timeout = null)
    {
        var effectiveTimeout = timeout ?? TestSettings.DefaultTimeout;
        var result = Retry.WhileFalse(condition, effectiveTimeout, ignoreException: true);

        if (!result.Success)
            throw new TimeoutException(
                $"Не дочекалися: {description} (таймаут {effectiveTimeout.TotalSeconds} с).", result.LastException);
    }

    /// <summary>
    /// Шукає нащадка за AutomationId і чекає, доки він з'явиться.
    /// </summary>
    public static AutomationElement FindByAutomationId(
        this AutomationElement parent, string automationId, TimeSpan? timeout = null)
    {
        var effectiveTimeout = timeout ?? TestSettings.DefaultTimeout;
        var element = Retry.WhileNull(
            () => parent.FindFirstDescendant(cf => cf.ByAutomationId(automationId)),
            effectiveTimeout,
            ignoreException: true).Result;

        return element ?? throw new TimeoutException(
            $"Елемент з AutomationId '{automationId}' не знайдено за {effectiveTimeout.TotalSeconds} с.");
    }
}
