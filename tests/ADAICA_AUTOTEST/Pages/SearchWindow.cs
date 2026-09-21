using System.Text.RegularExpressions;
using ADAICA_AUTOTEST.Controls;
using ADAICA_AUTOTEST.Infrastructure;
using Allure.Net.Commons.Attributes;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using FlaUI.Core.Tools;

namespace ADAICA_AUTOTEST.Pages;

/// <summary>
/// Вікно пошуку «Finden» (System → Finden…), вкладка Dokumentation: повнотекстовий пошук записів (Elasticsearch)
/// за назвою, приміткою, номером та іншими полями. Це дочірнє вікно головного вікна; його AutomationId («This»)
/// нічого не каже, тому вікно впізнаємо за вкладками пошуку. Стан джокерів і режиму ADAICA не запам'ятовує:
/// щоразу вікно відкривається з «Txt*» (джокер ззаду) увімкненим і «*Txt» вимкненим.
/// </summary>
public sealed partial class SearchWindow
{
    private const string SearchTabsId = "tcSearchParts";

    // UIA_ScrollPatternNoScroll: не змінювати прокрутку по цій осі.
    private const double NoScroll = -1;

    // Пошук триває близько 3 с; «Abbrechen» стає доступною приблизно через 0,2 с після «Finden».
    private static readonly TimeSpan SearchStartTimeout = TimeSpan.FromSeconds(3);
    private static readonly TimeSpan SearchTimeout = TimeSpan.FromSeconds(30);

    // Новий запис Elasticsearch індексує із затримкою, тож свіжий запис знаходиться не одразу.
    private static readonly TimeSpan IndexTimeout = TimeSpan.FromSeconds(60);

    private readonly Window _owner;

    public SearchWindow(Window owner)
    {
        _owner = owner;
        Root = Wait.For(
            () => ChildWindows(owner).FirstOrDefault(window => window.FindFirstDescendant(cf => cf.ByAutomationId(SearchTabsId)) is not null),
            "вікна пошуку «Finden»");

        SearchTerm = new TextBoxControl("Suchbegriff", Locate.ById(Root, "ctrRecordBox", "txtBox"));
        WildcardBefore = new ToggleButtonControl("*Txt", Locate.ById(Root, "fbRecordTabItemPercentToBeginWndFindCreate"));
        WildcardAfter = new ToggleButtonControl("Txt*", Locate.ById(Root, "fbRecordTabItemPercentToEndWndFindCreate"));
        Mode = new ComboBoxControl("Suchoption", Locate.ById(Root, "cmbSearchRecordSearchOpt"));
        FindButton = new ButtonControl("Finden", Locate.ById(Root, "btnSearchFindCreateRecord"));
        StopButton = new ButtonControl("Abbrechen", Locate.ById(Root, "btnStopFindCreateRecord"));
        CloseButton = new ButtonControl("Schließen", Locate.ById(Root, "btnClose"));
        Filters = new SearchFilters(Root);
    }

    public TextBoxControl SearchTerm { get; }

    /// <summary>«*Txt»: джокер перед кожним словом, тож слово може бути кінцем чи серединою довшого.</summary>
    public ToggleButtonControl WildcardBefore { get; }

    /// <summary>«Txt*»: джокер після кожного слова, тож слово може бути початком довшого. Увімкнений за замовчуванням.</summary>
    public ToggleButtonControl WildcardAfter { get; }

    /// <summary>Режим для кількох слів (<see cref="SearchMode"/>); для одного слова список недоступний.</summary>
    public ComboBoxControl Mode { get; }

    public ButtonControl FindButton { get; }

    /// <summary>«Abbrechen» доступна, лише поки триває пошук.</summary>
    public ButtonControl StopButton { get; }

    public ButtonControl CloseButton { get; }

    /// <summary>Фільтри «Suchen nach (Klassentypen)» і «Suchen in (Suchkriterien)». ADAICA не запам'ятовує їх між відкриттями вікна.</summary>
    public SearchFilters Filters { get; }

