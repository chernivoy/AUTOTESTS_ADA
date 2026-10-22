using System.Text.RegularExpressions;
using ADAICA_AUTOTEST.Controls;
using ADAICA_AUTOTEST.Infrastructure;
using Allure.Net.Commons;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using FlaUI.Core.WindowsAPI;

namespace ADAICA_AUTOTEST.Pages;

/// <summary>
/// Вікно «Workflowkonfiguration» (Konfiguration → Workflowkonfiguration, `wndWorkflowConfiguration`): шаблони workflow,
/// спільні для всіх користувачів. Над списком — «Neu», «Bearbeiten», «Löschen» (картинки без підпису), швидкий пошук і
/// «Nur meine»; під списком — область перегляду вибраного шаблону (той самий бланк <see cref="WorkflowForm"/>,
/// лише для читання). Усе, що зроблено у вікні (новий шаблон, зміни, видалення), ADAICA тримає в одній транзакції:
/// у базу воно потрапляє після OK чи «Übernehmen», а «Abbrechen» відкочує все, що не «übernommen».
/// </summary>
/// <remarks>
/// Пошук і «Nur meine» працюють над уже завантаженим списком (з коду ADAICA, <c>WorkflowConfigurationVM.IsItemSuitable</c>):
/// пошук без урахування регістру шукає частину тексту в назві, «Anwender», «Geändert von» і датах створення та зміни
/// (як «дд.ММ.рррр гг:хх»); «Nur meine» лишає шаблони, створені поточним користувачем.
/// </remarks>
public sealed partial class WorkflowConfigWindow
{
    public const string Title = "Workflowkonfiguration";

    private const string WindowId = "wndWorkflowConfiguration";
    private const string ListId = "dgCtrWorkflowConf";

    /// <summary>Колонки списку: підпис → AutomationId комірки.</summary>
    public static readonly IReadOnlyDictionary<string, string> ColumnIds = new Dictionary<string, string>
    {
        ["Workflow"] = "RowData.Row.Workflow",
        ["Workflowname"] = "RowData.Row.WorkflowName",
        ["Anwender"] = "RowData.Row.EntryUserName",
        ["Erstelldatum"] = "RowData.Row.EntryDate",
        ["Geändert von"] = "RowData.Row.ChangeUserName",
        ["Änderungsdatum"] = "RowData.Row.ChangeDate",
    };

    private readonly Window _mainWindow;

    public WorkflowConfigWindow(Window mainWindow)
    {
        _mainWindow = mainWindow;
        // Одразу після входу, поки ADAICA завантажує Dashboard, вікно відкривається довше за звичайне очікування.
        Root = Wait.For(() => Find(mainWindow), $"вікна «{Title}»", TimeSpan.FromSeconds(30));
        NewButton = new ButtonControl("Neu", Locate.ById(Root, "btnNewCtrWorkflowConf"));
        EditButton = new ButtonControl("Bearbeiten", Locate.ById(Root, "btnEditCtrWorkflowConf"));
        DeleteButton = new ButtonControl("Löschen", Locate.ById(Root, "btnDeleteCtrWorkflowConf"));
        QuickSearch = new TextBoxControl("Suchbegriff eingeben...", Locate.ById(Root, "qsFilterControlCtWorkflowConf", "txtQuickSearch"));
        OnlyMine = new CheckBoxControl("Nur meine", Locate.ById(Root, "chkFiltersAllMyCtrWorkflowConf"));
        OkButton = new ButtonControl("OK", FooterButton("OK"));
        CancelButton = new ButtonControl("Abbrechen", FooterButton("Abbrechen"));
        ApplyButton = new ButtonControl("Übernehmen", FooterButton("Übernehmen"));
        Preview = new WorkflowForm(() => Root.FindByAutomationId("ctrWorkflowWndConfiguration"), Root, isReadOnly: true);
        // Список завантажується у фоні: доки він не готовий, «Neu» недоступна.
        Wait.Until(() => NewButton.IsEnabled, $"завантаження списку «{Title}»");
    }

    public ButtonControl NewButton { get; }

