using ADAICA_AUTOTEST.Controls;
using ADAICA_AUTOTEST.Infrastructure;
using Allure.Net.Commons;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;

namespace ADAICA_AUTOTEST.Pages;

/// <summary>
/// Список вузла «Papierkorb» навігатора Dokumentation в області перегляду: записи, видалені пунктом «Löschen» (див.
/// <see cref="RecycleBinEntry"/>). Видалений проєкт, Ordner тощо потрапляє сюди разом зі своїми дочірніми записами —
/// кожен окремим рядком. Номера запису в списку немає, тож рядок шукаємо за внутрішнім ID
/// (<see cref="PropertiesWindow.Id"/>).
/// </summary>
/// <remarks>
/// Список посторінковий (по 25 рядків, кнопки ◀ ▶ над ним реагують лише на клік) і впорядкований за назвою у
/// зворотному порядку, тож свій запис може бути на будь-якій сторінці. Меню рядка: «Wiederherstellen», «Löschen»
/// (остаточно, з тим самим питанням «Löschen», що й у дереві), «Aktualisieren», «Eigenschaften».
/// </remarks>
public sealed class RecycleBin(Window window)
{
    private const string RestoreMenuItemId = "mnuRecordRestore";
    private const string DeleteForeverMenuItemId = "mnuRecordDeleteForcedItem";

    // За цією колонкою список Papierkorb відрізняємо від списків інших вузлів.
    private static readonly string ParentColumn = RecycleBinEntry.Columns[6];

    /// <summary>Рядок запису <paramref name="id"/> або null, якщо його немає на жодній сторінці.</summary>
    public RecycleBinEntry? Entry(string id) => FindRow(id) is { } row ? ReadRow(row) : null;

    /// <summary>Чекає рядок запису <paramref name="id"/> (список заповнюється не одразу) і повертає його.</summary>
    public RecycleBinEntry WaitForEntry(string id) =>
        AllureApi.Step($"Знайти в Papierkorb запис {id}", () =>
        {
            var entry = Wait.For(() => Entry(id), $"запису {id} у Papierkorb");
            AllureApi.AddAttachment($"Рядок Papierkorb {id}", "text/plain", System.Text.Encoding.UTF8.GetBytes(entry.ToString()), ".txt");
            return entry;
        });

    /// <summary>
    /// Перевіряє, що запису <paramref name="id"/> у Papierkorb немає. Список заповнюється не одразу, тож спершу
    /// чекаємо, доки лічильник над списком («Anzahl von Elementen in der Liste») зрівняється з кількістю рядків.
    /// </summary>
    public void ShouldNotContain(string id) =>
        AllureApi.Step($"Перевірити, що запису {id} у Papierkorb немає", () =>
        {
            Wait.Until(IsLoaded, "заповнення списку Papierkorb");
            Entry(id).ShouldBeNull($"Запис {id} лишився в Papierkorb");
        });

    /// <summary>
    /// «Wiederherstellen» у меню рядка. Питання ADAICA не ставить: відновлений запис одразу відкривається в області
    /// перегляду й виділяється у вузлі «Aktuelle».
    /// </summary>
    public void Restore(string id) =>
        AllureApi.Step($"Відновити запис {id}: Papierkorb → контекстне меню → Wiederherstellen", () =>
            RowMenuItem(id, "Wiederherstellen", RestoreMenuItemId).Click());

    /// <summary>«Löschen» у меню рядка: остаточне видалення. Повертає питання «Löschen», на яке відповідає тест.</summary>
    public MessageBoxDialog DeleteForever(string id) =>
        AllureApi.Step($"Остаточно видалити запис {id}: Papierkorb → контекстне меню → Löschen", () =>
        {
            RowMenuItem(id, "Löschen", DeleteForeverMenuItemId).Click();
            return Wait.For(() => MessageBoxDialog.Find(window), "питання «Löschen»");
        });

    private MenuItemControl RowMenuItem(string id, string name, string automationId)
    {
        var row = Wait.For(() => FindRow(id), $"запису {id} у Papierkorb");
        UiInput.RightClick(
            () => NameCell(ScrolledIntoView(FindRow(id) ?? row)),
            () => FindInPopups(automationId) is not null,
            $"контекстного меню запису {id} у Papierkorb");
        return new MenuItemControl(name, () => Wait.For(() => FindInPopups(automationId), $"пункту «{name}» у контекстному меню"));
    }

