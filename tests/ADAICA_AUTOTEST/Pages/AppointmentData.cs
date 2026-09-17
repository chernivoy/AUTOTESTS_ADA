namespace ADAICA_AUTOTEST.Pages;

/// <summary>Поля терміну, які користувач може змінити.</summary>
/// <param name="Effort">
/// Трудовитрати («Aufwand (z.B. Arbeitszeit)»). Якщо null, прапорець вимкнено, і ADAICA сама показує
/// тривалість між Begin і End. Якщо значення відрізняється від цієї тривалості, ADAICA підсвічує поле попередженням.
/// </param>
public sealed record AppointmentData(
    string Name,
    string ShortName,
    DateTime Begin,
    DateTime End,
    bool AllDay,
    decimal? Effort,
    string EffortUnit,
    string Importance,
    string Location,
    string Remark);
