using System.Drawing;
using Allure.Net.Commons;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Tools;
using Keyboard = FlaUI.Core.Input.Keyboard;
using VirtualKeyShort = FlaUI.Core.WindowsAPI.VirtualKeyShort;

namespace ADAICA_AUTOTEST.Infrastructure;

/// <summary>Як тести клікають і натискають клавіші там, де UI Automation не допомагає.</summary>
public enum InputMode
{
    /// <summary>Повідомленнями вікну; якщо дія не подіяла — мишею й клавіатурою.</summary>
    Background,

    /// <summary>Лише повідомленнями вікну: тест ніколи не чіпає мишу й клавіатуру, а не подіяла дія — падає.</summary>
    BackgroundOnly,

    /// <summary>Лише мишею й клавіатурою, як користувач.</summary>
    Mouse,
}

/// <summary>
/// Клік і клавіші для UI, який не відповідає на UI Automation (список навігаторів, вкладки, вузли дерева, контекстні
/// меню, кнопки-перемикачі). Спершу дія йде повідомленням у вікно ADAICA (<see cref="WindowMessageInput"/>) — без
/// миші й клавіатури, тож користувач може працювати за комп'ютером. Чи подіяла дія, видно лише з результату, тому
/// кожен виклик передає умову: якщо вона не виконалася, дія повторюється справжньою мишею (<see cref="AtomicMouse"/>)
/// чи клавіатурою, і це видно у звіті Allure. Режим задає <see cref="TestSettings.InputMode"/>.
/// </summary>
/// <remarks>
/// Ціль передається функцією, бо до повторної спроби WPF може перебудувати дерево. Повтор мишею буває лише тоді,
/// коли умова не виконалася за весь таймаут, тож на повільну ADAICA зайвого кліку не буде.
/// </remarks>
public static class UiInput
{
    /// <summary>Клік лівою кнопкою по <paramref name="target"/>, після якого має виконатися <paramref name="done"/>.</summary>
    /// <param name="offset">Точка кліку відносно лівого верхнього кута елемента (див. <see cref="RightClick"/>).</param>
    public static void LeftClick(
        Func<AutomationElement> target, Func<bool> done, string expectation, TimeSpan? timeout = null, Point? offset = null) =>
        Perform(
            () => WindowMessageInput.LeftClick(target(), offset),
            () => AtomicMouse.LeftClick(target(), offset),
            "клік мишею",
            done, expectation, timeout);

    /// <summary>
    /// Подвійний клік лівою кнопкою. ADAICA спершу стає активною: так подвійний клік працює й у Popup (наприклад, у
    /// списку сповіщень), який WPF закриває в неактивному застосунку.
    /// </summary>
    public static void DoubleClick(
        Func<AutomationElement> target, Func<bool> done, string expectation, TimeSpan? timeout = null, Point? offset = null) =>
        Perform(
            () =>
            {
                var element = target();
                WindowMessageInput.Activate(element);
                WindowMessageInput.DoubleClick(element, offset);
            },
            () => AtomicMouse.DoubleClick(target(), offset),
            "подвійний клік мишею",
            done, expectation, timeout);

    /// <summary>
    /// Клік лівою кнопкою, який відкриває меню (наприклад, «+» біля «Adressen»). Меню — WPF Popup, а WPF його одразу
    /// закриває, якщо застосунок неактивний; правий клік ADAICA активує сама, а кнопку — ні. Тому перед кліком ADAICA
    /// стає активною, а <see cref="ForegroundGuard"/> поверне фокус користувачеві, щойно меню закриється.
    /// </summary>
    public static void LeftClickOpeningMenu(
        Func<AutomationElement> target, Func<bool> opened, string expectation, TimeSpan? timeout = null, Point? offset = null) =>
        Perform(
            () =>
            {
                var element = target();
                WindowMessageInput.Activate(element);
                WindowMessageInput.LeftClick(element, offset);
            },
            () => AtomicMouse.LeftClick(target(), offset),
            "клік мишею",
            opened, expectation, timeout);

