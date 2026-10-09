using ADAICA_AUTOTEST.Controls;
using ADAICA_AUTOTEST.Infrastructure;
using Allure.Net.Commons;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;

namespace ADAICA_AUTOTEST.Pages;

/// <summary>
/// Картка чату (вкладка <c>tiMainRecordChat</c>): назва з олівцем для перейменування, швидкий пошук і період
/// «Nachrichten der letzten: N Tage von M Tagen» над повідомленнями, поле нового повідомлення з кнопкою надсилання
/// й праворуч «Teilnehmerliste» (групи «Administrator» і «Teilnehmer»). Та сама картка відкривається під списком
/// «Meine Chats» у Dashboard, у навігаторі Dokumentation (з Gesamtliste → Chats чи зі сповіщення) і в окремому вікні.
/// </summary>
public sealed class ChatCard
{
    public const string TabId = "tiMainRecordChat";

    private const string MessagesId = "lstBoxMessages";
    private const string ParticipantsId = "treeChatContactsView";
    private const string AdministratorsGroup = "Administrator";
    private const string ParticipantsGroup = "Teilnehmer";

    private readonly Window _window;

    public ChatCard(Window window)
    {
        _window = window;
        Wait.For(() => FindRoot(), "картки чату");

        NameField = new TextBoxControl("Name des Chats", () => Title().FindByAutomationId("txtRecName"));
        RenameOkButton = new ButtonControl("OK (Name)", () => Title().FindByAutomationId("btnOk"));
        QuickSearch = new TextBoxControl("Schnelle Suche", () => Root.FindByAutomationId("textBoxQuickSearch"));
    }

    /// <summary>Поле назви — лише після кліку по олівцю (<see cref="StartRename"/>).</summary>
    public TextBoxControl NameField { get; }

    /// <summary>Зберігає нову назву; з порожньою чи з одних пробілів назвою недоступна.</summary>
    public ButtonControl RenameOkButton { get; }

    /// <summary>Швидкий пошук у повідомленнях (текст і автор); застосовується лише по Enter.</summary>
    public TextBoxControl QuickSearch { get; }

    /// <summary>Назва чату над повідомленнями. Текст заголовка видно лише в raw-дереві.</summary>
    public string Name => Wait.For(
        () => Title().FindFirstDescendantRaw(cf => cf.ByAutomationId("tbRecName"))?.Name,
        "назви чату");

    private AutomationElement Root => Wait.For(() => FindRoot(), "картки чату");

    private AutomationElement? FindRoot() => _window.FindFirstDescendant(cf => cf.ByAutomationId(TabId));

    private AutomationElement Title() => Root.FindByAutomationId("ctrChatTitle");

    /// <summary>
    /// Повідомлення зверху вниз. Список не віртуалізовано: повідомлення поза видимою областю теж є в дереві.
    /// </summary>
    public IReadOnlyList<ChatMessage> Messages =>
        Root.FindByAutomationId(MessagesId).FindAllChildren(cf => cf.ByControlType(ControlType.ListItem))
            .Select(ReadMessage)
            .ToList();

    /// <summary>Учасники з групи «Administrator» з особистим статусом: «Chernivoy, Igor (Aktiv)».</summary>
    public IReadOnlyList<string> Administrators => GroupMembers(AdministratorsGroup);

    /// <summary>Учасники з групи «Teilnehmer» з особистим статусом.</summary>
    public IReadOnlyList<string> Participants => GroupMembers(ParticipantsGroup);

    /// <summary>
    /// Перейменовує чат: олівець → нова назва → OK. Назва з'являється над повідомленнями й у списку «Meine Chats».
    /// </summary>
    public void Rename(string name) => AllureApi.Step($"Перейменувати чат: {name}", () =>
    {
        StartRename();
        NameField.Fill(name);
        RenameOkButton.Click();
        Wait.Until(() => Name == name, $"назви чату «{name}»");
    });

    /// <summary>Клік по олівцю біля назви: замість назви з'являються поле, OK і «Abbrechen».</summary>
    public void StartRename() => AllureApi.Step("Почати перейменування чату (олівець)", () =>
        UiInput.LeftClick(
            () => Title().FindByAutomationId("btnEdit"),
            () => Title().FindFirstChild(cf => cf.ByAutomationId("txtRecName")) is not null,
            "поля назви чату"));

    /// <summary>«Abbrechen» біля поля назви: назва лишається попередньою.</summary>
    public void CancelRename() => AllureApi.Step("Скасувати перейменування чату", () =>
        UiInput.LeftClick(
            () => Title().FindByAutomationId("btnCancel"),
            () => Title().FindFirstChild(cf => cf.ByAutomationId("txtRecName")) is null,
            "закриття поля назви чату"));

