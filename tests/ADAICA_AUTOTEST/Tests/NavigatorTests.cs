using ADAICA_AUTOTEST.Infrastructure;
using ADAICA_AUTOTEST.Pages;
using Allure.Net.Commons;
using Allure.Net.Commons.Attributes;

namespace ADAICA_AUTOTEST.Tests;

[AllureSuite("Навігатори")]
public class NavigatorTests : AdaicaTestBase
{
    private const string Dokumentation = "Dokumentation";

    [Test]
    [Category("Smoke")]
    [AllureSeverity(SeverityLevel.critical)]
    public void DokumentationNavigator_NewProject()
    {
        var mainPage = new LoginPage(Session).LoginAs(
            TestSettings.UserName, TestSettings.Password, TestSettings.Database, TestSettings.Schema);
        var tabsBefore = mainPage.NavigatorTabNames;
        OnCleanup(() => mainPage.CloseLastNavigatorTabs(keep: tabsBefore.Count));

        mainPage.OpenNavigator(Dokumentation);

        mainPage.ActiveNavigatorName.ShouldBe(Dokumentation);
        mainPage.NavigatorTabNames.ShouldBe(tabsBefore.Append(Dokumentation));

        var projectDialog = mainPage.OpenNewProjectDialog();
        // Якщо тест упаде до збереження, вікно закриється через «Abbrechen». Збережені проєкти лишаються в базі.
        OnCleanup(projectDialog.CancelIfOpen);

        var today = DateTime.Today;
        var project = new ProjectData(
            Name: "Main_autotest_project",
            ShortName: "MAT",
            BeginDate: today.AddDays(1),
            EndDate: today.AddMonths(1),
            Cost: 1500m,
            CostCurrency: "USD",
            Outlay: 8m,
            OutlayUnit: "Tag(e)",
            Comment: "Created by ADAICA_AUTOTEST");

        projectDialog.Fill(project);

        projectDialog.ReadValues().ShouldBe(project);
        // Назви проєктів можуть повторюватися між прогонами, тому свій проєкт впізнаємо за номером.
        var number = projectDialog.Number;
        number.ShouldNotBeNullOrWhiteSpace();

        projectDialog.Save();

        // Після збереження ADAICA сама переходить до нового проєкту.
        var savedProject = mainPage.WaitForOpenedProject(number);
        mainPage.SelectedNavigatorNodeName.ShouldBe($"{project.Name} ({number})");
        savedProject.ReadValues().ShouldBe(project);
    }
}