    /// <summary>Підсумок останнього пошуку внизу вікна, наприклад «Es wurden 5 Datensätze gefunden.», або null.</summary>
    public string? ResultSummary =>
        Root.FindAllDescendants(cf => cf.ByControlType(ControlType.Text))
            .Select(text => text.Name)
            .FirstOrDefault(text => Summary().IsMatch(text));

    // IsAvailable для закритого вікна WPF лишається true, тому перевіряємо, чи вікно ще серед дочірніх.
    public bool IsOpen => ChildWindows(_owner).Contains(Root);

    private Window Root { get; }

    /// <summary>
    /// Шукає записи й чекає, доки пошук завершиться. Результати — <see cref="ResultFor"/> і <see cref="ResultSummary"/>.
    /// Якщо замість результатів ADAICA показала повідомлення (наприклад, про помилку запиту), закриває його й падає з його текстом.
    /// </summary>
    [AllureStep("Знайти {words}: {mode}, джокер спереду — {wildcardBefore}, ззаду — {wildcardAfter}")]
    public void FindRecords(string words, SearchMode mode = SearchMode.AllWords, bool wildcardBefore = false, bool wildcardAfter = true)
    {
        SearchTerm.Fill(words);
        SetOn(WildcardBefore, wildcardBefore);
        SetOn(WildcardAfter, wildcardAfter);

        // Режим можна вибрати, лише коли слів кілька. Слова можуть бути розділені й не пробілом (табуляцією,
        // нерозривним пробілом), тому режим вибираємо щоразу, коли список доступний: інакше лишився б режим попереднього пошуку.
        if (words.Trim().Contains(' '))
            Wait.Until(() => Mode.IsEnabled, "доступності режиму пошуку");
        if (Mode.IsEnabled)
            Mode.SelectAt((int)mode);

        FindButton.Click();

        // Поки пошук триває, доступна «Abbrechen»: чекаємо, доки пошук почнеться і закінчиться.
        // Запит лише зі службових слів («und», «für») ADAICA завершує миттєво, і «Abbrechen» не встигає стати доступною.
        Retry.WhileFalse(() => StopButton.IsEnabled || MessageBox() is not null, SearchStartTimeout, ignoreException: true);
        Wait.Until(() => !StopButton.IsEnabled, "завершення пошуку", SearchTimeout);

        if (MessageBox() is { } message)
        {
            var text = message.Message.Text;
            message.Button("OK").Click();
            throw new InvalidOperationException($"Замість результатів пошуку «{words}» ADAICA показала повідомлення «{message.Title}»: {text}");
        }
    }

    /// <summary>
    /// Повторює пошук, доки серед результатів не з'явиться запис із номером <paramref name="number"/>:
    /// Elasticsearch індексує новий запис не одразу. Потрібно перед перевірками, що запис <em>не</em> знаходиться.
    /// </summary>
    [AllureStep("Шукати {words}, доки не знайдеться запис {number}")]
    public SearchResult WaitUntilFound(
        string words, string number, SearchMode mode = SearchMode.AllWords, bool wildcardBefore = false, bool wildcardAfter = true) =>
        WaitUntil(words, () => ResultFor(number), $"запису {number}", mode, wildcardBefore, wildcardAfter);

    /// <summary>
    /// Як <see cref="WaitUntilFound"/>, але для запису без номера (інфозапису): шукає рядок, у якому «Bemerkung»
    /// дорівнює <paramref name="remark"/>.
    /// </summary>
    [AllureStep("Шукати {words}, доки не знайдеться запис з Bemerkung {remark}")]
    public SearchResult WaitUntilFoundWithRemark(
        string words, string remark, SearchMode mode = SearchMode.AllWords, bool wildcardBefore = false, bool wildcardAfter = true) =>
        WaitUntil(words, () => ResultWithRemark(remark), $"запису з Bemerkung «{remark}»", mode, wildcardBefore, wildcardAfter);

    /// <summary>
    /// Рядок останнього пошуку із номером <paramref name="number"/> або null. Список віртуалізований: рядків поза
    /// видимою частиною в дереві UI Automation немає, тому список гортаємо від початку до кінця.
    /// </summary>
    public SearchResult? ResultFor(string number) => FindRow("Nummer / Version", number);

