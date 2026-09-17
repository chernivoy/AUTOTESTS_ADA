using System.ComponentModel;
using System.Drawing;
using System.Runtime.InteropServices;

namespace ADAICA_AUTOTEST.Infrastructure;

/// <summary>
/// Клік мишею одним викликом SendInput: переміщення й натискання кнопки йдуть разом.
/// FlaUI спершу ставить курсор, а потім окремо натискає кнопку. Якщо між цими кроками курсор зсуне
/// користувач або програма віддаленого доступу, клік піде в довільне місце екрана.
/// </summary>
public static class AtomicMouse
{
    private const uint InputMouse = 0;
    private const uint MouseEventMove = 0x0001;
    private const uint MouseEventLeftDown = 0x0002;
    private const uint MouseEventLeftUp = 0x0004;
    private const uint MouseEventRightDown = 0x0008;
    private const uint MouseEventRightUp = 0x0010;
    private const uint MouseEventVirtualDesk = 0x4000;
    private const uint MouseEventAbsolute = 0x8000;

    private const int SmXVirtualScreen = 76;
    private const int SmYVirtualScreen = 77;
    private const int SmCxVirtualScreen = 78;
    private const int SmCyVirtualScreen = 79;

    public static void LeftClick(Point point) => Click(point, MouseEventLeftDown, MouseEventLeftUp);

    public static void RightClick(Point point) => Click(point, MouseEventRightDown, MouseEventRightUp);

    private static void Click(Point point, uint buttonDown, uint buttonUp)
    {
        var (x, y) = ToAbsolute(point);
        var move = MouseEventMove | MouseEventAbsolute | MouseEventVirtualDesk;
        Input[] inputs =
        [
            MouseInput(x, y, move | buttonDown),
            MouseInput(x, y, move | buttonUp),
        ];

        // SendInput вставляє події підряд, не перемежовуючи їх з іншим введенням.
        if (SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<Input>()) != inputs.Length)
            throw new Win32Exception(Marshal.GetLastWin32Error(), "SendInput не надіслав клік.");
    }

    // Абсолютні координати SendInput задаються від 0 до 65535 у межах усього віртуального робочого столу.
    // Координати фізичні: тестовий процес per-monitor DPI aware (див. TestRunSetup).
    private static (int X, int Y) ToAbsolute(Point point)
    {
        var left = GetSystemMetrics(SmXVirtualScreen);
        var top = GetSystemMetrics(SmYVirtualScreen);
        var width = GetSystemMetrics(SmCxVirtualScreen);
        var height = GetSystemMetrics(SmCyVirtualScreen);

        return ((int)Math.Round((point.X - left) * 65535.0 / (width - 1)),
                (int)Math.Round((point.Y - top) * 65535.0 / (height - 1)));
    }

    private static Input MouseInput(int x, int y, uint flags) =>
        new() { Type = InputMouse, Mouse = new MouseInputData { Dx = x, Dy = y, Flags = flags } };

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint SendInput(uint count, Input[] inputs, int size);

    [DllImport("user32.dll")]
    private static extern int GetSystemMetrics(int index);

    // INPUT з union: для миші достатньо MOUSEINPUT — найбільшого з варіантів.
    [StructLayout(LayoutKind.Sequential)]
    private struct Input
    {
        public uint Type;
        public MouseInputData Mouse;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MouseInputData
    {
        public int Dx;
        public int Dy;
        public uint MouseData;
        public uint Flags;
        public uint Time;
        public nint ExtraInfo;
    }
}
