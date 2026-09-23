using ADAICA_AUTOTEST.Controls;
using Allure.Net.Commons;
using FlaUI.Core.AutomationElements;

namespace ADAICA_AUTOTEST.Pages;

/// <summary>
/// Вікно «Firmendetails» (кнопка «...» біля назви в картці фірми): три рядки назви, рядок адреси й звертання.
/// У картці назва показується рядками через пробіл. Рядок адреси й звертання ADAICA складає сама,
/// тому, щоб задати свої, їх треба зафіксувати («Fixieren»).
/// </summary>
public sealed class CompanyNameDialog(Window owner)
    : ChildDialog(owner, "wndCompanyDetails", "Firmendetails", "btnOKCompanyDetails", "btnCancelCompanyDetails")
{
    public TextBoxControl NameLine1 => new("Firmenname 1. Zeile", Locate.ById(Root, "txtCompanyNameA"));

    public TextBoxControl NameLine2 => new("Firmenname 2. Zeile", Locate.ById(Root, "txtCompanyNameB"));

    public TextBoxControl NameLine3 => new("Firmenname 3. Zeile", Locate.ById(Root, "txtCompanyNameC"));

    public CheckBoxControl AddressLineFixed => new("Anschriftenzeile: Fixieren", Locate.ById(Root, "chkToAddressSaved"));

    public TextBoxControl AddressLine => new("Anschriftenzeile", Locate.ById(Root, "txtToAddressCompanyWndDetails"));

    public CheckBoxControl SalutationFixed => new("Briefanrede: Fixieren", Locate.ById(Root, "chkSalutationSavedCompanyDetails"));

    public TextBoxControl Salutation => new("Briefanrede", Locate.ById(Root, "txtSalutationCompanyWndDetails"));

    public void Fill(CompanyGeneralData general) => AllureApi.Step("Заповнити назву й звертання фірми", () =>
    {
        NameLine2.Fill(general.NameLine2);
        NameLine3.Fill(general.NameLine3);
        AddressLineFixed.SetChecked(true);
        AddressLine.Fill(general.AddressLine);
        SalutationFixed.SetChecked(true);
        Salutation.Fill(general.Salutation);
    });

    /// <summary>Перевіряє всі поля й повідомляє про всі розбіжності разом, а не лише про першу.</summary>
    public void ShouldHaveValues(string nameLine1, CompanyGeneralData general) =>
        AllureApi.Step("Перевірити назву й звертання фірми", () =>
            this.ShouldSatisfyAllConditions(
                () => NameLine1.ShouldHaveText(nameLine1),
                () => NameLine2.ShouldHaveText(general.NameLine2),
                () => NameLine3.ShouldHaveText(general.NameLine3),
                () => AddressLineFixed.ShouldBeChecked(true),
                () => AddressLine.ShouldHaveText(general.AddressLine),
                () => SalutationFixed.ShouldBeChecked(true),
                () => Salutation.ShouldHaveText(general.Salutation)));
}
