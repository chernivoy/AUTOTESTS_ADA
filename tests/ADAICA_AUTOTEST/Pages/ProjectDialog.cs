using FlaUI.Core.AutomationElements;

namespace ADAICA_AUTOTEST.Pages;

/// <summary>Вікно нового проєкту («Neues Projekt …»).</summary>
public sealed class ProjectDialog : RecordDialog
{
    public ProjectDialog(Window owner)
        : base(owner, "tiMainRecordFolder", "проєкт")
    {
        Form = new ProjectForm(Root);
    }

    public ProjectForm Form { get; }
}
