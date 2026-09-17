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
    /// Чекає, доки <paramref name="find"/> поверне не null, і повертає знайдене.
    /// </summary>
    public static T For<T>(Func<T?> find, string description, TimeSpan? timeout = null)
        where T : class
    {
        var effectiveTimeout = timeout ?? TestSettings.DefaultTimeout;
        var result = Retry.WhileNull(find, effectiveTimeout, ignoreException: true);

        return result.Result ?? throw new TimeoutException(
            $"Не дочекалися: {description} (таймаут {effectiveTimeout.TotalSeconds} с).", result.LastException);
    }

    /// <summary>
    /// Шукає нащадка за AutomationId і чекає, доки він з'явиться.
    /// </summary>
    public static AutomationElement FindByAutomationId(
        this AutomationElement parent, string automationId, TimeSpan? timeout = null) =>
        For(
            () => parent.FindFirstDescendant(cf => cf.ByAutomationId(automationId)),
            $"елемента з AutomationId '{automationId}'",
            timeout);

    /// <summary>
    /// Шукає прямий дочірній елемент за AutomationId і чекає, доки він з'явиться.
    /// Потрібно, коли такий самий AutomationId є й глибше в дереві.
    /// </summary>
    public static AutomationElement FindChildByAutomationId(
        this AutomationElement parent, string automationId, TimeSpan? timeout = null) =>
        For(
            () => parent.FindFirstChild(cf => cf.ByAutomationId(automationId)),
            $"дочірнього елемента з AutomationId '{automationId}'",
            timeout);
}
