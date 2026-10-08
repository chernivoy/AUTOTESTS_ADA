using ADAICA_AUTOTEST.Controls;
using ADAICA_AUTOTEST.Infrastructure;
using Allure.Net.Commons;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;

namespace ADAICA_AUTOTEST.Pages;

/// <summary>
/// Вікно «Konfiguration der Vorgabewerte für Kontakte (Personen &amp; Firmen)» (`wndPACReferenceData`): довідники
/// контактів на вкладках «Art der Kommunikationsnummer», «Person», «Firma», «Organisationseinheit». Відкриває його
/// кнопка «Konfiguration der Liste von Kommunikationsarten» у картці фірми. На вкладці «Firma» ліворуч дерево довідників
/// (Adresskategorie, Kategorie, Firmentyp, Mitarbeiterfunktion, …), праворуч — пункти вибраного. Новий пункт додає
/// контекстне меню рядка (на порожньому місці таблиці меню немає) → «Hinzufügen» → <see cref="ElementCardDialog"/>.
/// Зміни зберігає лише OK вікна; після нього ADAICA пише, що вони діють після перезапуску, але нові функції вікна
/// співробітника показують одразу.
/// </summary>
public sealed class ContactDefaultsWindow
{
    private const string WindowId = "wndPACReferenceData";
    private const string RestartMessage = "Änderungen werden erst nach dem Neustart der Anwendung wirksam";

    // UIA_ScrollPatternNoScroll: не змінювати прокрутку по цій осі.
    private const double NoScroll = -1;

    private readonly Window _mainWindow;

    public ContactDefaultsWindow(Window mainWindow)
    {
        _mainWindow = mainWindow;
        Root = Wait.For(() => Find(mainWindow), "вікна «Konfiguration der Vorgabewerte für Kontakte»");
        OkButton = new ButtonControl("OK", Locate.ById(Root, "btnOkWndPACReferenceData"));
        CancelButton = new ButtonControl("Abbrechen", Locate.ById(Root, "btnCancelWndPACReferenceData"));
    }

    public ButtonControl OkButton { get; }

    public ButtonControl CancelButton { get; }

    public bool IsOpen => Find(_mainWindow) is not null;

    private Window Root { get; }

    private AutomationElement CompanyTab => Root.FindByAutomationId("tiCompanyManagerWndConfiguration");

    // Вміст невибраної вкладки WPF у дерево UI Automation не показує, тож таблиця тут — лише вкладки «Firma».
    private AutomationElement Items => Root.FindByAutomationId("ctrCompanyManager").FindByAutomationId("dgData");

    /// <summary>
    /// Пункти довідника, вибраного на вкладці «Firma», у порядку показу. Таблиця віртуалізована: рядків поза видимою
    /// областю в UI Automation немає, тож гортаємо її від початку до кінця.
    /// </summary>
    public IReadOnlyList<string> CompanyItems
    {
        get
        {
            var items = Items;
            var names = new List<string>();
            void Collect() =>
                names.AddRange(items.FindAllChildren(cf => cf.ByControlType(ControlType.DataItem))
                    .Select(row => row.FindFirstChild()?.Patterns.Value.PatternOrDefault?.Value.ValueOrDefault ?? string.Empty)
                    .Where(name => !names.Contains(name))
                    .ToList());

            var scroll = items.Patterns.Scroll.PatternOrDefault;
            if (scroll?.VerticallyScrollable.Value != true)
            {
                Collect();
                return names;
            }

            scroll.SetScrollPercent(NoScroll, 0);
            while (true)
            {
                Collect();
                var before = scroll.VerticalScrollPercent.Value;
                scroll.Scroll(ScrollAmount.NoAmount, ScrollAmount.LargeIncrement);
                if (scroll.VerticalScrollPercent.Value <= before)
                    return names;
            }
        }
    }

