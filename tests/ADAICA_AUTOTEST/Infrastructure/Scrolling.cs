using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;

namespace ADAICA_AUTOTEST.Infrastructure;

/// <summary>
/// Прокрутка до елемента, у якого немає ScrollItemPattern (наприклад, вкладки посередині області перегляду):
/// найближчий предок, що прокручується вертикально, прокручуємо на початок, а далі кроками вниз, доки елемент не стане
/// видимим.
/// </summary>
public static class Scrolling
{
    private const int MaxSteps = 100;

    public static void IntoView(Func<AutomationElement> element)
    {
        if (!element().IsOffscreen)
            return;

        for (var parent = element().Parent; parent is not null; parent = parent.Parent)
        {
            if (parent.Patterns.Scroll.PatternOrDefault is not { } scroll || !scroll.VerticallyScrollable.Value)
                continue;

            scroll.SetScrollPercent(-1, 0);
            for (var step = 0; step < MaxSteps && element().IsOffscreen && scroll.VerticalScrollPercent.Value < 100; step++)
                scroll.Scroll(ScrollAmount.NoAmount, ScrollAmount.SmallIncrement);
            return;
        }
    }
}
