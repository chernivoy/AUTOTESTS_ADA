using ADAICA_AUTOTEST.Controls;
using FlaUI.Core.AutomationElements;

namespace ADAICA_AUTOTEST.Pages;

/// <summary>
/// Вікно «Karte des Elements» (`wndCommonOptions`): новий чи змінений пункт довідника у вікні
/// <see cref="ContactDefaultsWindow"/>, наприклад Mitarbeiterfunktion. Поле «Name»; OK доступна, коли назву введено.
/// Якщо такий пункт уже є, ADAICA після OK показує «Bitte achten Sie darauf, dass doppelte Einträge unzulässig sind.»
/// </summary>
public sealed class ElementCardDialog : ChildDialog
{
    public ElementCardDialog(Window owner)
        : base(owner, "wndCommonOptions", "Karte des Elements", "btnOkWndCommon", "btnCancelWndCommon")
    {
        Name = new TextBoxControl("Name", Locate.ById(Root, "txtName"));
    }

    public TextBoxControl Name { get; }
}
