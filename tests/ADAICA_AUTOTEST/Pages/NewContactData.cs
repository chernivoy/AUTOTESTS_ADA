namespace ADAICA_AUTOTEST.Pages;

/// <summary>
/// Фірма й персона, які тест створює разом: рядок пошуку «Finden &amp; Erstellen» з персоною, фірмою, адресою,
/// телефоном і e-mail, а потім вікно «Neue Person/Firma», де тест доповнює порожні поля.
/// Персона стає співробітником фірми у стандартному відділі «andere Tätigkeiten».
/// </summary>
/// <param name="TradeRegisterNumber">HRG-Nr. фірми.</param>
/// <param name="CorrespondencePreference">Korrespondenz-Präferenz: «Firma» чи «Primärer Briefempfänger».</param>
/// <param name="Address">Адреса фірми; <see cref="AddressData.Category"/> — «Straßenanschrift».</param>
/// <param name="Phone">Телефон персони (<see cref="CommunicationData.Type"/> — Kommunikationsart).</param>
/// <param name="Email">E-mail персони.</param>
public sealed record NewContactData(
    CompanyData Company,
    string TradeRegisterNumber,
    string CorrespondencePreference,
    EmployeeData Person,
    AddressData Address,
    CommunicationData Phone,
    CommunicationData Email)
{
    /// <summary>Відділ, до якого ADAICA додає персону нової фірми.</summary>
    public const string Department = "andere Tätigkeiten";
}
