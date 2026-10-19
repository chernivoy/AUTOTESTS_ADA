using ADAICA_AUTOTEST.Infrastructure;
using Allure.Net.Commons;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;

namespace ADAICA_AUTOTEST.Pages;

/// <summary>
/// Вікно «Kalender (Nachname, Vorname)» — кнопка «Kalender» над списком «Meine Aufgaben» навігатора Dashboard: панель
/// фільтрів з усіма кнопками стану <see cref="TaskListFilter"/> і планувальник (<see cref="TaskCalendar"/>), під ним —
/// бланк вибраного завдання лише для читання («Element zur Bearbeitung frei»). Вікна, що відкривають меню й кнопки
/// планувальника, належать вікну «Kalender».
/// </summary>
public sealed class TaskCalendarWindow : TaskCalendar
{
    private readonly Window _window;

    /// <param name="owner">Головне вікно ADAICA.</param>
    /// <param name="userName">Користувач, чий календар («Chernivoy, Igor»), — він у заголовку вікна.</param>
    public TaskCalendarWindow(Window owner, string userName)
        : base(owner, Wait.For(() => Find(owner, TitleOf(userName)), $"вікна «{TitleOf(userName)}»", LoadTimeout), "у вікні «Kalender»", Enum.GetValues<TaskListFilter>())
    {
        Title = TitleOf(userName);
        _window = Root.AsWindow();
    }

    public string Title { get; }

    public bool IsOpen => Find(Owner, Title) is not null;

    /// <summary>Бланк вибраного завдання в панелі під планувальником (лише для читання).</summary>
    public override TaskForm Details => new(_window, _window);

    protected override Window DialogOwner => _window;

    // Дочірні вікна «Kalender» і головного вікна та вікна робочого столу, коли ADAICA неактивна (див. Popups).
    protected override IEnumerable<AutomationElement> PopupWindows =>
        Popups.Of(_window).Concat(Owner.FindAllChildren(cf => cf.ByClassName("Popup")));

    public void Close() => AllureApi.Step($"Закрити вікно «{Title}»", () =>
    {
        // Відкрите меню чи випадний список не дає закрити вікно (наприклад, коли тест упав посеред них).
        if (HasOpenPopup())
            UiInput.LeftClick(CounterElement, () => !HasOpenPopup(), "закриття меню й списків вікна «Kalender»");

        _window.Close();
        Wait.Until(() => !IsOpen, $"закриття вікна «{Title}»");
    });

    /// <summary>Для прибирання після тесту: закриває вікно, лише якщо воно ще відкрите.</summary>
    public void CloseIfOpen()
    {
        if (IsOpen)
            Close();
    }

    /// <summary>Згортає нижню панель з бланком вибраного завдання (кнопка «_» унизу вікна), якщо її розгорнуто.</summary>
    protected override void CollapseDetails()
    {
        if (_window.FindFirstChild(cf => cf.ByAutomationId("Minimize")) is not { IsEnabled: true } minimize)
            return;

        AllureApi.Step("Згорнути панель з бланком завдання під планувальником", () =>
        {
            minimize.Patterns.Invoke.Pattern.Invoke();
            Wait.Until(() => _window.FindFirstChild(cf => cf.ByAutomationId("Minimize")) is not { IsEnabled: true }, "згортання панелі з бланком завдання");
            WaitUntilSettled("згортання панелі з бланком завдання");
        });
    }

    private static string TitleOf(string userName) => $"Kalender ({userName})";

    private static Window? Find(Window owner, string title) =>
        owner.FindFirstChild(cf => cf.ByControlType(ControlType.Window).And(cf.ByName(title)))?.AsWindow();
}
