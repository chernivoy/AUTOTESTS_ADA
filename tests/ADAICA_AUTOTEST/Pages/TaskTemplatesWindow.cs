using ADAICA_AUTOTEST.Controls;
using ADAICA_AUTOTEST.Infrastructure;
using Allure.Net.Commons;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Conditions;
using FlaUI.Core.Definitions;
using FlaUI.Core.WindowsAPI;

namespace ADAICA_AUTOTEST.Pages;

/// <summary>
/// Вікно «Aufgabenvorlagen» — шаблони завдань, спільні для всіх користувачів: кнопки «Neu», «Ansehen», «Bearbeiten»,
/// «Löschen» (картинки без підпису), швидкий пошук, список (спершу обрані користувача, далі за назвою), «Verwenden» і
/// «Schließen». Відкривається з «Vorlage erstellen/bearbeiten» у меню нового завдання (навігатор, планувальник) і з
/// «Aufgabenvorlage auswählen» у вікні нового завдання; «Verwenden» переносить поля вибраного шаблону в нове завдання.
/// У вікна немає AutomationId, тож шукаємо його за заголовком серед дочірніх вікон того, з якого його відкрито.
/// </summary>
public sealed class TaskTemplatesWindow
{
    public const string Title = "Aufgabenvorlagen";

    private const string ListId = "lstTemplates";

    // Підказка зірки в першій колонці: «Favorit» або «Gehört nicht zu Favoriten».
    private const string FavoriteHint = "Favorit";

    private readonly Window _owner;

    /// <param name="owner">Головне вікно ADAICA або вікно нового завдання.</param>
    public TaskTemplatesWindow(Window owner)
    {
        _owner = owner;
        Root = Wait.For(() => Find(owner), $"вікна «{Title}»");
        NewButton = new ButtonControl("Neu", Locate.ById(Root, "btnNew"));
        ViewButton = new ButtonControl("Ansehen", Locate.ById(Root, "btnView"));
        EditButton = new ButtonControl("Bearbeiten", Locate.ById(Root, "btnEdit"));
        DeleteButton = new ButtonControl("Löschen", Locate.ById(Root, "btnDelete"));
        UseButton = new ButtonControl("Verwenden", Locate.ById(Root, "btnSelect"));
        CloseButton = new ButtonControl("Schließen", Locate.ById(Root, "btnClose"));
        QuickSearch = new TextBoxControl("Suchbegriff eingeben...", Locate.ById(Root, "searchFilterControl", "txtQuickSearch"));
    }

    public ButtonControl NewButton { get; }

    public ButtonControl ViewButton { get; }

    public ButtonControl EditButton { get; }

    public ButtonControl DeleteButton { get; }

    public ButtonControl UseButton { get; }

    public ButtonControl CloseButton { get; }

    public TextBoxControl QuickSearch { get; }

    /// <summary>Підписи колонок списку шаблонів.</summary>
    public IReadOnlyList<string> ColumnNames =>
        Root.FindByAutomationId(ListId)
            .FindAllDescendants(cf => cf.ByControlType(ControlType.HeaderItem))
            .Select(header => header.FindFirstChild(cf => cf.ByAutomationId("PART_Content"))?.Name)
            .OfType<string>()
            .ToList();

    /// <summary>Шаблони в порядку списку.</summary>
    public IReadOnlyList<TaskTemplateRow> Templates
    {
        get
        {
            var list = Root.FindByAutomationId(ListId);
            // Колонки можна переставити, тож комірки беремо за заголовками; у колонки зірки заголовка-тексту немає.
            var columns = list.FindFirstChild(cf => cf.ByControlType(ControlType.Header))!
                .FindAllChildren(cf => cf.ByControlType(ControlType.HeaderItem))
                .Select(header => header.FindFirstChild(cf => cf.ByAutomationId("PART_Content"))?.Name)
                .ToList();
            return list.FindAllChildren(cf => cf.ByControlType(ControlType.DataItem))
                .Select(row => ReadRow(row, columns))
                .ToList();
        }
    }

    /// <summary>Назви шаблонів у порядку списку.</summary>
    public IReadOnlyList<string> TemplateNames => Templates.Select(template => template.Name).ToList();

    public bool IsOpen => Find(_owner) is not null;

    private Window Root { get; }

    public TaskTemplateRow Template(string name) =>
        Templates.FirstOrDefault(template => template.Name == name)
            ?? throw new InvalidOperationException($"Шаблону «{name}» немає у вікні «{Title}».");

    public bool Contains(string name) => TemplateNames.Contains(name);

    /// <summary>Виділяє рядок шаблону кліком: стають доступні «Ansehen», «Bearbeiten», «Löschen» і «Verwenden».</summary>
    public void Select(string name) => AllureApi.Step($"Вибрати шаблон «{name}»", () =>
        UiInput.LeftClick(() => Row(name), () => IsSelected(name), $"виділення шаблону «{name}»"));

