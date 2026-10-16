using ADAICA_AUTOTEST.Controls;
using ADAICA_AUTOTEST.Infrastructure;
using Allure.Net.Commons.Attributes;
using FlaUI.Core.Tools;

namespace ADAICA_AUTOTEST.Pages;

/// <summary>Форма входу ADAICA: користувач, пароль, база даних і схема.</summary>
public sealed class LoginPage : AdaicaWindow
{
    private const string VersionMismatchMessage = "Ihre ADAICA-Version stimmt nicht mit der des Servers überein";

    // Заставка «Please wait while the application opens» — Pane, а не Window, тож GetAllTopLevelWindows її не бачить.
    private const string SplashClassName = "SplashScreen";

    // ADAICA на старті показала, що Outlook не встановлено, — тоді повідомлення буде й після входу.
    private readonly bool _outlookMissing;

    public LoginPage(AppSession session)
        : this(session, WaitForStartup(session))
    {
    }

    private LoginPage(AppSession session, bool outlookMissing)
        : base(session)
    {
        _outlookMissing = outlookMissing;

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

    /// <summary>
    /// Чекає кінця запуску: форма входу на місці, а заставка «Please wait while the application opens» (окреме вікно
    /// процесу) закрилася. Форма з'являється раніше, ніж ADAICA завантажить бази й схеми. Якщо Outlook не встановлено,
    /// ADAICA посеред запуску показує «Keine gültige Installation von Microsoft Outlook gefunden…» — то окремим вікном
    /// ще до вікна ADAICA, то вже поверх форми входу — і далі не запускається, доки його не закрити, тож тест натискає
    /// OK. Інший діалог означає, що запуск не вдався: тест одразу падає з його текстом.
    /// </summary>
    /// <returns>true, якщо ADAICA показала, що Outlook не встановлено.</returns>
    private static bool WaitForStartup(AppSession session)
    {
        var outlookMissing = false;
        string? error = null;
        Wait.Until(() =>
        {
            if (MessageBoxDialog.FindInApplication(session) is { } dialog)
            {
                if (CloseIfOutlookMissing(dialog))
                {
                    outlookMissing = true;
                    return false;
                }

                error = $"{dialog.Title}: {dialog.Message.Text}";
                return true;
            }

            var processWindows = session.Automation.GetDesktop()
                .FindAllChildren(cf => cf.ByProcessId(session.Application.ProcessId));
            return processWindows.All(window => window.ClassName != SplashClassName)
                && processWindows.FirstOrDefault(window => window.AutomationId == AutomationId)
                    ?.FindFirstDescendant(cf => cf.ByAutomationId("btnOK")) is not null;
        }, "форми входу після запуску ADAICA", TestSettings.AppStartTimeout);

        if (error is not null)
            throw new InvalidOperationException($"Запуск ADAICA зупинився на діалозі «{error}».");

        return outlookMissing;
    }

    private void WaitForMainWindow()
    {
        string? error = null;
        var outlookMessageClosed = false;
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

            if (CloseIfOutlookMissing(dialog))
            {
                outlookMessageClosed = true;
                return false;
            }

            // Інший діалог означає, що вхід не вдався: одразу падаємо з його текстом, а не чекаємо таймаут.
            error = $"{dialog.Title}: {message}";
            return true;
        }, "головного вікна після входу", TestSettings.LoginTimeout);

        if (error is not null)
            throw new InvalidOperationException($"Вхід не вдався, ADAICA показала діалог «{error}».");

        // Без Outlook ADAICA показує повідомлення ще раз, коли після входу завантажує поштовий обліковий запис Outlook
        // користувача, — зазвичай за мить після появи робочого простору. Не з'явилося за таймаут — тест іде далі.
        if (_outlookMissing && !outlookMessageClosed)
            Retry.WhileFalse(
                () => FindMessageBox() is { } dialog && CloseIfOutlookMissing(dialog),
                TestSettings.DefaultTimeout,
                ignoreException: true);
    }
}
