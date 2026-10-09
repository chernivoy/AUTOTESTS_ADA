using ADAICA_AUTOTEST.Controls;
using ADAICA_AUTOTEST.Infrastructure;
using ADAICA_AUTOTEST.Pages;
using Allure.Net.Commons;
using Allure.Net.Commons.Attributes;

namespace ADAICA_AUTOTEST.Tests;

/// <summary>
/// Чати в навігаторі Dashboard («Meine Chats»). Чат пишуть двоє: основний користувач (<see cref="TestSettings.UserName"/>)
/// і другий (<see cref="TestSettings.SecondUserName"/>), тож тест посередині виходить з ADAICA й входить другим
/// користувачем. Чати лишаються в базі; назва чату щоразу унікальна (<see cref="CustomerCase.NewChat"/>).
/// </summary>
[AllureSuite("Навігатори")]
public class ChatTests : AdaicaTestBase
{
    /// <summary>
    /// Створює чат кнопкою «Neu» над «Meine Chats», перейменовує його, додає другого користувача учасником і пише
    /// повідомлення. Потім входить другим користувачем: іконка чатів праворуч угорі показує непрочитаний чат,
    /// подвійний клік відкриває його з повідомленням автора, і другий користувач відповідає.
    /// </summary>
    [Test]
    [AllureSeverity(SeverityLevel.critical)]
    public void DashboardNavigator_ChatWithReply()
    {
        var chat = CustomerCase.Random().NewChat();
        var partner = TestSettings.SecondUserContact;

        var mainPage = LogInAndShowSingleNavigator("Dashboard");
        var author = mainPage.UserDisplayName;
        var chats = mainPage.OpenUserChats();
        var card = chats.CreateChat();

        AllureApi.Step("Перевірити новий чат: назва за автором, автор — адміністратор, учасників немає", () =>
        {
            card.Name.ShouldBe($"{author} - Chat");
            card.Administrators.ShouldBe([$"{author} (Aktiv)"]);
            card.Participants.ShouldBeEmpty();
            card.Messages.ShouldBeEmpty();
        });

        card.Rename(chat.Name);
        chats.ShouldShow(chat.Name, "Aktiv");

        card.AddParticipant(partner);
        card.Administrators.ShouldBe([$"{author} (Aktiv)"]);
        card.Participants.ShouldBe([$"{partner} (Aktiv)"]);

        var sentFrom = DateTime.Now;
        var message = card.Send(chat.Message);
        ShouldBeSentBetween(message, sentFrom, DateTime.Now);
        card.Messages.ShouldBe([new ChatMessage(null, chat.Message, message.Sent)]);

        RestartForAnotherUser();
        var partnerPage = LogInAndShowSingleNavigator("Dashboard", TestSettings.SecondUserName);
        var tabsBefore = partnerPage.NavigatorTabNames;
        // Чат зі сповіщення відкривається у вкладці Dokumentation (або в новій), тож після тесту повертаємо Dashboard.
        OnCleanup(() =>
        {
            partnerPage.CloseLastNavigatorTabs(keep: tabsBefore.Count);
            partnerPage.SwitchToNavigatorTab("Dashboard");
        });

        var notifications = partnerPage.ChatNotifications;
        var unread = notifications.Count;
        unread.ShouldBeGreaterThan(0, "Іконка чатів не показує нових повідомлень");
        notifications.Open();
        notifications.Chats.ShouldContain(chat.Name);

        var partnerCard = notifications.OpenChat(chat.Name);
        AllureApi.Step("Перевірити чат у другого користувача: повідомлення автора, ті самі учасники", () =>
        {
            partnerCard.Messages.ShouldBe([new ChatMessage(author, chat.Message, message.Sent)]);
            partnerCard.Administrators.ShouldBe([$"{author} (Aktiv)"]);
            partnerCard.Participants.ShouldBe([$"{partner} (Aktiv)"]);
        });
        Wait.Until(() => notifications.Count == unread - 1, "лічильника нових повідомлень на один менше");

        var replyFrom = DateTime.Now;
        var reply = partnerCard.Send(chat.Reply);
        ShouldBeSentBetween(reply, replyFrom, DateTime.Now);
        partnerCard.Messages.ShouldBe(
        [
            new ChatMessage(author, chat.Message, message.Sent),
            new ChatMessage(null, chat.Reply, reply.Sent),
        ]);
    }

