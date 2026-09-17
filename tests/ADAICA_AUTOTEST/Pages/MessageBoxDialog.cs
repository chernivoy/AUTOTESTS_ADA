using Allure.Net.Commons.Attributes;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;

namespace ADAICA_AUTOTEST.Pages;

/// <summary>
/// Діалог повідомлення ADAICA: текст у tbMessage, кнопки без AutomationId, тому їх шукаємо за підписом.
/// </summary>
public sealed class MessageBoxDialog
{
    private const string MessageId = "tbMessage";

    private readonly Window _dialog;

    private MessageBoxDialog(Window dialog) => _dialog = dialog;

    public string Title => _dialog.Name;

    public string Message => _dialog.FindFirstChild(cf => cf.ByAutomationId(MessageId))?.Name ?? string.Empty;

    public static MessageBoxDialog? Find(Window owner)
    {
        var dialog = owner.FindAllChildren(cf => cf.ByControlType(ControlType.Window))
            .FirstOrDefault(window => window.FindFirstChild(cf => cf.ByAutomationId(MessageId)) is not null);

        return dialog is null ? null : new MessageBoxDialog(dialog.AsWindow());
    }

    [AllureStep("Натиснути {buttonText} у діалозі")]
    public void Click(string buttonText)
    {
        // Кнопка містить підпис і пояснення як два дочірні TextBlock.
        var button = _dialog.FindAllDescendants(cf => cf.ByControlType(ControlType.Button))
            .FirstOrDefault(b => b.FindFirstChild(cf => cf.ByName(buttonText)) is not null)
            ?? throw new InvalidOperationException($"У діалозі «{Title}» немає кнопки «{buttonText}».");

        button.AsButton().Invoke();
    }
}
