using ADAICA_AUTOTEST.Controls;
using ADAICA_AUTOTEST.Infrastructure;
using Allure.Net.Commons.Attributes;

namespace ADAICA_AUTOTEST.Pages;

/// <summary>Форма входу ADAICA: користувач, пароль, база даних і схема.</summary>
public sealed class LoginPage : AdaicaWindow
{
    private const string VersionMismatchMessage = "Ihre ADAICA-Version stimmt nicht mit der des Servers überein";

    public LoginPage(AppSession session)
        : base(session)
    {
        // Спершу вікно показує «Please wait while the application opens», форма входу з'являється пізніше.
        Window.FindByAutomationId("btnOK", TestSettings.AppStartTimeout);

        // ComboBox усередині ImageComboBox завжди має AutomationId "combo", тому шлях іде через контейнер.
        UserName = new ComboBoxControl("Benutzername", Locate.ById(Window, "cmbUserNames", "combo"));
        Password = new PasswordBoxControl("Passwort", Locate.ById(Window, "txtUserPassword", "PART_Editor"));
        Database = new ComboBoxControl("Datenbank", Locate.ById(Window, "cmbDataBase", "combo"));
        Schema = new ComboBoxControl("Schema", Locate.ById(Window, "cmbSchema", "combo"));
        OkButton = new ButtonControl("OK", Locate.ById(Window, "btnOK"));
    }

    /// <summary>Редагований список: користувача можна ввести, навіть якщо його немає серед збережених.</summary>
    public ComboBoxControl UserName { get; }

    public PasswordBoxControl Password { get; }

    public ComboBoxControl Database { get; }

    public ComboBoxControl Schema { get; }

    public ButtonControl OkButton { get; }

    [AllureStep("Увійти як {userName} у {database}:{schema}")]
    public MainPage LoginAs(
        string userName,
        [AllureParameter(Ignore = true)] string password,
        string database,
        string schema)
    {
        // Зміна бази перезавантажує форму й скидає користувача та пароль, тому спершу база й схема.
        Database.Select(database);
        Schema.Select(schema);
        UserName.EnterText(userName);
        Password.Fill(password);
        OkButton.Click();
        WaitForMainWindow();
        return new MainPage(Session);
    }

    private void WaitForMainWindow()
    {
        string? error = null;
        Wait.Until(() =>
        {
            var dialog = FindMessageBox();
            if (dialog is null)
                return MainPage.IsShownIn(Window);

            var message = dialog.Message.Text;
            if (message.StartsWith(VersionMismatchMessage, StringComparison.Ordinal))
            {
                // Тестова збірка клієнта може не збігатися з версією сервера; ADAICA дозволяє продовжити.
                dialog.Button("Ja").Click();
                return false;
            }

            // Інший діалог означає, що вхід не вдався: одразу падаємо з його текстом, а не чекаємо таймаут.
            error = $"{dialog.Title}: {message}";
            return true;
        }, "головного вікна після входу", TestSettings.LoginTimeout);

        if (error is not null)
            throw new InvalidOperationException($"Вхід не вдався, ADAICA показала діалог «{error}».");
    }
}
