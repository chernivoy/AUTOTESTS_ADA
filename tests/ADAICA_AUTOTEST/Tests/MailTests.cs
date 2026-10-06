using ADAICA_AUTOTEST.Controls;
using ADAICA_AUTOTEST.Infrastructure;
using ADAICA_AUTOTEST.Pages;
using Allure.Net.Commons;
using Allure.Net.Commons.Attributes;

namespace ADAICA_AUTOTEST.Tests;

/// <summary>
/// Листи через Outlook. ADAICA не надсилає пошту сама: новий лист вона відкриває в Outlook, а надісланий пропонує
/// зберегти й зв'язати з контактами та записами («E-Mail-Speicherndialog», <see cref="MailSaveDialog"/>). Outlook
/// зазвичай відкритий у користувача, тож тест чіпає в ньому лише свій лист, а адреси — у домені example.com, куди
/// листи не доходять. Збережені листи, персони й записи лишаються в базі.
/// </summary>
[AllureSuite("Навігатори")]
public partial class MailTests : AdaicaTestBase
{
    // За скільки днів ADAICA синхронізує листи з Outlook («Innerhalb der letzten … Tage»).
    private const int SyncDays = 30;

    // Рядки вкладки «Kontakte» у вікні збереження листа: спершу відправник, далі отримувачі, далі копія.
    private const int SenderRow = 0;
    private const int RecipientRow = 1;
    private const int CopyRow = 2;

    /// <summary>
    /// Новий лист новій людині з навігатора Dokumentation. Якщо поштового облікового запису ще немає, тест спершу
    /// додає обліковий запис Outlook (Benutzereinstellungen → Maileinstellungen: стандартні теки, листи за 30 днів).
    /// Вузол «E-Mails» → «Neu» відкриває лист в Outlook; тест вписує унікальну адресу @…example.com, тему й текст
    /// і надсилає лист. У вікні збереження відправник — користувач, отримувач невідомий («?»), і лист не зберегти.
    /// Для отримувача тест створює персону через «Zugehörigen Kontakt finden/erstellen»: ім'я ADAICA бере з адреси
    /// сама. Збережений лист показує персону отримувачем; у «Zugriffspfade» — вузол «E-Mails» і обидва контакти;
    /// у «Kontakte» під листом — нова персона з адресою листа (ADAICA додала її сама); у «Notizen» — автоматичний
    /// інфозапис про створення листа.
    /// </summary>
    [Test]
    [AllureSeverity(SeverityLevel.critical)]
    public void DokumentationNavigator_NewMailToNewContact()
    {
        TurnOffSalutationForSending();
        var mainPage = LogInAndOpenDokumentation();
        var mail = CustomerCase.Random().NewMail();
        var recipient = mail.Recipient;
        var account = OutlookApplication.DefaultAccountAddress();

        EnsureOutlookAccount(mainPage, account);

        var (saveDialog, sentFrom) = SendMail(mainPage, mail);
        saveDialog.ShouldBeSavable(false);
        saveDialog.NoContactAssignment.ShouldBeChecked(false);
        saveDialog.ShouldHaveContactState(SenderRow, MailContactState.Linked);
        saveDialog.ShouldHaveContactState(RecipientRow, MailContactState.Unknown);

        CreateRecipient(saveDialog, mail);
        saveDialog.ShouldHaveContactState(RecipientRow, MailContactState.Linked);
        saveDialog.ShouldBeSavable(true);

        saveDialog.Save();
        var sentTo = DateTime.Now;

        var mailView = mainPage.WaitForOpenedMail(mail.To);
        mailView.ShouldHaveValues(mail, account, sentFrom, sentTo);
        var paths = mailView.ReadAccessPaths();
        AllureApi.Step("Перевірити «Zugriffspfade»: лист у вузлі «E-Mails», контакти — користувач і нова персона", () =>
            paths.ShouldSatisfyAllConditions(
                () => paths.Records.ShouldBe([["ROOT EMAIL"]], "Записи в «Zugriffspfade»"),
                () => paths.Contacts.ShouldBe([mainPage.UserDisplayName, recipient.SortName], ignoreOrder: true, "Контакти в «Zugriffspfade»"),
                () => paths.Objects.ShouldBeEmpty("Об'єкти в «Zugriffspfade»")));

        var mailNode = mail.NodeName(ParseSentDate(mailView), mainPage.UserDisplayName);
        var contacts = mainPage.OpenRecordFolder(mailNode, "Kontakte");
        ContactRowShouldBe(contacts, "(Von)", mainPage.UserDisplayName, account, $"E-Mail: {account}");
        // Адресу листа ADAICA сама додала новій персоні, хоч у «Finden & Erstellen» її не вводили.
        ContactRowShouldBe(contacts, "(An)", recipient.SortName, mail.To, $"E-Mail: {mail.To}");

        var note = mainPage.OpenRecordFolder(mailNode, "Notizen").WaitForRow("Name", "Dokumentations-Element wurde erstellt.");
        AllureApi.Step("Перевірити автоматичний інфозапис про створення листа", () =>
            note.ShouldSatisfyAllConditions(
                () => note.GetValueOrDefault("Klasse").ShouldBe("E-Mail", "Колонка «Klasse»"),
                () => note.GetValueOrDefault("Unterklasse").ShouldBe("E-Mail erstellt", "Колонка «Unterklasse»"),
                () => note.GetValueOrDefault("Systemnotiz").ShouldBe("Automatischer Eintrag", "Колонка «Systemnotiz»"),
                () => note.GetValueOrDefault("Bemerkung").ShouldNotBeNull().ShouldMatch(@"^Die E-Mail ""\d{12}MAIL\d{6}"" wurde erstellt\.$", "Колонка «Bemerkung»"),
                () => note.GetValueOrDefault("Erstellt durch").ShouldBe(mainPage.UserDisplayName, "Колонка «Erstellt durch»")));
    }

