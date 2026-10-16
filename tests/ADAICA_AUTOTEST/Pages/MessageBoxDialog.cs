using ADAICA_AUTOTEST.Controls;
using ADAICA_AUTOTEST.Infrastructure;
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

    private MessageBoxDialog(Window dialog)
    {
        _dialog = dialog;
        Message = new LabelControl("Повідомлення", Locate.ById(dialog, MessageId));
    }

    public string Title => _dialog.Name;

    public LabelControl Message { get; }

    public static MessageBoxDialog? Find(Window owner)
    {
        var dialog = owner.FindAllChildren(cf => cf.ByControlType(ControlType.Window))
            .FirstOrDefault(window => window.FindFirstChild(cf => cf.ByAutomationId(MessageId)) is not null);

        return dialog is null ? null : new MessageBoxDialog(dialog.AsWindow());
    }

    /// <summary>
    /// Відкритий діалог повідомлення застосунку або null. Шукає не лише серед дочірніх вікон: на старті, поки вікна
    /// ADAICA ще не видно, повідомлення стоїть окремим вікном робочого столу.
    /// </summary>
    public static MessageBoxDialog? FindInApplication(AppSession session)
    {
        foreach (var window in session.Application.GetAllTopLevelWindows(session.Automation))
        {
            if (window.FindFirstChild(cf => cf.ByAutomationId(MessageId)) is not null)
                return new MessageBoxDialog(window);
            if (Find(window) is { } dialog)
                return dialog;
        }

        return null;
    }

    /// <summary>Кнопка за підписом. Вона містить підпис і пояснення як два дочірні TextBlock.</summary>
    public ButtonControl Button(string caption) => new(
        caption,
        () => Wait.For(
            () => _dialog.FindAllDescendants(cf => cf.ByControlType(ControlType.Button))
                .FirstOrDefault(button => button.FindFirstChild(cf => cf.ByName(caption)) is not null),
            $"кнопки «{caption}» у діалозі «{Title}»"));
}
