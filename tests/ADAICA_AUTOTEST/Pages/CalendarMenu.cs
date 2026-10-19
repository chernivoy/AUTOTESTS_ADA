using ADAICA_AUTOTEST.Infrastructure;
using Allure.Net.Commons;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using FlaUI.Core.Tools;
using VirtualKeyShort = FlaUI.Core.WindowsAPI.VirtualKeyShort;

namespace ADAICA_AUTOTEST.Pages;

/// <summary>
/// Контекстне меню планувальника у вікні «Kalender»: меню DevExpress у WPF Popup. AutomationId пунктів нестабільні (номер
/// за порядком або випадковий GUID), тож пункти шукаємо за підписом. Підменю DevExpress відкриває вкладеним Popup, а
/// приховані пункти («Zeitskala» у Wochenansicht, «Wiederherstellen») лишає в дереві поза екраном.
/// </summary>
/// <remarks>
/// Меню — прямий нащадок свого Popup, підменю — прямий нащадок Popup, вкладеного в Popup меню, а пункти — прямі нащадки
/// меню. Тому шукаємо лише серед дітей: поруч буває Popup нагадувань, і обхід усього його дерева займає секунду на
/// кожну перевірку.
/// </remarks>
public sealed class CalendarMenu
{
    private const string ItemClass = "LightweightBarItemLinkControl";
    private const string MenuClass = "PopupMenu";
    private const string SubmenuClass = "PopupMenuBase";
    private const string PopupClass = "Popup";

    // Скільки чекати, поки меню закриється саме після вибору пункту.
    private static readonly TimeSpan SelfCloseTimeout = TimeSpan.FromSeconds(1.5);

    private readonly AutomationElement _window;
    private readonly Func<IEnumerable<AutomationElement>> _popups;

    /// <param name="window">Вікно «Kalender»: у нього йде Esc, що закриває меню.</param>
    /// <param name="popups">Вікна WPF Popup застосунку, серед яких відкрито меню.</param>
    internal CalendarMenu(AutomationElement window, Func<IEnumerable<AutomationElement>> popups)
    {
        _window = window;
        _popups = popups;
        Wait.For(() => FindMenu(popups), "контекстного меню планувальника");
    }

    /// <summary>Показані пункти меню в порядку показу (приховані пропускає).</summary>
    public IReadOnlyList<CalendarMenuItem> Items => Entries(Wait.For(() => FindMenu(_popups), "контекстного меню планувальника"));

    public bool IsOpen => FindMenu(_popups) is not null;

    /// <summary>Чи є пункт <paramref name="name"/> у меню, зокрема прихований.</summary>
    internal static bool Contains(Func<IEnumerable<AutomationElement>> popups, string name) =>
        FindMenu(popups)?.FindFirstChild(cf => cf.ByClassName(ItemClass).And(cf.ByName(name))) is not null;

    /// <summary>Розгортає підменю <paramref name="name"/> і повертає його показані пункти.</summary>
    public IReadOnlyList<CalendarMenuItem> Submenu(string name) => AllureApi.Step($"Розгорнути підменю «{name}»", () =>
    {
        Item(name).Patterns.ExpandCollapse.Pattern.Expand();
        var submenu = Wait.For(
            () => Levels(_popups).FirstOrDefault(menu => menu.ClassName == SubmenuClass && Entries(menu).Count > 0),
            $"підменю «{name}»");
        return Entries(submenu);
    });

    /// <summary>
    /// Вибирає показаний пункт <paramref name="name"/> меню або розгорнутого підменю. Пункти-прапорці («Tagesansicht» в
    /// «Ansichtwechsel») DevExpress через Invoke виконує, не закриваючи меню, а відкрите меню не дає закрити вікно
    /// «Kalender», тож тоді меню закриваємо самі.
    /// </summary>
    public void Click(string name) => AllureApi.Step($"Вибрати пункт меню «{name}»", () =>
    {
        Item(name).Patterns.Invoke.Pattern.Invoke();
        if (!Retry.WhileFalse(() => !IsOpen, SelfCloseTimeout, ignoreException: true).Success)
            Close();
    });

    /// <summary>
    /// Закриває меню й розгорнуті підменю клавішею Esc: кожне натискання закриває один рівень. Рахуємо рівні меню, а не
    /// вікна Popup: підменю — Popup усередині Popup меню, а поруч може з'явитися Popup нагадувань.
    /// </summary>
    public void Close() => AllureApi.Step("Закрити контекстне меню", () =>
    {
        for (var levels = Levels(_popups).Count; levels > 0; levels = Levels(_popups).Count)
        {
            var before = levels;
            UiInput.PressKey(() => _window, VirtualKeyShort.ESCAPE, () => Levels(_popups).Count < before, "закриття рівня контекстного меню");
        }
    });

    private AutomationElement Item(string name) =>
        Wait.For(
            () => Levels(_popups)
                .SelectMany(menu => menu.FindAllChildren(cf => cf.ByClassName(ItemClass).And(cf.ByName(name))))
                .FirstOrDefault(item => !item.IsOffscreen),
            $"пункту «{name}» у контекстному меню");

    private static AutomationElement? FindMenu(Func<IEnumerable<AutomationElement>> popups) =>
        Levels(popups).FirstOrDefault(menu => menu.ClassName == MenuClass);

    /// <summary>Відкриті рівні меню: саме меню й розгорнуті підменю.</summary>
    private static List<AutomationElement> Levels(Func<IEnumerable<AutomationElement>> popups)
    {
        var levels = new List<AutomationElement>();
        var pending = new Queue<AutomationElement>(popups());
        while (pending.TryDequeue(out var popup))
        {
            foreach (var child in popup.FindAllChildren())
            {
                // Не всі елементи мають ClassName.
                var className = child.Properties.ClassName.ValueOrDefault;
                if (className is MenuClass or SubmenuClass)
                    levels.Add(child);
                else if (className == PopupClass)
                    pending.Enqueue(child);
            }
        }

        return levels;
    }

    private static List<CalendarMenuItem> Entries(AutomationElement menu) =>
        menu.FindAllChildren(cf => cf.ByClassName(ItemClass))
            .Where(item => !item.IsOffscreen)
            .Select(item => new CalendarMenuItem(
                item.Name,
                item.IsEnabled,
                item.Patterns.Toggle.PatternOrDefault is { } toggle ? toggle.ToggleState.Value == ToggleState.On : null))
            .ToList();
}

/// <summary>Пункт контекстного меню: підпис, чи доступний і, для пунктів-прапорців («Ansichtwechsel»), чи позначений.</summary>
public sealed record CalendarMenuItem(string Name, bool IsEnabled, bool? IsChecked = null)
{
    public override string ToString() =>
        Name + (IsEnabled ? string.Empty : " (недоступний)") + (IsChecked == true ? " ✓" : string.Empty);
}