    /// <summary>
    /// Вибирає шаблон і натискає «Verwenden». Вікно закривається, а поля шаблону ADAICA переносить у вікно нового
    /// завдання: те, з якого відкрито вибір, або нове (з меню навігатора).
    /// </summary>
    public void Use(string name) => AllureApi.Step($"Використати шаблон «{name}»: рядок → Verwenden", () =>
    {
        Select(name);
        UseButton.Click();
        Wait.Until(() => !IsOpen, $"закриття вікна «{Title}»");
    });

    /// <summary>«Neu»: порожній шаблон («Neue Aufgabenvorlage»).</summary>
    public TaskTemplateDialog OpenNew() => AllureApi.Step("Створити шаблон кнопкою «Neu»", () =>
    {
        NewButton.Click();
        return new TaskTemplateDialog(Root);
    });

    /// <summary>«Ansehen»: шаблон лише для читання, OK недоступна.</summary>
    public TaskTemplateDialog View(string name) => AllureApi.Step($"Відкрити шаблон «{name}» кнопкою «Ansehen»", () =>
    {
        Select(name);
        ViewButton.Click();
        return new TaskTemplateDialog(Root);
    });

    /// <summary>«Bearbeiten»: шаблон для змін; OK стає доступною, коли щось змінено.</summary>
    public TaskTemplateDialog Edit(string name) => AllureApi.Step($"Відкрити шаблон «{name}» кнопкою «Bearbeiten»", () =>
    {
        Select(name);
        EditButton.Click();
        return new TaskTemplateDialog(Root);
    });

    /// <summary>Меню рядка → «Duplizieren»: «Neue Aufgabenvorlage» з полями шаблону й порожнім «Name der Vorlage».</summary>
    public TaskTemplateDialog Duplicate(string name) => AllureApi.Step($"Дублювати шаблон «{name}»: меню рядка → Duplizieren", () =>
    {
        ChooseInRowMenu(name, "mnuDuplicate", "Duplizieren");
        return new TaskTemplateDialog(Root);
    });

    /// <summary>Меню рядка → «Umbenennen»: вікно «Vorlage umbenennen».</summary>
    public TemplateRenameDialog Rename(string name) => AllureApi.Step($"Перейменувати шаблон «{name}»: меню рядка → Umbenennen", () =>
    {
        ChooseInRowMenu(name, "mnuRename", "Umbenennen");
        return new TemplateRenameDialog(Root);
    });

    /// <summary>Меню рядка → «Zu Favoriten hinzufügen»: шаблон іде вгору списку й меню «Neu», у колонці — зірка.</summary>
    public void AddToFavorites(string name) => AllureApi.Step($"Додати шаблон «{name}» в обране: меню рядка → Zu Favoriten hinzufügen", () =>
    {
        ChooseInRowMenu(name, "mnuAddToFavorites", "Zu Favoriten hinzufügen");
        Wait.Until(() => Template(name).IsFavorite, $"зірки в рядку шаблону «{name}»");
    });

    public void RemoveFromFavorites(string name) => AllureApi.Step($"Прибрати шаблон «{name}» з обраного: меню рядка → Aus Favoriten entfernen", () =>
    {
        ChooseInRowMenu(name, "mnuRemoveFromFavorites", "Aus Favoriten entfernen");
        Wait.Until(() => !Template(name).IsFavorite, $"рядка шаблону «{name}» без зірки");
    });

    /// <summary>Видимі пункти контекстного меню рядка (роздільники пропущено). Меню після цього закривається.</summary>
    public IReadOnlyList<string> RowMenuItems(string name) => AllureApi.Step($"Прочитати контекстне меню шаблону «{name}»", () =>
    {
        OpenRowMenu(name);
        var items = FindInPopups(cf => cf.ByControlType(ControlType.Menu))!
            .FindAllDescendants(cf => cf.ByControlType(ControlType.MenuItem))
            .Where(item => !item.BoundingRectangle.IsEmpty)
            .Select(item => item.Name)
            .ToList();
        UiInput.PressKey(() => Root, VirtualKeyShort.ESCAPE, () => FindInPopups(cf => cf.ByControlType(ControlType.Menu)) is null, "закриття контекстного меню");
        return items;
    });

    /// <summary>«Löschen» для шаблону: ADAICA питає «Soll die ausgewählte Vorlage gelöscht werden?»; питання повертаємо тесту.</summary>
    public MessageBoxDialog Delete(string name) => AllureApi.Step($"Видалити шаблон «{name}» кнопкою «Löschen»", () =>
    {
        Select(name);
        DeleteButton.Click();
        return Wait.For(() => MessageBoxDialog.Find(Root), "питання «Löschen»");
    });

