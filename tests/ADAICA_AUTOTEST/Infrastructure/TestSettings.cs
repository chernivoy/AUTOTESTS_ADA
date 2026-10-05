using System.Globalization;

namespace ADAICA_AUTOTEST.Infrastructure;

/// <summary>
/// Налаштування прогону. Пріоритет джерел: змінна середовища → параметр з .runsettings → значення за замовчуванням.
/// </summary>
public static class TestSettings
{
    /// <summary>Повний шлях до exe застосунку під тестом.</summary>
    public static string AppPath => Require("ADAICA_APP_PATH", "AppPath");

    public static TimeSpan AppStartTimeout => TimeSpan.FromSeconds(GetInt("ADAICA_APP_START_TIMEOUT", "AppStartTimeoutSeconds", 60));

    /// <summary>Скільки чекати появи елемента або виконання умови.</summary>
    public static TimeSpan DefaultTimeout => TimeSpan.FromSeconds(GetInt("ADAICA_DEFAULT_TIMEOUT", "DefaultTimeoutSeconds", 10));

    /// <summary>Скільки чекати головного вікна після натискання кнопки входу.</summary>
    public static TimeSpan LoginTimeout => TimeSpan.FromSeconds(GetInt("ADAICA_LOGIN_TIMEOUT", "LoginTimeoutSeconds", 60));

    public static string UserName => Require("ADAICA_USER", "UserName");

    public static string Password => Require("ADAICA_PASSWORD", "Password");

    public static string Database => Require("ADAICA_DATABASE", "Database");

    public static string Schema => Require("ADAICA_SCHEMA", "Schema");

    /// <summary>
    /// Як тести клікають і натискають клавіші: <see cref="InputMode.Background"/> (за замовчуванням) — повідомленнями
    /// вікну, а якщо дія не подіяла, мишею й клавіатурою; <see cref="InputMode.BackgroundOnly"/> — лише повідомленнями;
    /// <see cref="InputMode.Mouse"/> — лише мишею й клавіатурою. Див. <see cref="UiInput"/>.
    /// </summary>
    public static InputMode InputMode =>
        Get("ADAICA_INPUT_MODE", "InputMode") is { } value
            ? Enum.Parse<InputMode>(value, ignoreCase: true)
            : InputMode.Background;

    /// <summary>
    /// Монітор для вікна ADAICA: «secondary» (перший неосновний), «primary» або ім'я пристрою («\\.\DISPLAY2»).
    /// Не задано або такого монітора немає — вікно лишається там, де його відкрила ADAICA.
    /// </summary>
    public static string? Monitor => Get("ADAICA_MONITOR", "Monitor");

    /// <summary>Куди складати скріншоти та інші артефакти падінь.</summary>
    public static string ArtifactsDirectory =>
        Get("ADAICA_ARTIFACTS_DIR", "ArtifactsDirectory") ?? Path.Combine(TestContext.CurrentContext.WorkDirectory, "artifacts");

    /// <summary>Змінні середовища, з якими запускається застосунок.</summary>
    public static IReadOnlyDictionary<string, string> AppEnvironment { get; } = new Dictionary<string, string>
    {
        // Без цього ADAICA підміняє AutomationPeer вікон на порожні, і UI Automation не бачить жодного елемента.
        // Змінна перекриває AutomationTests:AutomationPeerFeatureEnabled з appsettings.json лише для запущеного процесу.
        ["AutomationTests__AutomationPeerFeatureEnabled"] = "true",
    };

    private static string? Get(string environmentVariable, string runSettingsParameter)
    {
        var value = Environment.GetEnvironmentVariable(environmentVariable);
        if (string.IsNullOrWhiteSpace(value))
            value = TestContext.Parameters.Get(runSettingsParameter);

        return string.IsNullOrWhiteSpace(value) ? null : value;
    }

    private static string Require(string environmentVariable, string runSettingsParameter) =>
        Get(environmentVariable, runSettingsParameter)
        ?? throw new InvalidOperationException(
            $"Не задано {environmentVariable}: встановіть змінну середовища або параметр {runSettingsParameter} у .runsettings.");

    private static int GetInt(string environmentVariable, string runSettingsParameter, int defaultValue) =>
        int.TryParse(Get(environmentVariable, runSettingsParameter), NumberStyles.Integer, CultureInfo.InvariantCulture, out var value)
            ? value
            : defaultValue;
}
