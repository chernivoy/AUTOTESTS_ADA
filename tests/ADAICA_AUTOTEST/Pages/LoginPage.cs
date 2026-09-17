using ADAICA_AUTOTEST.Infrastructure;
using Allure.Net.Commons.Attributes;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;

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
    }

    public ComboBox UserNameComboBox => Combo("cmbUserNames");

    public TextBox PasswordBox => Element("txtUserPassword").FindByAutomationId("PART_Editor").AsTextBox();

    public ComboBox DatabaseComboBox => Combo("cmbDataBase");

    public ComboBox SchemaComboBox => Combo("cmbSchema");

    public Button OkButton => Element("btnOK").AsButton();

    [AllureStep("Увійти як {userName} у {database}:{schema}")]
    public MainPage LoginAs(
        string userName,
        [AllureParameter(Ignore = true)] string password,
        string database,
        string schema)
    {
        // Зміна бази перезавантажує форму й скидає користувача та пароль, тому спершу база й схема.
        SelectDatabase(database);
        SelectSchema(schema);
        EnterUserName(userName);
        EnterPassword(password);
        OkButton.Invoke();
        WaitForMainWindow();
        return new MainPage(Session);
    }

    [AllureStep("Ввести користувача {userName}")]
    public LoginPage EnterUserName(string userName)
    {
        // Список користувачів редагований: значення задаємо напряму, його може й не бути серед збережених.
        UserNameComboBox.Patterns.Value.Pattern.SetValue(userName);
        Wait.Until(() => UserNameComboBox.Patterns.Value.Pattern.Value.Value == userName, $"користувача «{userName}» у полі");
        return this;
    }

    [AllureStep("Ввести пароль")]
    public LoginPage EnterPassword([AllureParameter(Ignore = true)] string password)
    {
        PasswordBox.Text = password;
        return this;
    }

    [AllureStep("Вибрати базу даних {database}")]
    public LoginPage SelectDatabase(string database)
    {
        Select(DatabaseComboBox, database);
        return this;
    }

    [AllureStep("Вибрати схему {schema}")]
    public LoginPage SelectSchema(string schema)
    {
        Select(SchemaComboBox, schema);
        return this;
    }

    // ComboBox усередині ImageComboBox завжди має AutomationId "combo", тому шукаємо в межах контейнера.
    private ComboBox Combo(string containerId) => Element(containerId).FindByAutomationId("combo").AsComboBox();

    private static void Select(ComboBox comboBox, string text)
    {
        if (comboBox.SelectedItem?.Text == text)
            return;

        try
        {
            // Список схем заповнюється після вибору бази, тому чекаємо на потрібний пункт.
            Wait.Until(() => comboBox.Items.Any(item => item.Text == text), $"пункту «{text}» у списку");
            comboBox.Select(text);
        }
        finally
        {
            // Пошук пунктів розгортає список, а поки він розгорнутий, ADAICA ігнорує закриття вікна.
            if (comboBox.ExpandCollapseState == ExpandCollapseState.Expanded)
                comboBox.Collapse();
        }

        Wait.Until(() => comboBox.SelectedItem?.Text == text, $"вибору «{text}»");
    }

    private void WaitForMainWindow()
    {
        string? error = null;
        Wait.Until(() =>
        {
            var dialog = FindMessageBox();
            if (dialog is null)
                return MainPage.IsShownIn(Window);

            if (dialog.Message.StartsWith(VersionMismatchMessage, StringComparison.Ordinal))
            {
                // Тестова збірка клієнта може не збігатися з версією сервера; ADAICA дозволяє продовжити.
                dialog.Click("Ja");
                return false;
            }

            // Інший діалог означає, що вхід не вдався: одразу падаємо з його текстом, а не чекаємо таймаут.
            error = $"{dialog.Title}: {dialog.Message}";
            return true;
        }, "головного вікна після входу", TestSettings.LoginTimeout);

        if (error is not null)
            throw new InvalidOperationException($"Вхід не вдався, ADAICA показала діалог «{error}».");
    }
}
