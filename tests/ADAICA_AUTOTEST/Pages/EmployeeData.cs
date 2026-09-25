namespace ADAICA_AUTOTEST.Pages;

/// <summary>Співробітник фірми, якого тест створює у вікні «Neue Person/Firma».</summary>
/// <param name="Salutation">Anrede: «Frau» або «Herr».</param>
/// <param name="Function">Funktion у фірмі; ADAICA дописує її до вузла персони в дереві.</param>
/// <param name="Title">Titel, наприклад «Dr.»; порожній рядок, якщо його немає.</param>
/// <param name="NameSuffix">Namenszusatz перед прізвищем, наприклад «von»; порожній рядок, якщо його немає.</param>
/// <param name="Birthday">Geburtstag або null.</param>
/// <param name="MaidenName">Geburtsname; порожній рядок, якщо його немає.</param>
public sealed record EmployeeData(
    string FirstName,
    string LastName,
    string Salutation,
    string ShortName,
    string Function,
    string Title = "",
    string NameSuffix = "",
    DateTime? Birthday = null,
    string MaidenName = "")
{
    /// <summary>Як ADAICA записує персону з рядка пошуку «Vorname Nachname»: «Hartmann, Sabine».</summary>
    public string SortName => $"{LastName}, {FirstName}";

    /// <summary>Повне ім'я в картці персони: «Frau Dr. Claudia von Gerber».</summary>
    public string FullName => JoinWords(Salutation, Title, FirstName, NameSuffix, LastName);

    /// <summary>Вузол персони в дереві навігатора: «von Gerber, Claudia Dr. (Mitarbeiter)».</summary>
    public string NodeName => $"{JoinWords(NameSuffix, LastName)}, {JoinWords(FirstName, Title)} ({Function})";

    private static string JoinWords(params string[] words) => string.Join(" ", words.Where(word => word.Length > 0));
}