    /// <summary>
    /// Контроли картки чату в автора (адміністратора): «Abbrechen» і порожня назва при перейменуванні, повідомлення
    /// кнопкою й клавішею Enter, порожнє повідомлення й повідомлення з самих пробілів (не надсилаються), швидкий пошук
    /// (лише по Enter, без урахування регістру), контекстні меню списку учасників і особистий статус «Erledigt».
    /// </summary>
    [Test]
    [AllureSeverity(SeverityLevel.normal)]
    public void DashboardNavigator_ChatCardControls()
    {
        var chat = CustomerCase.Random().NewChat();
        var partner = TestSettings.SecondUserContact;

        var mainPage = LogInAndShowSingleNavigator("Dashboard");
        var author = mainPage.UserDisplayName;
        var chats = mainPage.OpenUserChats();
        var card = chats.CreateChat();
        card.Rename(chat.Name);

        AllureApi.Step("Перейменування: «Abbrechen» лишає назву, з порожньою назвою чи пробілами OK недоступна", () =>
        {
            card.StartRename();
            card.NameField.Fill("Besprechung verschoben");
            card.CancelRename();
            card.Name.ShouldBe(chat.Name);

            card.StartRename();
            card.NameField.Fill(string.Empty);
            card.RenameOkButton.ShouldBeDisabled();
            card.NameField.Fill("   ");
            card.RenameOkButton.ShouldBeDisabled();
            card.CancelRename();
            card.Name.ShouldBe(chat.Name);
        });

        card.AddParticipant(partner);

        card.Send(chat.Message);
        AllureApi.Step("Без тексту повідомлення не надсилається", () =>
        {
            card.ClickSend();
            // Нового повідомлення не має з'явитися, тож чекаємо стільки, скільки зазвичай займає надсилання.
            Thread.Sleep(TimeSpan.FromSeconds(2));
            card.Messages.Count.ShouldBe(1);
        });

        card.Type(chat.Reply);
        var reply = card.SendWithEnter();
        reply.ShouldBe(new ChatMessage(null, chat.Reply, reply.Sent));
        card.DraftText.ShouldBeEmpty();

        AllureApi.Step("Швидкий пошук: слово відповіді великими літерами, слово, якого немає, порожній рядок", () =>
        {
            var word = chat.Reply.Split(' ', ',', '.', '!').Where(part => part.Length > 4)
                .First(part => !chat.Message.Contains(part, StringComparison.OrdinalIgnoreCase));
            card.Search(word.ToUpperInvariant());
            Wait.Until(() => card.Messages.Select(message => message.Text).SequenceEqual([chat.Reply]), $"лише відповіді за словом «{word}»");
            card.Search("Kaffeemaschine");
            Wait.Until(() => card.Messages.Count == 0, "порожнього списку повідомлень");
            card.Search(string.Empty);
            Wait.Until(() => card.Messages.Count == 2, "усіх повідомлень з порожнім пошуком");
        });

        AllureApi.Step("Контекстні меню списку учасників в адміністратора", () =>
        {
            card.ParticipantMenu("Administrator").ShouldBe(new Dictionary<string, bool> { ["Administrator hinzufügen"] = true });
            card.ParticipantMenu("Teilnehmer").ShouldBe(new Dictionary<string, bool> { ["Teilnehmer hinzufügen"] = true });
            card.ParticipantMenu($"{author} (Aktiv)").ShouldBe(new Dictionary<string, bool>
            {
                ["Aktiv"] = false,
                ["Erledigt"] = true,
                ["Administrator löschen"] = false,
            });
            card.ParticipantMenu($"{partner} (Aktiv)").ShouldBe(new Dictionary<string, bool>
            {
                ["Aktiv"] = false,
                ["Erledigt"] = true,
                ["Teilnehmer löschen"] = true,
            });
        });

        AllureApi.Step("Особистий статус «Erledigt» і назад «Aktiv»", () =>
        {
            card.ChooseInParticipantMenu($"{author} (Aktiv)", "Erledigt");
            Wait.Until(() => card.Administrators.SequenceEqual([$"{author} (Erledigt)"]), "статусу «Erledigt» у списку учасників");
            chats.ShouldShow(chat.Name, "Erledigt");
            card.ChooseInParticipantMenu($"{author} (Erledigt)", "Aktiv");
            Wait.Until(() => card.Administrators.SequenceEqual([$"{author} (Aktiv)"]), "статусу «Aktiv» у списку учасників");
            chats.ShouldShow(chat.Name, "Aktiv");
        });

        // Останнім, бо пробіли лишаються в полі й потрапили б на початок наступного повідомлення.
        AllureApi.Step("Повідомлення з самих пробілів не надсилається", () =>
        {
            card.Type("   ");
            card.ClickSend();
            Thread.Sleep(TimeSpan.FromSeconds(2));
            card.Messages.Count.ShouldBe(2);
        });
    }

    // ADAICA показує час до хвилини, тож межі округлюємо до хвилин.
    private static void ShouldBeSentBetween(ChatMessage message, DateTime from, DateTime to)
    {
        var sent = DateTime.ParseExact(
            message.Sent, $"{AdaicaFormat.DatePattern} {AdaicaFormat.TimePattern}", System.Globalization.CultureInfo.InvariantCulture);
        sent.ShouldBeInRange(
            from.AddTicks(-(from.Ticks % TimeSpan.TicksPerMinute)),
            to.AddTicks(-(to.Ticks % TimeSpan.TicksPerMinute)),
            $"Час повідомлення «{message.Text}»");
    }
}
