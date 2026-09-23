using ADAICA_AUTOTEST.Controls;
using Allure.Net.Commons;
using FlaUI.Core.AutomationElements;

namespace ADAICA_AUTOTEST.Pages;

/// <summary>Картка фірми в області перегляду: вкладка «Stammdaten» → «Allgemein».</summary>
public class CompanyForm(AutomationElement root)
{
    public const string NameId = "txtFullNameCompany";

    public TextBoxControl Name { get; } = new("Firmenname", Locate.ById(root, NameId));

    public TextBoxControl ShortName { get; } = new("Kurzname", Locate.ById(root, "txtCompanyShortName"));

    /// <summary>Перевіряє всі поля й повідомляє про всі розбіжності разом, а не лише про першу.</summary>
    public void ShouldHaveValues(CompanyData expected) => AllureApi.Step("Перевірити поля фірми", () =>
        this.ShouldSatisfyAllConditions(
            () => Name.ShouldHaveText(expected.Name),
            () => ShortName.ShouldHaveText(expected.ShortName)));
}
