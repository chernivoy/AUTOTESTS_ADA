using ADAICA_AUTOTEST.Pages;
using Allure.Net.Commons;
using Allure.Net.Commons.Attributes;

namespace ADAICA_AUTOTEST.Tests;

/// <summary>
/// Створення контактів через навігатор Kontakte. Збережені контакти лишаються в базі. Назва фірми щоразу унікальна
/// (<see cref="CustomerCase.NewCompany"/>), тож за нею фірму можна знайти пошуком і відрізнити від створених раніше.
/// </summary>
[AllureSuite("Навігатори")]
public class ContactTests : AdaicaTestBase
{
    [Test]
    [Category("Smoke")]
    [AllureSeverity(SeverityLevel.critical)]
    public void KontakteNavigator_NewCompany()
    {
        var mainPage = LogInAndOpenNavigator("Kontakte");
        var company = CustomerCase.Random().NewCompany();

        var savedCompany = CreateCompany(mainPage, company);

        savedCompany.ShouldHaveValues(company);
    }

    /// <summary>
    /// Нова фірма, у картці якої змінено всі поля вкладок Allgemein, Details і Bankverbindungen, які ADAICA дає
    /// редагувати в тестовій базі. Після збереження фірму перевідкрито з дерева, тож перевіряються збережені значення.
    /// </summary>
    [Test]
    [AllureSeverity(SeverityLevel.critical)]
    public void KontakteNavigator_EditCompany()
    {
        var mainPage = LogInAndOpenNavigator("Kontakte");
        var customer = CustomerCase.Random();
        var company = customer.NewCompany();
        var edit = customer.EditCompany();

        var savedCompany = CreateCompany(mainPage, company);
        // Якщо тест упаде посеред редагування, незбережені зміни буде відкинуто.
        OnCleanup(savedCompany.CancelIfEditing);
        edit = savedCompany.Edit(edit, CustomerCase.AnotherAddress);

        // Вузол фірми в дереві — повна назва й категорія в дужках.
        var fullName = CompanyForm.FullName(company.Name, edit.General);
        var node = $"{fullName} ({edit.General.Category})";
        AllureApi.Step($"Перевірити, що в дереві виділено «Aktuelle → {node}»",
            () => mainPage.SelectedNavigatorNodePath.ShouldBe(new[] { "Aktuelle", node }));

        var reopenedCompany = mainPage.ReopenCompany(node, fullName);
        reopenedCompany.ShouldHaveValues(company.Name, company.ShortName, edit);
    }

    /// <summary>
    /// Створює фірму: Gesamtliste → «Neue Firma» → «Finden &amp; Erstellen» → «Neue Person/Firma» → OK.
    /// Після збереження ADAICA сама відкриває фірму у вузлі Aktuelle.
    /// </summary>
    private CompanyForm CreateCompany(MainPage mainPage, CompanyData company)
    {
        var findCreateWindow = mainPage.OpenNewCompanyWindow();
        // Якщо тест упаде до збереження, вікна закриються без створення фірми.
        OnCleanup(findCreateWindow.CloseIfOpen);

        findCreateWindow.FocusedField.ShouldHaveText("Firma");
        findCreateWindow.Company.Fill(company.Name);
        // Дію під рядком пошуку ADAICA визначає за введеними даними, не одразу після введення.
        findCreateWindow.Action.WaitForText("Firma erstellen");

        var companyDialog = findCreateWindow.OpenNewCompanyDialog();
        OnCleanup(companyDialog.CancelIfOpen);

        companyDialog.Fill(company);
        companyDialog.ShouldHaveValues(company);

        companyDialog.Save();

        AllureApi.Step("Перевірити, що вікно «Finden & Erstellen» закрилося",
            () => findCreateWindow.IsOpen.ShouldBeFalse("Вікно «Finden & Erstellen» не закрилося після збереження фірми"));
        var savedCompany = mainPage.WaitForOpenedCompany(company.Name);
        AllureApi.Step($"Перевірити, що в дереві виділено «Aktuelle → {company.Name}»",
            () => mainPage.SelectedNavigatorNodePath.ShouldBe(new[] { "Aktuelle", company.Name }));
        return savedCompany;
    }
}
