using ADAICA_AUTOTEST.Infrastructure;
using Allure.Net.Commons.Attributes;
using FlaUI.Core.AutomationElements;

namespace ADAICA_AUTOTEST.Pages.Demo;

public sealed class MainPage(AppSession session) : PageBase(session, "MainWindow")
{
    public string WelcomeText => Element("WelcomeText").Name;

    public TextBox NewItem => Element("NewItemTextBox").AsTextBox();

    public Button AddItemButton => Element("AddItemButton").AsButton();

    public ListBox ItemsList => Element("ItemsListBox").AsListBox();

    public string ItemsCountText => Element("ItemsCountText").Name;

    public Button LogoutButton => Element("LogoutButton").AsButton();

    public IReadOnlyList<string> ItemNames => ItemsList.Items.Select(item => item.Name).ToList();

    [AllureStep("Додати елемент {text}")]
    public MainPage AddItem(string text)
    {
        var countBefore = ItemsList.Items.Length;
        NewItem.Text = text;
        AddItemButton.Invoke();
        Wait.Until(() => ItemsList.Items.Length == countBefore + 1, $"появи елемента '{text}' у списку");
        return this;
    }

    [AllureStep("Вийти з системи")]
    public LoginPage Logout()
    {
        LogoutButton.Invoke();
        return new LoginPage(Session);
    }
}
