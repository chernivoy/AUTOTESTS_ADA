using System.ComponentModel;
using System.Drawing;
using System.Runtime.InteropServices;
using FlaUI.Core.AutomationElements;

namespace ADAICA_AUTOTEST.Infrastructure;

/// <summary>
/// Клік і клавіші без миші й клавіатури: повідомлення Windows (WM_LBUTTONDOWN, WM_KEYDOWN…) йдуть прямо у вікно,
/// якому належить елемент. Курсор не рухається, фокус не змінюється, а вікно може бути перекрите чи стояти на іншому
/// моніторі — тож користувач тим часом працює за комп'ютером. Викликайте через <see cref="UiInput"/>: лише він
/// перевіряє, чи подіяв клік, і за потреби повторює його мишею.
/// </summary>
/// <remarks>
/// Щоб WPF прийняла такий клік, потрібні дві речі (див. HwndMouseInputProvider у вихідному коді WPF):
/// <list type="bullet">
/// <item>Подія миші для вікна, яке зараз не отримує мишу, приймається, лише якщо курсор над цим вікном або вікно
/// захопило мишу (GetCapture); інакше WPF вважає її «фальшивою» й відкидає. Курсор лишається в користувача,
/// тому на час кліку захоплюємо мишу вікном ADAICA.</item>
/// <item>Стан кнопки (<c>MouseButtonEventArgs.ButtonState</c>) WPF бере із системи (GetKeyState), і кнопки WPF
/// (ButtonBase: вкладки навігаторів, лупа швидкого пошуку, перемикачі) без натиснутої кнопки кліку не бачать.</item>
/// </list>
/// Тому тестовий потік на час кліку підключається до черги введення потоку ADAICA (AttachThreadInput): захоплює мишу
/// її вікном, позначає кнопку натиснутою лише в цій черзі й надсилає повідомлення синхронно (SendMessage), щоб
/// ADAICA обробила їх, поки стан виставлено. Справжня миша й клавіатура користувача цього не відчувають.
/// </remarks>
public static class WindowMessageInput
{
    private const uint WmKeyDown = 0x0100;
    private const uint WmKeyUp = 0x0101;
    private const uint WmChar = 0x0102;
    private const uint WmMouseMove = 0x0200;
    private const uint WmLButtonDown = 0x0201;
    private const uint WmLButtonUp = 0x0202;
    private const uint WmLButtonDblClk = 0x0203;
    private const uint WmRButtonDown = 0x0204;
    private const uint WmRButtonUp = 0x0205;
    private const nint MkLButton = 0x0001;
    private const nint MkRButton = 0x0002;
    private const int VkLButton = 0x01;
    private const int VkRButton = 0x02;
    private const byte KeyPressed = 0x80;

    private const uint SmtoAbortIfHung = 0x0002;
    private const uint GaRoot = 2;

    // Обробник кліку може відкрити модальне вікно й не повернути керування; тоді не чекаємо його закриття.
    private const uint SendTimeoutMs = 5000;

    /// <param name="offset">
    /// Точка кліку відносно лівого верхнього кута елемента, у фізичних пікселях; без неї — середина елемента. Потрібна
    /// там, де вміст елемента UI Automation не показує (наприклад, рядки списку контактів у вікні збереження листа).
    /// </param>
    public static void LeftClick(AutomationElement target, Point? offset = null) =>
        Click(target, offset, new MouseButton(WmLButtonDown, WmLButtonUp, MkLButton, VkLButton));

    public static void RightClick(AutomationElement target, Point? offset = null) =>
        Click(target, offset, new MouseButton(WmRButtonDown, WmRButtonUp, MkRButton, VkRButton));

    /// <summary>
    /// Подвійний клік лівою кнопкою: друге натискання Windows надсилає як WM_LBUTTONDBLCLK, і WPF рахує його другим
    /// кліком (ClickCount = 2). Обидва кліки йдуть за одне захоплення миші: Popup (наприклад, список сповіщень) WPF
    /// закриває, щойно втрачає захоплення, тож окремий перший клік закрив би його ще до другого.
    /// </summary>
    public static void DoubleClick(AutomationElement target, Point? offset = null) =>
        Click(target, offset, new MouseButton(WmLButtonDown, WmLButtonUp, MkLButton, VkLButton), WmLButtonDblClk);

