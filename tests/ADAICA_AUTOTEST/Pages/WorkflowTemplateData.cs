namespace ADAICA_AUTOTEST.Pages;

/// <summary>
/// Шаблон workflow (<see cref="WorkflowForm"/>): назва й назва головного завдання на вкладці «Workflow», кроки —
/// на вкладках «N Schritt». Префікси ADAICA підставляє сама («Workflow», «Workflowaufgabe», «Schritt»), тож тести
/// їх зазвичай не змінюють. «Workflowname» — до 30 символів, і він має бути унікальним серед шаблонів.
/// </summary>
/// <remarks>
/// Назви з номером версії документа (<c>documentVersion</c>) — це назви workflow, запущеного на документі: до них
/// ADAICA дописує « - DV{версія документа} - WF1». Так називаються папка workflow, головне завдання й завдання кроків.
/// </remarks>
public sealed record WorkflowTemplateData(
    string Name,
    string MainTaskName,
    IReadOnlyList<WorkflowStepData> Steps,
    string Prefix = WorkflowTemplateData.DefaultPrefix,
    string MainTaskPrefix = WorkflowTemplateData.DefaultMainTaskPrefix,
    bool ConvertToPdf = false)
{
    public const string DefaultPrefix = "Workflow";
    public const string DefaultMainTaskPrefix = "Workflowaufgabe";

    /// <summary>Версія шаблону й workflow, запущеного з шаблону: завжди перша.</summary>
    public const string Version = "WF1";

    /// <summary>
    /// «Anzeigename» workflow і підпис головної вкладки: «Workflow - Назва», а запущеного — «Workflow - Назва - DV1 - WF1»
    /// (так названо й папку workflow під документом).
    /// </summary>
    public string DisplayName(int? documentVersion = null) =>
        documentVersion is { } version ? $"{Prefix} - {Name} - DV{version} - {Version}" : $"{Prefix} - {Name}";

    /// <summary>«Anzeigename» головного завдання: «Workflowaufgabe-Назва - WF1» (без пробілів біля першого дефіса).</summary>
    public string MainTaskDisplayName(int? documentVersion = null) => $"{MainTaskPrefix}-{MainTaskName}{Versions(documentVersion)}";

    public override string ToString() => $"«{Name}» ({Steps.Count} крок.)";

    // « - WF1» у шаблоні, « - DV1 - WF1» у запущеному workflow.
    internal static string Versions(int? documentVersion) =>
        documentVersion is { } version ? $" - DV{version} - {Version}" : $" - {Version}";
}

/// <summary>
/// Крок шаблону workflow: назва й «Prüfer» (хто отримає завдання кроку) — контакти, як їх підписує список кроку.
/// «Annahmeart» обов'язкова; ADAICA ставить «Bestätigung». <paramref name="Publication"/> — публікація документа після
/// кроку («Nach diesem Schritt veröffentlichen und verteilen»); її можна ввімкнути лише на одному кроці шаблону.
/// </summary>
public sealed record WorkflowStepData(
    string Name,
    IReadOnlyList<string> Reviewers,
    string Prefix = WorkflowStepData.DefaultPrefix,
    string Authority = WorkflowStepData.DefaultAuthority,
    WorkflowPublicationData? Publication = null)
{
    public const string DefaultPrefix = "Schritt";
    public const string DefaultAuthority = "Bestätigung";

    /// <summary>«Anzeigename» кроку й підпис його вкладки: «1 Schritt - Назва - WF1», у запущеному — «… - DV1 - WF1».</summary>
    public string DisplayName(int step, int? documentVersion = null) =>
        $"{step} {Prefix} - {Name}{WorkflowTemplateData.Versions(documentVersion)}";
}

/// <summary>
/// Публікація після кроку (<see cref="WorkflowPublicationPanel"/>): позначені «Verknüpft» записи дерева «DMS –
/// Veröffentlichungen» (папки чи один документ), прапорець кроку «Veröffentlichen als aktuelle Version einer früheren
/// Veröffentlichung» і розсилка в чат (з контактами — користувачами) та e-mail.
/// </summary>
/// <param name="Paths">Підписи рядків дерева, як їх показує ADAICA: «назва (OPE…)» для папки, ім'я файлу для документа.</param>
public sealed record WorkflowPublicationData(
    IReadOnlyList<string> Paths,
    bool AsActualVersion = false,
    bool Chat = false,
    bool Mail = false)
{
    /// <summary>Користувачі, яким ADAICA напише в чат про публікацію (якщо <see cref="Chat"/>).</summary>
    public IReadOnlyList<string> ChatContacts { get; init; } = [];
}
