using FlaUI.Core.AutomationElements;

namespace ADAICA_AUTOTEST.Pages;

/// <summary>
/// Вікно нового Ordner («Neuer Ordner …»). ADAICA показує в ньому той самий бланк, що й у вікні проєкту:
/// ті самі поля й AutomationId, навіть головна вкладка tiMainRecordFolder. Тому бланк — <see cref="ProjectForm"/>,
/// а дані — <see cref="ProjectData"/>; Ordner відрізняє лише номер (OPE… замість PRO…).
/// </summary>
public sealed class FolderDialog : RecordDialog
{
    public FolderDialog(Window owner)
        : base(owner, "tiMainRecordFolder", "Ordner")
    {
        Form = new ProjectForm(Root, "Ordner");
    }

    public ProjectForm Form { get; }
}
