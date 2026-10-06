namespace ADAICA_AUTOTEST.Pages;

/// <summary>
/// Лист, який тест пише в Outlook. Адреси щоразу унікальні й у домені example.com, зарезервованому для прикладів
/// (RFC 2606), тож лист нікому не дійде, а свій лист і створену з нього персону тест упізнає за адресою.
/// </summary>
/// <param name="Recipient">Отримувач — персона, яку тест створює з листа чи бере наявну.</param>
/// <param name="To">Адреса отримувача: «vorname.nachname@t260930101500.example.com».</param>
/// <param name="Cc">Адреса в копії або порожній рядок.</param>
public sealed record MailData(EmployeeData Recipient, string To, string Subject, string Body, string Cc = "")
{
    /// <summary>
    /// Як ADAICA заповнює рядок пошуку «Finden &amp; Erstellen» з адреси «vorname.nachname@…»: «Vorname Nachname».
    /// </summary>
    public string PrefilledName => $"{Recipient.FirstName} {Recipient.LastName}";

    /// <summary>Як ADAICA показує отримувача, зв'язаного з персоною: «Thomas Weber &lt;адреса&gt;».</summary>
    public string RecipientLine => $"{Recipient.FirstName} {Recipient.LastName} <{To}>";

    /// <summary>
    /// Вузол листа в дереві навігатора: «05.10.2026  Тема Von: Прізвище, Ім'я;» — дата надсилання, два пробіли, тема
    /// й відправник.
    /// </summary>
    public string NodeName(DateTime sentAt, string sender) =>
        $"{sentAt.ToString("dd.MM.yyyy", System.Globalization.CultureInfo.InvariantCulture)}  {Subject} Von: {sender};";
}
