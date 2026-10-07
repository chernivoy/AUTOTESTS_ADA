using ADAICA_AUTOTEST.Infrastructure;
using ADAICA_AUTOTEST.Pages;
using Allure.Net.Commons;
using Allure.Net.Commons.Attributes;

namespace ADAICA_AUTOTEST.Tests;

/// <summary>
/// Опції пошти з Konfiguration → Einstellungen → «Kontakte» → «E-Mail» (<see cref="MailOptions"/>) у вікні збереження
/// листа. Опції діють лише після перезапуску ADAICA, тож кожен тест ставить свої опції, перезапускає ADAICA, а після
/// себе повертає опції, як були (частина з них — для всієї системи).
/// </summary>
public partial class MailTests
{
    /// <summary>
    /// «Wenig strenge Regel»: відправник і ОДИН «An» — лише з реальними контактами. Лист двом новим людям.
    /// «Keine Kontaktzuordnung» (AUTO) для обох — лист однаково не зберегти (це дозволяє лише «Einfache Regel»).
    /// Після «Manuell» тест створює персону для одного отримувача — і лист можна зберегти, хоча другий невідомий.
    /// У «Zugriffspfade» збереженого листа — користувач і лише ця персона.
    /// </summary>
    [Test]
    [AllureSeverity(SeverityLevel.normal)]
    public void DokumentationNavigator_MailRuleSenderAndOneRecipient()
    {
        SetMailOptionsAndRestart(options => options with { Rule = MailRecognitionRule.SenderAndOneRecipient, SalutationUser = false });
        var mainPage = LogInAndOpenDokumentation();
        EnsureOutlookAccount(mainPage, OutlookApplication.DefaultAccountAddress());
        var mail = CustomerCase.Random().NewMail(moreRecipients: 1);

        var (saveDialog, _) = SendMail(mainPage, mail);
        saveDialog.ShouldBeSavable(false);
        saveDialog.ShouldHaveContactState(RecipientRow, MailContactState.Unknown);
        saveDialog.ShouldHaveContactState(RecipientRow + 1, MailContactState.Unknown);

        saveDialog.AssignNoContacts();
        saveDialog.ShouldHaveContactState(RecipientRow, MailContactState.NoContact);
        saveDialog.ShouldHaveContactState(RecipientRow + 1, MailContactState.NoContact);
        AllureApi.Step("«Keine Kontaktzuordnung» за цим правилом не рахується", () => saveDialog.ShouldBeSavable(false));

        saveDialog.ChooseInContactMenu(RecipientRow, "Manuell");
        saveDialog.ChooseInContactMenu(RecipientRow + 1, "Manuell");
        var linked = CreateContactForRow(saveDialog, RecipientRow, mail);
        saveDialog.ShouldBeSavable(true);

        saveDialog.Save();
        ContactsInAccessPathsShouldBe(mainPage, linked.Address, linked.Person);
    }

    /// <summary>
    /// «Strenge Regel»: відправник і ВСІ «An» — з реальними контактами. Лист двом новим людям і ще одній у копії.
    /// Персона для першого отримувача — лист ще не зберегти; для другого — можна, хоча копія невідома.
    /// У «Zugriffspfade» — користувач і обидва отримувачі.
    /// </summary>
    [Test]
    [AllureSeverity(SeverityLevel.normal)]
    public void DokumentationNavigator_MailRuleSenderAndAllRecipients()
    {
        SetMailOptionsAndRestart(options => options with { Rule = MailRecognitionRule.SenderAndAllRecipients, SalutationUser = false });
        var mainPage = LogInAndOpenDokumentation();
        EnsureOutlookAccount(mainPage, OutlookApplication.DefaultAccountAddress());
        var mail = CustomerCase.Random().NewMail(moreRecipients: 1, copies: 1);

        var (saveDialog, _) = SendMail(mainPage, mail);
        saveDialog.ShouldBeSavable(false);

        var first = CreateContactForRow(saveDialog, RecipientRow, mail);
        AllureApi.Step("Один отримувач із двох зв'язаний — лист не зберегти", () => saveDialog.ShouldBeSavable(false));
        var second = CreateContactForRow(saveDialog, RecipientRow + 1, mail);
        AllureApi.Step("Обидва отримувачі зв'язані, копія — ні: лист можна зберегти", () => saveDialog.ShouldBeSavable(true));
        new[] { first, second }.ShouldAllBe(addressee => addressee.Kind == MailAddresseeKind.To, "Рядки 2 і 3 — отримувачі «An»");

        saveDialog.Save();
        ContactsInAccessPathsShouldBe(mainPage, first.Address, first.Person, second.Person);
    }

