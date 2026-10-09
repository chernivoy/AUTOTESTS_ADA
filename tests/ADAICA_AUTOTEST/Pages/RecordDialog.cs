using ADAICA_AUTOTEST.Controls;
using ADAICA_AUTOTEST.Infrastructure;
using Allure.Net.Commons;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;

namespace ADAICA_AUTOTEST.Pages;

/// <summary>
/// Вікно нового запису ADAICA (проєкт, завдання тощо): бланк і кнопки OK / Abbrechen.
/// Це дочірнє вікно без AutomationId, тому його впізнаємо за головною вкладкою бланку,
/// а не за заголовком, який залежить від мови.
/// </summary>
public abstract class RecordDialog
{
    // Вікно завдання відкривається до 6 с — довше за стандартний таймаут.
    private static readonly TimeSpan OpenTimeout = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan SaveTimeout = TimeSpan.FromSeconds(30);

    private readonly Window _owner;
    private readonly string _recordName;

    /// <param name="owner">Головне вікно ADAICA.</param>
    /// <param name="mainTabId">AutomationId головної вкладки бланку, за яким впізнаємо вікно.</param>
    /// <param name="recordName">Що це за запис — для звіту («проєкт», «завдання»).</param>
    protected RecordDialog(Window owner, string mainTabId, string recordName)
    {
        _owner = owner;
        _recordName = recordName;
        Root = Wait.For(
            () => ChildWindows(owner).FirstOrDefault(window => window.FindFirstDescendant(cf => cf.ByAutomationId(mainTabId)) is not null),
            $"вікна нового запису ({recordName})",
            OpenTimeout);

        OkButton = new ButtonControl("OK", Locate.ById(Root, "btnOKCtrMainForm"));
        CancelButton = new ButtonControl("Abbrechen", Locate.ById(Root, "btnCancelCtrMainForm"));
    }

    public string Title => Root.Name;

    // IsAvailable для закритого вікна WPF лишається true, тому перевіряємо, чи вікно ще серед дочірніх.
    public bool IsOpen => ChildWindows(_owner).Contains(Root);

    public ButtonControl OkButton { get; }

    public ButtonControl CancelButton { get; }

    /// <summary>
    /// Панель зв'язків унизу вікна нового проєкту, Ordner, завдання, терміну чи інфозапису: «Kontakte» (контакт-батько
    /// ADAICA додає сама), «Dokumentation» (батько-запис і пропозиції завдань і термінів контактів), «Objekte».
    /// </summary>
    public RecordLinksPanel Links => new(Root, _owner, "ctrRecNewCO", labelsFromRecordWindow: true);

    protected Window Root { get; }

    public void Save() => AllureApi.Step($"Зберегти {_recordName}", () =>
    {
        OkButton.Click();

        string? error = null;
        Wait.Until(() =>
        {
            if (!IsOpen)
                return true;

            // Якщо збереження не вдалося, ADAICA показує повідомлення й лишає вікно відкритим.
            var message = MessageBoxDialog.Find(Root) ?? MessageBoxDialog.Find(_owner);
            if (message is null)
                return false;

            error = $"{message.Title}: {message.Message.Text}";
            return true;
        }, $"збереження ({_recordName})", SaveTimeout);

        if (error is not null)
            throw new InvalidOperationException($"Запис ({_recordName}) не збережено, ADAICA показала діалог «{error}».");
    });

    public void Cancel() => AllureApi.Step($"Закрити вікно без збереження ({_recordName})", () =>
    {
        CancelButton.Click();
        Wait.Until(() => !IsOpen, $"закриття вікна ({_recordName})");
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
