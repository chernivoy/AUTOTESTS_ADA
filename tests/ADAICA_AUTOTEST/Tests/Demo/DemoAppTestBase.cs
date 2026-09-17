using ADAICA_AUTOTEST.Infrastructure;

namespace ADAICA_AUTOTEST.Tests.Demo;

/// <summary>
/// Тести демо-застосунку перевіряють сам фреймворк, тому завжди запускають демо, незалежно від AppPath.
/// </summary>
[Category("Demo")]
public abstract class DemoAppTestBase : UiTestBase
{
    // Облікові дані зашиті в демо-застосунок (LoginWindow.xaml.cs).
    protected const string DemoUserName = "admin";
    protected const string DemoPassword = "admin123";

    protected override string ApplicationPath => TestSettings.DemoAppPath;
}
