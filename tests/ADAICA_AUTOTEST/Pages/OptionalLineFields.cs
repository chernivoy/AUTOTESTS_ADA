using ADAICA_AUTOTEST.Infrastructure;
using FlaUI.Core.AutomationElements;

namespace ADAICA_AUTOTEST.Pages;

/// <summary>
/// Поля Bundesland і Ortsteil у рядку пошуку «Finden &amp; Erstellen». Їх показують перемикачі «Bundesland
/// berücksichtigen» і «Ortsteil berücksichtigen» (на вкладці Kontakte — `fbStatesPACWndFindCreate` і
/// `fbCommPartsPACWndFindCreate`). У BLANK_IGOR вони ввімкнені одразу, а в AUTOTEST (користувач angela) — ні,
/// і без Bundesland ADAICA не створить адресу. Id перемикачів на різних вкладках різні, тому шукаємо за префіксом.
/// </summary>
internal static class OptionalLineFields
{
    /// <summary>Вмикає «Bundesland berücksichtigen» і «Ortsteil berücksichtigen», якщо цих полів у рядку пошуку немає.</summary>
    public static void Show(AutomationElement window)
    {
        Show(window, "txtBoxState", "fbStates", "Bundesland");
        Show(window, "txtBoxCommunityPart", "fbCommParts", "Ortsteil");
    }

    // Перемикачі реагують на клік мишею.
    private static void Show(AutomationElement window, string boxId, string toggleIdPrefix, string field)
    {
        if (IsShown(window, boxId))
            return;

        var toggle = Wait.For(
            () => window.FindAllDescendants()
                .FirstOrDefault(element => element.Properties.AutomationId.ValueOrDefault?.StartsWith(toggleIdPrefix, StringComparison.Ordinal) == true),
            $"перемикача «{field} berücksichtigen»");
        AtomicMouse.LeftClick(toggle);
        Wait.Until(() => IsShown(window, boxId), $"поля {field} у рядку пошуку");
    }

    private static bool IsShown(AutomationElement window, string boxId) =>
        window.FindFirstDescendant(cf => cf.ByAutomationId(boxId)) is not null;
}
