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
    private readonly ForegroundGuard? _foregroundGuard;

    private AppSession(Application application, UIA3Automation automation, ForegroundGuard? foregroundGuard)
    {
        Application = application;
        Automation = automation;
        _foregroundGuard = foregroundGuard;
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

        // Вікно, у якому зараз працює користувач: сюди ForegroundGuard повертатиме фокус, якщо ADAICA його забере.
        var userWindow = ForegroundGuard.CurrentForeground;
        var application = Application.Launch(startInfo);
        var foregroundGuard = ForegroundGuard.Start(application.ProcessId, userWindow);
        application.WaitWhileMainHandleIsMissing(TestSettings.AppStartTimeout);
        // Переносимо одразу, ще до форми входу, щоб вікно не встигло показатися на моніторі користувача.
        // Не вийшло (вікно саме змінюється) — AdaicaWindow спробує ще раз, коли з'явиться форма входу.
        try
        {
            PlaceOnTestMonitor(application.MainWindowHandle);
        }
        catch (Exception ex)
        {
            TestContext.Out.WriteLine($"[AppSession] Не вдалося одразу перенести вікно ADAICA: {ex.Message}");
        }
        return new AppSession(application, new UIA3Automation(), foregroundGuard);
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

    /// <summary>
    /// Переносить вікно на монітор із <see cref="TestSettings.Monitor"/> (розгорнутим, без активації), щоб тест ішов
    /// на іншому моніторі, ніж працює користувач. Назад вікно не повертаємо: ADAICA при виході запам'ятовує, де воно
    /// стояло, і наступного разу одразу відкривається на тестовому моніторі.
    /// </summary>
    public static void PlaceOnTestMonitor(nint window)
    {
        if (TestSettings.Monitor is not { } setting || window == 0)
            return;

        var monitor = MonitorPlacement.Find(setting);
        if (monitor is null)
        {
            TestContext.Out.WriteLine($"[AppSession] Монітора «{setting}» немає, вікно ADAICA лишається, де є.");
            return;
        }

        if (!MonitorPlacement.IsPlaced(window, monitor))
            MonitorPlacement.MoveTo(window, monitor);
    }

    public void Dispose()
    {
        try
        {
            CloseAllWindows();
        }
        finally
        {
            if (_foregroundGuard is not null)
            {
                _foregroundGuard.Dispose();
                TestContext.Out.WriteLine(
                    $"[ForegroundGuard] ADAICA ставала активною: {_foregroundGuard.Stolen}, фокус повернуто користувачеві: {_foregroundGuard.Restored}.");
            }

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
