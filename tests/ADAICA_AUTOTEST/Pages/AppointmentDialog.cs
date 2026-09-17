using FlaUI.Core.AutomationElements;

namespace ADAICA_AUTOTEST.Pages;

/// <summary>Вікно нового терміну («Neuer Termin …»).</summary>
public sealed class AppointmentDialog : RecordDialog
{
    public AppointmentDialog(Window owner)
        : base(owner, "tiMainRecordAppoint", "термін")
    {
        Form = new AppointmentForm(Root);
    }

    public AppointmentForm Form { get; }
}
