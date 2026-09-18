namespace ADAICA_AUTOTEST.Pages;

/// <summary>Рядок результатів пошуку «Finden» на вкладці «Übersicht».</summary>
/// <param name="Number">«Nummer / Version», наприклад TAS0000028; в інфозапису порожня.</param>
/// <param name="ClassType">«Klassentyp»: ADAICA показує технічний код — TASK, APPOINT, HISTORY, OPERATION, PROJECT.</param>
/// <param name="Name">«Betreff/Name».</param>
/// <param name="FoundIn">«Fundstelle» — поле, де знайдено слова, наприклад «Name (Volltextsuche)». На екрані колонка схована.</param>
/// <param name="FoundText">«Fundtext». На екрані колонка схована.</param>
/// <param name="Class">«Klasse».</param>
/// <param name="Subclass">«Unterklasse».</param>
/// <param name="Remark">«Bemerkung».</param>
/// <param name="Created">«Erstelldatum» у форматі «18.09.2026 08:48».</param>
/// <param name="Id">«ID», наприклад 260918084746TASK480974.</param>
public sealed record SearchResult(
    string Number,
    string ClassType,
    string Name,
    string FoundIn,
    string FoundText,
    string Class,
    string Subclass,
    string Remark,
    string Created,
    string Id);
