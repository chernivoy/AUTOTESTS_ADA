using ADAICA_AUTOTEST.Controls;
using ADAICA_AUTOTEST.Infrastructure;
using Allure.Net.Commons;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;

namespace ADAICA_AUTOTEST.Pages;

/// <summary>
/// Публікація після кроку workflow (`ctrPublicationWorkflow`): панель під вкладками бланка, яку видно, поки відкрито
/// вкладку кроку з «Nach diesem Schritt veröffentlichen und verteilen». Вкладка «Veröffentlichung» — «Pfad der
/// Veröffentlichung»: дерево «DMS – Veröffentlichungen» з прапорцем «Verknüpft» у кожному рядку (куди ADAICA покладе
/// опублікований документ). Вкладка «Verteilung» — розсилка: «In Chat verteilen» і «Per Mail verteilen» зі своїми
/// списками контактів і текст повідомлення (редактор DevExpress, доступний, коли ввімкнено чат чи e-mail).
/// </summary>
/// <remarks>
/// Дерево — TreeListView: Name рядка — тип моделі, підпис — його TextBlock, прапорець — `chkColumnRelatedSingle`.
/// Позначити можна або папки й інші записи, або один документ (тоді опублікований файл стане новою версією цього
/// документа). Недозволений вибір ADAICA відхиляє модальним повідомленням ще до того, як прапорець зміниться, тож
/// прапорці дерева тест клікає: TogglePattern чекав би, доки повідомлення закриють.
/// </remarks>
public sealed class WorkflowPublicationPanel
{
    private const string PanelId = "ctrPublicationCtrWorkflow";
    private const string PathCheckId = "chkColumnRelatedSingle";

    private readonly Func<AutomationElement> _form;
    private readonly Window _window;

    /// <param name="form">Елемент `ctrWorkflow` (бланк workflow).</param>
    /// <param name="window">Вікно бланка: повідомлення ADAICA про недозволений шлях — його дочірнє вікно.</param>
    public WorkflowPublicationPanel(Func<AutomationElement> form, Window window)
    {
        _form = form;
        _window = window;
        Chat = new CheckBoxControl("In Chat verteilen", Field("chkCreateChatPublication"));
        ChatContactsButton = new ButtonControl("In Chat verteilen: ...", Field("btnContacts"));
        Mail = new CheckBoxControl("Per Mail verteilen", Field("chkMailPublication"));
        MailContactsButton = new ButtonControl("Per Mail verteilen: ...", Field("btnMailContacts"));
    }

    /// <summary>Чи видно панель: її показує лише вкладка кроку, на якому ввімкнено публікацію.</summary>
    public bool IsShown => _form().FindFirstDescendant(cf => cf.ByAutomationId(PanelId)) is not null;

    public CheckBoxControl Chat { get; }

    /// <summary>«...» біля «In Chat verteilen»: доступна, коли чат увімкнено.</summary>
    public ButtonControl ChatContactsButton { get; }

    public CheckBoxControl Mail { get; }

    /// <summary>«...» біля «Per Mail verteilen»: доступна, коли e-mail увімкнено.</summary>
    public ButtonControl MailContactsButton { get; }

    /// <summary>Контакти чату, як їх підписує список.</summary>
    public IReadOnlyList<string> ChatContacts => ListItems("lstContactsList");

    /// <summary>Контакти розсилки e-mail, як їх підписує список.</summary>
    public IReadOnlyList<string> MailContacts => ListItems("lstMailContactsList");

    /// <summary>
    /// Чи можна редагувати текст повідомлення. Сам текст (DevExpress RichEdit) UI Automation не показує, а доступність
    /// видно з кнопок його стрічки: «Fett» вимкнена, поки редактор лише для читання.
    /// </summary>
    public bool IsMessageEditable => Field("BarCheckItemLink0biFormatFontBold")().IsEnabled;

    /// <summary>Рядки дерева «Pfad der Veröffentlichung» зверху вниз (розгорнуті вузли).</summary>
    public IReadOnlyList<PublicationPath> Paths => PathItems()
        .Select(item => new PublicationPath(PathName(item), IsOn(PathCheck(item)), PathCheck(item).IsEnabled))
        .ToList();

