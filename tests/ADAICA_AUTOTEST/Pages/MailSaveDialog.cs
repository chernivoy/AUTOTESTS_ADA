using System.Drawing;
using System.Runtime.InteropServices;
using ADAICA_AUTOTEST.Controls;
using ADAICA_AUTOTEST.Infrastructure;
using Allure.Net.Commons;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using VirtualKeyShort = FlaUI.Core.WindowsAPI.VirtualKeyShort;

namespace ADAICA_AUTOTEST.Pages;

/// <summary>
/// «E-Mail-Speicherndialog» — асистент збереження листа в ADAICA після надсилання з Outlook. Угорі — листи, що
/// зберігаються (`lvMail`), унизу — вкладки зв'язків вибраного листа: «Kontakte» (відправник «Von», отримувачі «An»,
/// «CC», додані вручну «Kontakt»), «Dokumentation» (записи, під якими буде лист; спершу — вузол, з якого його
/// створено, «ROOT EMAIL») і «Objekte». Поки хоч один отримувач не зв'язаний з контактом, OK недоступна, а внизу —
/// «Gemäß der Konfiguration müssen eine oder mehrere Verknüpfungen erfolgen».
/// </summary>
/// <remarks>
/// <para>
/// Вміст вкладок зв'язків (списки, рядок пошуку, кнопки над списком) UI Automation не показує навіть у raw-дереві
/// і через FromPoint: TabControlEx ADAICA тримає вміст вкладок у шаблоні поза TabItem. Тому рядки й кнопки тест
/// знаходить за координатами в межах «recordLinkView» — розмітка в незалежних від DPI одиницях WPF, помножених на
/// масштаб вікна, — а стан контакту бачить зі складу його контекстного меню (меню UI Automation бачить): ADAICA
/// показує в ньому лише доступні дії (<see cref="MailContactState"/>).
/// </para>
/// <para>
/// Прапорець «Keine Kontaktzuordnung» виконує ту саму команду, що й кнопка AUTO, а ADAICA після збереження
/// запам'ятовує його для користувача: наступні листи відкриваються вже з ним. «Abbrechen» нічого не запам'ятовує.
/// </para>
/// </remarks>
public sealed class MailSaveDialog
{
    private const string WindowTitle = "E-Mail-Speicherndialog";
    private const string RequiredLinksMessage = "Gemäß der Konfiguration müssen eine oder mehrere Verknüpfungen erfolgen";
    private const string FindCreateContactItem = "Zugehörigen Kontakt finden/erstellen";

    // Розмітка вкладок зв'язків в одиницях WPF (1/96 дюйма) від кута «recordLinkView»: середина першого рядка списку
    // й висота рядка, середина колонки «Funktion» (у «Dokumentation» — «Name»), середина рядка кнопок над списком.
    private const double FirstRowCenter = 64.5;
    private const double RowHeight = 20;
    private const double FirstColumnCenter = 60;
    private const double ButtonsCenter = 16.5;

    // Кнопки над списком — квадрати по 24 одиниці, праворуч; відстань від правого краю до середини кнопки.
    private const double LastButtonFromRight = 15;
    private const double ButtonStep = 25;

    private const int StateReadAttempts = 3;
    private static readonly TimeSpan StateReadInterval = TimeSpan.FromSeconds(1);

    // Після OK ADAICA зберігає лист і його вкладення.
    private static readonly TimeSpan SaveTimeout = TimeSpan.FromSeconds(60);

    private readonly Window _mainWindow;

