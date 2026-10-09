using ADAICA_AUTOTEST.Controls;
using ADAICA_AUTOTEST.Infrastructure;
using Allure.Net.Commons;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;

namespace ADAICA_AUTOTEST.Pages;

/// <summary>
/// Вікно нового документа у вбудованому редакторі («Neues Dokument (übergeordnet: …)», `wndEditorNew`): назва, клас,
/// текст і внизу панель зв'язків, як у вікнах нових записів. Без змін тексту «Abbrechen» закриває вікно без питань.
/// </summary>
public sealed class DocumentEditorDialog
{
    private const string WindowId = "wndEditorNew";

    private static readonly TimeSpan OpenTimeout = TimeSpan.FromSeconds(30);

    private readonly Window _owner;

    public DocumentEditorDialog(Window owner)
    {
        _owner = owner;
        Root = Wait.For(() => owner.FindFirstChild(cf => cf.ByAutomationId(WindowId))?.AsWindow(), "вікна нового документа (Editor)", OpenTimeout);
        CancelButton = new ButtonControl("Abbrechen", Locate.ById(Root, "btnCloseWndEditor"));
    }

    public ButtonControl CancelButton { get; }

    /// <summary>Панель зв'язків унизу вікна (див. <see cref="RecordDialog.Links"/>).</summary>
    public RecordLinksPanel Links => new(Root, _owner, "ctrRecordLinkView", labelsFromRecordWindow: true);

    public bool IsOpen => _owner.FindAllChildren(cf => cf.ByControlType(ControlType.Window)).Contains(Root);

    private Window Root { get; }

    public void Cancel() => AllureApi.Step("Закрити вікно нового документа без збереження", () =>
    {
        CancelButton.Click();
        Wait.Until(() => !IsOpen, "закриття вікна нового документа");
    });

    /// <summary>Для прибирання після тесту: закриває вікно, лише якщо воно ще відкрите.</summary>
    public void CancelIfOpen()
    {
        if (IsOpen)
            Cancel();
    }
}