    /// <summary>
    /// Видаляє шаблони, які є в списку («Löschen» → «Ja»), спершу прибравши їх з обраного користувача. У Papierkorb
    /// видалений шаблон не потрапляє.
    /// </summary>
    public void DeleteIfPresent(params string[] names)
    {
        foreach (var name in names.Where(Contains))
        {
            AllureApi.Step($"Видалити шаблон «{name}»", () =>
            {
                if (Template(name).IsFavorite)
                    RemoveFromFavorites(name);
                Delete(name).Button("Ja").Click();
                Wait.Until(() => !Contains(name), $"зникнення шаблону «{name}» зі списку");
            });
        }
    }

    /// <summary>
    /// Звужує список швидким пошуком (шукає в назві, «Erstellt durch» і «Geändert durch») і чекає, доки список покаже
    /// саме <paramref name="expectedNames"/>. Пошук застосовує лише клік по лупі або Enter: Toggle з UI Automation команду
    /// кнопки не виконує.
    /// </summary>
    public void Search(string text, IReadOnlyCollection<string> expectedNames) =>
        AllureApi.Step($"Знайти «{text}» швидким пошуком у «{Title}»", () =>
        {
            QuickSearch.Fill(text);
            UiInput.LeftClick(
                () => Root.FindByAutomationId("btnQuickSearchApplyFilter"),
                () => TemplateNames.ToHashSet().SetEquals(expectedNames),
                $"шаблонів {string.Join(", ", expectedNames)} у списку «{Title}»");
        });

    /// <summary>Скасовує швидкий пошук хрестиком у полі й чекає, доки список знову покаже <paramref name="expectedNames"/>.</summary>
    public void ClearSearch(IReadOnlyCollection<string> expectedNames) =>
        AllureApi.Step($"Скасувати швидкий пошук у «{Title}»", () =>
        {
            Root.FindByAutomationId("btnQuickSearchClear").Patterns.Invoke.Pattern.Invoke();
            Wait.Until(() => TemplateNames.ToHashSet().SetEquals(expectedNames), $"усіх шаблонів у списку «{Title}»");
        });

    public void Close() => AllureApi.Step($"Закрити вікно «{Title}» кнопкою «Schließen»", () =>
    {
        CloseButton.Click();
        Wait.Until(() => !IsOpen, $"закриття вікна «{Title}»");
    });

    /// <summary>Для прибирання після тесту: закриває вікно, лише якщо воно ще відкрите.</summary>
    public void CloseIfOpen()
    {
        if (IsOpen)
            Close();
    }

    private static Window? Find(Window owner) =>
        owner.FindFirstChild(cf => cf.ByControlType(ControlType.Window).And(cf.ByName(Title)))?.AsWindow();

    private AutomationElement Row(string name) =>
        Wait.For(
            () => Root.FindByAutomationId(ListId).FindAllChildren(cf => cf.ByControlType(ControlType.DataItem))
                .FirstOrDefault(row => row.FindFirstDescendant(cf => cf.ByAutomationId("TextElement").And(cf.ByName(name))) is not null),
            $"шаблону «{name}» у списку «{Title}»");

    private bool IsSelected(string name) => Row(name).Patterns.SelectionItem.Pattern.IsSelected.Value;

    private static TaskTemplateRow ReadRow(AutomationElement row, IReadOnlyList<string?> columns)
    {
        var cells = row.FindAllChildren();
        string Cell(string column) => cells[columns.ToList().IndexOf(column)].FindFirstChild()?.Name ?? string.Empty;
        DateTime Date(string column) =>
            AdaicaFormat.ParseDateTime(Cell(column)) ?? throw new FormatException($"«{column}» шаблону — не дата: «{Cell(column)}».");

        return new TaskTemplateRow(
            Name: Cell("Name"),
            Created: Date("Erstelldatum"),
            CreatedBy: Cell("Erstellt durch"),
            Changed: Date("Änderungsdatum"),
            ChangedBy: Cell("Geändert durch"),
            IsFavorite: cells[columns.ToList().IndexOf(null)].FindFirstChild()?.HelpText == FavoriteHint);
    }

    // Контекстне меню рядка відкривається лише правим кліком; правий клік і виділяє рядок.
    private void OpenRowMenu(string name) =>
        UiInput.RightClick(
            () => Row(name),
            () => FindInPopups(cf => cf.ByControlType(ControlType.Menu)) is not null,
            $"контекстного меню шаблону «{name}»");

    private void ChooseInRowMenu(string name, string itemId, string itemName)
    {
        OpenRowMenu(name);
        new MenuItemControl(itemName, () => Wait.For(() => FindInPopups(cf => cf.ByAutomationId(itemId)), $"пункту «{itemName}» у контекстному меню")).Click();
    }

    // Меню — WPF Popup вікна «Aufgabenvorlagen» або того, з якого його відкрито (див. Popups).
    private AutomationElement? FindInPopups(Func<ConditionFactory, ConditionBase> condition) =>
        Popups.FindDescendant(Root, condition) ?? Popups.FindDescendant(_owner, condition);
}