    /// <summary>Позначені рядки дерева.</summary>
    public IReadOnlyList<string> LinkedPaths => Paths.Where(path => path.IsLinked).Select(path => path.Name).ToList();

    /// <summary>Відкриває вкладку «Veröffentlichung» і чекає дерево: ADAICA заповнює його у фоні, коли панель з'являється вперше.</summary>
    public void ShowPublicationPath() => AllureApi.Step("Відкрити вкладку «Veröffentlichung» панелі публікації", () =>
    {
        SelectTab("tiGeneralWndPublication", "Veröffentlichung");
        Wait.Until(() => PathItems().Length > 0, "дерева «Pfad der Veröffentlichung»");
    });

    public void ShowDistribution() => AllureApi.Step("Відкрити вкладку «Verteilung» панелі публікації", () => SelectTab("tiPublishWndPublication", "Verteilung"));

    /// <summary>
    /// Ставить чи знімає «Verknüpft» у рядку <paramref name="path"/>. Якщо ADAICA відхилила вибір повідомленням, тест
    /// падає з його текстом.
    /// </summary>
    public void SetLinked(string path, bool linked) => AllureApi.Step($"«Pfad der Veröffentlichung»: {(linked ? "позначити" : "зняти")} «{path}»", () =>
    {
        ShowPublicationPath();
        if (IsLinked(path) == linked)
            return;

        if (ClickPath(path) is { } refused)
            throw new InvalidOperationException($"ADAICA не дала {(linked ? "позначити" : "зняти")} «{path}»: «{refused}».");
        Wait.Until(() => IsLinked(path) == linked, $"стану «Verknüpft» рядка «{path}»");
    });

    /// <summary>
    /// Пробує позначити рядок <paramref name="path"/>, який не можна поєднати з уже позначеними: повертає текст
    /// повідомлення ADAICA (і закриває його кнопкою OK) або null, якщо ADAICA рядок позначила.
    /// </summary>
    public string? TryLink(string path) => AllureApi.Step($"«Pfad der Veröffentlichung»: спробувати позначити «{path}»", () =>
    {
        ShowPublicationPath();
        return ClickPath(path);
    });

    /// <summary>Заповнює шляхи, чат і e-mail за <paramref name="data"/>: спершу знімає зайві шляхи, потім позначає потрібні.</summary>
    public void Fill(WorkflowPublicationData data) => AllureApi.Step("Заповнити панель публікації", () =>
    {
        ShowPublicationPath();
        foreach (var path in LinkedPaths.Except(data.Paths).ToList())
            SetLinked(path, false);
        foreach (var path in data.Paths)
            SetLinked(path, true);

        ShowDistribution();
        Chat.SetChecked(data.Chat);
        Mail.SetChecked(data.Mail);
        foreach (var user in data.ChatContacts.Where(user => !ChatContacts.Any(name => name.StartsWith(user, StringComparison.Ordinal))))
            AddChatContact(user);
        ShowPublicationPath();
    });

    /// <summary>«...» біля «In Chat verteilen» → прапорець користувача → «>>» → OK.</summary>
    public void AddChatContact(string user) => AllureApi.Step($"Додати до розсилки в чат «{user}»", () =>
    {
        var before = ChatContacts.Count;
        ChatContactsButton.Click();
        var dialog = new ChatParticipantsDialog(_window);
        dialog.Choose(user);
        dialog.Confirm();
        Wait.Until(() => ChatContacts.Count == before + 1, $"«{user}» у списку чату");
    });

    public void ShouldHaveValues(WorkflowPublicationData expected) => AllureApi.Step("Перевірити панель публікації", () =>
    {
        ShowPublicationPath();
        var linked = LinkedPaths;
        ShowDistribution();
        var chatContacts = ChatContacts;
        this.ShouldSatisfyAllConditions(
            () => linked.ShouldBe(expected.Paths, ignoreOrder: true, "Позначені «Verknüpft» шляхи публікації"),
            () => Chat.IsChecked.ShouldBe(expected.Chat, $"Прапорець «{Chat.Name}»"),
            () => Mail.IsChecked.ShouldBe(expected.Mail, $"Прапорець «{Mail.Name}»"),
            () => chatContacts.Count.ShouldBe(expected.ChatContacts.Count, $"Контакти чату: {string.Join("; ", chatContacts)}"),
            () => expected.ChatContacts.ShouldAllBe(
                user => chatContacts.Any(name => name.StartsWith(user, StringComparison.Ordinal)),
                $"Контакти чату: {string.Join("; ", chatContacts)}"));
        ShowPublicationPath();
    });

