using ADAICA_AUTOTEST.Controls;
using ADAICA_AUTOTEST.Infrastructure;
using Allure.Net.Commons;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;

namespace ADAICA_AUTOTEST.Pages;

/// <summary>
/// Окреме вікно збереженого завдання («Öffnen» → «In einem neuen Fenster öffnen» у меню смужки планувальника):
/// заголовок — назва завдання, бланк той самий, що в огляді (<see cref="TaskForm"/>), унизу OK / Abbrechen / Übernehmen.
/// AutomationId у вікна немає, тож шукаємо його за бланком завдання серед дочірніх вікон власника.
/// </summary>
public sealed class TaskEditWindow
{
    private readonly Window _owner;

    /// <param name="owner">Вікно, якому належить вікно завдання (для планувальника — вікно «Kalender»).</param>
    /// <param name="number">Номер завдання, за яким упізнаємо його вікно.</param>
    public TaskEditWindow(Window owner, string number)
    {
        _owner = owner;
        Number = number;
        Root = Wait.For(() => Find(owner, number), $"вікна завдання {number}");
        Form = new TaskForm(Root, Root);
        CancelButton = new ButtonControl("Abbrechen", Locate.ById(Root, "btnCancelCtrMainForm"));
    }

    public string Number { get; }

    public string Title => Root.Name;

    public TaskForm Form { get; }

    public ButtonControl CancelButton { get; }

    public bool IsOpen => Find(_owner, Number) is not null;

    private Window Root { get; }

    public void Close() => AllureApi.Step($"Закрити вікно завдання {Number} кнопкою «Abbrechen»", () =>
    {
        CancelButton.Click();
        Wait.Until(() => !IsOpen, $"закриття вікна завдання {Number}");
    });

    /// <summary>Для прибирання після тесту: закриває вікно, лише якщо воно ще відкрите.</summary>
    public void CloseIfOpen()
    {
        if (IsOpen)
            Close();
    }

    private static Window? Find(Window owner, string number) =>
        owner.FindAllChildren(cf => cf.ByControlType(ControlType.Window))
            .FirstOrDefault(window => window.FindFirstDescendant(cf => cf.ByAutomationId(TaskForm.NumberId)) is { } field
                && field.Patterns.Value.PatternOrDefault?.Value.Value == number)
            ?.AsWindow();
}
