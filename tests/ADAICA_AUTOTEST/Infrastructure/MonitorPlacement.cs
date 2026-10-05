using System.ComponentModel;
using System.Drawing;
using System.Runtime.InteropServices;

namespace ADAICA_AUTOTEST.Infrastructure;

/// <summary>
/// Переносить вікно застосунку на заданий монітор, не активуючи його: тест іде на одному моніторі,
/// а користувач працює на іншому. Координати фізичні (тестовий процес per-monitor DPI aware).
/// </summary>
public static class MonitorPlacement
{
    private const int SwShowNoActivate = 4;
    private const uint MonitorDefaultToNull = 0;
    private const uint SwpNoSize = 0x0001;
    private const uint SwpNoZOrder = 0x0004;
    private const int GwlStyle = -16;
    private const int WsMaximizeBox = 0x00010000;
    private const uint SwpNoActivate = 0x0010;
    private const uint WmSysCommand = 0x0112;
    private const nint ScMaximize = 0xF030;
    private const uint MonitorInfoPrimary = 0x0001;

    /// <summary>Монітори в порядку Windows; перший — основний.</summary>
    public static IReadOnlyList<MonitorArea> Monitors
    {
        get
        {
            var monitors = new List<MonitorArea>();
            EnumDisplayMonitors(0, 0, (monitor, _, _, _) =>
            {
                var info = new MonitorInfoEx { Size = Marshal.SizeOf<MonitorInfoEx>() };
                if (GetMonitorInfo(monitor, ref info))
                    monitors.Add(new MonitorArea(info.Device, (info.Flags & MonitorInfoPrimary) != 0, info.Monitor.ToRectangle(), info.Work.ToRectangle()));
                return true;
            }, 0);
            return monitors.OrderByDescending(monitor => monitor.IsPrimary).ToList();
        }
    }

    /// <summary>
    /// Монітор за налаштуванням: «primary», «secondary» (перший неосновний) або ім'я пристрою на кшталт «\\.\DISPLAY2».
    /// Якщо такого монітора немає (наприклад, другий вимкнено), повертає null — вікно лишається, де є.
    /// </summary>
    public static MonitorArea? Find(string setting) =>
        setting.ToLowerInvariant() switch
        {
            "primary" => Monitors.FirstOrDefault(monitor => monitor.IsPrimary),
            "secondary" => Monitors.FirstOrDefault(monitor => !monitor.IsPrimary),
            _ => Monitors.FirstOrDefault(monitor => string.Equals(monitor.Device, setting, StringComparison.OrdinalIgnoreCase)),
        };

    /// <summary>Чи стоїть вікно (його центр) на моніторі <paramref name="monitor"/>.</summary>
    public static bool IsOn(nint window, MonitorArea monitor)
    {
        GetWindowRect(window, out var rect);
        var bounds = rect.ToRectangle();
        return monitor.Bounds.Contains(bounds.X + bounds.Width / 2, bounds.Y + bounds.Height / 2);
    }

    /// <summary>Вікно вже там, куди його ставить <see cref="MoveTo"/>: на моніторі й розгорнуте, якщо це можливо.</summary>
    public static bool IsPlaced(nint window, MonitorArea monitor) =>
        IsOn(window, monitor) && (IsZoomed(window) || !IsMaximizable(window));

    /// <summary>
    /// Ставить вікно на робочу область монітора й розгортає його там. Вікно не активується: розгорнуте вікно спершу
    /// відновлюється без активації, а розгортання йде повідомленням SC_MAXIMIZE.
    /// </summary>
    public static void MoveTo(nint window, MonitorArea monitor)
    {
        if (IsZoomed(window))
            ShowWindow(window, SwShowNoActivate);

        var work = monitor.WorkArea;
        if (!IsMaximizable(window))
        {
            // Вікно, яке не розгортається (заставка «Please wait» на старті ADAICA), ставимо в центр монітора як є.
            GetWindowRect(window, out var rect);
            var size = rect.ToRectangle().Size;
            SetWindowPos(window, 0, work.X + (work.Width - size.Width) / 2, work.Y + (work.Height - size.Height) / 2, 0, 0, SwpNoZOrder | SwpNoActivate | SwpNoSize);
            Wait.Until(() => IsOn(window, monitor), $"вікна на моніторі {monitor.Device}");
            return;
        }

        if (!SetWindowPos(window, 0, work.X, work.Y, work.Width, work.Height, SwpNoZOrder | SwpNoActivate))
            throw new Win32Exception(Marshal.GetLastWin32Error(), $"SetWindowPos не переніс вікно на {monitor.Device}.");

        PostMessage(window, WmSysCommand, ScMaximize, 0);
        Wait.Until(() => IsZoomed(window) && IsOn(window, monitor), $"розгорнутого вікна на моніторі {monitor.Device}");
    }

    private static bool IsMaximizable(nint window) => (GetWindowLong(window, GwlStyle) & WsMaximizeBox) != 0;

    /// <summary>Монітор, на якому більша частина вікна, або null, якщо дескриптор уже недійсний.</summary>
    public static MonitorArea? MonitorOf(nint window)
    {
        var monitor = MonitorFromWindow(window, MonitorDefaultToNull);
        var info = new MonitorInfoEx { Size = Marshal.SizeOf<MonitorInfoEx>() };
        return monitor != 0 && GetMonitorInfo(monitor, ref info)
            ? new MonitorArea(info.Device, (info.Flags & MonitorInfoPrimary) != 0, info.Monitor.ToRectangle(), info.Work.ToRectangle())
            : null;
    }

    private delegate bool MonitorEnumProc(nint monitor, nint hdc, nint rect, nint data);

    [DllImport("user32.dll")]
    private static extern bool EnumDisplayMonitors(nint hdc, nint clip, MonitorEnumProc callback, nint data);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern bool GetMonitorInfo(nint monitor, ref MonitorInfoEx info);

    [DllImport("user32.dll")]
    private static extern nint MonitorFromWindow(nint window, uint flags);

    [DllImport("user32.dll")]
    private static extern bool GetWindowRect(nint window, out NativeRect rect);

    [DllImport("user32.dll")]
    private static extern int GetWindowLong(nint window, int index);

    [DllImport("user32.dll")]
    private static extern bool IsZoomed(nint window);

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(nint window, int command);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SetWindowPos(nint window, nint insertAfter, int x, int y, int width, int height, uint flags);

    [DllImport("user32.dll")]
    private static extern bool PostMessage(nint window, uint message, nint wParam, nint lParam);

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeRect
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;

        public readonly Rectangle ToRectangle() => Rectangle.FromLTRB(Left, Top, Right, Bottom);
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct MonitorInfoEx
    {
        public int Size;
        public NativeRect Monitor;
        public NativeRect Work;
        public uint Flags;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string Device;
    }
}

/// <summary>Монітор: ім'я пристрою, межі й робоча область без панелі задач (фізичні координати).</summary>
public sealed record MonitorArea(string Device, bool IsPrimary, Rectangle Bounds, Rectangle WorkArea);
