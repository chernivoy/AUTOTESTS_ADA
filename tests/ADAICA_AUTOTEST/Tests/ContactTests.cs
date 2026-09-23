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

        // Після збереження ADAICA закриває вікно пошуку й сама переходить до нової фірми у вузлі Aktuelle.
        AllureApi.Step("Перевірити, що вікно «Finden & Erstellen» закрилося",
            () => findCreateWindow.IsOpen.ShouldBeFalse("Вікно «Finden & Erstellen» не закрилося після збереження фірми"));
        var savedCompany = mainPage.WaitForOpenedCompany(company.Name);
        AllureApi.Step($"Перевірити, що в дереві виділено «Aktuelle → {company.Name}»",
            () => mainPage.SelectedNavigatorNodePath.ShouldBe(new[] { "Aktuelle", company.Name }));
        savedCompany.ShouldHaveValues(company);
    }
}
