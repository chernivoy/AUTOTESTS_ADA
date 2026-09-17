using ADAICA_AUTOTEST.Infrastructure;
using ADAICA_AUTOTEST.Pages;

namespace ADAICA_AUTOTEST.Tests;

/// <summary>
/// Базовий клас тестів ADAICA: після тесту застосунок завершується з підтвердженням виходу,
/// щоб коректно закрити сесію, а не зупиняється примусово.
/// </summary>
public abstract class AdaicaTestBase : UiTestBase
{
    protected override void CloseApplication() => new AdaicaWindow(Session).Exit();
}
