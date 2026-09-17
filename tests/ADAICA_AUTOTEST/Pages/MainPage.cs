using System.Text.RegularExpressions;
using ADAICA_AUTOTEST.Infrastructure;
using FlaUI.Core.AutomationElements;

namespace ADAICA_AUTOTEST.Pages;

/// <summary>Робочий простір ADAICA після входу.</summary>
public sealed partial class MainPage : AdaicaWindow
{
    private const string MainMenuId = "mnuSystemMenuWndMain";
    private const string DemoVersionWindowId = "wndDemoVersion";

    public MainPage(AppSession session)
        : base(session)
    {
        Element(MainMenuId);
        CloseDemoVersionWindowIfShown();
    }

    /// <summary>База даних, до якої підключено застосунок (із заголовка «… [БАЗА:СХЕМА] …»).</summary>
    public string Database => ParseTitle().Groups["database"].Value;

    /// <summary>Схема, до якої підключено застосунок.</summary>
    public string Schema => ParseTitle().Groups["schema"].Value;

    public static bool IsShownIn(Window window) =>
        window.FindFirstDescendant(cf => cf.ByAutomationId(MainMenuId)) is not null;

    private void CloseDemoVersionWindowIfShown()
    {
        // Без ліцензії («Lizenziert für: Not Registered») після входу завжди відкривається вікно демоверсії.
        if (!Title.Contains("Not Registered", StringComparison.Ordinal))
            return;

        Element(DemoVersionWindowId).FindByAutomationId("btnExitWndDemoVersion").AsButton().Invoke();
        Wait.Until(
            () => Window.FindFirstChild(cf => cf.ByAutomationId(DemoVersionWindowId)) is null,
            "закриття вікна демоверсії");
    }

    private Match ParseTitle()
    {
        var match = ConnectionInTitle().Match(Title);
        return match.Success
            ? match
            : throw new InvalidOperationException($"У заголовку вікна немає бази й схеми: «{Title}».");
    }

    [GeneratedRegex(@"\[(?<database>[^:\]]+):(?<schema>[^\]]+)\]")]
    private static partial Regex ConnectionInTitle();
}
