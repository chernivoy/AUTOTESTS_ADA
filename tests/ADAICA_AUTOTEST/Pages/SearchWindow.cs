using ADAICA_AUTOTEST.Controls;
using ADAICA_AUTOTEST.Infrastructure;
using Allure.Net.Commons.Attributes;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;

namespace ADAICA_AUTOTEST.Pages;

/// <summary>
/// Вікно пошуку «Finden» (System → Finden…), вкладка Dokumentation: повнотекстовий пошук записів за назвою,
/// приміткою та іншими полями. Це дочірнє вікно головного вікна; його AutomationId («This») нічого не каже,
/// тому вікно впізнаємо за вкладками пошуку.
/// </summary>
public sealed class SearchWindow
{
    private const string SearchTabsId = "tcSearchParts";

    // UIA_ScrollPatternNoScroll: не змінювати прокрутку по цій осі.
    private const double NoScroll = -1;

    // Пошук іде на сервері й може тривати кілька секунд.
    private static readonly TimeSpan SearchTimeout = TimeSpan.FromSeconds(30);

    private readonly Window _owner;

    public SearchWindow(Window owner)
    {
        _owner = owner;
        Root = Wait.For(
            () => ChildWindows(owner).FirstOrDefault(window => window.FindFirstDescendant(cf => cf.ByAutomationId(SearchTabsId)) is not null),
            "вікна пошуку «Finden»");

        SearchTerm = new TextBoxControl("Suchbegriff", Locate.ById(Root, "ctrRecordBox", "txtBox"));
        FindButton = new ButtonControl("Finden", Locate.ById(Root, "btnSearchFindCreateRecord"));
        CloseButton = new ButtonControl("Schließen", Locate.ById(Root, "btnClose"));
    }

    public TextBoxControl SearchTerm { get; }

    public ButtonControl FindButton { get; }

    public ButtonControl CloseButton { get; }

    // IsAvailable для закритого вікна WPF лишається true, тому перевіряємо, чи вікно ще серед дочірніх.
    public bool IsOpen => ChildWindows(_owner).Contains(Root);

    private Window Root { get; }

    /// <summary>Шукає записи, що містять усі слова (режим «Alle Wörter», стоїть за замовчуванням).</summary>
    [AllureStep("Знайти записи за словами {words}")]
    public void FindRecords(string words)
    {
        SearchTerm.Fill(words);
        FindButton.Click();
    }

    /// <summary>Чекає, доки серед результатів з'явиться запис із номером <paramref name="number"/>, і повертає його рядок.</summary>
    [AllureStep("Дочекатися запису {number} у результатах пошуку")]
    public SearchResult WaitForResult(string number) =>
        Wait.For(() => FindResult(number), $"запису {number} у результатах пошуку", SearchTimeout);

    [AllureStep("Закрити вікно пошуку")]
    public void Close()
    {
        CloseButton.Click();
        Wait.Until(() => !IsOpen, "закриття вікна пошуку");
    }

    /// <summary>Для прибирання після тесту: закриває вікно, лише якщо воно ще відкрите.</summary>
    public void CloseIfOpen()
    {
        if (IsOpen)
            Close();
    }

    /// <summary>
    /// Рядок результатів із номером <paramref name="number"/> або null. Список віртуалізований: рядків поза видимою
    /// частиною в дереві UI Automation немає, а записів зі спільними словами в базі щодалі більше, тому список гортаємо.
    /// </summary>
    private SearchResult? FindResult(string number)
    {
        var list = Root.FindFirstDescendant(cf => cf.ByAutomationId("RecordTabControl"))
            ?.FindFirstDescendant(cf => cf.ByAutomationId("listView"));
        if (list is null)
            return null;

        var columns = list.FindFirstChild(cf => cf.ByControlType(ControlType.Header))?.FindAllChildren()
            .Select(header => header.FindFirstChild(cf => cf.ByAutomationId("PART_Content"))?.Name ?? string.Empty)
            .ToList();
        if (columns is null)
            return null;

        var scroll = list.Patterns.Scroll.PatternOrDefault;
        var scrollable = scroll?.VerticallyScrollable.Value == true;
        if (scrollable)
            scroll!.SetScrollPercent(NoScroll, 0);

        while (true)
        {
            var result = list.FindAllChildren(cf => cf.ByControlType(ControlType.DataItem))
                .Select(row => ReadRow(row, columns))
                .FirstOrDefault(row => row.Number == number);
            if (result is not null || !scrollable)
                return result;

            var before = scroll!.VerticalScrollPercent.Value;
            scroll.Scroll(ScrollAmount.NoAmount, ScrollAmount.LargeIncrement);
            if (scroll.VerticalScrollPercent.Value <= before)
                return null;
        }
    }

    // Колонки ADAICA дозволяє переставляти, тому комірку беремо за заголовком, а не за номером колонки.
    private static SearchResult ReadRow(AutomationElement row, List<string> columns)
    {
        var cells = row.FindAllChildren().Select(cell => cell.FindFirstChild()?.Name ?? string.Empty).ToList();

        string Cell(string column)
        {
            var index = columns.IndexOf(column);
            return index >= 0 && index < cells.Count
                ? cells[index]
                : throw new InvalidOperationException($"У результатах пошуку немає колонки «{column}».");
        }

        return new SearchResult(Cell("Nummer / Version"), Cell("Klassentyp"), Cell("Betreff/Name"), Cell("Fundstelle"), Cell("Bemerkung"));
    }

    private static IEnumerable<Window> ChildWindows(Window owner) =>
        owner.FindAllChildren(cf => cf.ByControlType(ControlType.Window)).Select(window => window.AsWindow());
}
