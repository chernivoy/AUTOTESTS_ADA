using ADAICA_AUTOTEST.Infrastructure;
using ADAICA_AUTOTEST.Pages.Demo;
using Allure.Net.Commons;
using Allure.Net.Commons.Attributes;

namespace ADAICA_AUTOTEST.Tests.Demo;

[AllureSuite("Демо: вхід у систему")]
public class DemoLoginTests : DemoAppTestBase
{
    [Test]
    [Category("Smoke")]
    [AllureSeverity(SeverityLevel.blocker)]
    public void Login_WithValidCredentials_OpensMainWindow()
    {
        var mainPage = new LoginPage(Session).LoginAs(DemoUserName, DemoPassword);

        mainPage.WelcomeText.ShouldBe($"Вітаємо, {DemoUserName}!");
    }

    [TestCase(DemoUserName, "wrong-password", TestName = "Login_WithWrongPassword_ShowsError")]
    [TestCase("unknown-user", DemoPassword, TestName = "Login_WithUnknownUser_ShowsError")]
    [AllureSeverity(SeverityLevel.critical)]
    public void Login_WithInvalidCredentials_ShowsError(string userName, string password)
    {
        var loginPage = new LoginPage(Session).LoginExpectingError(userName, password);

        loginPage.ErrorMessage.ShouldBe("Невірний логін або пароль");
    }

    [Test]
    public void LoginButton_IsEnabled_OnlyWhenBothFieldsFilled()
    {
        var loginPage = new LoginPage(Session);
        loginPage.LoginButton.IsEnabled.ShouldBeFalse();

        loginPage.UserName.Text = DemoUserName;
        loginPage.LoginButton.IsEnabled.ShouldBeFalse();

        loginPage.Password.Text = DemoPassword;
        Wait.Until(() => loginPage.LoginButton.IsEnabled, "активації кнопки входу");
    }
}