    /// <summary>
    /// Стани адрес у вікні збереження листа. Лист іде новій людині з копією (CC) ще одній. Спершу відправник
    /// зв'язаний з користувачем, отримувач і копія невідомі, лист не зберегти. Кнопка «CC» ігнорує копію. Кнопка AUTO
    /// вмикає «Keine Kontaktzuordnung» (адреси без контакту) — лист можна зберегти; «Manuell» повертає отримувача
    /// в невідомі, «Ignorieren» його ігнорує (лист і так не зберегти: потрібен хоча б один отримувач), «Wiederherstellen»
    /// повертає. Стан кожної адреси тест читає з контекстного меню її рядка. Наприкінці «Abbrechen»: лист не
    /// зберігається, а «Keine Kontaktzuordnung» ADAICA не запам'ятовує.
    /// </summary>
    [Test]
    [AllureSeverity(SeverityLevel.normal)]
    public void DokumentationNavigator_MailSaveDialogContactStates()
    {
        TurnOffSalutationForSending();
        var mainPage = LogInAndOpenDokumentation();
        var mail = CustomerCase.Random().NewMail(copies: 1);

        EnsureOutlookAccount(mainPage, OutlookApplication.DefaultAccountAddress());

        var (saveDialog, _) = SendMail(mainPage, mail);
        saveDialog.ShouldBeSavable(false);
        saveDialog.NoContactAssignment.ShouldBeChecked(false);
        saveDialog.ShouldHaveContactState(SenderRow, MailContactState.Linked);
        saveDialog.ShouldHaveContactState(RecipientRow, MailContactState.Unknown);
        saveDialog.ShouldHaveContactState(CopyRow, MailContactState.Unknown);

        saveDialog.IgnoreCopyRecipients();
        saveDialog.ShouldHaveContactState(CopyRow, MailContactState.Ignored);
        saveDialog.ShouldHaveContactState(RecipientRow, MailContactState.Unknown);
        saveDialog.ShouldBeSavable(false);

        saveDialog.AssignNoContacts();
        saveDialog.NoContactAssignment.ShouldBeChecked(true);
        saveDialog.ShouldHaveContactState(RecipientRow, MailContactState.NoContact);
        saveDialog.ShouldBeSavable(true);

        saveDialog.ChooseInContactMenu(RecipientRow, "Manuell");
        saveDialog.ShouldHaveContactState(RecipientRow, MailContactState.Unknown);
        saveDialog.NoContactAssignment.ShouldBeChecked(false);
        saveDialog.ShouldBeSavable(false);

        saveDialog.ChooseInContactMenu(RecipientRow, "Ignorieren");
        saveDialog.ShouldHaveContactState(RecipientRow, MailContactState.Ignored);
        saveDialog.ShouldBeSavable(false);

        saveDialog.ChooseInContactMenu(RecipientRow, "Wiederherstellen");
        saveDialog.ShouldHaveContactState(RecipientRow, MailContactState.Unknown);
        saveDialog.ShouldBeSavable(false);

        saveDialog.Cancel();
    }

