using Allure.Net.Commons;
using Allure.NUnit;
using FlaUI.Core.Capturing;
using NUnit.Framework.Interfaces;

namespace ADAICA_AUTOTEST.Infrastructure;

/// <summary>
/// Базовий клас UI-тестів: кожен тест отримує свіжо запущений застосунок,
/// а при падінні знімається скріншот екрана.
/// </summary>
[AllureNUnit]
public abstract class UiTestBase
{
    protected AppSession Session { get; private set; } = null!;

    /// <summary>Який exe запускати перед кожним тестом.</summary>
    protected virtual string ApplicationPath => TestSettings.AppPath;

    [SetUp]
    public void StartApplication() => Session = AppSession.Launch(ApplicationPath);

    [TearDown]
    public void StopApplication()
    {
        if (Session is null)
            return;

        try
        {
            if (TestContext.CurrentContext.Result.Outcome.Status == TestStatus.Failed)
                CaptureScreenshot();

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

    private static void CaptureScreenshot()
    {
        var directory = Path.Combine(TestSettings.ArtifactsDirectory, "screenshots");
        Directory.CreateDirectory(directory);

        var invalidChars = Path.GetInvalidFileNameChars();
        var testName = string.Concat(TestContext.CurrentContext.Test.Name.Select(c => invalidChars.Contains(c) ? '_' : c));
        var file = Path.Combine(directory, $"{testName}_{DateTime.Now:yyyyMMdd_HHmmss}.png");

        Capture.Screen().ToFile(file);
        TestContext.AddTestAttachment(file, "Скріншот у момент падіння");
        AllureApi.AddAttachment("Скріншот у момент падіння", "image/png", file);
    }
}