    /// <summary>
    /// «Sehr strenge Regel»: відправник, ВСІ «An» і ВСІ «CC» — з реальними контактами. Лист новій людині з копією
    /// ще одній. Персона для отримувача — лист ще не зберегти; для копії — можна. У «Zugriffspfade» — усі троє.
    /// </summary>
    [Test]
    [AllureSeverity(SeverityLevel.normal)]
    public void DokumentationNavigator_MailRuleSenderRecipientsAndCopies()
    {
        SetMailOptionsAndRestart(options => options with { Rule = MailRecognitionRule.SenderAllRecipientsAndCopies, SalutationUser = false });
        var mainPage = LogInAndOpenDokumentation();
        EnsureOutlookAccount(mainPage, OutlookApplication.DefaultAccountAddress());
        var mail = CustomerCase.Random().NewMail(copies: 1);

        var (saveDialog, _) = SendMail(mainPage, mail);
        saveDialog.ShouldBeSavable(false);

        var recipient = CreateContactForRow(saveDialog, RecipientRow, mail);
        AllureApi.Step("Отримувач зв'язаний, копія — ні: лист не зберегти", () => saveDialog.ShouldBeSavable(false));
        var copy = CreateContactForRow(saveDialog, CopyRow, mail);
        copy.Kind.ShouldBe(MailAddresseeKind.Cc, "Рядок 3 — копія «CC»");
        saveDialog.ShouldBeSavable(true);

        saveDialog.Save();
        ContactsInAccessPathsShouldBe(mainPage, recipient.Address, recipient.Person, copy.Person);
    }

    /// <summary>
    /// Алгоритм для адреси кількох контактів, варіант 1 «Kontakterkennung nach Priorität» — лише головні контакти,
    /// без додаткових. Дві персони з однією адресою; лист на цю адресу ADAICA зв'язує лише з однією: рядка «Kontakt»
    /// немає, у «Zugriffspfade» — одна з двох.
    /// </summary>
    [Test]
    [AllureSeverity(SeverityLevel.normal)]
    public void DokumentationNavigator_MailToAddressOfTwoContactsByPriority()
    {
        SetMailOptionsAndRestart(options => options with { MultipleContacts = MailMultipleContactsMode.Priority, SalutationUser = false });
        var (mainPage, customer, shared, persons) = CreateTwoPersonsWithOneAddress();

        var mail = customer.NewMailTo(persons[0]) with { To = shared };
        var (saveDialog, _) = SendMail(mainPage, mail);
        saveDialog.ShouldHaveContactState(RecipientRow, MailContactState.Linked);
        saveDialog.ShouldHaveContactRows(2);
        saveDialog.ShouldBeSavable(true);
        saveDialog.Save();

        var paths = mainPage.WaitForOpenedMail(shared).ReadAccessPaths();
        AllureApi.Step("Перевірити: з двох персон з цією адресою в «Zugriffspfade» лише одна", () =>
            paths.Contacts.Count(contact => persons.Any(person => person.SortName == contact)).ShouldBe(1, "Персони з адресою листа в «Zugriffspfade»"));
    }

