namespace ADAICA_AUTOTEST.Pages;

/// <summary>Рядок результатів пошуку «Finden» на вкладці «Übersicht».</summary>
/// <param name="Number">«Nummer / Version», наприклад TAS0000028.</param>
/// <param name="ClassType">«Klassentyp»: TASK, APPOINT тощо.</param>
/// <param name="Name">«Betreff/Name».</param>
/// <param name="FoundIn">«Fundstelle» — поле, де знайдено слова, наприклад «Name (Volltextsuche)». На екрані колонка схована.</param>
/// <param name="Remark">«Bemerkung».</param>
public sealed record SearchResult(string Number, string ClassType, string Name, string FoundIn, string Remark);
