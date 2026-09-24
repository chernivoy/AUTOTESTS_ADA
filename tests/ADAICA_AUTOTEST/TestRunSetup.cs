using System.Runtime.InteropServices;
using Allure.Net.Commons;

namespace ADAICA_AUTOTEST;

/// <summary>
/// Виконується один раз перед усіма тестами. Лежить у кореневому namespace,
/// бо NUnit застосовує SetUpFixture лише до тестів свого namespace і вкладених.
/// </summary>
[SetUpFixture]
public class TestRunSetup
{
    private const nint DpiAwarenessContextPerMonitorAwareV2 = -4;

    [OneTimeSetUp]
    public void PrepareTestRun()
    {
        // testhost не DPI-aware: на моніторах з масштабом понад 100% Windows віртуалізує координати,
        // і скріншоти виходять обрізаними й збільшеними. Вмикаємо per-monitor DPI до першої роботи з екраном.
        SetProcessDpiAwarenessContext(DpiAwarenessContextPerMonitorAwareV2);

        // Інакше в allure-results накопичуються результати попередніх запусків. Веб-запускач (tools/TestRunner)
        // запускає кожен тест окремим dotnet test і збирає один звіт на всі, тому теку чистить сам.
        if (Environment.GetEnvironmentVariable("ADAICA_KEEP_ALLURE_RESULTS") != "true")
            AllureLifecycle.Instance.CleanupResultDirectory();
    }

    [DllImport("user32.dll")]
    private static extern bool SetProcessDpiAwarenessContext(nint value);
}
