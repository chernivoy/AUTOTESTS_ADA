using ADAICA_AUTOTEST.Controls;
using FlaUI.Core.AutomationElements;

namespace ADAICA_AUTOTEST.Pages;

/// <summary>
/// Вікно «Festlegungen für das Duplikat» (дочірнє вікно головного): що перенести в копію завдання, вставлену «Einfügen
/// als Duplikat». Дати бере клітинка, куди вставлено копію, тож «Beginn- und Enddatum», «Erledigt am» і «Geschlossen am»
/// недоступні й вимкнені; відповідального й виконавців ADAICA пропонує перенести. «Diese Meldung nicht mehr anzeigen»
/// ADAICA, певно, запам'ятовує для користувача, тож тест його не чіпає.
/// </summary>
public sealed class DuplicateOptionsDialog(Window owner)
    : ChildDialog(owner, "wndOptionsCopyFromRecordTemplate", "Festlegungen für das Duplikat", "btnOkWndOptionsCopyFromRecordTemplate", "btnCancelWndOptionsCopyFromRecordTemplate")
{
    public CheckBoxControl BeginEndDate => new("Beginn- und Enddatum übernehmen", Locate.ById(Root, "chkApplyBeginEndDateForTaskWndOptCopyFromRecTemplate"));

    public CheckBoxControl DoneDate => new("Datum \"Erledigt am\" übernehmen", Locate.ById(Root, "chkApplyDoneForTaskWndOptionsCopyFromRecordTemplate"));

    public CheckBoxControl ClosedDate => new("Datum \"Geschlossen am\" übernehmen", Locate.ById(Root, "chkApplyCloseForTaskWndOptionsCopyFromRecordTemplate"));

    public CheckBoxControl Administrator => new("Verantwortlichen übernehmen", Locate.ById(Root, "chkApplyAdministratorForTaskWndOptionsCopyFromRecordTemplate"));

    public CheckBoxControl Participants => new("Ausführende(n) übernehmen", Locate.ById(Root, "chkApplyParticipantForTaskWndOptionsCopyFromRecordTemplate"));

    public CheckBoxControl DoNotShowAgain => new("Diese Meldung nicht mehr anzeigen", Locate.ById(Root, "chkDoNotShowAgainWndOptionsCopyFromRecordTemplate"));
}
