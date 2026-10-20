namespace ADAICA_AUTOTEST.Pages;

/// <summary>
/// Рядок списку «Aufgabenvorlagen» (<see cref="TaskTemplatesWindow"/>): назва шаблону, коли й ким його створено та
/// востаннє змінено (з точністю до хвилини) і чи він в обраному користувача (зірка в першій колонці).
/// </summary>
public sealed record TaskTemplateRow(
    string Name,
    DateTime Created,
    string CreatedBy,
    DateTime Changed,
    string ChangedBy,
    bool IsFavorite);