    public MailSaveDialog(Window mainWindow)
    {
        _mainWindow = mainWindow;
        Root = Wait.For(
            () => mainWindow.FindAllChildren(cf => cf.ByControlType(ControlType.Window))
                .FirstOrDefault(window => window.FindFirstDescendant(cf => cf.ByAutomationId("lvMail")) is not null)?.AsWindow(),
            $"вікна «{WindowTitle}»");

        OkButton = new ButtonControl("OK", () => Root.FindFirstChild(cf => cf.ByControlType(ControlType.Button).And(cf.ByName("OK"))));
        CancelButton = new ButtonControl("Abbrechen", () => Root.FindFirstChild(cf => cf.ByControlType(ControlType.Button).And(cf.ByName("Abbrechen"))));
        NoContactAssignment = new CheckBoxControl(
            "Keine Kontaktzuordnung",
            () => Root.FindFirstChild(cf => cf.ByControlType(ControlType.CheckBox).And(cf.ByName("Keine Kontaktzuordnung"))));
    }

    public ButtonControl OkButton { get; }

    public ButtonControl CancelButton { get; }

    /// <summary>
    /// «Keine Kontaktzuordnung» («Nur zu den bekannten Kontakten speichern»). Лише для читання: Toggle з UI Automation
    /// змінює галочку, але не виконує команду ADAICA. Увімкнути — <see cref="AssignNoContacts"/>.
    /// </summary>
    public CheckBoxControl NoContactAssignment { get; }

    /// <summary>Теми листів у списку «Betreff».</summary>
    public IReadOnlyList<string> Subjects =>
        Root.FindByAutomationId("lvMail")
            .FindAllChildren(cf => cf.ByControlType(ControlType.DataItem))
            .Select(row => row.FindFirstDescendant(cf => cf.ByAutomationId("TextElement"))?.Name ?? string.Empty)
            .ToList();

    /// <summary>Чи показує вікно «Gemäß der Konfiguration müssen eine oder mehrere Verknüpfungen erfolgen».</summary>
    public bool RequiredLinksMissing =>
        Root.FindFirstChild(cf => cf.ByControlType(ControlType.Text).And(cf.ByName(RequiredLinksMessage))) is not null;

    // IsAvailable для закритого вікна WPF лишається true, тому перевіряємо, чи вікно ще серед дочірніх.
    public bool IsOpen => _mainWindow.FindAllChildren(cf => cf.ByControlType(ControlType.Window)).Contains(Root);

    private Window Root { get; }

    private AutomationElement Links => Root.FindByAutomationId("recordLinkView");

    public void ShouldHaveSubjects(params string[] expected) =>
        AllureApi.Step($"Перевірити листи у вікні «{WindowTitle}»: {string.Join("; ", expected)}",
            () => Subjects.ShouldBe(expected, $"Список «Betreff» у вікні «{WindowTitle}»"));

    /// <summary>
    /// Перевіряє, чи можна зберегти лист: OK доступна і внизу немає «Gemäß der Konfiguration…», або навпаки.
    /// </summary>
    public void ShouldBeSavable(bool expected) =>
        AllureApi.Step(expected
                ? "Перевірити, що лист можна зберегти: OK доступна, зв'язків вистачає"
                : "Перевірити, що лист не зберегти: OK недоступна, «Gemäß der Konfiguration müssen eine oder mehrere Verknüpfungen erfolgen»",
            () => this.ShouldSatisfyAllConditions(
                () => Wait.Until(() => OkButton.IsEnabled == expected, $"стану кнопки OK: {(expected ? "доступна" : "недоступна")}"),
                () => Wait.Until(() => RequiredLinksMissing != expected, $"повідомлення «{RequiredLinksMessage}»: {(expected ? "немає" : "є")}")));

    /// <summary>
    /// Контекстне меню рядка <paramref name="contactRow"/> (з нуля: «Von», далі «An», «CC», додані контакти) на вкладці
    /// «Kontakte»: пункти з доступністю. Меню закривається клавішею Esc у самому меню — Esc у вікні закрив би вікно.
    /// </summary>
    public IReadOnlyList<MenuEntry> ContactMenu(int contactRow) =>
        AllureApi.Step($"Прочитати контекстне меню рядка {contactRow + 1} на вкладці «Kontakte»", () =>
        {
            SelectTab("Kontakte");
            var menu = OpenRowMenu(contactRow, "ContactMenu");
            var entries = menu.FindAllChildren(cf => cf.ByControlType(ControlType.MenuItem))
                .Select(item => new MenuEntry(item.Name, item.IsEnabled))
                .ToList();
            CloseMenu("ContactMenu");
            AllureApi.Step($"Пункти меню: {string.Join("; ", entries)}");
            return (IReadOnlyList<MenuEntry>)entries;
        });

