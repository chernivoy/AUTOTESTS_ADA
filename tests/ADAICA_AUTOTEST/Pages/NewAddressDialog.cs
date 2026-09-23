using ADAICA_AUTOTEST.Controls;
using Allure.Net.Commons;
using FlaUI.Core.AutomationElements;

namespace ADAICA_AUTOTEST.Pages;

/// <summary>
/// Вікно «Neue Adresse anlegen» — дочірнє вікно «Finden &amp; Erstellen» (<see cref="AddressFindCreateWindow"/>).
/// Адресу воно бере з рядка пошуку: поля, крім індексу, лише для читання. Після OK ADAICA додає адресу до контакту
/// й закриває обидва вікна.
/// </summary>
/// <param name="owner">Вікно «Finden &amp; Erstellen».</param>
public sealed class NewAddressDialog(Window owner)
    : ChildDialog(owner, "wndAddressCreate", "Neue Adresse anlegen", "btnFinishWndAddressCreate", "btnBackWndAddressCreate")
{
    public TextBoxControl PostalCode => new("Postleitzahl", Locate.ById(Root, "txtAddressCreatePLZ", "txtBox"));

    public TextBoxControl Country => new("Land", Locate.ById(Root, "txtAddressCreateCountry"));

    public TextBoxControl State => new("Bundesland", Locate.ById(Root, "txtAddressCreateState"));

    public TextBoxControl City => new("Ort", Locate.ById(Root, "txtAddressCreateCommunity"));

    public TextBoxControl District => new("Ortsteil", Locate.ById(Root, "txtAddressCreateCommunityPart"));

    public TextBoxControl Street => new("Straße", Locate.ById(Root, "txtAddressCreateStreet"));

    public TextBoxControl HouseNumber => new("Hausnummer", Locate.ById(Root, "txtAddressCreateAddress"));

    /// <summary>Перевіряє, що вікно взяло з рядка пошуку всі частини адреси; повідомляє про всі розбіжності разом.</summary>
    public void ShouldHaveValues(AddressData expected) => AllureApi.Step("Перевірити поля нової адреси", () =>
        this.ShouldSatisfyAllConditions(
            () => Country.ShouldHaveText(expected.Country),
            () => State.ShouldHaveText(expected.State),
            () => PostalCode.ShouldHaveText(expected.PostalCode),
            () => City.ShouldHaveText(expected.City),
            () => District.ShouldHaveText(expected.District),
            () => Street.ShouldHaveText(expected.Street),
            () => HouseNumber.ShouldHaveText(expected.HouseNumber)));
}
