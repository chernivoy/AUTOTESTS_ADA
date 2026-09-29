using ADAICA_AUTOTEST.Controls;
using ADAICA_AUTOTEST.Infrastructure;
using Allure.Net.Commons;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;

namespace ADAICA_AUTOTEST.Pages;

/// <summary>Вікно нового завдання («Neue Aufgabe …»).</summary>
public sealed class TaskDialog : RecordDialog
{
    public TaskDialog(Window owner)
        : base(owner, "tiMainRecordTask", "завдання")
    {
        Form = new TaskForm(Root);
        // Кнопки вкладки «Aufgabenzyklus» не мають AutomationId, тож шукаємо їх за підписом.
        StartSeriesButton = new ButtonControl("Serie starten", () => Wait.For(() => ButtonNamed("Serie starten"), "кнопки «Serie starten»"));
    }

    public TaskForm Form { get; }

    /// <summary>З'являється на вкладці «Aufgabenzyklus», коли для завдання задано серію.</summary>
    public ButtonControl StartSeriesButton { get; }

    /// <summary>
    /// Робить завдання першим у щоденній серії з <paramref name="count"/> завдань і запускає серію. Решту завдань
    /// ADAICA створює одразу після збереження, по одному на кожен наступний день, о тій самій годині.
    /// </summary>
    public void StartDailySeries(int count) => AllureApi.Step($"Зробити завдання щоденною серією з {count} завдань і запустити її", () =>
    {
        Form.SeriesTypeButton.Click();
        new TaskSeriesDialog(Root).SetDaily(count);
        StartSeriesButton.Click();
        // Запущену серію кнопка на тому самому місці вже зупиняє.
        Wait.Until(() => ButtonNamed("Serie stoppen") is not null, "запуску серії («Serie stoppen»)");
    });

    private AutomationElement? ButtonNamed(string name) =>
        Root.FindFirstDescendant(cf => cf.ByControlType(ControlType.Button).And(cf.ByName(name)));
}