    /// <summary>
    /// Чи є на вкладці «Kontakte» рядок <paramref name="contactRow"/>: правий клік по рядку відкриває меню рядка
    /// («ContactMenu»), а по порожньому місцю під рядками — меню списку («ContactsListContextMenu»).
    /// </summary>
    public bool HasContactRow(int contactRow) =>
        AllureApi.Step($"Перевірити, чи є рядок {contactRow + 1} на вкладці «Kontakte»", () =>
        {
            SelectTab("Kontakte");
            UiInput.RightClick(
                () => Links,
                () => FindPopup("ContactMenu") is not null || FindPopup("ContactsListContextMenu") is not null,
                $"контекстного меню на місці рядка {contactRow + 1}",
                offset: Row(contactRow));
            var exists = FindPopup("ContactMenu") is not null;
            CloseMenu(exists ? "ContactMenu" : "ContactsListContextMenu");
            return exists;
        });

    /// <summary>Перевіряє, скільки рядків на вкладці «Kontakte»: відправник, отримувачі, копії й додаткові контакти.</summary>
    public void ShouldHaveContactRows(int expected) =>
        AllureApi.Step($"Перевірити, що на вкладці «Kontakte» {expected} рядки", () =>
            this.ShouldSatisfyAllConditions(
                () => HasContactRow(expected - 1).ShouldBeTrue($"Рядка {expected} на вкладці «Kontakte» немає"),
                () => HasContactRow(expected).ShouldBeFalse($"На вкладці «Kontakte» є зайвий рядок {expected + 1}")));

    /// <summary>
    /// Перевіряє стан контакту в рядку <paramref name="contactRow"/> за його контекстним меню: ADAICA показує в ньому
    /// лише ті дії, які в цьому стані можливі (<see cref="MailContactState"/>).
    /// </summary>
    public void ShouldHaveContactState(int contactRow, MailContactState expected) =>
        AllureApi.Step($"Перевірити стан контакту в рядку {contactRow + 1}: {expected}", () =>
        {
            // Після дії ADAICA оновлює стан не миттєво, тож меню читаємо кілька разів.
            var expectedMenu = MailContactStates.Menu(expected);
            var menu = ContactMenu(contactRow);
            for (var attempt = 1; attempt < StateReadAttempts && !menu.SequenceEqual(expectedMenu); attempt++)
            {
                Thread.Sleep(StateReadInterval);
                menu = ContactMenu(contactRow);
            }

            menu.ShouldBe(expectedMenu, $"Контекстне меню рядка {contactRow + 1} на вкладці «Kontakte»");
        });

    /// <summary>Пункт <paramref name="item"/> контекстного меню рядка <paramref name="contactRow"/> на вкладці «Kontakte».</summary>
    public void ChooseInContactMenu(int contactRow, string item) =>
        AllureApi.Step($"Рядок {contactRow + 1} на вкладці «Kontakte» → контекстне меню → «{item}»", () =>
        {
            SelectTab("Kontakte");
            var menu = OpenRowMenu(contactRow, "ContactMenu");
            new MenuItemControl(item, () => Wait.For(
                () => menu.FindFirstChild(cf => cf.ByControlType(ControlType.MenuItem).And(cf.ByName(item))),
                $"пункту «{item}»")).Click();
        });