    /// <summary>
    /// Пише повідомлення в поле «Nachricht eingeben» і надсилає його кнопкою зі стрілкою. Повідомлення з'являється
    /// в кінці списку як своє (праворуч, без автора). Поле — RichTextBox без ValuePattern, тож текст друкується
    /// символами, як з клавіатури.
    /// </summary>
    public ChatMessage Send(string text) => AllureApi.Step($"Надіслати повідомлення: {text}", () =>
    {
        Type(text);
        var before = Messages.Count;
        UiInput.LeftClick(
            () => Root.FindByAutomationId("btnSendMessage"),
            () => Messages.Count == before + 1,
            "нового повідомлення в чаті");
        return Messages[^1];
    });

    /// <summary>
    /// Натискає кнопку надсилання, не чекаючи нового повідомлення: так перевіряють, що порожнє поле чи поле з
    /// пробілами нічого не надсилає. Кнопка доступна й тоді, коли надсилати нічого.
    /// </summary>
    public void ClickSend() => AllureApi.Step("Натиснути кнопку надсилання повідомлення", () =>
        UiInput.LeftClick(() => Root.FindByAutomationId("btnSendMessage"), () => true, "натискання кнопки надсилання"));

    /// <summary>Друкує текст у поле нового повідомлення, не надсилаючи його.</summary>
    public void Type(string text) => AllureApi.Step($"Написати в полі повідомлення: {text}", () =>
    {
        var before = DraftText;
        UiInput.LeftClick(
            () => Root.FindByAutomationId("txtBottom"),
            () => Root.FindByAutomationId("txtBottom").Properties.HasKeyboardFocus.ValueOrDefault,
            "фокусу в полі повідомлення");
        UiInput.TypeText(
            () => Root.FindByAutomationId("txtBottom"),
            text,
            () => DraftText == before + text,
            "тексту в полі повідомлення",
            // Поле приймає символи по одному, а довгий текст ADAICA обробляє кілька секунд.
            TimeSpan.FromSeconds(10 + text.Length / 50));
    });

    /// <summary>Enter у полі повідомлення: надсилає набраний текст, як кнопка зі стрілкою.</summary>
    public ChatMessage SendWithEnter() => AllureApi.Step("Надіслати повідомлення клавішею Enter", () =>
    {
        var before = Messages.Count;
        UiInput.PressKey(
            () => Root.FindByAutomationId("txtBottom"),
            FlaUI.Core.WindowsAPI.VirtualKeyShort.ENTER,
            () => Messages.Count == before + 1,
            "нового повідомлення в чаті");
        return Messages[^1];
    });

    /// <summary>Текст у полі нового повідомлення (без кінцевого переходу на новий рядок, який додає RichTextBox).</summary>
    public string DraftText =>
        Root.FindByAutomationId("txtBottom").Patterns.Text.Pattern.DocumentRange.GetText(-1).TrimEnd('\r', '\n');

    /// <summary>
    /// Застосовує швидкий пошук: текст у поле «Schnelle Suche» і Enter. Без Enter список не змінюється, а з порожнім
    /// полем Enter знову показує всі повідомлення. Чи подіяв пошук, видно лише зі списку, тож результат чекає виклик.
    /// </summary>
    public void Search(string text) => AllureApi.Step($"Швидкий пошук у чаті: «{text}»", () =>
    {
        QuickSearch.Fill(text);
        UiInput.PressKey(
            () => Root.FindByAutomationId("textBoxQuickSearch"),
            FlaUI.Core.WindowsAPI.VirtualKeyShort.ENTER,
            () => true,
            "застосування швидкого пошуку");
    });

    /// <summary>
    /// Контекстне меню «Teilnehmer» → «Teilnehmer hinzufügen» → вікно вибору користувачів (<see cref="ChatParticipantsDialog"/>).
    /// </summary>
    public ChatParticipantsDialog OpenAddParticipant()
    {
        OpenParticipantMenu(ParticipantsGroup);
        PopupMenuItem("Teilnehmer hinzufügen").Click();
        return new ChatParticipantsDialog(_window);
    }

