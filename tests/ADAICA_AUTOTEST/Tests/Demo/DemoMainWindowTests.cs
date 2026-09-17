using ADAICA_AUTOTEST.Infrastructure;
using ADAICA_AUTOTEST.Pages.Demo;
using Allure.Net.Commons.Attributes;

namespace ADAICA_AUTOTEST.Tests.Demo;

[AllureSuite("Демо: головне вікно")]
public class DemoMainWindowTests : DemoAppTestBase
{
    private MainPage _mainPage = null!;

    // Виконується після StartApplication() з базового класу.
    [SetUp]
    public void LogIn() => _mainPage = new LoginPage(Session).LoginAs(DemoUserName, DemoPassword);

    [Test]
    [Category("Smoke")]
    public void AddItem_AppearsInListAndUpdatesCounter()
    {
        _mainPage.AddItem("Перший запис");

        _mainPage.ItemNames.ShouldBe(new[] { "Перший запис" });
        _mainPage.ItemsCountText.ShouldBe("Елементів: 1");
    }

    [Test]
    public void AddItem_ClearsInputField()
    {
        _mainPage.AddItem("Запис");

        _mainPage.NewItem.Text.ShouldBe(string.Empty);
    }

    [Test]
    public void AddItem_WithBlankText_IsIgnored()
    {
        _mainPage.NewItem.Text = "   ";
        _mainPage.AddItemButton.Invoke();

        // Invoke() асинхронний: даємо застосунку секунду й перевіряємо, що елемент так і не з'явився.
        Should.Throw<TimeoutException>(
            () => Wait.Until(() => _mainPage.ItemNames.Count > 0, "появи елемента", TimeSpan.FromSeconds(1)));
        _mainPage.ItemsCountText.ShouldBe("Елементів: 0");
    }

    [Test]
    public void Logout_ReturnsToLoginWindow()
    {
        var loginPage = _mainPage.Logout();

        loginPage.UserName.Text.ShouldBe(string.Empty);
        loginPage.LoginButton.IsEnabled.ShouldBeFalse();
    }
}
