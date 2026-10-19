using ADAICA_AUTOTEST.Infrastructure;
using Allure.Net.Commons;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;

namespace ADAICA_AUTOTEST.Pages;

/// <summary>
/// Випадне дерево з прапорцями на панелі фільтрів вікна «Kalender»: «Kontakt» (стовпці учасників) і «Suchen in
/// (Suchkriterien)» (поля швидкого пошуку). Корінь — «Alle auswählen», під ним пункти. Пункти й прапорці не мають
/// AutomationId, тож прапорець шукаємо поруч із підписом. Зміну ADAICA застосовує одразу, без кнопки OK.
/// </summary>
/// <remarks>
/// Прапорець реагує лише на клік, а клік повідомленням забирає захоплення миші в Popup, і WPF його закриває (README,
/// «Робота паралельно з тестами»). Прапорець при цьому перемикається, тож після кліку дерево відкриваємо знову.
/// </remarks>
public sealed class TreeFilterPopup
{
    /// <summary>Підпис кореня дерева.</summary>
    public const string SelectAll = "Alle auswählen";

    private readonly Func<AutomationElement> _outside;
    private readonly Func<IEnumerable<AutomationElement>> _popups;
    private readonly string _title;
    private readonly Action _open;
    private readonly Action _changed;

    /// <param name="outside">Елемент вікна поза списком, клік по якому нічого не змінює: ним список закривається.</param>
    /// <param name="open">Відкриває дерево: кнопка на панелі фільтрів.</param>
    /// <param name="changed">Чекає, доки вікно застосує зміну прапорця.</param>
    internal TreeFilterPopup(
        Func<AutomationElement> outside, Func<IEnumerable<AutomationElement>> popups, string title, Action open, Action changed)
    {
        _outside = outside;
        _popups = popups;
        _title = title;
        _open = open;
        _changed = changed;
        if (!IsOpen)
            _open();
        _ = Tree;
    }

    /// <summary>Пункти під «Alle auswählen» у порядку показу: підпис, стан прапорця й чи доступний пункт.</summary>
    public IReadOnlyList<TreeFilterItem> Items =>
        Tree.FindAllDescendants(cf => cf.ByControlType(ControlType.TreeItem))
            .Where(item => item.AutomationId != "SelectAll")
            .Select(item => new TreeFilterItem(Label(item), State(CheckBox(item)), item.IsEnabled))
            .ToList();

    /// <summary>Стан прапорця «Alle auswählen»: true, false або null, якщо позначено не всі пункти.</summary>
    public bool? AllSelected => State(CheckBox(Tree.FindByAutomationId("SelectAll")));

    public bool IsOpen => FindTree(_popups) is not null;

    private AutomationElement Tree => Wait.For(() => FindTree(_popups), $"списку «{_title}»");

    /// <summary>Ставить або знімає прапорець пункту <paramref name="item"/> (або «Alle auswählen»).</summary>
    public void Set(string item, bool isChecked) =>
        AllureApi.Step($"{(isChecked ? "Позначити" : "Зняти позначку з")} «{item}» у «{_title}»", () =>
        {
            AutomationElement Box() => CheckBox(Wait.For(
                () => Tree.FindAllDescendants(cf => cf.ByControlType(ControlType.TreeItem))
                    .FirstOrDefault(treeItem => Label(treeItem) == item),
                $"пункту «{item}» у «{_title}»"));

            // Як і в CheckTreeDialog, прапорець перемикаємо кліком: зміну через TogglePattern список може не побачити.
            // «Alle auswählen», позначене частково, після кліку стає знятим, тож може знадобитися другий клік.
            for (var click = 0; click < 2 && State(Box()) != isChecked; click++)
            {
                var before = State(Box());
                UiInput.LeftClick(Box, () => !IsOpen || State(Box()) != before, $"зміни прапорця «{item}»");
                _changed();
                if (!IsOpen)
                    _open();
            }

            Wait.Until(() => State(Box()) == isChecked, $"{(isChecked ? "позначки" : "знятої позначки")} «{item}» у «{_title}»");
        });

    /// <summary>
    /// Закриває список кліком у вікні «Kalender» поза ним: Esc список не закриває, а клік повідомленням забирає в Popup
    /// захоплення миші.
    /// </summary>
    public void Close() => AllureApi.Step($"Закрити список «{_title}»", () =>
    {
        if (IsOpen)
            UiInput.LeftClick(_outside, () => !IsOpen, $"закриття списку «{_title}»");
    });

    private static AutomationElement? FindTree(Func<IEnumerable<AutomationElement>> popups) =>
        popups().Select(popup => popup.FindFirstDescendant(cf => cf.ByControlType(ControlType.Tree))).FirstOrDefault(tree => tree is not null);

    private static string Label(AutomationElement treeItem) =>
        treeItem.FindFirstChild(cf => cf.ByControlType(ControlType.Text))?.Name ?? string.Empty;

    private static AutomationElement CheckBox(AutomationElement treeItem) =>
        treeItem.FindFirstChild(cf => cf.ByControlType(ControlType.CheckBox))
        ?? throw new InvalidOperationException($"У пункті «{Label(treeItem)}» немає прапорця.");

    private static bool? State(AutomationElement checkBox) => checkBox.Patterns.Toggle.Pattern.ToggleState.Value switch
    {
        ToggleState.On => true,
        ToggleState.Off => false,
        _ => null,
    };
}

/// <summary>Пункт випадного дерева: підпис, стан прапорця (null — частково) і чи доступний.</summary>
public sealed record TreeFilterItem(string Name, bool? IsChecked, bool IsEnabled);
