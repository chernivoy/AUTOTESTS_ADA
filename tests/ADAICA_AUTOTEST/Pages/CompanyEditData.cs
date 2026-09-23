namespace ADAICA_AUTOTEST.Pages;

/// <summary>Усе, що тест змінює в картці збереженої фірми: вкладки Allgemein, Details і Bankverbindungen.</summary>
public sealed record CompanyEditData(
    CompanyGeneralData General,
    CompanyDetailsData Details,
    BankAccountData BankAccount);

/// <summary>
/// Вкладка Allgemein. Рядки назви, рядок адреси й звертання задаються у вікні «Firmendetails» (кнопка «...» біля назви).
/// </summary>
/// <param name="Category">Категорія фірми з вікна «Kategorie»; ADAICA дописує її до вузла в дереві.</param>
/// <param name="ElectronicContact">«Bevorzugt elektronische Kontaktaufnahme»: «?», «Ja» чи «Nein».</param>
public sealed record CompanyGeneralData(
    string NameLine2,
    string NameLine3,
    string AddressLine,
    string Salutation,
    string CompanyType,
    string TradeRegister,
    string TradeRegisterNumber,
    string TaxNumber,
    string VatNumber,
    IReadOnlyList<CommunicationData> Communications,
    AddressData Address,
    string Remark,
    string Category,
    string CorrespondencePreference,
    string ElectronicContact,
    bool NoAdvertisingEmails,
    bool NoAdvertisingLetters,
    bool NoAdvertisingCalls,
    bool PrivacyStatementOk);

/// <summary>
/// Нова адреса у списку «Adressen». Адресу створюють у вікні «Finden &amp; Erstellen»: якщо така вже є в довіднику адрес,
/// ADAICA її знаходить і «Neu» недоступна, тому для нової адреси тест бере інший номер будинку.
/// </summary>
/// <param name="Category">Пункт меню «+» біля «Adressen» і колонка «Kategorie»: Straßenanschrift, Lieferanschrift, Zweigstelle, …</param>
/// <param name="Country">Єдина країна в довіднику адрес тестової бази — Deutschland.</param>
/// <param name="District">Ortsteil.</param>
public sealed record AddressData(
    string Category,
    string Country,
    string State,
    string PostalCode,
    string City,
    string District,
    string Street,
    string HouseNumber)
{
    /// <summary>
    /// Рядок у списку «Adressen»: Kategorie, Land, Postleitzahl, Ort, Ortsteil, Straße, Hausnummer.
    /// Bundesland список не показує.
    /// </summary>
    public string ListRow => string.Join(" | ", Category, Country, PostalCode, City, District, Street, HouseNumber);

    public override string ToString() => $"{Street} {HouseNumber}, {PostalCode} {City}";
}

/// <summary>Рядок у списку «Verbindung».</summary>
/// <param name="Kind">Пункт меню «+» і група в списку: Telefon, Fax, E-Mail, Webseite, Sonstige.</param>
/// <param name="Type">Тип у межах групи, наприклад «Telefon Zentrale».</param>
/// <param name="Remark">Примітка; порожній рядок, якщо її немає.</param>
public sealed record CommunicationData(
    string Kind,
    string Value,
    string Type,
    string Remark);

/// <summary>Вкладка Details. «Branche» не задаємо: довідник галузей у тестовій базі порожній, а вільний текст ADAICA не зберігає.</summary>
public sealed record CompanyDetailsData(
    decimal Turnover,
    string TurnoverCurrency,
    string Creditworthiness);

/// <summary>
/// Рахунок на вкладці Bankverbindungen. Назву банку, SWIFT-BIC і BLZ вручну ввести не можна: їх заповнює вибір банку
/// з довідника у вікні рахунку, тому тест шукає банк за <paramref name="BankCode"/>.
/// </summary>
public sealed record BankAccountData(
    string Holder,
    string Iban,
    string BankCode,
    string BankName,
    string Bic,
    string AccountNumber,
    bool IsPrimary);
