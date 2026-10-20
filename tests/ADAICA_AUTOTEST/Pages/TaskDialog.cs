using ADAICA_AUTOTEST.Controls;
using Allure.Net.Commons;
using FlaUI.Core.AutomationElements;

namespace ADAICA_AUTOTEST.Pages;

/// <summary>Вікно нового завдання («Neue Aufgabe …»).</summary>
public sealed class TaskDialog : RecordDialog
{
    private const string MainTabId = "tiMainRecordTask";

    public TaskDialog(Window owner)
        : base(owner, MainTabId, "завдання")
    {
        Form = new TaskForm(Root, Root);
        // Обидві кнопки — картинки без підпису праворуч унизу, підказка — у HelpText.
        ChooseTemplateButton = new ButtonControl("Aufgabenvorlage auswählen", Locate.ById(Root, "btnSelectTemplate"));
        SaveAsTemplateButton = new ButtonControl("Als Aufgabenvorlage speichern", Locate.ById(Root, "btnSaveAsTemplate"));
    }

    public TaskForm Form { get; }

    /// <summary>«Aufgabenvorlage auswählen»: «Aufgabenvorlagen» з «Verwenden», що заповнює це вікно полями шаблону.</summary>
    public ButtonControl ChooseTemplateButton { get; }

    /// <summary>«Als Aufgabenvorlage speichern»: «Neue Aufgabenvorlage» з полями цього вікна.</summary>
    public ButtonControl SaveAsTemplateButton { get; }

    /// <summary>
    /// «Als Aufgabenvorlage speichern»: відкриває «Neue Aufgabenvorlage» з усіма полями цього вікна, крім «Beginnt am».
    /// Шаблон зберігається окремо, а вікно завдання лишається відкритим і незміненим.
    /// </summary>
    public TaskTemplateDialog SaveAsTemplate() => AllureApi.Step("Відкрити «Neue Aufgabenvorlage» кнопкою «Als Aufgabenvorlage speichern»", () =>
    {
        SaveAsTemplateButton.Click();
        return new TaskTemplateDialog(Root);
    });

    /// <summary>
    /// «Aufgabenvorlage auswählen»: «Aufgabenvorlagen» з кнопкою «Verwenden» (<see cref="TaskTemplatesWindow.Use"/>), яка
    /// переносить у це вікно поля шаблону.
    /// </summary>
    public TaskTemplatesWindow ChooseTemplate() => AllureApi.Step("Відкрити «Aufgabenvorlagen» кнопкою «Aufgabenvorlage auswählen»", () =>
    {
        ChooseTemplateButton.Click();
        return new TaskTemplatesWindow(Root);
    });

    /// <summary>Чи відкрито вікно нового завдання серед дочірніх вікон <paramref name="owner"/>.</summary>
    public static bool IsShownIn(Window owner) => IsShownIn(owner, MainTabId);

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
