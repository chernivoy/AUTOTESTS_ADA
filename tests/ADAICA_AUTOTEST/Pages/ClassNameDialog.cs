using ADAICA_AUTOTEST.Controls;
using ADAICA_AUTOTEST.Infrastructure;
using Allure.Net.Commons;
using FlaUI.Core.AutomationElements;

namespace ADAICA_AUTOTEST.Pages;

/// <summary>
/// Вікно нового класу чи підкласу (контекстне меню списку у вікні конфігурації класів → «Neu»): назва, коротка назва
/// й «Sichtbar» (вимкнено за замовчуванням).
/// </summary>
public sealed class ClassNameDialog
{
    private const string WindowId = "wndNewEditVubClassSubClass";

    private readonly Window _mainWindow;

    public ClassNameDialog(Window mainWindow)
    {
        _mainWindow = mainWindow;
        Root = Wait.For(() => Find(mainWindow), "вікна нового класу");
        Name = new TextBoxControl("Name", Locate.ById(Root, "txtNameWndNewEditCkassSubClass"));
        ShortName = new TextBoxControl("Kurzname", Locate.ById(Root, "txtShortNameWndNewEditCkassSubClass"));
        Visible = new CheckBoxControl("Sichtbar", Locate.ById(Root, "chkVisibilityWndNewEditCkassSubClass"));
        OkButton = new ButtonControl("OK", Locate.ById(Root, "btnOkWndNewEditCkassSubClass"));
    }

    public TextBoxControl Name { get; }

    public TextBoxControl ShortName { get; }

    /// <summary>За замовчуванням вимкнено: невидимий клас списки показують лише з «Unsichtbare anzeigen».</summary>
    public CheckBoxControl Visible { get; }

    public ButtonControl OkButton { get; }

    private Window Root { get; }

    public void Save(string name, string shortName) => AllureApi.Step($"Зберегти «{name} ({shortName})»", () =>
    {
        Name.Fill(name);
        ShortName.Fill(shortName);
        Visible.SetChecked(true);
        OkButton.Click();
        Wait.Until(() => Find(_mainWindow) is null, "закриття вікна нового класу");
    });

    private static Window? Find(Window mainWindow) =>
        mainWindow.FindFirstDescendant(cf => cf.ByAutomationId(WindowId))?.AsWindow();
}
