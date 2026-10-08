namespace ADAICA_AUTOTEST.Pages;

/// <summary>
/// Рядок списку Papierkorb: видалений запис і звідки його видалено. Імена властивостей — колонки списку.
/// </summary>
/// <param name="Name">«Name»: назва запису без номера («Sanierung Heizung – Hotel Seeblick»), у документа — без розширення.</param>
/// <param name="Id">«ID»: внутрішній ID запису (<see cref="PropertiesWindow.Id"/>), а не номер PRO….</param>
/// <param name="ClassType">«Klassentyp»: PROJECT, OPERATION, TASK, APPOINT, HISTORY, BLOB.</param>
/// <param name="Date">«Datum»: коли запис видалено, «08.10.2026 10:52».</param>
/// <param name="User">«Anwender»: хто видалив, «Account: cher, Person: Chernivoy, Igor».</param>
/// <param name="RealUserId">«Eindeutiger Anwenderschlüssel (RealUserID)»: ID користувача, «260825162055USER000290».</param>
/// <param name="ParentClassType">«Klasse des übergeordneten Dokumentationselements».</param>
/// <param name="ParentId">«ID des übergeordneten Dokumentationselements»; у запису з Gesamtliste — «ROOT_PROJECT_000000000».</param>
/// <param name="ParentName">«Name des übergeordneten Dokumentationselements»: «Назва (Номер)» або «ROOT PROJECT».</param>
/// <param name="Person">«Person»: ID персони користувача, що видалив, «260825162047PERS959162» (не ім'я).</param>
/// <param name="ContactClassType">«Klasse des übergeordneten Kontaktes»: CONTACT, якщо запис пов'язано з контактом.</param>
/// <param name="ContactId">«ID des übergeordneten Kontaktes»: ID контакту (відповідального).</param>
/// <param name="ContactName">«Name des übergeordneten Kontaktes»: «Прізвище, Ім'я».</param>
/// <param name="ObjectClassType">«Klasse des übergeordneten Objektes» (запис навігатора Objekte).</param>
/// <param name="ObjectId">«ID des übergeordneten Objektes».</param>
/// <param name="ObjectName">«Name des übergeordneten Objektes».</param>
public sealed record RecycleBinEntry(
    string Name,
    string Id,
    string ClassType,
    string Date,
    string User,
    string RealUserId,
    string ParentClassType,
    string ParentId,
    string ParentName,
    string Person,
    string ContactClassType,
    string ContactId,
    string ContactName,
    string ObjectClassType,
    string ObjectId,
    string ObjectName)
{
    /// <summary>Підписи колонок списку в тому порядку, у якому їх показує ADAICA.</summary>
    public static readonly IReadOnlyList<string> Columns =
    [
        "Name",
        "ID",
        "Klassentyp",
        "Datum",
        "Anwender",
        "Eindeutiger Anwenderschlüssel (RealUserID)",
        "Klasse des übergeordneten Dokumentationselements",
        "ID des übergeordneten Dokumentationselements",
        "Name des übergeordneten Dokumentationselements",
        "Person",
        "Klasse des übergeordneten Kontaktes",
        "ID des übergeordneten Kontaktes",
        "Name des übergeordneten Kontaktes",
        "Klasse des übergeordneten Objektes",
        "ID des übergeordneten Objektes",
        "Name des übergeordneten Objektes",
    ];

    public static RecycleBinEntry FromCells(Func<string, string> cell) => new(
        cell(Columns[0]), cell(Columns[1]), cell(Columns[2]), cell(Columns[3]),
        cell(Columns[4]), cell(Columns[5]), cell(Columns[6]), cell(Columns[7]),
        cell(Columns[8]), cell(Columns[9]), cell(Columns[10]), cell(Columns[11]),
        cell(Columns[12]), cell(Columns[13]), cell(Columns[14]), cell(Columns[15]));

    public override string ToString() =>
        string.Join(Environment.NewLine, Columns.Zip(
            new[]
            {
                Name, Id, ClassType, Date, User, RealUserId, ParentClassType, ParentId, ParentName,
                Person, ContactClassType, ContactId, ContactName, ObjectClassType, ObjectId, ObjectName,
            },
            (column, value) => $"{column}: {value}"));
}