    /// <summary>Рядок останнього пошуку, у якому «Bemerkung» дорівнює <paramref name="remark"/>, або null (для записів без номера).</summary>
    public SearchResult? ResultWithRemark(string remark) => FindRow("Bemerkung", remark);

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

    private SearchResult WaitUntil(
        string words, Func<SearchResult?> find, string what, SearchMode mode, bool wildcardBefore, bool wildcardAfter) =>
        Wait.For(
            () =>
            {
                FindRecords(words, mode, wildcardBefore, wildcardAfter);
                return find();
            },
            $"{what} у результатах пошуку «{words}»",
            IndexTimeout);

    private SearchResult? FindRow(string keyColumn, string keyValue)
    {
        var list = Root.FindByAutomationId("RecordTabControl").FindFirstDescendant(cf => cf.ByAutomationId("listView"));
        var headers = list?.FindFirstChild(cf => cf.ByControlType(ControlType.Header));
        if (list is null || headers is null)
            return null;

        var columns = headers.FindAllChildren()
            .Select(header => header.FindFirstChild(cf => cf.ByAutomationId("PART_Content"))?.Name ?? string.Empty)
            .ToList();
        var keyIndex = ColumnIndex(columns, keyColumn);

        var scroll = list.Patterns.Scroll.PatternOrDefault;
        var scrollable = scroll?.VerticallyScrollable.Value == true;
        if (scrollable)
            scroll!.SetScrollPercent(NoScroll, 0);

        while (true)
        {
            // Спершу читаємо лише ключову комірку: результатів може бути багато, а повний рядок — це 14 комірок.
            var row = list.FindAllChildren(cf => cf.ByControlType(ControlType.DataItem))
                .FirstOrDefault(candidate => CellText(candidate, keyIndex) == keyValue);
            if (row is not null)
                return ReadRow(row, columns);
            if (!scrollable)
                return null;

            var before = scroll!.VerticalScrollPercent.Value;
            scroll.Scroll(ScrollAmount.NoAmount, ScrollAmount.LargeIncrement);
            if (scroll.VerticalScrollPercent.Value <= before)
                return null;
        }
    }

    // Повідомлення ADAICA може бути дочірнім вікном і головного вікна, і вікна пошуку.
    private MessageBoxDialog? MessageBox() => MessageBoxDialog.Find(Root) ?? MessageBoxDialog.Find(_owner);

    private static void SetOn(ToggleButtonControl toggle, bool on)
    {
        if (on)
            toggle.SwitchOn();
        else
            toggle.SwitchOff();
    }

    // Колонки ADAICA дозволяє переставляти, тому комірку беремо за заголовком, а не за номером колонки.
    private static SearchResult ReadRow(AutomationElement row, List<string> columns)
    {
        string Cell(string column) => CellText(row, ColumnIndex(columns, column));

        return new SearchResult(
            Cell("Nummer / Version"),
            Cell("Klassentyp"),
            Cell("Betreff/Name"),
            Cell("Fundstelle"),
            Cell("Fundtext"),
            Cell("Klasse"),
            Cell("Unterklasse"),
            Cell("Bemerkung"),
            Cell("Erstelldatum"),
            Cell("ID"));
    }

    private static int ColumnIndex(List<string> columns, string column)
    {
        var index = columns.IndexOf(column);
        return index >= 0 ? index : throw new InvalidOperationException($"У результатах пошуку немає колонки «{column}».");
    }

    // Комірка — ContentPresenter з одним TextBlock (у першій колонці — картинка).
    private static string CellText(AutomationElement row, int column) =>
        row.FindAllChildren().ElementAtOrDefault(column)?.FindFirstChild()?.Name ?? string.Empty;

    private static IEnumerable<Window> ChildWindows(Window owner) =>
        owner.FindAllChildren(cf => cf.ByControlType(ControlType.Window)).Select(window => window.AsWindow());

    // «Es wurden 5 Datensätze gefunden.»; для одного запису правильно «Es wurde 1 Datensatz gefunden.».
    [GeneratedRegex(@"^Es wurden? \d+ Datens\w+ gefunden\.$")]
    private static partial Regex Summary();
}
