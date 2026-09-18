using FlaUI.Core.AutomationElements;

namespace ADAICA_AUTOTEST.Pages;

/// <summary>Вікно нового інфозапису («Neue Notiz …»).</summary>
public sealed class NoteDialog : RecordDialog
{
    public NoteDialog(Window owner)
        : base(owner, "tiMainRecordHistory", "інфозапис")
    {
        Form = new NoteForm(Root);
    }

    public NoteForm Form { get; }
}