    public ButtonControl EditButton { get; }

    public ButtonControl DeleteButton { get; }

    public TextBoxControl QuickSearch { get; }

    public CheckBoxControl OnlyMine { get; }

    /// <summary>Доступна, коли у вікні є незбережені зміни (або їх щойно «übernommen»).</summary>
    public ButtonControl OkButton { get; }

    public ButtonControl CancelButton { get; }

    /// <summary>«Übernehmen»: зберігає зміни в базу, вікно лишається відкритим. Доступна, коли є незбережені зміни.</summary>
    public ButtonControl ApplyButton { get; }

    /// <summary>Область перегляду вибраного шаблону під списком (лише для читання).</summary>
    public WorkflowForm Preview { get; }

    /// <summary>Чи видно область перегляду: ADAICA ховає її, коли у списку немає жодного шаблону.</summary>
    public bool IsPreviewShown => Root.FindByAutomationId("previewControl").IsOffscreen is false;

    public bool IsOpen => Find(_mainWindow) is not null;

    /// <summary>Підписи колонок зліва направо; у першої колонки (картинка) підпис — у підказці.</summary>
    public IReadOnlyList<string> ColumnNames =>
        List.FindByAutomationId("HeaderPanel").FindAllChildren(cf => cf.ByControlType(ControlType.HeaderItem))
            .Select(header => header.HelpText)
            .ToList();

    /// <summary>
    /// Шаблони, які зараз показує список, у його порядку. Список віртуалізований: рядків поза видимою областю тут немає.
    /// </summary>
    public IReadOnlyList<WorkflowTemplateRow> Templates =>
        Rows().Select(row => new WorkflowTemplateRow(
                Name: CellText(row, "Workflowname"),
                CreatedBy: CellText(row, "Anwender"),
                Created: AdaicaFormat.ParseDateTime(CellText(row, "Erstelldatum")) ?? DateTime.MinValue,
                ChangedBy: CellText(row, "Geändert von"),
                Changed: AdaicaFormat.ParseDateTime(CellText(row, "Änderungsdatum")) ?? DateTime.MinValue))
            .ToList();

    public IReadOnlyList<string> TemplateNames => Templates.Select(template => template.Name).ToList();

    /// <summary>Назва шаблону, виділеного у списку, або null.</summary>
    public string? SelectedTemplate =>
        Rows().FirstOrDefault(row => row.Patterns.SelectionItem.Pattern.IsSelected.Value) is { } row ? CellText(row, "Workflowname") : null;

    private Window Root { get; }

    private AutomationElement List => Root.FindByAutomationId(ListId);

    public WorkflowTemplateRow Template(string name) =>
        Templates.FirstOrDefault(template => template.Name == name)
            ?? throw new InvalidOperationException($"Шаблону «{name}» немає у списку «{Title}»: {string.Join(", ", TemplateNames)}.");

    public bool Contains(string name) => TemplateNames.Contains(name);

    /// <summary>Виділяє шаблон кліком по рядку; область перегляду показує його бланк.</summary>
    public void Select(string name) => AllureApi.Step($"Вибрати шаблон «{name}»", () =>
        UiInput.LeftClick(() => NameCell(name), () => SelectedTemplate == name, $"виділення шаблону «{name}»"));

    /// <summary>«Neu»: порожній шаблон з одним кроком.</summary>
    public WorkflowDialog OpenNew() => AllureApi.Step("Створити шаблон кнопкою «Neu»", () =>
    {
        NewButton.Click();
        return new WorkflowDialog(Root);
    });

    /// <summary>«Bearbeiten» для шаблону <paramref name="name"/>.</summary>
    public WorkflowDialog Edit(string name) => AllureApi.Step($"Відкрити шаблон «{name}» кнопкою «Bearbeiten»", () =>
    {
        Select(name);
        EditButton.Click();
        return new WorkflowDialog(Root);
    });

