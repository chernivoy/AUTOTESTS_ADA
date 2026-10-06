namespace ADAICA_AUTOTEST.Pages;

/// <summary>
/// Лист, який тест пише в Outlook. Адреси щоразу унікальні й у домені example.com, зарезервованому для прикладів
/// (RFC 2606), тож лист нікому не дійде, а свій лист і створені з нього персони тест упізнає за адресою.
/// </summary>
/// <param name="Recipient">Перший отримувач («An») — персона, яку тест створює з листа чи бере наявну.</param>
/// <param name="To">Адреса першого отримувача: «vorname.nachname@t260930101500.example.com».</param>
/// <param name="Others">Інші отримувачі «An» і копії «CC».</param>
public sealed record MailData(EmployeeData Recipient, string To, string Subject, string Body, IReadOnlyList<MailAddressee> Others)
{
    /// <summary>Усі адресати: перший отримувач, далі решта в тому порядку, як їх вписано в лист.</summary>
    public IReadOnlyList<MailAddressee> Addressees => [new(Recipient, To, MailAddresseeKind.To), .. Others];

    /// <summary>Поле «Кому» в Outlook: адреси через «; ».</summary>
    public string ToLine => Line(MailAddresseeKind.To);

    /// <summary>Поле «Копія» в Outlook або порожній рядок.</summary>
    public string CcLine => Line(MailAddresseeKind.Cc);

    /// <summary>
    /// Як ADAICA заповнює рядок пошуку «Finden &amp; Erstellen» з адреси «vorname.nachname@…»: «Vorname Nachname».
    /// </summary>
    public string PrefilledName => Recipient.PrefilledName();

    /// <summary>Як ADAICA показує першого отримувача, зв'язаного з персоною: «Thomas Weber &lt;адреса&gt;».</summary>
    public string RecipientLine => $"{Recipient.FirstName} {Recipient.LastName} <{To}>";

    /// <summary>
    /// Вузол листа в дереві навігатора: «05.10.2026  Тема Von: Прізвище, Ім'я;» — дата надсилання, два пробіли, тема
    /// й відправник.
    /// </summary>
    public string NodeName(DateTime sentAt, string sender) =>
        $"{sentAt.ToString("dd.MM.yyyy", System.Globalization.CultureInfo.InvariantCulture)}  {Subject} Von: {sender};";

    private string Line(MailAddresseeKind kind) =>
        string.Join("; ", Addressees.Where(addressee => addressee.Kind == kind).Select(addressee => addressee.Address));
}

/// <summary>Адресат листа: людина, її адреса й поле листа, де вона стоїть.</summary>
public sealed record MailAddressee(EmployeeData Person, string Address, MailAddresseeKind Kind);

public enum MailAddresseeKind
{
    /// <summary>«Кому» — в ADAICA «An».</summary>
    To,

    /// <summary>«Копія» — в ADAICA «CC».</summary>
    Cc,
}

public static class MailPersonExtensions
{
    /// <summary>Ім'я, яке ADAICA бере з адреси «vorname.nachname@…» для рядка пошуку: «Vorname Nachname».</summary>
    public static string PrefilledName(this EmployeeData person) => $"{person.FirstName} {person.LastName}";
}
