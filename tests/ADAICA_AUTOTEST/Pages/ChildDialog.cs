using ADAICA_AUTOTEST.Controls;
using ADAICA_AUTOTEST.Infrastructure;
using Allure.Net.Commons;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;

namespace ADAICA_AUTOTEST.Pages;

/// <summary>
/// Невелике дочірнє вікно головного вікна з власним AutomationId і кнопками OK / Abbrechen
/// (наприклад, «Firmendetails» чи «Kategorie» у картці фірми).
/// </summary>
public abstract class ChildDialog
{
    private readonly Window _owner;
    private readonly string _title;

    /// <param name="owner">Вікно, дочірнім для якого є це вікно: зазвичай головне вікно ADAICA.</param>
    /// <param name="title">Заголовок вікна для звіту.</param>
    protected ChildDialog(Window owner, string windowId, string title, string okButtonId, string cancelButtonId)
    {
        _owner = owner;
        _title = title;
        Root = Wait.For(() => owner.FindFirstChild(cf => cf.ByAutomationId(windowId))?.AsWindow(), $"вікна «{title}»");

        OkButton = new ButtonControl("OK", Locate.ById(Root, okButtonId));
        CancelButton = new ButtonControl("Abbrechen", Locate.ById(Root, cancelButtonId));
    }

    public ButtonControl OkButton { get; }

    public ButtonControl CancelButton { get; }

    // IsAvailable для закритого вікна WPF лишається true, тому перевіряємо, чи вікно ще серед дочірніх.
    // Власник теж може закритися разом із вікном (як «Finden & Erstellen» після нової адреси), тоді пошук кидає виняток.
    public bool IsOpen
    {
        get
        {
            try
            {
                return _owner.FindAllChildren(cf => cf.ByControlType(ControlType.Window)).Contains(Root);
            }
            catch (Exception)
            {
                return false;
            }
        }
    }

    protected Window Root { get; }

    /// <summary>Натискає OK і чекає, доки вікно закриється; якщо ADAICA показала повідомлення, падає з його текстом.</summary>
    public void Confirm() => AllureApi.Step($"Підтвердити вікно «{_title}»", () =>
    {
        OkButton.Click();

        string? error = null;
        Wait.Until(() =>
        {
            if (!IsOpen)
                return true;

            var message = MessageBoxDialog.Find(Root) ?? MessageBoxDialog.Find(_owner);
            if (message is null)
                return false;

            error = $"{message.Title}: {message.Message.Text}";
            return true;
        }, $"закриття вікна «{_title}»");

        if (error is not null)
            throw new InvalidOperationException($"Вікно «{_title}» не закрилося, ADAICA показала діалог «{error}».");
    });

    public void Cancel() => AllureApi.Step($"Закрити вікно «{_title}» без змін", () =>
    {
        CancelButton.Click();
        Wait.Until(() => !IsOpen, $"закриття вікна «{_title}»");
    });

    /// <summary>Для прибирання після тесту: закриває вікно, лише якщо воно ще відкрите.</summary>
    public void CancelIfOpen()
    {
        if (IsOpen)
            Cancel();
    }
}
