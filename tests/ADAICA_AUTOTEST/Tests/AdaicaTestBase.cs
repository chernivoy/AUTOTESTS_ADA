using ADAICA_AUTOTEST.Infrastructure;
using ADAICA_AUTOTEST.Pages;

namespace ADAICA_AUTOTEST.Tests;

/// <summary>
/// Базовий клас тестів ADAICA: після тесту виконує зареєстроване прибирання і завершує застосунок
/// з підтвердженням виходу, щоб коректно закрити сесію, а не зупиняти процес примусово.
/// </summary>
public abstract class AdaicaTestBase : UiTestBase
{
    private readonly List<Action> _cleanups = [];

    /// <summary>
    /// Реєструє дію, яка поверне застосунок у попередній стан (наприклад, закриє відкриту вкладку).
    /// Виконується після скріншота падіння, тож він показує стан у момент помилки.
    /// </summary>
    protected void OnCleanup(Action cleanup) => _cleanups.Add(cleanup);

    protected override void CloseApplication()
    {
        // Один екземпляр класу обслуговує всі тести, тому список очищаємо щоразу.
        var cleanups = _cleanups.AsEnumerable().Reverse().ToList();
        _cleanups.Clear();

        foreach (var cleanup in cleanups)
        {
            try
            {
                cleanup();
            }
            catch (Exception ex)
            {
                TestContext.Out.WriteLine($"Прибирання після тесту не вдалося: {ex}");
            }
        }

        new AdaicaWindow(Session).Exit();
    }
}