    /// <summary>
    /// Вкладка «Firma» → довідник <paramref name="node"/>. Вузол реагує лише на клік; вкладку пробуємо вибрати через
    /// UI Automation, а якщо вона не вибралася, — кліком.
    /// </summary>
    public void OpenCompanyList(string node) => AllureApi.Step($"Відкрити довідник «Firma → {node}»", () =>
    {
        bool IsSelected(AutomationElement item) => item.Patterns.SelectionItem.Pattern.IsSelected.Value;

        CompanyTab.Patterns.SelectionItem.Pattern.Select();
        if (!FlaUI.Core.Tools.Retry.WhileFalse(() => IsSelected(CompanyTab), TimeSpan.FromSeconds(2), ignoreException: true).Success)
            UiInput.LeftClick(() => CompanyTab, () => IsSelected(CompanyTab), "вибору вкладки «Firma»");

        AutomationElement Node() => Wait.For(
            () => Root.FindByAutomationId("ctrCompanyManager").FindByAutomationId("treeView")
                .FindAllChildren(cf => cf.ByControlType(ControlType.ListItem))
                .FirstOrDefault(item => item.FindFirstChild(cf => cf.ByAutomationId("TextBlock").And(cf.ByName(node))) is not null),
            $"вузла «{node}»");
        UiInput.LeftClick(
            () => Node().FindFirstChild(cf => cf.ByAutomationId("TextBlock")) ?? Node(),
            () => IsSelected(Node()),
            $"вибору вузла «{node}»");
    });

    /// <summary>Додає пункт у відкритий довідник вкладки «Firma»: меню рядка → «Hinzufügen» → назва → OK.</summary>
    public void AddCompanyItem(string name) => AllureApi.Step($"Додати пункт «{name}»", () =>
    {
        // Меню рядка — дочірнє вікно цього вікна, а коли ADAICA неактивна, — окремий Popup робочого столу.
        AutomationElement? AddItem() =>
            Root.FindFirstChild(cf => cf.ByControlType(ControlType.Window))?.FindFirstDescendant(cf => cf.ByAutomationId("mnu_Add"))
            ?? Popups.FindDescendant(Root, cf => cf.ByAutomationId("mnu_Add"))
            ?? Popups.FindDescendant(_mainWindow, cf => cf.ByAutomationId("mnu_Add"));

        UiInput.RightClick(
            // Клікаємо по комірці рядка, видимого у вікні: таблиця прокручується до нового пункту, і перші рядки ховаються.
            () => Wait.For(
                () => Items.FindAllChildren(cf => cf.ByControlType(ControlType.DataItem))
                    .Select(row => row.FindFirstChild())
                    .LastOrDefault(cell => cell is not null && !cell.IsOffscreen),
                "видимого рядка довідника"),
            () => AddItem() is not null,
            "контекстного меню рядка довідника");
        new MenuItemControl("Hinzufügen", () => Wait.For(AddItem, "пункту «Hinzufügen»")).Click();

        var card = new ElementCardDialog(_mainWindow);
        card.Name.Fill(name);
        card.Confirm();
        Wait.Until(() => CompanyItems.Contains(name), $"пункту «{name}» у довіднику");
    });

    /// <summary>OK: зберігає зміни й підтверджує повідомлення про перезапуск.</summary>
    public void Save() => AllureApi.Step("Зберегти довідники кнопкою OK", () =>
    {
        OkButton.Click();

        string? error = null;
        Wait.Until(() =>
        {
            if (FindMessage() is { } message)
            {
                if (message.Message.Text != RestartMessage)
                {
                    error = $"{message.Title}: {message.Message.Text}";
                    return true;
                }

                message.Button("OK").Click();
            }

            return !IsOpen && FindMessage() is null;
        }, "збереження довідників і закриття вікна", TimeSpan.FromSeconds(30));

        if (error is not null)
            throw new InvalidOperationException($"Довідники не збережено, ADAICA показала діалог «{error}».");
    });

    /// <summary>Для прибирання після тесту й коли нічого не змінено: закриває вікно без збереження.</summary>
    public void CancelIfOpen()
    {
        if (!IsOpen)
            return;

        AllureApi.Step("Закрити вікно довідників без змін", () =>
        {
            CancelButton.Click();
            Wait.Until(() => !IsOpen, "закриття вікна довідників");
        });
    }

    private MessageBoxDialog? FindMessage() => MessageBoxDialog.Find(_mainWindow) ?? (IsOpen ? MessageBoxDialog.Find(Root) : null);

    private static Window? Find(Window mainWindow) =>
        mainWindow.FindFirstChild(cf => cf.ByAutomationId(WindowId))?.AsWindow();
}
