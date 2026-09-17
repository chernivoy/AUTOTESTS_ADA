using FlaUI.Core.AutomationElements;

namespace ADAICA_AUTOTEST.Pages;

/// <summary>Вікно нового завдання («Neue Aufgabe …»).</summary>
public sealed class TaskDialog : RecordDialog
{
    public TaskDialog(Window owner)
        : base(owner, "tiMainRecordTask", "завдання")
    {
        Form = new TaskForm(Root);
    }

    public TaskForm Form { get; }
}
