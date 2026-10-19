using System.Runtime.InteropServices;
using FlaUI.Core;
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
    private const string PopupClass = "Popup";
    private const uint GwOwner = 4;

    public static IEnumerable<AutomationElement> Of(AutomationElement owner)
    {
        var processId = owner.Properties.ProcessId.Value;
        return owner.FindAllChildren(cf => cf.ByClassName(PopupClass))
            .Concat(UnownedPopups(owner.Automation, processId));
    }

    /// <summary>Перший елемент, що відповідає умові, у будь-якому Popup застосунку, або null.</summary>
    public static AutomationElement? FindDescendant(AutomationElement owner, Func<ConditionFactory, ConditionBase> condition) =>
        Of(owner)
            .Select(popup => popup.FindFirstDescendant(condition))
            .FirstOrDefault(element => element is not null);

    // Дочірні вікна робочого столу UI Automation перелічує понад секунду, а меню тест перевіряє десятки разів, тож вікна
    // процесу без власника беремо з EnumWindows і в UI Automation перетворюємо лише їх.
    private static IEnumerable<AutomationElement> UnownedPopups(AutomationBase automation, int processId)
    {
        var windows = new List<nint>();
        EnumWindows((window, _) =>
        {
            if (IsWindowVisible(window) && GetWindow(window, GwOwner) == 0 && ProcessOf(window) == processId)
                windows.Add(window);
            return true;
        }, 0);

        foreach (var window in windows)
        {
            AutomationElement? element;
            try
            {
                element = automation.FromHandle(window);
            }
            catch (COMException)
            {
                // Вікно закрилося, поки ми його перелічували.
                continue;
            }

            if (element.Properties.ClassName.ValueOrDefault == PopupClass)
                yield return element;
        }
    }

    private static int ProcessOf(nint window)
    {
        GetWindowThreadProcessId(window, out var processId);
        return (int)processId;
    }

    private delegate bool EnumWindowsProc(nint window, nint data);

    [DllImport("user32.dll")]
    private static extern bool EnumWindows(EnumWindowsProc callback, nint data);

    [DllImport("user32.dll")]
    private static extern bool IsWindowVisible(nint window);

    [DllImport("user32.dll")]
    private static extern nint GetWindow(nint window, uint command);

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(nint window, out uint processId);
}