    /// <summary>
    /// Другий лист уже відомій людині, але з нової адреси, — і зв'язки з записами. Тест створює завдання й першим
    /// листом — персону. У вікні збереження другого листа отримувача тест знаходить через «Zugehörigen Kontakt
    /// finden/erstellen» → «Finden» (за першою адресою) → OK; на вкладці «Dokumentation» лупою додає завдання,
    /// а «+» → «Notiz» — новий інфозапис. Збережений лист показує персону з новою адресою; у «Zugriffspfade» —
    /// вузол «E-Mails», завдання (у «ROOT TASK») та інфозапис; у «Kontakte» під листом — персона, у якої тепер
    /// обидві адреси (нову ADAICA додала сама).
    /// </summary>
    [Test]
    [AllureSeverity(SeverityLevel.critical)]
    public void DokumentationNavigator_MailToExistingContactWithRecords()
    {
        TurnOffSalutationForSending();
        var mainPage = LogInAndOpenDokumentation();
        var customer = CustomerCase.Random();
        var account = OutlookApplication.DefaultAccountAddress();

        var taskDialog = mainPage.OpenNewTaskDialog();
        OnCleanup(taskDialog.CancelIfOpen);
        var task = customer.NewTask();
        taskDialog.Form.Fill(task);
        var taskNumber = taskDialog.Form.Number.Text;
        taskDialog.Save();
        mainPage.WaitForOpenedTask(taskNumber);

        EnsureOutlookAccount(mainPage, account);

        var firstMail = customer.NewMail();
        var recipient = firstMail.Recipient;
        var (firstDialog, _) = SendMail(mainPage, firstMail);
        CreateRecipient(firstDialog, firstMail);
        firstDialog.Save();
        mainPage.WaitForOpenedMail(firstMail.To);

        var mail = customer.NewMailTo(recipient);
        var (saveDialog, sentFrom) = SendMail(mainPage, mail);
        saveDialog.ShouldHaveContactState(RecipientRow, MailContactState.Unknown);

        var findCreateWindow = saveDialog.OpenFindCreateContact(RecipientRow);
        OnCleanup(findCreateWindow.CloseIfOpen);
        findCreateWindow.Person.ShouldHaveText(mail.PrefilledName);
        findCreateWindow.ChooseFound(firstMail.To);
        saveDialog.ShouldHaveContactState(RecipientRow, MailContactState.Linked);

        saveDialog.LinkRecord(taskNumber, taskNumber);
        var noteDialog = saveDialog.OpenNewNote();
        OnCleanup(noteDialog.CancelIfOpen);
        var note = customer.NewNote();
        noteDialog.Form.Fill(note);
        noteDialog.Save();

        saveDialog.ShouldBeSavable(true);
        saveDialog.Save();
        var sentTo = DateTime.Now;

        var mailView = mainPage.WaitForOpenedMail(mail.To);
        mailView.ShouldHaveValues(mail, account, sentFrom, sentTo);
        var paths = mailView.ReadAccessPaths();
        AllureApi.Step("Перевірити «Zugriffspfade»: вузол «E-Mails», завдання, інфозапис і обидва контакти", () =>
            paths.ShouldSatisfyAllConditions(
                () => paths.Records.ShouldBe(
                    [["ROOT EMAIL"], [task.Name, "ROOT TASK"], [NoteNode(note)]], ignoreOrder: true, "Записи в «Zugriffspfade»"),
                () => paths.Contacts.ShouldBe([mainPage.UserDisplayName, recipient.SortName], ignoreOrder: true, "Контакти в «Zugriffspfade»"),
                () => paths.Objects.ShouldBeEmpty("Об'єкти в «Zugriffspfade»")));

        // У колонці «E-Mail» ADAICA показує першу адресу контакту й «,...», якщо адрес кілька.
        var contacts = mainPage.OpenRecordFolder(mail.NodeName(ParseSentDate(mailView), mainPage.UserDisplayName), "Kontakte");
        ContactRowShouldBe(contacts, "(An)", recipient.SortName, mail.To, $"E-Mail: {firstMail.To},...");
    }

