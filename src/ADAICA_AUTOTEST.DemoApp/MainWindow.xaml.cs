using System.Collections.ObjectModel;
using System.Windows;

namespace ADAICA_AUTOTEST.DemoApp;

public partial class MainWindow : Window
{
    private readonly ObservableCollection<string> _items = [];

    public MainWindow(string userName)
    {
        InitializeComponent();
        WelcomeText.Text = $"Вітаємо, {userName}!";
        ItemsListBox.ItemsSource = _items;
        UpdateItemsCount();
    }

    private void OnAddItemClick(object sender, RoutedEventArgs e)
    {
        var text = NewItemTextBox.Text.Trim();
        if (text.Length == 0)
            return;

        _items.Add(text);
        NewItemTextBox.Clear();
        NewItemTextBox.Focus();
        UpdateItemsCount();
    }

    private void OnLogoutClick(object sender, RoutedEventArgs e)
    {
        var loginWindow = new LoginWindow();
        Application.Current.MainWindow = loginWindow;
        loginWindow.Show();
        Close();
    }

    private void UpdateItemsCount() => ItemsCountText.Text = $"Елементів: {_items.Count}";
}