    /// <summary>
    /// Додає учасника: «Teilnehmer hinzufügen» → прапорець користувача → «&gt;&gt;» → OK. Учасник з'являється в групі
    /// «Teilnehmer» зі статусом «Aktiv».
    /// </summary>
    public void AddParticipant(string contact) => AllureApi.Step($"Додати учасника чату: {contact}", () =>
    {
        var dialog = OpenAddParticipant();
        dialog.Choose(contact);
        dialog.Confirm();
        Wait.Until(() => Participants.Contains($"{contact} (Aktiv)"), $"учасника «{contact}» у групі «Teilnehmer»");
    });

    /// <summary>
    /// Пункти контекстного меню вузла «Teilnehmerliste»: групи («Administrator», «Teilnehmer») або учасника
    /// («Chernivoy, Igor (Aktiv)») — назва → доступність. Меню закривається клавішею Esc.
    /// </summary>
    public IReadOnlyDictionary<string, bool> ParticipantMenu(string node) =>
        AllureApi.Step($"Прочитати контекстне меню «{node}» у списку учасників", () =>
        {
            OpenParticipantMenu(node);
            var items = Popups.Of(_window)
                .SelectMany(popup => popup.FindAllDescendants(cf => cf.ByControlType(ControlType.MenuItem)))
                .ToDictionary(item => item.Name, item => item.IsEnabled);
            CloseMenu();
            return (IReadOnlyDictionary<string, bool>)items;
        });

    /// <summary>Пункт контекстного меню учасника: «Aktiv», «Erledigt», «Teilnehmer löschen», «Administrator löschen».</summary>
    public void ChooseInParticipantMenu(string node, string item) =>
        AllureApi.Step($"Вибрати «{item}» у меню учасника «{node}»", () =>
        {
            OpenParticipantMenu(node);
            PopupMenuItem(item).Click();
        });

    private void OpenParticipantMenu(string node) =>
        UiInput.RightClick(
            () => ParticipantText(node),
            () => Popups.FindDescendant(_window, cf => cf.ByControlType(ControlType.MenuItem)) is not null,
            $"контекстного меню «{node}» у списку учасників");

    private void CloseMenu() =>
        UiInput.PressKey(
            () => Root.FindByAutomationId(ParticipantsId),
            FlaUI.Core.WindowsAPI.VirtualKeyShort.ESCAPE,
            () => Popups.FindDescendant(_window, cf => cf.ByControlType(ControlType.MenuItem)) is null,
            "закриття контекстного меню");

    private AutomationElement ParticipantText(string node) => Wait.For(
        () => Root.FindByAutomationId(ParticipantsId).FindFirstDescendant(cf => cf.ByControlType(ControlType.Text).And(cf.ByName(node))),
        $"«{node}» у списку учасників");

    // Пункти меню учасників не мають AutomationId, тож шукаємо за підписом.
    private MenuItemControl PopupMenuItem(string name) =>
        new(name, () => Wait.For(
            () => Popups.FindDescendant(_window, cf => cf.ByControlType(ControlType.MenuItem).And(cf.ByName(name))),
            $"пункту «{name}» у контекстному меню"));

    // Група — TreeItem з підписом групи, учасники — вкладені TreeItem з підписом «Ім'я (Статус)».
    private List<string> GroupMembers(string group)
    {
        var groupItem = Root.FindByAutomationId(ParticipantsId).FindAllChildren(cf => cf.ByControlType(ControlType.TreeItem))
            .First(item => item.FindFirstChild(cf => cf.ByControlType(ControlType.Text))?.Name == group);
        return groupItem.FindAllChildren(cf => cf.ByControlType(ControlType.TreeItem))
            .Select(member => member.FindFirstChild(cf => cf.ByControlType(ControlType.Text)).Name)
            .ToList();
    }

    // Чуже повідомлення має аватар з ім'ям автора (обрізане «Chernivoy, Igor...», повне — у підказці), своє — ні.
    private static ChatMessage ReadMessage(AutomationElement item)
    {
        var author = item.FindAllChildren(cf => cf.ByControlType(ControlType.Edit))
            .FirstOrDefault(edit => edit.AutomationId != "tbMessageDate")?.HelpText;
        var text = item.FindFirstChild(cf => cf.ByControlType(ControlType.Text))?.Name ?? string.Empty;
        var sent = TextInput.Read(item.FindFirstChild(cf => cf.ByAutomationId("tbMessageDate")));
        return new ChatMessage(author, text, sent);
    }
}

/// <param name="Author">Автор чужого повідомлення («Chernivoy, Igor»); null — власне повідомлення.</param>
/// <param name="Sent">Час надсилання, як його показує ADAICA: «09.10.2026 15:56».</param>
public sealed record ChatMessage(string? Author, string Text, string Sent)
{
    public bool IsOwn => Author is null;
}