    /// <summary>
    /// «Zugehörigen Kontakt finden/erstellen» у меню рядка <paramref name="contactRow"/>. ADAICA відкриває пошук
    /// контакту «Finden &amp; Erstellen» як дочірнє вікно цього вікна. Якщо адреса має вигляд «vorname.nachname@…»,
    /// ADAICA сама вписує в рядок пошуку «Vorname Nachname»; e-mail не вписує.
    /// </summary>
    public ContactFindCreateWindow OpenFindCreateContact(int contactRow)
    {
        ChooseInContactMenu(contactRow, FindCreateContactItem);
        return new ContactFindCreateWindow(Root);
    }

    /// <summary>Кнопка «CC» над списком контактів: ігнорувати всіх отримувачів копії.</summary>
    public void IgnoreCopyRecipients() => AllureApi.Step("Натиснути «CC» на вкладці «Kontakte» (ігнорувати отримувачів копії)", () =>
    {
        SelectTab("Kontakte");
        // Що кнопка подіяла, видно лише з меню рядків, тож тест перевіряє стан рядків окремо.
        UiInput.LeftClick(() => Links, () => true, "кнопки «CC»", offset: Button(fromRight: 3));
    });

    /// <summary>
    /// Кнопка AUTO над списком контактів: усі нерозпізнані адреси — без зв'язку з контактом («Keine Kontaktzuordnung»).
    /// Те саме робить прапорець «Keine Kontaktzuordnung» унизу, і він після цього ввімкнений.
    /// </summary>
    public void AssignNoContacts() => AllureApi.Step("Натиснути AUTO на вкладці «Kontakte» («Keine Kontaktzuordnung» для всіх нерозпізнаних)", () =>
    {
        SelectTab("Kontakte");
        UiInput.LeftClick(() => Links, () => NoContactAssignment.IsChecked, "прапорця «Keine Kontaktzuordnung» після AUTO", offset: Button(fromRight: 2));
    });

    /// <summary>
    /// Лупа на вкладці «Dokumentation»: пошук запису, під яким буде лист (окрім вузла, з якого його створено).
    /// Вибраний запис з'являється в списку з позначкою «Verknüpft».
    /// </summary>
    public void LinkRecord(string words, string number) => AllureApi.Step($"Зв'язати лист із записом {number}: «Dokumentation» → лупа", () =>
    {
        SelectTab("Dokumentation");
        UiInput.LeftClick(
            () => Links,
            () => Root.FindFirstDescendant(cf => cf.ByAutomationId("tcSearchParts")) is not null,
            "вікна «Finden» з вкладки «Dokumentation»",
            offset: Button(fromRight: 0));
        new SearchWindow(Root).Choose(words, number);
    });

    /// <summary>
    /// «+» на вкладці «Dokumentation» → «Notiz»: новий інфозапис, з яким буде зв'язано лист. Вікно «Neue Notiz» —
    /// дочірнє вікно цього вікна; після OK інфозапис з'являється в списку з позначкою «Verknüpft».
    /// </summary>
    public NoteDialog OpenNewNote() => AllureApi.Step("Новий інфозапис для листа: «Dokumentation» → «+» → «Notiz»", () =>
    {
        SelectTab("Dokumentation");
        AutomationElement? Menu() => FindPopup("RecordsLinkGroupContextMenu");
        UiInput.LeftClickOpeningMenu(() => Links, () => Menu() is not null, "меню «+» на вкладці «Dokumentation»", offset: Button(fromRight: 1));
        new MenuItemControl("Notiz", () => Wait.For(
            () => Menu()?.FindFirstChild(cf => cf.ByControlType(ControlType.MenuItem).And(cf.ByName("Notiz"))),
            "пункту «Notiz»")).Click();
        return new NoteDialog(Root);
    });

