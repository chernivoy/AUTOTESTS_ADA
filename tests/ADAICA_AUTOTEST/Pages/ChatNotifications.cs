using System.Text.RegularExpressions;
using ADAICA_AUTOTEST.Infrastructure;
using Allure.Net.Commons;
using FlaUI.Core.AutomationElements;

namespace ADAICA_AUTOTEST.Pages;

/// <summary>
/// Іконка чатів у заголовку вікна праворуч угорі (поруч із завданнями й термінами): червоний лічильник — скільки
/// чатів мають нові чи змінені повідомлення, підказка — «Chats (N neue/geänderte Einträge)». Клік відкриває список
/// таких чатів (WPF Popup), подвійний клік по чату відкриває його в навігаторі Dokumentation (вузол «Aktuelle»)
/// і скидає лічильник.
/// </summary>
public sealed partial class ChatNotifications
{
    private const string GroupName = "LizenzaDevelopment.ADAICA.NotificationArea.ViewModels.Chats.ChatGroupViewModel";
    private const string ItemName = "LizenzaDevelopment.ADAICA.NotificationArea.ViewModels.Chats.ChatGroupItemViewModel";

    private readonly Window _window;

    public ChatNotifications(Window window) => _window = window;

    /// <summary>Скільки чатів з новими чи зміненими повідомленнями — з підказки іконки.</summary>
    public int Count => int.Parse(CountInHint().Match(Button().HelpText).Groups["count"].Value);

    /// <summary>Назви чатів у відкритому списку сповіщень.</summary>
    public IReadOnlyList<string> Chats =>
        Popups.Of(_window)
            .SelectMany(popup => popup.FindAllDescendants(cf => cf.ByName(ItemName)))
            .Select(item => item.FindFirstChild(cf => cf.ByAutomationId("textBlockTitle")).Name)
            .ToList();

    /// <summary>Список відкритий: іконка — кнопка-перемикач, увімкнена, поки список показано.</summary>
    public bool IsOpen => Button().Patterns.Toggle.Pattern.ToggleState.Value == FlaUI.Core.Definitions.ToggleState.On;

    /// <summary>Клік по іконці: відкриває список чатів з новими повідомленнями (повторний клік його закрив би).</summary>
    public void Open() => AllureApi.Step("Відкрити список чатів з новими повідомленнями (іконка праворуч угорі)", () =>
    {
        if (IsOpen)
            return;

        UiInput.LeftClickOpeningMenu(Button, () => IsOpen, "списку чатів з новими повідомленнями");
    });

    /// <summary>
    /// Подвійний клік по чату <paramref name="name"/> у списку сповіщень: чат відкривається карткою в навігаторі
    /// Dokumentation. Одинарний клік лише виділяє рядок.
    /// </summary>
    public ChatCard OpenChat(string name) => AllureApi.Step($"Відкрити чат «{name}» зі списку сповіщень (подвійний клік)", () =>
    {
        // Список сповіщень WPF закриває, щойно ADAICA стає неактивною, тож фокус тримаємо в ній від відкриття списку
        // до подвійного кліку, а закритий список відкриваємо знову.
        using (ForegroundGuard.Pause())
        {
            Open();
            UiInput.DoubleClick(
                () => Wait.For(() => ChatTitle(name), $"чату «{name}» у списку сповіщень"),
                () => _window.FindFirstDescendant(cf => cf.ByAutomationId(ChatCard.TabId)) is not null,
                $"картки чату «{name}»");
        }

        var card = new ChatCard(_window);
        Wait.Until(() => card.Name == name, $"картки чату «{name}»");
        return card;
    });

    private AutomationElement Button() => Wait.For(
        () => _window.FindFirstDescendant(cf => cf.ByName(GroupName))?.FindFirstChild(cf => cf.ByAutomationId("tbtnNotificationGroup")),
        "іконки чатів праворуч угорі");

    private AutomationElement? ChatTitle(string name) =>
        Popups.FindDescendant(_window, cf => cf.ByAutomationId("textBlockTitle").And(cf.ByName(name)));

    [GeneratedRegex(@"\((?<count>\d+) ")]
    private static partial Regex CountInHint();
}
