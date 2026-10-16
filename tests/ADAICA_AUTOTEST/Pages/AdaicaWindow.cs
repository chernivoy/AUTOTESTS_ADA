using ADAICA_AUTOTEST.Infrastructure;
using Allure.Net.Commons;
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
    private const string OutlookMissingMessage = "Keine gültige Installation von Microsoft Outlook gefunden";

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

    /// <summary>
    /// Натискає OK, якщо <paramref name="dialog"/> — повідомлення «Keine gültige Installation von Microsoft Outlook
    /// gefunden…». Без Outlook ADAICA показує його на старті й після входу; на тести, крім листів, це не впливає.
    /// </summary>
    /// <returns>true, якщо це було повідомлення про Outlook і його закрито.</returns>
    protected static bool CloseIfOutlookMissing(MessageBoxDialog dialog)
    {
        var message = dialog.Message.Text;
        if (!message.StartsWith(OutlookMissingMessage, StringComparison.Ordinal))
            return false;

        AllureApi.Step($"Закрити повідомлення «{message}»", () => dialog.Button("OK").Click());
        return true;
    }

    [AllureStep("Закрити ADAICA")]
    public void Exit()
    {
        // Поки відкрите повідомлення про Outlook, ADAICA не закривається.
        if (FindMessageBox() is { } message)
            CloseIfOutlookMissing(message);

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
