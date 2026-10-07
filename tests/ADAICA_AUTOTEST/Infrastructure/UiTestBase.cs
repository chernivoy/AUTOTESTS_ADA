using Allure.Net.Commons;
using Allure.NUnit;
using FlaUI.Core.Capturing;
using NUnit.Framework.Interfaces;

namespace ADAICA_AUTOTEST.Infrastructure;

/// <summary>
/// Базовий клас UI-тестів: кожен тест отримує свіжо запущений застосунок,
/// а при падінні знімається скріншот монітора, на якому стоїть застосунок.
/// </summary>
[AllureNUnit]
public abstract class UiTestBase
{
    protected AppSession Session { get; private set; } = null!;

    [SetUp]
    public void StartApplication() => Session = AppSession.Launch(TestSettings.AppPath);

    /// <summary>
    /// Перезапускає застосунок посеред тесту — наприклад, щоб застосувати опції, які діють лише після перезапуску.
    /// <paramref name="exit"/> коректно закриває застосунок; якщо він так і не завершився, процес зупиняється.
    /// </summary>
    protected void RestartApplication(Action exit)
    {
        exit();
        Session.Dispose();
        Session = AppSession.Launch(TestSettings.AppPath);
    }

    [TearDown]
    public void StopApplication()
    {
        if (Session is null)
            return;

        try
        {
            // Скріншот може не вдатися (UI Automation не відповідає), але прибирання після тесту все одно потрібне.
            if (TestContext.CurrentContext.Result.Outcome.Status == TestStatus.Failed)
            {
                try
                {
                    CaptureScreenshot();
                }
                catch (Exception ex)
                {
                    TestContext.Out.WriteLine($"Не вдалося зняти скріншот падіння: {ex}");
                }
            }

            CloseApplication();
        }
        catch (Exception ex)
        {
            // Застосунок усе одно буде зупинено в Dispose, тому не підміняємо результат тесту.
            TestContext.Out.WriteLine($"Не вдалося коректно закрити застосунок: {ex}");
        }
        finally
        {
            Session.Dispose();
        }
    }

    /// <summary>
    /// Коректне завершення застосунку (підтвердження виходу тощо). Викликається після скріншота,
    /// а якщо застосунок так і не завершився, <see cref="AppSession.Dispose"/> зупиняє процес.
    /// </summary>
    protected virtual void CloseApplication()
    {
    }

    private void CaptureScreenshot()
    {
        var directory = Path.Combine(TestSettings.ArtifactsDirectory, "screenshots");
        Directory.CreateDirectory(directory);

        var invalidChars = Path.GetInvalidFileNameChars();
        var testName = string.Concat(TestContext.CurrentContext.Test.Name.Select(c => invalidChars.Contains(c) ? '_' : c));
        var file = Path.Combine(directory, $"{testName}_{DateTime.Now:yyyyMMdd_HHmmss}.png");

        // Лише монітор застосунку: на іншому може працювати користувач, і його вікнам у звіті не місце.
        var monitor = Session.Application.HasExited
            ? null
            : Session.Application.GetAllTopLevelWindows(Session.Automation)
                .Select(window => MonitorPlacement.MonitorOf(window.Properties.NativeWindowHandle.ValueOrDefault))
                .FirstOrDefault(found => found is not null);
        using (var capture = monitor is null ? Capture.Screen() : Capture.Rectangle(monitor.Bounds))
            capture.ToFile(file);
        TestContext.AddTestAttachment(file, "Скріншот у момент падіння");
        AllureApi.AddAttachment("Скріншот у момент падіння", "image/png", file);
    }
}