    /// <summary>
    /// Відкриває новий лист з вузла «E-Mails» в Outlook, заповнює й надсилає його, а на питання ADAICA про збереження
    /// відповідає «Ja». Повертає вікно збереження листа й час перед надсиланням.
    /// </summary>
    private (MailSaveDialog Dialog, DateTime SentFrom) SendMail(MainPage mainPage, MailData mail)
    {
        var mailWindowsBefore = OutlookApplication.OpenMailWindowCount();
        OutlookApplication.OutlookMail outlookMail;
        // Поки ADAICA налаштовує вікно листа, воно має лишатися активним (див. OutlookApplication.WaitForNewMail).
        using (ForegroundGuard.Pause())
        {
            mainPage.OpenNewMail();
            outlookMail = OutlookApplication.WaitForNewMail(mailWindowsBefore);
        }

        // Якщо тест упаде до надсилання, вікно листа закриється без чернетки.
        OnCleanup(outlookMail.DiscardIfNotSent);
        outlookMail.Fill(mail.ToLine, mail.Subject, mail.Body, mail.CcLine);
        var sentFrom = DateTime.Now;
        outlookMail.Send();

        var saveDialog = mainPage.ConfirmSaveSentMail();
        // «Abbrechen» не зберігає ні листа, ні «Keine Kontaktzuordnung».
        OnCleanup(saveDialog.CancelIfOpen);
        saveDialog.ShouldHaveSubjects(mail.Subject);
        return (saveDialog, sentFrom);
    }

    /// <summary>
    /// Нова персона для отримувача: «Zugehörigen Kontakt finden/erstellen» → «Finden &amp; Erstellen» (ім'я ADAICA
    /// вписує з адреси «vorname.nachname@…» сама, e-mail — ні) → «Neu» → «Neue Person/Firma» → OK.
    /// </summary>
    private void CreateRecipient(MailSaveDialog saveDialog, MailData mail) => CreateContactForRow(saveDialog, RecipientRow, mail);

