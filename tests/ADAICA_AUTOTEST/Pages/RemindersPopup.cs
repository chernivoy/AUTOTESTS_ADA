using ADAICA_AUTOTEST.Infrastructure;
using FlaUI.Core.AutomationElements;

namespace ADAICA_AUTOTEST.Pages;

/// <summary>
/// Вікно нагадувань про терміни (<c>RemindersPopupPart</c>, WPF Popup) праворуч унизу робочої області. ADAICA показує
/// його через кілька секунд після входу, коли настає час нагадування; терміни, які створюють тести, дають нагадування
/// в кожному прогоні. З кількома нагадуваннями вікно закриває нижню частину робочої області, наприклад список
/// учасників картки чату під «Meine Chats».
/// </summary>
public static class RemindersPopup
{
    /// <summary>
    /// Згортає вікно до вузької смужки біля правого краю, якщо воно розгорнуте. «Alle schließen» не натискаємо:
    /// воно закрило б нагадування користувача, а згортання їх не змінює.
    /// </summary>
    public static void Collapse(Window window)
    {
        AutomationElement? CollapseButton() => Popups.FindDescendant(window, cf => cf.ByAutomationId("btnCollapse"));
        if (CollapseButton() is not { } button)
            return;

        UiInput.LeftClick(
            () => CollapseButton() ?? button,
            () => Popups.FindDescendant(window, cf => cf.ByAutomationId("btnExpand")) is not null,
            "згорнутого вікна нагадувань");
    }
}
