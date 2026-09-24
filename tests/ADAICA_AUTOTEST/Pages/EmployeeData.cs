namespace ADAICA_AUTOTEST.Pages;

/// <summary>Співробітник фірми, якого тест створює у вікні «Neue Person/Firma» з вузла відділу.</summary>
/// <param name="Salutation">Anrede: «Frau» або «Herr».</param>
/// <param name="Function">Funktion у фірмі; ADAICA дописує її до вузла персони в дереві.</param>
public sealed record EmployeeData(
    string FirstName,
    string LastName,
    string Salutation,
    string ShortName,
    string Function)
{
    /// <summary>Як ADAICA записує персону з рядка пошуку «Vorname Nachname»: «Hartmann, Sabine».</summary>
    public string SortName => $"{LastName}, {FirstName}";

    /// <summary>Повне ім'я в картці персони: «Frau Sabine Hartmann».</summary>
    public string FullName => $"{Salutation} {FirstName} {LastName}";

    /// <summary>Вузол персони в дереві навігатора: «Hartmann, Sabine (Mitarbeiter)».</summary>
    public string NodeName => $"{SortName} ({Function})";
}