    /// <summary>
    /// Клік правою кнопкою по <paramref name="target"/>, після якого має виконатися <paramref name="done"/>. Правий клік
    /// відкриває контекстне меню, а меню WPF живе лише в активному застосунку, тож ADAICA спершу стає активною
    /// (див. <see cref="LeftClickOpeningMenu"/>).
    /// </summary>
    /// <param name="offset">
    /// Точка кліку відносно лівого верхнього кута елемента, у фізичних пікселях, — для вмісту, якого UI Automation
    /// не показує; без неї — середина елемента.
    /// </param>
    public static void RightClick(
        Func<AutomationElement> target, Func<bool> done, string expectation, TimeSpan? timeout = null, Point? offset = null) =>
        Perform(
            () =>
            {
                var element = target();
                WindowMessageInput.Activate(element);
                WindowMessageInput.RightClick(element, offset);
            },
            () => AtomicMouse.RightClick(target(), offset),
            "правий клік мишею",
            done, expectation, timeout);

    /// <summary>
    /// Натискає клавішу у вікні <paramref name="target"/> (наприклад, Esc у контекстному меню). Справжня клавіатура
    /// друкує в активне вікно, тож перед нею вікно ADAICA виводиться на передній план. Повідомлення клавіш WPF теж
    /// доставляє лише активному застосунку, тому ADAICA стає активною й для них.
    /// </summary>
    public static void PressKey(Func<AutomationElement> target, VirtualKeyShort key, Func<bool> done, string expectation, TimeSpan? timeout = null) =>
        Perform(
            () =>
            {
                var element = target();
                WindowMessageInput.Activate(element);
                WindowMessageInput.PressKey(element, (ushort)key);
            },
            () =>
            {
                AtomicMouse.TopLevelWindow(target()).SetForeground();
                Keyboard.Type(key);
            },
            $"клавіша {key} на клавіатурі",
            done, expectation, timeout);

    /// <summary>
    /// Друкує текст у поле з фокусом у вікні <paramref name="target"/> — для полів, яких UI Automation не бачить
    /// (поле треба спершу клацнути). Що текст надруковано, видно лише з наслідку, тож умову передає виклик.
    /// </summary>
    public static void TypeText(Func<AutomationElement> target, string text, Func<bool> done, string expectation, TimeSpan? timeout = null) =>
        Perform(
            () =>
            {
                var element = target();
                WindowMessageInput.Activate(element);
                WindowMessageInput.TypeText(element, text);
            },
            () =>
            {
                AtomicMouse.TopLevelWindow(target()).SetForeground();
                Keyboard.Type(text);
            },
            $"текст «{text}» на клавіатурі",
            done, expectation, timeout);

    private static void Perform(Action viaMessages, Action viaDevice, string deviceAction, Func<bool> done, string expectation, TimeSpan? timeout)
    {
        var effectiveTimeout = timeout ?? TestSettings.DefaultTimeout;
        var mode = TestSettings.InputMode;

        if (mode != InputMode.Mouse)
        {
            // Поки дія не подіяла, фокус лишається в ADAICA: меню, яке вона заповнює пунктами при відкритті, WPF закрила б,
            // якби ForegroundGuard повернув фокус раніше. Далі охоронець сам чекає, доки меню закриється.
            RetryResult<bool> result;
            using (ForegroundGuard.Pause())
            {
                viaMessages();
                result = Retry.WhileFalse(done, effectiveTimeout, ignoreException: true);
            }

            if (result.Success)
                return;

            if (mode == InputMode.BackgroundOnly)
                throw new TimeoutException(
                    $"Не дочекалися: {expectation} (таймаут {effectiveTimeout.TotalSeconds} с). Дію надіслано повідомленням вікну, "
                    + $"а повтор мишею й клавіатурою вимкнено (InputMode={mode}).",
                    result.LastException);

            AllureApi.Step($"Повідомлення вікну не подіяло ({expectation}) — повтор: {deviceAction}");
            TestContext.Out.WriteLine($"[UiInput] Повідомлення вікну не подіяло ({expectation}) — повтор: {deviceAction}.");
        }

        // Справжній миші й клавіатурі ADAICA має бути активною, тож охорону фокусу на цей час знімаємо.
        using (ForegroundGuard.Pause())
        {
            viaDevice();
            Wait.Until(done, expectation, effectiveTimeout);
        }
    }
}