    /// <summary>
    /// Алгоритм для адреси кількох контактів, варіант 2 (стандартний) — головні контакти й усі додаткові для
    /// отримувачів («An» і «CC»), але не для відправника. Підпис варіанта в ADAICA («… als Empfänger adressiert
    /// (ohne Empfänger von Kopien (CC))») перекладено неправильно — логіку підтвердив користувач 06.10.2026.
    /// Дві персони з однією адресою: лист на неї — друга персона рядком «Kontakt», у «Zugriffspfade» — обидві; лист
    /// з цією адресою в копії — так само рядок «Kontakt». Тимчасова персона з адресою відправника рядка «Kontakt» не дає
    /// (адресу тест одразу прибирає).
    /// </summary>
    [Test]
    [AllureSeverity(SeverityLevel.normal)]
    public void DokumentationNavigator_MailToAddressOfTwoContactsForRecipients()
    {
        SetMailOptionsAndRestart(options => options with { MultipleContacts = MailMultipleContactsMode.Recipients, SalutationUser = false });
        var (mainPage, customer, shared, persons) = CreateTwoPersonsWithOneAddress();

        var toShared = customer.NewMailTo(persons[0]) with { To = shared };
        var (saveDialog, _) = SendMail(mainPage, toShared);
        saveDialog.ShouldHaveContactState(RecipientRow, MailContactState.Linked);
        saveDialog.ShouldHaveContactState(RecipientRow + 1, MailContactState.Additional);
        saveDialog.ShouldHaveContactRows(3);
        saveDialog.ShouldBeSavable(true);
        saveDialog.Save();
        ContactsInAccessPathsShouldBe(mainPage, shared, persons);

        var copyToShared = customer.NewMail() with { Others = [new MailAddressee(persons[0], shared, MailAddresseeKind.Cc)] };
        (saveDialog, _) = SendMail(mainPage, copyToShared);
        saveDialog.ShouldHaveContactState(CopyRow, MailContactState.Linked);
        saveDialog.ShouldHaveContactState(CopyRow + 1, MailContactState.Additional);
        saveDialog.ShouldHaveContactRows(4);
        saveDialog.Cancel();

        SenderOfTwoContactsShouldGiveAdditionalRow(mainPage, customer, expected: false);
    }

    /// <summary>
    /// Алгоритм для адреси кількох контактів, варіант 3 — усі головні й усі додаткові контакти для всіх адрес листа,
    /// зокрема відправника (підпис «… inklusive aller Empfänger von Kopien (CC)» перекладено неправильно). Лист новій
    /// людині з адресою двох персон у копії — обидві персони, у «Zugriffspfade» — обидві. Тимчасова персона з адресою
    /// відправника — ще й рядок «Kontakt» з нею (адресу тест одразу прибирає).
    /// </summary>
    [Test]
    [AllureSeverity(SeverityLevel.normal)]
    public void DokumentationNavigator_MailToAddressOfTwoContactsForAllAddresses()
    {
        SetMailOptionsAndRestart(options => options with { MultipleContacts = MailMultipleContactsMode.All, SalutationUser = false });
        var (mainPage, customer, shared, persons) = CreateTwoPersonsWithOneAddress();

        var mail = customer.NewMail() with { Others = [new MailAddressee(persons[0], shared, MailAddresseeKind.Cc)] };
        var (saveDialog, _) = SendMail(mainPage, mail);
        saveDialog.ShouldHaveContactState(CopyRow, MailContactState.Linked);
        saveDialog.ShouldHaveContactState(CopyRow + 1, MailContactState.Additional);
        saveDialog.ShouldHaveContactRows(4);
        CreateRecipient(saveDialog, mail);
        saveDialog.Save();

        var paths = mainPage.WaitForOpenedMail(mail.To).ReadAccessPaths();
        AllureApi.Step("Перевірити: обидві персони з адресою копії — у «Zugriffspfade»", () =>
            paths.Contacts.ShouldBe(
                [mainPage.UserDisplayName, mail.Recipient.SortName, .. persons.Select(person => person.SortName)],
                ignoreOrder: true,
                "Контакти в «Zugriffspfade»"));

        SenderOfTwoContactsShouldGiveAdditionalRow(mainPage, customer, expected: true);
    }

