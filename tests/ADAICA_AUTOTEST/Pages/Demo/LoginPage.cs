using ADAICA_AUTOTEST.Infrastructure;
using Allure.Net.Commons.Attributes;
using FlaUI.Core.AutomationElements;

namespace ADAICA_AUTOTEST.Pages.Demo;

public sealed class LoginPage(AppSession session) : PageBase(session, "LoginWindow")
{
    public TextBox UserName => Element("UserNameTextBox").AsTextBox();

    public TextBox Password => Element("PasswordBox").AsTextBox();

    public Button LoginButton => Element("LoginButton").AsButton();

    public string ErrorMessage => Element("ErrorText").Name;

    [AllureStep("Ввести логін {userName} і пароль")]
    public LoginPage FillCredentials(
        string userName,
        [AllureParameter(Ignore = true)] string password)
    {
        UserName.Text = userName;
        Password.Text = password;
        return this;
    }

    [AllureStep("Увійти як {userName}")]
    public MainPage LoginAs(
        string userName,
        [AllureParameter(Ignore = true)] string password)
    {
        FillCredentials(userName, password);
        LoginButton.Invoke();
        return new MainPage(Session);
    }

    [AllureStep("Спробувати увійти як {userName} і дочекатися помилки")]
    public LoginPage LoginExpectingError(
        string userName,
        [AllureParameter(Ignore = true)] string password)
    {
        FillCredentials(userName, password);
        LoginButton.Invoke();
        Wait.Until(() => ErrorMessage.Length > 0, "повідомлення про помилку входу");
        return this;
    }
}
