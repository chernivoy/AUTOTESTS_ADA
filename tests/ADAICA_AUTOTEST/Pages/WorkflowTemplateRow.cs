namespace ADAICA_AUTOTEST.Pages;

/// <summary>
/// Рядок списку «Workflowkonfiguration» (<see cref="WorkflowConfigWindow"/>): назва шаблону, хто й коли його створив
/// («Anwender», «Erstelldatum») і востаннє змінив («Geändert von», «Änderungsdatum»), з точністю до хвилини.
/// </summary>
public sealed record WorkflowTemplateRow(
    string Name,
    string CreatedBy,
    DateTime Created,
    string ChangedBy,
    DateTime Changed);