    /// <summary>
    /// Адреса відправника в другого контакту: тест створює тимчасову персону з адресою облікового запису Outlook
    /// (у навігаторі Kontakte, новою вкладкою), надсилає лист новій людині й дивиться, чи з'явився для відправника
    /// рядок «Kontakt» з цією персоною. Лист не зберігає (щоб персона ні з чим не зв'язалася), а адресу з персони
    /// одразу видаляє: інакше ADAICA впізнавала б її в листах користувача. Якщо тест упаде раніше, адресу видалить
    /// прибирання.
    /// </summary>
    private void SenderOfTwoContactsShouldGiveAdditionalRow(MainPage mainPage, CustomerCase customer, bool expected) =>
        AllureApi.Step(expected
                ? "Адреса відправника є ще в одному контакті — рядок «Kontakt» з ним"
                : "Адреса відправника є ще в одному контакті — рядка «Kontakt» немає",
            () =>
            {
                var account = OutlookApplication.DefaultAccountAddress();
                var senderTwin = customer.NewMailPerson();
                mainPage.OpenNavigator("Kontakte");
                CreatePersonWithEmail(mainPage, senderTwin, account);
                var removed = false;
                OnCleanup(() =>
                {
                    if (!removed)
                        RemoveEmailFromPerson(new MainPage(Session), senderTwin, account);
                });
                mainPage.OpenNavigator("Dokumentation");

                var mail = customer.NewMail();
                var (saveDialog, _) = SendMail(mainPage, mail);
                saveDialog.ShouldHaveContactState(SenderRow, MailContactState.Linked);
                if (expected)
                {
                    saveDialog.ShouldHaveContactState(RecipientRow + 1, MailContactState.Additional);
                    saveDialog.ShouldHaveContactRows(3);
                }
                else
                {
                    saveDialog.ShouldHaveContactRows(2);
                }

                saveDialog.Cancel();
                RemoveEmailFromPerson(mainPage, senderTwin, account);
                removed = true;
            });

    /// <summary>
    /// Видаляє адресу <paramref name="email"/> з картки персони: System → «Finden &amp; Erstellen» (пошук за адресою)
    /// → картка в окремому вікні → «Verbindung» → «Löschen».
    /// </summary>
    private static void RemoveEmailFromPerson(MainPage mainPage, EmployeeData person, string email) =>
        AllureApi.Step($"Видалити адресу {email} з персони {person.SortName}", () =>
        {
            var search = mainPage.OpenContactSearch();
            try
            {
                search.Email.Fill(email);
                var card = search.OpenFoundInWindow(person.SortName);
                new CommunicationsList(mainPage.Window, card, "communications").Delete(email);
                card.Close();
            }
            finally
            {
                search.CloseIfOpen();
            }
        });

    /// <summary>
    /// «Anreden von E-Mail-Empfängern hinzufügen» увімкнено: щойно в листі з'являються отримувачі, надбудова ADAICA
    /// дописує на початок тексту звертання. Невідомій адресі — «Sehr geehrte Damen und Herren,», адресі персони —
    /// її Briefanrede з картки. Листи тест не надсилає (зі звертанням Outlook падає під час надсилання), а закриває.
    /// </summary>
    [Test]
    [AllureSeverity(SeverityLevel.normal)]
    public void DokumentationNavigator_MailSalutation()
    {
        SetMailOptionsAndRestart(options => options with { SalutationUser = true, SalutationSystem = true });
        var mainPage = LogInAndOpenNavigator("Kontakte");
        EnsureOutlookAccount(mainPage, OutlookApplication.DefaultAccountAddress());
        var customer = CustomerCase.Random();
        var person = customer.NewMailPerson();
        var address = CustomerCase.NewMailAddress(person);
        var letterSalutation = CreatePersonWithEmail(mainPage, person, address);
        mainPage.OpenNavigator("Dokumentation");

        var unknown = customer.NewMail();
        TextShouldStartWith(ComposeAndDiscard(mainPage, unknown), "Sehr geehrte Damen und Herren,", unknown.Body);

        var known = customer.NewMailTo(person) with { To = address };
        TextShouldStartWith(ComposeAndDiscard(mainPage, known), letterSalutation, known.Body);
    }

    /// <summary>
    /// «Anreden von E-Mail-Empfängern hinzufügen» вимкнено для користувача (для системи — увімкнено): звертання
    /// надбудова не дописує, у листі лишається лише текст, який увів тест. Лист тест не надсилає, а закриває.
    /// </summary>
    [Test]
    [AllureSeverity(SeverityLevel.normal)]
    public void DokumentationNavigator_MailWithoutSalutation()
    {
        SetMailOptionsAndRestart(options => options with { SalutationUser = false, SalutationSystem = true });
        var mainPage = LogInAndOpenDokumentation();
        EnsureOutlookAccount(mainPage, OutlookApplication.DefaultAccountAddress());
        var mail = CustomerCase.Random().NewMail();

        var text = ComposeAndDiscard(mainPage, mail);
        AllureApi.Step("Перевірити: у тексті листа лише текст тесту, без звертання", () =>
            Normalized(text).ShouldBe(Normalized(mail.Body), "Текст листа в Outlook"));
    }

