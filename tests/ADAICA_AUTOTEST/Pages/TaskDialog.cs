using Allure.Net.Commons;
using FlaUI.Core.AutomationElements;

namespace ADAICA_AUTOTEST.Pages;

/// <summary>Вікно нового завдання («Neue Aufgabe …»).</summary>
public sealed class TaskDialog : RecordDialog
{
    public TaskDialog(Window owner)
        : base(owner, "tiMainRecordTask", "завдання")
    {
        Form = new TaskForm(Root, Root);
    }

    public TaskForm Form { get; }

    /// <summary>
    /// Робить завдання першим у щоденній серії з <paramref name="count"/> завдань і запускає серію. Решту завдань
    /// ADAICA створює одразу після збереження, по одному на кожен наступний день, о тій самій годині.
    /// </summary>
    public void StartDailySeries(int count) => AllureApi.Step($"Зробити завдання щоденною серією з {count} завдань і запустити її", () =>
    {
        Form.OpenSeriesType().SetDaily(count);
        Form.StartSeries();
    });
}
