namespace ADAICA_AUTOTEST.Pages;

/// <summary>Шаблон завдання: назва шаблону, поля завдання й «Verantwortlich», як його показує бланк («Goldun, Anna»).</summary>
public sealed record TaskTemplateData(string Name, TaskData Task, string Responsible);