    /// <summary>
    /// Робить вікно елемента активним. Чужий процес не може просто викликати SetForegroundWindow, тому на мить
    /// підключаємося до черги введення потоку, чиє вікно зараз активне.
    /// </summary>
    public static void Activate(AutomationElement target)
    {
        var window = GetAncestor(HostWindow(target), GaRoot);
        var foreground = GetForegroundWindow();
        GetWindowThreadProcessId(window, out var targetProcess);
        var foregroundThread = GetWindowThreadProcessId(foreground, out var foregroundProcess);
        if (foreground == 0 || foregroundProcess == targetProcess)
            return;

        var ownThread = GetCurrentThreadId();
        var attached = AttachThreadInput(ownThread, foregroundThread, true);
        try
        {
            SetForegroundWindow(window);
        }
        finally
        {
            if (attached)
                AttachThreadInput(ownThread, foregroundThread, false);
        }
    }

    /// <summary>Натискає клавішу у вікні елемента (наприклад, Esc у вікні контекстного меню).</summary>
    public static void PressKey(AutomationElement target, ushort virtualKey)
    {
        var window = HostWindow(target);
        var scanCode = (nint)MapVirtualKey(virtualKey, 0);
        // lParam: лічильник повторів 1 і скан-код у бітах 16–23; для відпускання ще біти 30 і 31.
        Post(window, WmKeyDown, virtualKey, 1 | (scanCode << 16));
        Post(window, WmKeyUp, virtualKey, unchecked((nint)0xC0000001) | (scanCode << 16));
    }

    /// <summary>Друкує текст у вікно елемента символами WM_CHAR — у поле, яке вже має фокус.</summary>
    public static void TypeText(AutomationElement target, string text)
    {
        var window = HostWindow(target);
        foreach (var symbol in text)
            Post(window, WmChar, symbol, 1);
    }

    private static (nint Window, nint LParam) Target(AutomationElement target, Point? offset)
    {
        var window = HostWindow(target);
        var bounds = target.BoundingRectangle;
        var point = offset is { } relative ? new Point(bounds.X + relative.X, bounds.Y + relative.Y) : ClickPoint(target);
        return (window, ClientPoint(window, point));
    }

    /// <param name="secondDown">Повідомлення другого натискання для подвійного кліку; без нього — один клік.</param>
    private static void Click(AutomationElement target, Point? offset, MouseButton button, uint? secondDown = null)
    {
        var (window, lParam) = Target(target, offset);

        var targetThread = GetWindowThreadProcessId(window, out _);
        var ownThread = GetCurrentThreadId();
        if (!AttachThreadInput(ownThread, targetThread, true))
            throw new Win32Exception(Marshal.GetLastWin32Error(), "AttachThreadInput не підключився до черги введення ADAICA.");

        try
        {
            var keys = new byte[256];
            GetKeyboardState(keys);

            SetCapture(window);
            Send(window, WmMouseMove, 0, lParam);
            foreach (var down in secondDown is { } second ? [button.Down, second] : new[] { button.Down })
            {
                keys[button.VirtualKey] = KeyPressed;
                SetKeyboardState(keys);
                Send(window, down, button.Flag, lParam);
                keys[button.VirtualKey] = 0;
                SetKeyboardState(keys);
                Send(window, button.Up, 0, lParam);
            }
        }
        finally
        {
            // Захоплення, яке після кліку взяла сама ADAICA (наприклад, відкрите меню), не чіпаємо.
            if (GetCapture() == window)
                ReleaseCapture();
            AttachThreadInput(ownThread, targetThread, false);
        }
    }

    // Елементам з raw-дерева (заголовок вкладки) UI Automation не завжди дає точку кліку — тоді беремо центр.
    private static Point ClickPoint(AutomationElement target)
    {
        if (target.TryGetClickablePoint(out var point))
            return point;

        var bounds = target.BoundingRectangle;
        return new Point(bounds.X + bounds.Width / 2, bounds.Y + bounds.Height / 2);
    }