    /// <summary>Рядок запису <paramref name="id"/>: шукаємо на поточній сторінці, потім гортаємо сторінки від першої.</summary>
    private AutomationElement? FindRow(string id)
    {
        if (List() is not { } list)
            return null;

        var idIndex = Columns(list).IndexOf(RecycleBinEntry.Columns[1]);
        AutomationElement? RowOnPage() =>
            List()?.FindAllChildren(cf => cf.ByControlType(ControlType.DataItem))
                .FirstOrDefault(row => CellText(row, idIndex) == id);

        if (RowOnPage() is { } found)
            return found;

        if (PageButton("btnBegin") is { IsEnabled: true })
            TurnPage("btnBegin");
        while (true)
        {
            if (RowOnPage() is { } row)
                return row;
            if (PageButton("btnForward") is not { IsEnabled: true })
                return null;
            TurnPage("btnForward");
        }
    }

    // Кнопки сторінок — ToggleButton без InvokePattern, реагують лише на клік. Що сторінка змінилася, видно з рядків.
    private void TurnPage(string buttonId)
    {
        var before = PageIds();
        UiInput.LeftClick(
            () => Wait.For(() => PageButton(buttonId), $"кнопки сторінок «{buttonId}» над списком Papierkorb"),
            () => PageIds() is { Count: > 0 } after && !after.SequenceEqual(before),
            "іншої сторінки списку Papierkorb");
    }

    private List<string> PageIds()
    {
        var list = List();
        if (list is null)
            return [];
        var idIndex = Columns(list).IndexOf(RecycleBinEntry.Columns[1]);
        return list.FindAllChildren(cf => cf.ByControlType(ControlType.DataItem)).Select(row => CellText(row, idIndex)).ToList();
    }

    // Кнопок сторінок немає, коли в кошику записів не більше ніж на одну сторінку.
    private AutomationElement? PageButton(string buttonId) =>
        View()?.FindFirstDescendant(cf => cf.ByAutomationId(buttonId));

    // Лічильник прихований (offscreen) і є лише в raw-дереві, але значення в ньому є.
    private bool IsLoaded()
    {
        if (List() is not { } list
            || View()?.FindFirstDescendant(cf => cf.ByAutomationId("tabScroll"))?.FindFirstDescendantRaw(cf => cf.ByAutomationId("tabScrollCount")) is not { } counter)
            return false;
        var total = int.TryParse(counter.Patterns.Value.Pattern.Value.Value, out var count) ? count : -1;
        var rows = list.FindAllChildren(cf => cf.ByControlType(ControlType.DataItem)).Length;
        return total == 0 ? rows == 0 : rows > 0;
    }

    private static RecycleBinEntry ReadRow(AutomationElement row)
    {
        var list = row.Parent;
        var columns = Columns(list);
        var cells = row.FindAllChildren();
        return RecycleBinEntry.FromCells(column =>
        {
            var index = columns.IndexOf(column);
            if (index < 0)
                throw new InvalidOperationException($"У списку Papierkorb немає колонки «{column}»: {string.Join(", ", columns)}.");
            return CellText(cells, index);
        });
    }

    // Список Papierkorb — DataGrid «listView» в області перегляду з колонкою батьківського запису.
    private AutomationElement? List() =>
        View()?.FindFirstDescendant(cf => cf.ByAutomationId("listView").And(cf.ByControlType(ControlType.DataGrid))) is { } list
            && Columns(list).Contains(ParentColumn)
                ? list
                : null;

    private AutomationElement? View() => window.FindFirstDescendant(cf => cf.ByClassName("InformationView"));

    private static List<string> Columns(AutomationElement list) =>
        list.FindFirstChild(cf => cf.ByControlType(ControlType.Header))?
            .FindAllChildren()
            .Select(header => header.FindFirstChild(cf => cf.ByAutomationId("PART_Content"))?.Name ?? string.Empty)
            .ToList() ?? [];

    private static string CellText(AutomationElement row, int column) => CellText(row.FindAllChildren(), column);

    // Комірка — ContentPresenter з одним TextBlock (у першій колонці перед ним картинка).
    private static string CellText(AutomationElement[] cells, int column) =>
        cells.ElementAtOrDefault(column)?.FindFirstChild(cf => cf.ByControlType(ControlType.Text))?.Name ?? string.Empty;

    private static AutomationElement NameCell(AutomationElement row) =>
        row.FindFirstChild()?.FindFirstChild(cf => cf.ByControlType(ControlType.Text)) ?? row;

    private static AutomationElement ScrolledIntoView(AutomationElement row)
    {
        if (row.IsOffscreen && row.Patterns.ScrollItem.TryGetPattern(out var scrollItem))
            scrollItem.ScrollIntoView();
        return row;
    }

    // Меню ADAICA відкриваються у WPF Popup (див. Popups).
    private AutomationElement? FindInPopups(string automationId) =>
        Popups.FindDescendant(window, cf => cf.ByAutomationId(automationId));
}
