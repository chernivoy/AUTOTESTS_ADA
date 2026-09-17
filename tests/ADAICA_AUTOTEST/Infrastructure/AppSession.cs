using System.Diagnostics;
using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Conditions;
using FlaUI.Core.Tools;
using FlaUI.UIA3;

namespace ADAICA_AUTOTEST.Infrastructure;

/// <summary>
/// Один запущений екземпляр застосунку під тестом разом з UI Automation.
/// </summary>
public sealed class AppSession : IDisposable
{
    private AppSession(Application application, UIA3Automation automation)
    {
        Application = application;
        Automation = automation;
    }

    public Application Application { get; }

    public UIA3Automation Automation { get; }

    public ConditionFactory Cf => Automation.ConditionFactory;

    public static AppSession Launch(string appPath)
    {
        if (!File.Exists(appPath))
            throw new FileNotFoundException(
                $"Застосунок не знайдено: {appPath}. Зберіть рішення або задайте змінну ADAICA_APP_PATH.", appPath);

        var startInfo = new ProcessStartInfo(appPath)
        {
            WorkingDirectory = Path.GetDirectoryName(appPath)!,
        };
        foreach (var (name, value) in TestSettings.AppEnvironment)
            startInfo.Environment[name] = value;

        var application = Application.Launch(startInfo);
        application.WaitWhileMainHandleIsMissing(TestSettings.AppStartTimeout);
        return new AppSession(application, new UIA3Automation());
    }

    /// <summary>
    /// Чекає, поки в застосунку з'явиться верхньорівневе вікно з указаним AutomationId.
    /// </summary>
    public Window WaitForWindow(string automationId, TimeSpan? timeout = null)
    {
        var effectiveTimeout = timeout ?? TestSettings.DefaultTimeout;
        var condition = Cf.ByAutomationId(automationId).And(Cf.ByProcessId(Application.ProcessId));

        var window = Retry.WhileNull(
            () => Automation.GetDesktop().FindFirstChild(condition)?.AsWindow(),
            effectiveTimeout,
            ignoreException: true).Result;

        return window ?? throw new TimeoutException(
            $"Вікно '{automationId}' не з'явилося за {effectiveTimeout.TotalSeconds} с.");
    }

    public void Dispose()
    {
        try
        {
            CloseAllWindows();
        }
        finally
        {
            Automation.Dispose();
            Application.Dispose();
        }
    }

    // Application.Close() закриває лише Process.MainWindowHandle, який застаріває,
    // коли застосунок міняє вікна (логін → головне), тому закриваємо всі вікна процесу.
    private void CloseAllWindows()
    {
        if (Application.HasExited)
            return;

        try
        {
            foreach (var window in Application.GetAllTopLevelWindows(Automation))
                window.Close();
        }
        catch (Exception)
        {
            // Вікно могло зникнути під час закриття — нижче все одно перевіряємо завершення процесу.
        }

        var exited = Retry.WhileFalse(() => Application.HasExited, TimeSpan.FromSeconds(5), ignoreException: true).Success;
        if (!exited)
            Application.Kill();
    }
}
