using ADAICA_AUTOTEST.Controls;
using ADAICA_AUTOTEST.Infrastructure;
using Allure.Net.Commons;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;

namespace ADAICA_AUTOTEST.Pages;

/// <summary>
/// Список «Meine Chats» у навігаторі Dashboard (вузол «Meine Chats» під користувачем): чати, де користувач
/// учасник, з колонками Name, Erstelldatum, Änderungsdatum, Anwender, Geändert durch, Klassentyp, ID і
/// «Persönlicher Status». Над списком — пошук, фільтри за особистим статусом («Aktiv», «Erledigt», «Beobachtet»),
/// «Neu» і «Aktualisieren». Вибраний чат відкривається карткою під списком (<see cref="ChatCard"/>).
/// </summary>
/// <remarks>
/// «Neu» одразу зберігає чат з назвою «Прізвище, Ім'я - Chat» і користувачем-адміністратором, без вікна. Чати
/// зі статусом «Erledigt» ADAICA показує жирним курсивом — так само підписано й кнопку фільтра, тож жирний курсив
/// на кнопці «Erledigt» не означає, що фільтр увімкнено.
/// </remarks>
public sealed class ChatList
{
    private readonly Window _window;

    public ChatList(Window window)
    {
        _window = window;
        Wait.For(() => FindView(), "списку «Meine Chats»");

        ActiveFilter = new ToggleButtonControl("Aktiv", () => View.FindByAutomationId("tbtnChatPersonalStatusIsActive"));
        DoneFilter = new ToggleButtonControl("Erledigt", () => View.FindByAutomationId("tbtnChatPersonalStatusDone"));
        WatchedFilter = new ToggleButtonControl("Beobachtet", () => View.FindByAutomationId("tbtnChatPersonalStatusUnknown"));
    }

    public ToggleButtonControl ActiveFilter { get; }

    public ToggleButtonControl DoneFilter { get; }

    public ToggleButtonControl WatchedFilter { get; }

    /// <summary>Рядки списку: назва чату → особистий статус користувача в ньому.</summary>
    public IReadOnlyList<ChatListRow> Rows =>
        View.FindByAutomationId("dataControl").FindAllDescendants(cf => cf.ByControlType(ControlType.DataItem))
            .Select(row => new ChatListRow(Cell(row, "RowData.Row.Name"), Cell(row, "RowData.Row.PersonalStatusTranslated")))
            .ToList();

    private AutomationElement View => Wait.For(() => FindView(), "списку «Meine Chats»");

    // Фільтри статусу є лише над списком чатів, тож за ними й знаходимо список.
    private AutomationElement? FindView() =>
        _window.FindFirstDescendant(cf => cf.ByAutomationId("ChatStatesFilter"))?.Parent?.Parent;

    /// <summary>«Neu» над списком: новий чат з'являється в списку виділеним, а під списком — його картка.</summary>
    public ChatCard CreateChat() => AllureApi.Step("Створити чат кнопкою «Neu» над списком «Meine Chats»", () =>
    {
        var before = Rows.Count;
        UiInput.LeftClick(
            () => View.FindByAutomationId("btnAddNewItem"),
            () => Rows.Count == before + 1 && _window.FindFirstDescendant(cf => cf.ByAutomationId(ChatCard.TabId)) is not null,
            "нового чату в списку «Meine Chats»");
        return new ChatCard(_window);
    });

    /// <summary>Чекає, доки рядок чату <paramref name="name"/> покаже статус <paramref name="status"/>.</summary>
    public void ShouldShow(string name, string status) =>
        AllureApi.Step($"Перевірити, що в списку «Meine Chats» є чат «{name}» зі статусом «{status}»", () =>
            Wait.Until(
                () => Rows.Contains(new ChatListRow(name, status)),
                $"чату «{name}» зі статусом «{status}» у списку «Meine Chats»"));

    private static string Cell(AutomationElement row, string automationId) =>
        TextInput.Read(row.FindFirstDescendant(cf => cf.ByAutomationId(automationId)));
}

public sealed record ChatListRow(string Name, string PersonalStatus);
