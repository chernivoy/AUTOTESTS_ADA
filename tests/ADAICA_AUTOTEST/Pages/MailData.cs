namespace ADAICA_AUTOTEST.Pages;

/// <summary>
/// Лист, який тест пише в Outlook. Адреса отримувача щоразу унікальна й на домені example.com, зарезервованому
/// для прикладів (RFC 2606), тож лист нікому не дійде, а свій лист і створену з нього персону тест упізнає за адресою.
/// </summary>
/// <param name="Recipient">Отримувач — нова персона, яку тест створює з листа.</param>
/// <param name="To">Адреса отримувача.</param>
public sealed record MailData(EmployeeData Recipient, string To, string Subject, string Body)
{
    /// <summary>Як ADAICA показує отримувача, зв'язаного з персоною: «Thomas Weber &lt;адреса&gt;».</summary>
    public string RecipientLine => $"{Recipient.FirstName} {Recipient.LastName} <{To}>";
}