    /// <summary>
    /// Дві нові персони з однією адресою, створені в навігаторі Kontakte (Gesamtliste → «Neue Person»), далі —
    /// навігатор Dokumentation для листів.
    /// </summary>
    private (MainPage MainPage, CustomerCase Customer, string SharedAddress, EmployeeData[] Persons) CreateTwoPersonsWithOneAddress()
    {
        var mainPage = LogInAndOpenNavigator("Kontakte");
        EnsureOutlookAccount(mainPage, OutlookApplication.DefaultAccountAddress());
        var customer = CustomerCase.Random();
        EmployeeData[] persons = [customer.NewMailPerson(), customer.NewMailPerson()];
        var shared = CustomerCase.NewMailAddress(persons[0]);
        foreach (var person in persons)
            CreatePersonWithEmail(mainPage, person, shared);
        mainPage.OpenNavigator("Dokumentation");
        return (mainPage, customer, shared, persons);
    }

    /// <summary>
    /// Нова персона з адресою <paramref name="email"/>: Kontakte → Gesamtliste → «Neue Person» → «Finden &amp; Erstellen»
    /// (ім'я й e-mail) → «Neu» → «Neue Person/Firma» → OK. Повертає її Briefanrede.
    /// </summary>
    private string CreatePersonWithEmail(MainPage mainPage, EmployeeData person, string email) =>
        AllureApi.Step($"Створити персону {person.SortName} з e-mail {email}", () =>
        {
            var findCreateWindow = mainPage.OpenNewPersonWindow();
            OnCleanup(findCreateWindow.CloseIfOpen);
            findCreateWindow.Person.Fill(person.PrefilledName());
            findCreateWindow.Email.Fill(email);
            findCreateWindow.Action.WaitForText("Person erstellen");

            var personDialog = findCreateWindow.OpenNewPersonDialog();
            OnCleanup(personDialog.CancelIfOpen);
            personDialog.Salutation.Select(person.Salutation);
            var letterSalutation = personDialog.LetterSalutation.Text;
            personDialog.Save();
            findCreateWindow.WaitUntilClosed();
            return letterSalutation;
        });

    /// <summary>
    /// Відкриває новий лист з вузла «E-Mails», заповнює його й повертає текст, коли надбудова ADAICA закінчила його
    /// змінювати; лист закриває без надсилання.
    /// </summary>
    private string ComposeAndDiscard(MainPage mainPage, MailData mail)
    {
        var mailWindowsBefore = OutlookApplication.OpenMailWindowCount();
        OutlookApplication.OutlookMail outlookMail;
        using (ForegroundGuard.Pause())
        {
            mainPage.OpenNewMail();
            outlookMail = OutlookApplication.WaitForNewMail(mailWindowsBefore);
        }

        OnCleanup(outlookMail.DiscardIfNotSent);
        var text = outlookMail.Fill(mail.ToLine, mail.Subject, mail.Body, mail.CcLine);
        outlookMail.Discard();
        return text;
    }

    /// <summary>Контакти в «Zugriffspfade» листа до <paramref name="address"/>: користувач і <paramref name="persons"/>.</summary>
    private static void ContactsInAccessPathsShouldBe(MainPage mainPage, string address, params EmployeeData[] persons)
    {
        var paths = mainPage.WaitForOpenedMail(address).ReadAccessPaths();
        AllureApi.Step($"Перевірити контакти в «Zugriffspfade»: користувач, {string.Join(", ", persons.Select(person => person.SortName))}", () =>
            paths.Contacts.ShouldBe(
                [mainPage.UserDisplayName, .. persons.Select(person => person.SortName)],
                ignoreOrder: true,
                "Контакти в «Zugriffspfade»"));
    }

    private static void TextShouldStartWith(string text, string salutation, string body) =>
        AllureApi.Step($"Перевірити: лист починається зі звертання «{salutation}», далі текст тесту", () =>
            Normalized(text).ShouldBe(Normalized($"{salutation} {body}"), "Текст листа в Outlook"));

    // Outlook розставляє в тексті власні переноси й пробіли (порожній рядок після звертання — « \r\n»), тож порівнюємо слова.
    private static string Normalized(string text) =>
        string.Join(" ", text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
}