    /// <summary>
    /// Нова персона для адреси в рядку <paramref name="contactRow"/>: «Zugehörigen Kontakt finden/erstellen» →
    /// «Finden &amp; Erstellen» → «Neu» → «Neue Person/Firma» → OK. Чия це адреса, тест дізнається з імені, яке ADAICA
    /// вписує в рядок пошуку з адреси «vorname.nachname@…» (порядок отримувачів у списку визначає ADAICA).
    /// Повертає адресата, для якого створено персону.
    /// </summary>
    private MailAddressee CreateContactForRow(MailSaveDialog saveDialog, int contactRow, MailData mail)
    {
        var findCreateWindow = saveDialog.OpenFindCreateContact(contactRow);
        OnCleanup(findCreateWindow.CloseIfOpen);
        var prefilled = findCreateWindow.Person.Text;
        var addressee = mail.Addressees.SingleOrDefault(candidate => candidate.Person.PrefilledName() == prefilled)
            ?? throw new InvalidOperationException(
                $"У рядку пошуку «{prefilled}», а серед адресатів листа такої людини немає: {string.Join("; ", mail.Addressees.Select(candidate => candidate.Address))}.");
        AllureApi.Step($"Рядок {contactRow + 1} — {addressee.Address}: в рядку пошуку ім'я з адреси, e-mail порожній", () =>
            findCreateWindow.Email.ShouldHaveText(string.Empty));

        var personDialog = findCreateWindow.OpenNewPersonDialog();
        OnCleanup(personDialog.CancelIfOpen);
        personDialog.Person.ShouldHaveText(addressee.Person.SortName);
        personDialog.Salutation.Select(addressee.Person.Salutation);
        personDialog.Save();
        findCreateWindow.WaitUntilClosed();
        return addressee;
    }

    /// <summary>
    /// Вимикає «Anreden von E-Mail-Empfängern hinzufügen» для користувача й перезапускає ADAICA; після тесту опція
    /// повертається. Звертання, яке надбудова ADAICA дописує в лист, валить Outlook під час надсилання
    /// (RICHED20.DLL, приблизно кожен 3–5-й лист; без звертання 12 листів поспіль — без аварій, перевірено 06.10.2026),
    /// тож тести, що надсилають листи, працюють без нього. Саме звертання перевіряє <see cref="DokumentationNavigator_MailSalutation"/>
    /// без надсилання.
    /// </summary>
    private void TurnOffSalutationForSending() =>
        SetMailOptionsAndRestart(options => options with { SalutationUser = false });

    /// <summary>
    /// Рядок контакту під листом: функція, ім'я, адреса, з якою контакт у листі («Historisch: Kommunikationsnummer»),
    /// і адреси контакту («E-Mail»).
    /// </summary>
    private static void ContactRowShouldBe(RecordList contacts, string function, string name, string mailAddress, string contactEmails)
    {
        var row = contacts.WaitForRow("Historisch: Kommunikationsnummer", mailAddress);
        AllureApi.Step($"Перевірити контакт листа {function} {name}", () =>
            row.ShouldSatisfyAllConditions(
                () => row.GetValueOrDefault("Kontaktart (Funktion)").ShouldBe(function, "Колонка «Kontaktart (Funktion)»"),
                () => row.GetValueOrDefault("Name").ShouldBe(name, "Колонка «Name»"),
                () => row.GetValueOrDefault("E-Mail").ShouldBe(contactEmails, "Колонка «E-Mail»"),
                () => row.GetValueOrDefault("Historisch: Kommunikationstyp").ShouldBe("E-Mail", "Колонка «Historisch: Kommunikationstyp»")));
    }

    // Дата з «Versanddatum» листа: «05.10.2026 16:09».
    private static DateTime ParseSentDate(MailView mailView) => AdaicaFormat.ParseDate(mailView.SentAt.Split(' ')[0])!.Value;

    /// <summary>
    /// Додає обліковий запис Outlook <paramref name="account"/>, якщо поштового запису в користувача ще немає.
    /// Запис ADAICA зберігає для користувача, і він потрібен йому для роботи, тож наявний тест не змінює й не видаляє.
    /// </summary>
    private void EnsureOutlookAccount(MainPage mainPage, string account)
    {
        var wizard = mainPage.OpenMailSettings();
        OnCleanup(wizard.CancelIfOpen);
        if (wizard.HasAccount)
        {
            AllureApi.Step("Поштовий обліковий запис уже налаштовано («Kontoeinstellungen ändern» доступна) — без змін");
            wizard.Cancel();
            return;
        }

        wizard.AddOutlookAccount(account, SyncDays);
    }
}
