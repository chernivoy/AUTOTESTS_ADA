using ADAICA_AUTOTEST.Infrastructure;
using ADAICA_AUTOTEST.Pages;
using Allure.Net.Commons;
using Allure.Net.Commons.Attributes;

namespace ADAICA_AUTOTEST.Tests;

[AllureSuite("Вхід у систему")]
public class LoginTests : AdaicaTestBase
{
    [Test]
    [Category("Smoke")]
    [AllureSeverity(SeverityLevel.blocker)]
    public void Login_WithValidCredentials_OpensMainWindow()
    {
        var mainPage = new LoginPage(Session).LoginAs(
            TestSettings.UserName, TestSettings.Password, TestSettings.Database, TestSettings.Schema);

        mainPage.Database.ShouldBe(TestSettings.Database);
        mainPage.Schema.ShouldBe(TestSettings.Schema);
    }
}
