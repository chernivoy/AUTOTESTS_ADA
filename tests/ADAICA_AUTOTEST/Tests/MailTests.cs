using ADAICA_AUTOTEST.Infrastructure;
using ADAICA_AUTOTEST.Pages;
using Allure.Net.Commons;
using Allure.Net.Commons.Attributes;

namespace ADAICA_AUTOTEST.Tests;

/// <summary>
/// Листи через Outlook. ADAICA не надсилає пошту сама: новий лист вона відкриває в Outlook, а надісланий пропонує
/// зберегти й зв'язати з контактами. Outlook зазвичай відкритий у користувача, тож тест чіпає в ньому лише свій лист,
/// а адреси отримувачів — на домені example.com, куди листи не доходять. Збережені листи й персони лишаються в базі.
/// </summary>
[AllureSuite("Навігатори")]
public class MailTests : AdaicaTestBase
{
    // За скільки днів ADAICA синхронізує листи з Outlook («Innerhalb der letzten … Tage»).
    private const int SyncDays = 30;

    /// <summary>
    /// Новий лист новій людині з навігатора Dokumentation. Якщо поштового облікового запису ще немає, тест спершу
    /// додає обліковий запис Outlook (Benutzereinstellungen → Maileinstellungen: стандартні теки, листи за 30 днів).
    /// Далі вузол «E-Mails» → «Neu» відкриває лист в Outlook; тест вписує унікальну адресу @example.com, тему й текст
    /// і надсилає лист. ADAICA пропонує його зберегти: в асистенті збереження отримувач невідомий («?»), і тест
    /// створює для нього персону через «Zugehörigen Kontakt finden/erstellen». Збережений лист має показати персону
    /// отримувачем, а у вузлі «Kontakte» під листом — відправника й нову персону.
    /// </summary>
    [Test]
    [AllureSeverity(SeverityLevel.critical)]
    public void DokumentationNavigator_NewMailToNewContact()
    {
        var mainPage = LogInAndOpenDokumentation();
        var mail = CustomerCase.Random().NewMail();
        var recipient = mail.Recipient;
        var account = OutlookApplication.DefaultAccountAddress();

        EnsureOutlookAccount(mainPage, account);

        var mailWindowsBefore = OutlookApplication.OpenMailWindowCount();
        mainPage.OpenNewMail();
        var outlookMail = OutlookApplication.WaitForNewMail(mailWindowsBefore);
        // Якщо тест упаде до надсилання, вікно листа закриється без чернетки.
        OnCleanup(outlookMail.DiscardIfNotSent);
        outlookMail.Fill(mail.To, mail.Subject, mail.Body);
        var sentFrom = DateTime.Now;
        outlookMail.Send();

        var saveDialog = mainPage.ConfirmSaveSentMail();
        OnCleanup(saveDialog.CancelIfOpen);
        saveDialog.ShouldHaveSubjects(mail.Subject);
        // Отримувача ADAICA не знайшла серед контактів, а без зв'язку з контактом лист не зберегти.
        saveDialog.OkButton.ShouldBeDisabled();

        // Рядки на вкладці «Kontakte»: «Von» (користувач), далі «An» — отримувач.
        var findCreateWindow = saveDialog.OpenFindCreateContact(contactRow: 1);
        OnCleanup(findCreateWindow.CloseIfOpen);
        findCreateWindow.Person.Fill($"{recipient.FirstName} {recipient.LastName}");
        findCreateWindow.Email.Fill(mail.To);
        // Дію під рядком пошуку ADAICA визначає за введеними даними, не одразу після введення.
        findCreateWindow.Action.WaitForText("Person erstellen");

        var personDialog = findCreateWindow.OpenNewPersonDialog();
        OnCleanup(personDialog.CancelIfOpen);
        AllureApi.Step("Перевірити персону й e-mail, взяті з рядка пошуку", () =>
            personDialog.ShouldSatisfyAllConditions(
                () => personDialog.Person.ShouldHaveText(recipient.SortName),
                () => personDialog.Email.ShouldHaveText(mail.To)));
        personDialog.Salutation.Select(recipient.Salutation);
        personDialog.Save();
        findCreateWindow.WaitUntilClosed();

        saveDialog.Save();
        var sentTo = DateTime.Now;

        mainPage.WaitForOpenedMail(mail.To).ShouldHaveValues(mail, account, sentFrom, sentTo);
        var contacts = mainPage.OpenSelectedRecordContacts();
        contacts.ShouldHaveContact("(An)", recipient.SortName, mail.To);
        contacts.ShouldHaveContact("(Von)", mainPage.UserDisplayName, account);
    }

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