    /// <summary>Подвійний клік по рядку теж відкриває шаблон для змін.</summary>
    public WorkflowDialog EditByDoubleClick(string name) => AllureApi.Step($"Відкрити шаблон «{name}» подвійним кліком", () =>
    {
        Select(name);
        UiInput.DoubleClick(() => NameCell(name), () => WorkflowDialog.IsOpenIn(Root), $"вікна шаблону «{name}»");
        return new WorkflowDialog(Root);
    });

    /// <summary>«Löschen» для шаблону: ADAICA питає, чи видалити; питання повертаємо тесту.</summary>
    public MessageBoxDialog Delete(string name) => AllureApi.Step($"Видалити шаблон «{name}» кнопкою «Löschen»", () =>
    {
        Select(name);
        DeleteButton.Click();
        return Wait.For(() => MessageBoxDialog.Find(Root), "питання про видалення");
    });

    /// <summary>
    /// Для прибирання після тесту: видаляє шаблони, які є у списку («Löschen» → «Ja»), і зберігає видалення кнопкою OK;
    /// якщо видаляти нічого, закриває вікно без змін.
    /// </summary>
    public void DeleteIfPresentAndClose(params string[] names) => AllureApi.Step($"Видалити шаблони workflow: {string.Join(", ", names)}", () =>
    {
        var present = names.Where(Contains).ToList();
        foreach (var name in present)
        {
            Delete(name).Button("Ja").Click();
            Wait.Until(() => !Contains(name), $"зникнення шаблону «{name}» зі списку");
        }

        if (present.Count > 0)
            Save();
        else
            Cancel();
    });

    /// <summary>Пункти контекстного меню рядка шаблону. Меню після цього закривається.</summary>
    public IReadOnlyList<string> RowMenuItems(string name) => AllureApi.Step($"Прочитати контекстне меню шаблону «{name}»", () =>
    {
        UiInput.RightClick(() => NameCell(name), () => MenuItems().Any(), $"контекстного меню шаблону «{name}»");
        var items = MenuItems().Select(item => item.Name).ToList();
        UiInput.PressKey(() => Root, VirtualKeyShort.ESCAPE, () => !MenuItems().Any(), "закриття контекстного меню");
        return items;
    });

    /// <summary>
    /// Звужує список швидким пошуком і чекає, доки список покаже саме <paramref name="expectedNames"/>. Пошук
    /// застосовує лише клік по лупі: Toggle з UI Automation команду кнопки не виконує.
    /// </summary>
    public void Search(string text, IReadOnlyCollection<string> expectedNames) =>
        AllureApi.Step($"Знайти «{text}» швидким пошуком у «{Title}»", () =>
        {
            QuickSearch.Fill(text);
            UiInput.LeftClick(
                () => Root.FindByAutomationId("btnQuickSearchApplyFilter"),
                () => TemplateNames.ToHashSet().SetEquals(expectedNames),
                $"шаблонів [{string.Join(", ", expectedNames)}] у списку «{Title}»");
        });

    /// <summary>Скасовує швидкий пошук хрестиком у полі й чекає, доки список знову покаже <paramref name="expectedNames"/>.</summary>
    public void ClearSearch(IReadOnlyCollection<string> expectedNames) =>
        AllureApi.Step($"Скасувати швидкий пошук у «{Title}»", () =>
        {
            Root.FindByAutomationId("btnQuickSearchClear").Patterns.Invoke.Pattern.Invoke();
            Wait.Until(() => TemplateNames.ToHashSet().SetEquals(expectedNames), $"шаблонів [{string.Join(", ", expectedNames)}] у списку «{Title}»");
        });

    /// <summary>
    /// Вмикає чи вимикає «Nur meine» і чекає, доки список покаже саме <paramref name="expectedNames"/>. Прапорець ставимо
    /// кліком: Toggle з UI Automation змінює прапорець, але список не фільтрує (фільтр застосовує команда кліку).
    /// </summary>
    public void ShowOnlyMine(bool onlyMine, IReadOnlyCollection<string> expectedNames) =>
        AllureApi.Step($"{(onlyMine ? "Увімкнути" : "Вимкнути")} «Nur meine»", () =>
        {
            if (OnlyMine.IsChecked != onlyMine)
                UiInput.LeftClick(() => Root.FindByAutomationId("chkFiltersAllMyCtrWorkflowConf"), () => OnlyMine.IsChecked == onlyMine, "«Nur meine»");
            Wait.Until(() => TemplateNames.ToHashSet().SetEquals(expectedNames), $"шаблонів [{string.Join(", ", expectedNames)}] у списку «{Title}»");
        });