    private bool IsLinked(string path) => IsOn(PathCheck(PathItem(path)));

    // Клік по прапорцю рядка: повертає текст повідомлення ADAICA, якщо вона відхилила вибір, інакше null.
    private string? ClickPath(string path)
    {
        var before = IsLinked(path);
        // Дерево невисоке, а записів під «DMS – Veröffentlichungen» багато: рядок може бути нижче видимої частини.
        if (PathItem(path).Patterns.ScrollItem.PatternOrDefault is { } scroll)
            scroll.ScrollIntoView();
        UiInput.LeftClick(
            () => PathCheck(PathItem(path)),
            () => FindMessage() is not null || IsLinked(path) != before,
            $"зміни «Verknüpft» рядка «{path}» або повідомлення ADAICA");
        if (FindMessage() is not { } message)
            return null;

        var text = message.Message.Text;
        AllureApi.AddAttachment("Повідомлення ADAICA", "text/plain", System.Text.Encoding.UTF8.GetBytes(text), ".txt");
        message.Button("OK").Click();
        Wait.Until(() => FindMessage() is null, "закриття повідомлення ADAICA");
        return text;
    }

    // Повідомлення ADAICA належить не вікну бланка, а одному з його власників (вікну списку шаблонів чи головному вікну):
    // шукаємо серед дочірніх вікон бланка й усіх вікон над ним.
    private MessageBoxDialog? FindMessage()
    {
        for (AutomationElement? element = _window; element is not null; element = element.Parent)
        {
            if (element.ControlType == ControlType.Window && MessageBoxDialog.Find(element.AsWindow()) is { } message)
                return message;
        }

        return null;
    }

    private AutomationElement PathItem(string path) => Wait.For(
        () => PathItems().FirstOrDefault(item => PathName(item) == path),
        $"рядка «{path}» у «Pfad der Veröffentlichung»");

    // Дерево ADAICA заповнює у фоні, коли панель з'являється вперше.
    private AutomationElement[] PathItems() =>
        Field("treeListViewPublicationPath")().FindAllDescendants(cf => cf.ByControlType(ControlType.TreeItem));

    private static string PathName(AutomationElement item) =>
        item.FindFirstChild(cf => cf.ByControlType(ControlType.Text))?.Name ?? string.Empty;

    private static AutomationElement PathCheck(AutomationElement item) =>
        item.FindFirstChild(cf => cf.ByAutomationId(PathCheckId))
            ?? throw new InvalidOperationException($"У рядку «{PathName(item)}» немає прапорця «Verknüpft».");

    private static bool IsOn(AutomationElement check) =>
        check.Patterns.Toggle.Pattern.ToggleState.Value == FlaUI.Core.Definitions.ToggleState.On;

    private IReadOnlyList<string> ListItems(string listId) =>
        Field(listId)().FindAllChildren(cf => cf.ByControlType(ControlType.ListItem))
            .Select(item => item.FindFirstChild(cf => cf.ByControlType(ControlType.Text))?.Name ?? item.Name)
            .ToList();

    // Вкладки реагують лише на клік. В області перегляду під списком шаблонів панель нижче видимої частини.
    private void SelectTab(string tabId, string name)
    {
        bool IsSelected() => Field(tabId)().Patterns.SelectionItem.Pattern.IsSelected.Value;
        if (IsSelected())
            return;

        Scrolling.IntoView(Field(tabId));
        UiInput.LeftClick(Field(tabId), IsSelected, $"вибору вкладки «{name}»");
    }

    private Func<AutomationElement> Field(string automationId) =>
        () => Wait.For(() => _form().FindFirstDescendant(cf => cf.ByAutomationId(automationId)), $"елемента {automationId} панелі публікації");
}

/// <summary>Рядок дерева «Pfad der Veröffentlichung»: назва запису, чи позначено «Verknüpft» і чи можна його змінити.</summary>
public sealed record PublicationPath(string Name, bool IsLinked, bool IsEnabled);
