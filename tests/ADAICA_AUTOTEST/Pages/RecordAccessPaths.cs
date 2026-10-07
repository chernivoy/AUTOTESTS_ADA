namespace ADAICA_AUTOTEST.Pages;

/// <summary>
/// «Beziehungen» → «Zugriffspfade» запису: де в ADAICA лежить запис. Для листа тут видно все, що задано у вікні
/// збереження: записи Dokumentation, під якими лист (кожен — ланцюжком від запису до кореня, наприклад
/// «Назва завдання» ← «ROOT TASK»; вузол «E-Mails» — «ROOT EMAIL»), контакти й об'єкти.
/// </summary>
/// <param name="Records">Ланцюжки записів: назва запису, далі його предки до кореня.</param>
/// <param name="Contacts">Контакти: «Прізвище, Ім'я».</param>
/// <param name="Objects">Об'єкти: назви.</param>
public sealed record RecordAccessPaths(
    IReadOnlyList<IReadOnlyList<string>> Records,
    IReadOnlyList<string> Contacts,
    IReadOnlyList<string> Objects)
{
    public override string ToString() =>
        $"Dokumentation: {string.Join("; ", Records.Select(chain => string.Join(" ← ", chain)))}"
        + $"{Environment.NewLine}Kontakte: {string.Join("; ", Contacts)}"
        + $"{Environment.NewLine}Objekte: {string.Join("; ", Objects)}";
}