    /// <summary>OK: зберігає лист із зв'язками; ADAICA закриває вікно й відкриває збережений лист.</summary>
    public void Save() => AllureApi.Step($"Зберегти лист кнопкою OK у вікні «{WindowTitle}»", () =>
    {
        Wait.Until(() => OkButton.IsEnabled, $"доступної кнопки OK у вікні «{WindowTitle}» (усі зв'язки задано)");
        OkButton.Click();

        // Якщо лист зберегти не вдалося, ADAICA лишає вікно відкритим і показує «E-Mail-Speicherdialog» з причиною.
        string? error = null;
        Wait.Until(() =>
        {
            if (!IsOpen)
                return true;
            if (MessageBoxDialog.Find(Root) is not { } message)
                return false;

            error = message.Message.Text;
            return true;
        }, $"закриття вікна «{WindowTitle}»", SaveTimeout);

        if (error is not null)
            throw new InvalidOperationException($"ADAICA не зберегла лист: «{error}».");
    });

    public void Cancel() => AllureApi.Step($"Закрити вікно «{WindowTitle}» без збереження листа", () =>
    {
        CancelButton.Click();
        Wait.Until(() => !IsOpen, $"закриття вікна «{WindowTitle}»");
    });

    /// <summary>Для прибирання після тесту: закриває вікно, лише якщо воно ще відкрите.</summary>
    public void CancelIfOpen()
    {
        if (IsOpen)
            Cancel();
    }

    /// <summary>Вкладка зв'язків за підписом («Kontakte», «Dokumentation», «Objekte»); заголовки вкладок UI Automation бачить.</summary>
    private void SelectTab(string header)
    {
        AutomationElement Tab() => Wait.For(
            () => Root.FindByAutomationId("tcLinkGroups").FindAllChildren(cf => cf.ByControlType(ControlType.TabItem))
                .FirstOrDefault(tab => tab.FindFirstChild(cf => cf.ByControlType(ControlType.Text))?.Name == header),
            $"вкладки «{header}»");
        var selection = Tab().Patterns.SelectionItem.Pattern;
        if (!selection.IsSelected.Value)
        {
            selection.Select();
            Wait.Until(() => Tab().Patterns.SelectionItem.Pattern.IsSelected.Value, $"вибору вкладки «{header}»");
        }
    }

    private AutomationElement OpenRowMenu(int row, string menuId)
    {
        UiInput.RightClick(() => Links, () => FindPopup(menuId) is not null, $"контекстного меню рядка {row + 1}", offset: Row(row));
        return Wait.For(() => FindPopup(menuId), $"контекстного меню «{menuId}»");
    }

    // WPF сама закриває меню, коли ADAICA втрачає фокус, тож меню може вже не бути.
    private void CloseMenu(string menuId)
    {
        if (FindPopup(menuId) is not { } menu)
            return;

        UiInput.PressKey(() => FindPopup(menuId) ?? menu, VirtualKeyShort.ESCAPE, () => FindPopup(menuId) is null, "закриття контекстного меню");
    }

    private AutomationElement? FindPopup(string automationId) =>
        Popups.FindDescendant(Root, cf => cf.ByAutomationId(automationId))
        ?? Popups.FindDescendant(_mainWindow, cf => cf.ByAutomationId(automationId));

    private Point Row(int row) => Scaled(FirstColumnCenter, FirstRowCenter + row * RowHeight);

    // fromRight: 0 — крайня права кнопка, 1 — сусідня ліворуч тощо.
    private Point Button(int fromRight)
    {
        var scale = Scale;
        return new Point(
            Links.BoundingRectangle.Width - (int)Math.Round((LastButtonFromRight + fromRight * ButtonStep) * scale),
            (int)Math.Round(ButtonsCenter * scale));
    }

    private Point Scaled(double x, double y) => new((int)Math.Round(x * Scale), (int)Math.Round(y * Scale));

    private double Scale => GetDpiForWindow(Root.Properties.NativeWindowHandle.Value) / 96.0;

    [DllImport("user32.dll")]
    private static extern uint GetDpiForWindow(nint window);
}

/// <summary>Пункт контекстного меню і чи він доступний.</summary>
public sealed record MenuEntry(string Name, bool IsEnabled)
{
    public override string ToString() => IsEnabled ? Name : $"{Name} (недоступний)";
}
