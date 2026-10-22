using ADAICA_AUTOTEST.Infrastructure;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;

namespace ADAICA_AUTOTEST.Pages;

/// <summary>
/// Вікно workflow (`wndWorkflow`) з бланком <see cref="WorkflowForm"/> і кнопками OK / Abbrechen. Те саме вікно
/// відкривається для шаблону з «Workflowkonfiguration» («Neu», «Bearbeiten», подвійний клік; модальне, дочірнє вікна
/// списку) і для запуску workflow на документі («Workflow starten» → шаблон; немодальне, дочірнє головного вікна,
/// бланк заповнено з шаблону). Заголовок нового шаблону — «Workflowkonfiguration», як у вікна списку, наявного шаблону
/// й запуску — «Workflowkonfiguration: назва шаблону». У вікна немає AutomationId, тож шукаємо його серед дочірніх
/// вікон власника за бланком. OK доступна, коли заповнено все обов'язкове й (для наявного шаблону) щось змінено.
/// Шаблон OK зберігає в транзакції вікна списку — у базу він потрапляє лише після OK чи «Übernehmen» того вікна;
/// запуск OK зберігає одразу.
/// </summary>
public sealed class WorkflowDialog : ChildDialog
{
    public const string NewTitle = WorkflowConfigWindow.Title;

    private const string FormId = "ctrWorkflowWndWorkflow";

    /// <param name="owner">Вікно «Workflowkonfiguration» (шаблон) або головне вікно (запуск на документі).</param>
    public WorkflowDialog(Window owner)
        : base(owner, FindIn, "workflow", okButtonId: "OK", cancelButtonId: "Abbrechen")
    {
        Form = new WorkflowForm(() => Root.FindByAutomationId(FormId), Root);
    }

    public WorkflowForm Form { get; }

    public string Title => Root.Name;

    /// <summary>Заголовок вікна наявного шаблону й запуску workflow з шаблону.</summary>
    public static string EditTitle(string templateName) => $"{WorkflowConfigWindow.Title}: {templateName}";

    /// <summary>Чи відкрите вікно workflow серед дочірніх вікон <paramref name="owner"/>.</summary>
    public static bool IsOpenIn(Window owner) => FindIn(owner) is not null;

    // Кнопки вікна без AutomationId — шукаємо їх за підписом серед дочірніх елементів вікна.
    protected override Func<AutomationElement> FindButton(string button) =>
        () => Wait.For(
            () => Root.FindFirstChild(cf => cf.ByControlType(ControlType.Button).And(cf.ByName(button))),
            $"кнопки «{button}» у вікні workflow");

    private static AutomationElement? FindIn(Window owner) =>
        owner.FindAllChildren(cf => cf.ByControlType(ControlType.Window))
            .FirstOrDefault(window => window.FindFirstChild(cf => cf.ByAutomationId(FormId)) is not null);
}