    /// <summary>
    /// Вікно Windows, яке малює елемент. У WPF дескриптор мають лише вікна (головне, діалоги, Popup зі списками
    /// й меню), а їхній вміст — ні, тож піднімаємося до найближчого предка з дескриптором.
    /// </summary>
    private static nint HostWindow(AutomationElement element)
    {
        var walker = element.Automation.TreeWalkerFactory.GetRawViewWalker();
        for (var current = element; current is not null; current = walker.GetParent(current))
        {
            if (current.FrameworkAutomationElement.NativeWindowHandle.ValueOrDefault is var handle && handle != 0)
                return handle;
        }

        throw new InvalidOperationException($"Не знайдено вікна Windows для елемента «{element.Name}».");
    }

    /// <summary>
    /// Переводить фізичні екранні координати (тестовий процес per-monitor DPI aware) у клієнтські координати вікна
    /// в тому масштабі, у якому їх чекає застосунок: процес, не обізнаний з DPI монітора, бачить логічні координати.
    /// </summary>
    private static nint ClientPoint(nint window, Point screenPoint)
    {
        var point = new NativePoint { X = screenPoint.X, Y = screenPoint.Y };
        var previous = SetThreadDpiAwarenessContext(GetWindowDpiAwarenessContext(window));
        try
        {
            PhysicalToLogicalPointForPerMonitorDPI(window, ref point);
            if (!ScreenToClient(window, ref point))
                throw new Win32Exception(Marshal.GetLastWin32Error(), "ScreenToClient не перевів координати кліку.");
        }
        finally
        {
            SetThreadDpiAwarenessContext(previous);
        }

        return (nint)((point.Y << 16) | (point.X & 0xFFFF));
    }

    private static void Send(nint window, uint message, nint wParam, nint lParam) =>
        SendMessageTimeout(window, message, wParam, lParam, SmtoAbortIfHung, SendTimeoutMs, out _);

    private static void Post(nint window, uint message, nint wParam, nint lParam)
    {
        if (!PostMessage(window, message, wParam, lParam))
            throw new Win32Exception(Marshal.GetLastWin32Error(), $"PostMessage 0x{message:X4} не надіслав повідомлення.");
    }

    private readonly record struct MouseButton(uint Down, uint Up, nint Flag, int VirtualKey);

    // Unicode-версія: через ANSI-версію WM_CHAR з умлаутами проходить кодову сторінку системи («ö» → «ц» у cp1251).
    [DllImport("user32.dll", EntryPoint = "PostMessageW", SetLastError = true)]
    private static extern bool PostMessage(nint window, uint message, nint wParam, nint lParam);

    [DllImport("user32.dll")]
    private static extern nint SendMessageTimeout(nint window, uint message, nint wParam, nint lParam, uint flags, uint timeout, out nint result);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool ScreenToClient(nint window, ref NativePoint point);

    [DllImport("user32.dll")]
    private static extern bool PhysicalToLogicalPointForPerMonitorDPI(nint window, ref NativePoint point);

    [DllImport("user32.dll")]
    private static extern nint GetWindowDpiAwarenessContext(nint window);

    [DllImport("user32.dll")]
    private static extern nint SetThreadDpiAwarenessContext(nint context);

    [DllImport("user32.dll")]
    private static extern uint MapVirtualKey(uint code, uint mapType);

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(nint window, out uint processId);

    [DllImport("kernel32.dll")]
    private static extern uint GetCurrentThreadId();

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool AttachThreadInput(uint attach, uint attachTo, bool doAttach);

    [DllImport("user32.dll")]
    private static extern nint SetCapture(nint window);

    [DllImport("user32.dll")]
    private static extern nint GetAncestor(nint window, uint flags);

    [DllImport("user32.dll")]
    private static extern nint GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(nint window);


    [DllImport("user32.dll")]
    private static extern nint GetCapture();

    [DllImport("user32.dll")]
    private static extern bool ReleaseCapture();

    [DllImport("user32.dll")]
    private static extern bool GetKeyboardState(byte[] keys);

    [DllImport("user32.dll")]
    private static extern bool SetKeyboardState(byte[] keys);

    [StructLayout(LayoutKind.Sequential)]
    private struct NativePoint
    {
        public int X;
        public int Y;
    }
}
