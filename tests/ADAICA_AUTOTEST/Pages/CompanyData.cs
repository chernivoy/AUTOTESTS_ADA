namespace ADAICA_AUTOTEST.Pages;

/// <summary>Поля нової фірми, які тест задає у вікнах «Finden &amp; Erstellen» і «Neue Person/Firma».</summary>
/// <param name="Name">Назва фірми, унікальна в базі (див. <c>CustomerCase.NewCompany</c>).</param>
public sealed record CompanyData(
    string Name,
    string ShortName);
