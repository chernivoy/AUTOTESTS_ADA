using ADAICA_AUTOTEST.Controls;
using ADAICA_AUTOTEST.Infrastructure;
using Allure.Net.Commons;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;

namespace ADAICA_AUTOTEST.Pages;

/// <summary>
/// Вікно проєкту («Neues Projekt …»). Дочірнє вікно ADAICA без AutomationId, тому його впізнаємо
/// за вкладкою tiMainRecordFolder, а не за заголовком, який залежить від мови.
/// </summary>
public sealed class ProjectDialog : ProjectForm
{
    private const string MainTabId = "tiMainRecordFolder";

    private static readonly TimeSpan SaveTimeout = TimeSpan.FromSeconds(30);

    private readonly Window _owner;
    private readonly Window _dialog;

    public ProjectDialog(Window owner)
        : this(owner, Wait.For(
            () => ChildWindows(owner).FirstOrDefault(window => window.FindFirstDescendant(cf => cf.ByAutomationId(MainTabId)) is not null),
            "вікна проєкту"))
    {
    }

    private ProjectDialog(Window owner, Window dialog)
        : base(dialog)
    {
        _owner = owner;
        _dialog = dialog;
        OkButton = new ButtonControl("OK", Locate.ById(dialog, "btnOKCtrMainForm"));
        CancelButton = new ButtonControl("Abbrechen", Locate.ById(dialog, "btnCancelCtrMainForm"));
    }

    public string Title => _dialog.Name;

    // IsAvailable для закритого вікна WPF лишається true, тому перевіряємо, чи вікно ще серед дочірніх.
    public bool IsOpen => ChildWindows(_owner).Contains(_dialog);

    public ButtonControl OkButton { get; }

    public ButtonControl CancelButton { get; }

    public void Save() => AllureApi.Step("Зберегти проєкт", () =>
    {
        OkButton.Click();

        string? error = null;
        Wait.Until(() =>
        {
            if (!IsOpen)
                return true;

            // Якщо збереження не вдалося, ADAICA показує повідомлення й лишає вікно відкритим.
            var message = MessageBoxDialog.Find(_dialog) ?? MessageBoxDialog.Find(_owner);
            if (message is null)
                return false;

            error = $"{message.Title}: {message.Message.Text}";
            return true;
        }, "збереження проєкту", SaveTimeout);

        if (error is not null)
            throw new InvalidOperationException($"Проєкт не збережено, ADAICA показала діалог «{error}».");
    });

    public void Cancel() => AllureApi.Step("Закрити вікно проєкту без збереження", () =>
    {
        CancelButton.Click();
        Wait.Until(() => !IsOpen, "закриття вікна проєкту");
    });

    /// <summary>Для прибирання після тесту: закриває вікно, лише якщо воно ще відкрите.</summary>
    public void CancelIfOpen()
    {
        if (IsOpen)
            Cancel();
    }

    private static IEnumerable<Window> ChildWindows(Window owner) =>
        owner.FindAllChildren(cf => cf.ByControlType(ControlType.Window)).Select(window => window.AsWindow());
}
