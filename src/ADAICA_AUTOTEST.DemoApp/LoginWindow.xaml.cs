using System.Windows;

namespace ADAICA_AUTOTEST.DemoApp;

public partial class LoginWindow : Window
{
    // Демо-облікові дані; у реальному застосунку тут був би сервіс автентифікації.
    private const string ValidUserName = "admin";
    private const string ValidPassword = "admin123";

    public LoginWindow()
    {
        InitializeComponent();
    }

    private void OnCredentialsChanged(object sender, RoutedEventArgs e)
    {
        LoginButton.IsEnabled = UserNameTextBox.Text.Length > 0 && PasswordBox.Password.Length > 0;
        ErrorText.Text = string.Empty;
    }

    private void OnLoginClick(object sender, RoutedEventArgs e)
    {
        if (UserNameTextBox.Text != ValidUserName || PasswordBox.Password != ValidPassword)
        {
            ErrorText.Text = "Невірний логін або пароль";
            return;
        }

        var mainWindow = new MainWindow(UserNameTextBox.Text);
        Application.Current.MainWindow = mainWindow;
        mainWindow.Show();
        Close();
    }
}