    /// <summary>OK: зберігає зміни в базу й закриває вікно.</summary>
    public void Save() => AllureApi.Step($"Зберегти зміни у «{Title}» кнопкою OK", () =>
    {
        OkButton.Click();
        Wait.Until(() => !IsOpen, $"закриття вікна «{Title}»");
    });

    /// <summary>«Übernehmen»: зберігає зміни в базу, вікно лишається відкритим.</summary>
    public void Apply() => AllureApi.Step($"Зберегти зміни у «{Title}» кнопкою «Übernehmen»", () =>
    {
        ApplyButton.Click();
        Wait.Until(() => !ApplyButton.IsEnabled, "збереження змін («Übernehmen» стає недоступною)");
    });

    /// <summary>Abbrechen: закриває вікно; зміни, яких не «übernommen», ADAICA відкочує.</summary>
    public void Cancel() => AllureApi.Step($"Закрити «{Title}» кнопкою «Abbrechen»", () =>
    {
        CancelButton.Click();
        Wait.Until(() => !IsOpen, $"закриття вікна «{Title}»");
    });

    /// <summary>Для прибирання після тесту: закриває вікно без збереження, лише якщо воно ще відкрите.</summary>
    public void CancelIfOpen()
    {
        if (!IsOpen)
            return;

        // Вікно шаблону модальне: поки воно відкрите, вікно списку не закривається.
        if (WorkflowDialog.IsOpenIn(Root))
            new WorkflowDialog(Root).Cancel();
        Cancel();
    }

    private AutomationElement[] Rows() =>
        List.FindByAutomationId("dataPresenter").FindAllChildren(cf => cf.ByControlType(ControlType.DataItem));

    private AutomationElement NameCell(string name) =>
        Wait.For(
            () => Rows().FirstOrDefault(row => CellText(row, "Workflowname") == name)?.FindFirstChild(cf => cf.ByAutomationId(ColumnIds["Workflowname"])),
            $"шаблону «{name}» у списку «{Title}»");

    // Кнопки внизу вікна без AutomationId — шукаємо їх за підписом серед дочірніх елементів вікна.
    private Func<AutomationElement> FooterButton(string caption) =>
        () => Wait.For(
            () => Root.FindFirstChild(cf => cf.ByControlType(ControlType.Button).And(cf.ByName(caption))),
            $"кнопки «{caption}» у вікні «{Title}»");

    // Меню ADAICA відкриваються у WPF Popup (див. Popups).
    private IReadOnlyList<AutomationElement> MenuItems() =>
        Popups.Of(Root).Concat(Popups.Of(_mainWindow))
            .SelectMany(popup => popup.FindAllDescendants(cf => cf.ByControlType(ControlType.MenuItem)))
            .ToList();

    // Показаний текст лежить у полі редагування комірки; у назві комірки дата — без часу.
    private static string CellText(AutomationElement row, string column)
    {
        if (row.FindFirstChild(cf => cf.ByAutomationId(ColumnIds[column])) is not { } cell)
            return string.Empty;

        return cell.FindFirstDescendant(cf => cf.ByControlType(ControlType.Edit)) is { } editor
            ? editor.Patterns.Value.Pattern.Value.Value
            : CellName().Replace(cell.Name, string.Empty);
    }

    private static Window? Find(Window mainWindow) =>
        mainWindow.FindFirstChild(cf => cf.ByAutomationId(WindowId))?.AsWindow();

    // Комірка підписана «значення, Eintrag: 0, angezeigter Spaltenindex: 1».
    [GeneratedRegex(@", Eintrag: \d+, angezeigter Spaltenindex: \d+$")]
    private static partial Regex CellName();
}
