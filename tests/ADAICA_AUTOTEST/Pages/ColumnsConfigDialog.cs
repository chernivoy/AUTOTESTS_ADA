using ADAICA_AUTOTEST.Controls;
using ADAICA_AUTOTEST.Infrastructure;
using Allure.Net.Commons;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;

namespace ADAICA_AUTOTEST.Pages;

/// <summary>
/// Вікно «Spaltenkonfiguration» (контекстне меню заголовка колонки → «Änderung der Spaltenkonfiguration»): список
/// усіх колонок таблиці з прапорцями. Спершу йдуть увімкнені колонки в порядку показу, далі решта за абеткою.
/// Список віртуалізований, тож його гортаємо. Рядки й прапорці не мають AutomationId.
/// </summary>
public sealed class ColumnsConfigDialog
{
    private const string WindowId = "wndColumnsConfig";

    // UIA_ScrollPatternNoScroll: не змінювати прокрутку по цій осі.
    private const double NoScroll = -1;

    private readonly Window[] _owners;

    /// <param name="owners">
    /// Вікна, дочірнім для одного з яких буде це вікно: головне вікно ADAICA, а для таблиці в окремому вікні (версії
    /// документа) — і те вікно.
    /// </param>
    public ColumnsConfigDialog(params Window[] owners)
    {
        _owners = owners;
        Root = Wait.For(() => FindIn(owners), "вікна «Spaltenkonfiguration»");
        OkButton = new ButtonControl("OK", Locate.ById(Root, "btnClearWndCacheManager"));
    }

    public ButtonControl OkButton { get; }

    /// <summary>Усі колонки в порядку списку й чи увімкнена кожна.</summary>
    public IReadOnlyList<(string Name, bool IsChecked)> Columns
    {
        get
        {
            var columns = new List<(string Name, bool IsChecked)>();
            ForEachItem((name, checkBox) =>
            {
                if (columns.All(column => column.Name != name))
                    columns.Add((name, IsChecked(checkBox)));
            });
            return columns;
        }
    }

    private Window Root { get; }

    private AutomationElement List => Root.FindByAutomationId("lvColConfigWndColumnsConfig");

    /// <summary>Вмикає колонки <paramref name="names"/>, решту вимикає, і закриває вікно кнопкою OK.</summary>
    public void ShowOnly(IReadOnlyCollection<string> names) =>
        AllureApi.Step($"Залишити в «Spaltenkonfiguration» колонки: {string.Join(", ", names)}", () => Apply(names.Contains));

    /// <summary>Вмикає всі колонки й закриває вікно кнопкою OK.</summary>
    public void ShowAll() => AllureApi.Step("Увімкнути в «Spaltenkonfiguration» всі колонки", () => Apply(_ => true));

    private void Apply(Func<string, bool> shouldBeChecked)
    {
        // Увімкнені колонки список показує першими, тож після зміни прапорця рядки можуть переставитися: проходимо ще раз.
        List<string> wrong = [];
        for (var pass = 0; pass < 3; pass++)
        {
            ForEachItem((name, checkBox) =>
            {
                if (IsChecked(checkBox) != shouldBeChecked(name))
                    checkBox.Patterns.Toggle.Pattern.Toggle();
            });

            wrong = Columns.Where(column => column.IsChecked != shouldBeChecked(column.Name)).Select(column => column.Name).ToList();
            if (wrong.Count == 0)
                break;
        }

        if (wrong.Count > 0)
            throw new InvalidOperationException($"Не вдалося змінити прапорці колонок: {string.Join(", ", wrong)}.");

        OkButton.Click();
        Wait.Until(() => FindIn(_owners) is null, "закриття вікна «Spaltenkonfiguration»");
    }

    // IsAvailable закритого вікна WPF лишається true, тож шукаємо вікно серед дочірніх вікон власників.
    private static Window? FindIn(IEnumerable<Window> owners) =>
        owners.Select(owner => owner.FindFirstChild(cf => cf.ByAutomationId(WindowId)))
            .FirstOrDefault(found => found is not null)?.AsWindow();

    /// <summary>Гортає список від початку й викликає <paramref name="action"/> для кожного видимого рядка.</summary>
    private void ForEachItem(Action<string, AutomationElement> action)
    {
        var scroll = List.Patterns.Scroll.Pattern;
        scroll.SetScrollPercent(NoScroll, 0);
        while (true)
        {
            foreach (var item in List.FindAllChildren(cf => cf.ByControlType(ControlType.DataItem)).Where(item => !item.IsOffscreen))
            {
                var name = item.FindFirstDescendant(cf => cf.ByAutomationId("TextElement"))?.Name;
                var checkBox = item.FindFirstDescendant(cf => cf.ByControlType(ControlType.CheckBox));
                if (name is not null && checkBox is not null)
                    action(name, checkBox);
            }

            if (!scroll.VerticallyScrollable.Value || scroll.VerticalScrollPercent.Value >= 99.9)
                return;

            var before = scroll.VerticalScrollPercent.Value;
            scroll.Scroll(ScrollAmount.NoAmount, ScrollAmount.LargeIncrement);
            if (scroll.VerticalScrollPercent.Value <= before)
                return;
        }
    }

    private static bool IsChecked(AutomationElement checkBox) =>
        checkBox.Patterns.Toggle.Pattern.ToggleState.Value == ToggleState.On;
}
