using ADAICA_AUTOTEST.Infrastructure;
using Allure.Net.Commons.Attributes;

namespace ADAICA_AUTOTEST.Pages;

/// <summary>
/// Вікно ADAICA. Те саме вікно показує спершу форму входу, потім робочий простір,
/// а діалоги ADAICA в дереві UI Automation є його дочірніми вікнами, а не вікнами робочого столу.
/// </summary>
public class AdaicaWindow : PageBase
{
    public const string AutomationId = "FlatWnd";

    private const string ExitQuestion = "Wollen Sie ADAICA verlassen?";

    private static readonly TimeSpan ExitTimeout = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Знаходить вікно й ставить його на монітор для тестів (<see cref="AppSession.PlaceOnTestMonitor"/>): після входу
    /// ADAICA могла б перенести його туди, де воно стояло в користувача.
    /// </summary>
    public AdaicaWindow(AppSession session)
        : base(session, AutomationId)
    {
        AppSession.PlaceOnTestMonitor(Window.Properties.NativeWindowHandle.Value);
    }

    public string Title => Window.Name;

    /// <summary>Відкритий зараз діалог повідомлення або null.</summary>
    public MessageBoxDialog? FindMessageBox() => MessageBoxDialog.Find(Window);

    [AllureStep("Закрити ADAICA")]
    public void Exit()
    {
        // Close() лише надсилає запит на закриття; ADAICA перепитує, чи справді вийти.
        Window.Close();
        Wait.Until(() =>
        {
            if (Session.Application.HasExited)
                return true;

            var dialog = FindMessageBox();
            if (dialog?.Message.Text == ExitQuestion)
                dialog.Button("Ja").Click();
            return false;
        }, "завершення ADAICA", ExitTimeout);
    }
}
