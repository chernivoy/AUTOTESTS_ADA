using System.Runtime.InteropServices;

namespace ADAICA_AUTOTEST.Infrastructure;

/// <summary>
/// Не дає ADAICA забрати фокус у користувача. ADAICA сама активує своє вікно (під час запуску, після входу,
/// відкриваючи вікна), і тоді натискання клавіш, які користувач друкує в іншому застосунку, йдуть в ADAICA.
/// Заборонити це ззовні не можна, тому поки йде тест, фоновий потік стежить за активним вікном і повертає фокус
/// у вікно, де користувач працював перед тим.
/// </summary>
/// <remarks>
/// Повністю тримати ADAICA неактивною не можна: WPF закриває Popup (список навігаторів, контекстні меню), щойно
/// застосунок стає неактивним, і коли ADAICA відкриває Popup, вона сама активує вікно. Тому фокус лишається в ADAICA,
/// поки в неї відкрите вікно Popup, і повертається користувачеві приблизно через 0,4 с після того, як Popup закрився.
/// Фокус не повертається й тоді, коли курсор над вікном ADAICA (схоже, користувач клікнув у нього сам), і на час
/// запасного кліку справжньою мишею чи клавіатурою (<see cref="Pause"/>). Чужий процес не може просто зробити своє
/// вікно активним, тому на мить підключаємося до черги введення активного потоку (AttachThreadInput) — тоді Windows
/// дозволяє SetForegroundWindow.
/// </remarks>
public sealed class ForegroundGuard : IDisposable
{
    private const int GwlExStyle = -20;
    private const int WsExNoActivate = 0x08000000;

    // Скільки ADAICA має пробути активною без відкритих Popup, перш ніж фокус повернеться користувачеві.
    private static readonly TimeSpan Grace = TimeSpan.FromMilliseconds(400);
    private static readonly TimeSpan Interval = TimeSpan.FromMilliseconds(50);
    private static int _pauses;

    private readonly int _processId;
    private readonly Thread _thread;
    private volatile bool _stopped;
    private nint _userWindow;
    private bool _wasForeground;
    private DateTime _stolenAt;
    private DateTime _popupSeenAt;

    private ForegroundGuard(int processId, nint userWindow)
    {
        _processId = processId;
        _userWindow = userWindow;
        _thread = new Thread(Watch) { IsBackground = true, Name = "ForegroundGuard" };
        _thread.Start();
    }

    /// <summary>Скільки разів ADAICA ставала активною, коли користувач працював в іншому вікні.</summary>
    public int Stolen { get; private set; }

    /// <summary>Скільки разів фокус повернуто користувачеві.</summary>
    public int Restored { get; private set; }

    /// <summary>Вікно, активне зараз; запам'ятайте його до запуску ADAICA і передайте в <see cref="Start"/>.</summary>
    public static nint CurrentForeground => GetForegroundWindow();

    /// <summary>
    /// Починає стежити за процесом <paramref name="processId"/>. Повертає null, коли фокус тестам потрібен
    /// (<see cref="InputMode.Mouse"/>).
    /// </summary>
    public static ForegroundGuard? Start(int processId, nint userWindow) =>
        TestSettings.InputMode == InputMode.Mouse ? null : new ForegroundGuard(processId, userWindow);

    /// <summary>Призупиняє охорону, поки тест сам працює справжньою мишею чи клавіатурою.</summary>
    public static IDisposable Pause()
    {
        Interlocked.Increment(ref _pauses);
        return new Resume();
    }

    public void Dispose()
    {
        _stopped = true;
        _thread.Join(TimeSpan.FromSeconds(1));
    }

    private void Watch()
    {
        while (!_stopped)
        {
            try
            {
                Check();
            }
            catch (Exception)
            {
                // Вікна з'являються й зникають; наступна перевірка спробує знову.
            }

            Thread.Sleep(Interval);
        }
    }

    private void Check()
    {
        var foreground = GetForegroundWindow();
        if (foreground == 0)
            return;

        if (ProcessOf(foreground) != _processId)
        {
            _userWindow = foreground;
            _wasForeground = false;
            return;
        }

        var now = DateTime.UtcNow;
        if (HasOpenPopup())
            _popupSeenAt = now;

        if (!_wasForeground)
        {
            _wasForeground = true;
            _stolenAt = now;
            Stolen++;
        }

        if (Volatile.Read(ref _pauses) > 0 || _userWindow == 0 || !IsWindow(_userWindow) || !IsWindowVisible(_userWindow))
            return;

        // Відкритий Popup (список навігаторів, контекстне меню) WPF закриває, щойно застосунок стає неактивним, тож
        // поки він є, фокус лишаємо ADAICA. Перед поверненням трохи чекаємо: ADAICA активує вікно трохи раніше,
        // ніж з'являється Popup.
        if (now - _stolenAt < Grace || now - _popupSeenAt < Grace)
            return;

        // Курсор над ADAICA — найімовірніше, користувач сам клікнув у неї.
        if (GetCursorPos(out var cursor) && ProcessOf(WindowFromPoint(cursor)) == _processId)
            return;

        var foregroundThread = GetWindowThreadProcessId(foreground, out _);
        var ownThread = GetCurrentThreadId();
        var attached = AttachThreadInput(ownThread, foregroundThread, true);
        try
        {
            if (SetForegroundWindow(_userWindow))
            {
                Restored++;
                _wasForeground = false;
            }
        }
        finally
        {
            if (attached)
                AttachThreadInput(ownThread, foregroundThread, false);
        }
    }

    // Popup WPF — видиме вікно верхнього рівня з WS_EX_NOACTIVATE (див. Popup.CreateWindow у вихідному коді WPF).
    private bool HasOpenPopup()
    {
        var found = false;
        EnumWindows((window, _) =>
        {
            if (IsWindowVisible(window) && ProcessOf(window) == _processId && (GetWindowLong(window, GwlExStyle) & WsExNoActivate) != 0)
            {
                found = true;
                return false;
            }

            return true;
        }, 0);
        return found;
    }

    private static int ProcessOf(nint window)
    {
        GetWindowThreadProcessId(window, out var processId);
        return (int)processId;
    }

    private sealed class Resume : IDisposable
    {
        private int _disposed;

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) == 0)
                Interlocked.Decrement(ref _pauses);
        }
    }

    private delegate bool EnumWindowsProc(nint window, nint data);

    [DllImport("user32.dll")]
    private static extern bool EnumWindows(EnumWindowsProc callback, nint data);

    [DllImport("user32.dll")]
    private static extern int GetWindowLong(nint window, int index);

    [DllImport("user32.dll")]
    private static extern nint GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(nint window);

    [DllImport("user32.dll")]
    private static extern bool IsWindow(nint window);

    [DllImport("user32.dll")]
    private static extern bool IsWindowVisible(nint window);

    [DllImport("user32.dll")]
    private static extern bool GetCursorPos(out NativePoint point);

    [DllImport("user32.dll")]
    private static extern nint WindowFromPoint(NativePoint point);

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(nint window, out uint processId);

    [DllImport("kernel32.dll")]
    private static extern uint GetCurrentThreadId();

    [DllImport("user32.dll")]
    private static extern bool AttachThreadInput(uint attach, uint attachTo, bool doAttach);

    [StructLayout(LayoutKind.Sequential)]
    private struct NativePoint
    {
        public int X;
        public int Y;
    }
}
