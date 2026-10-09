using ADAICA_AUTOTEST.Controls;
using ADAICA_AUTOTEST.Infrastructure;
using Allure.Net.Commons;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;

namespace ADAICA_AUTOTEST.Pages;

/// <summary>
/// «Kontaktzuordnungs-Dialog» (`wndChooseContacts`): вибір контактів для панелі зв'язків запису
/// (<see cref="RecordLinksPanel.OpenAddContact"/>). Ліворуч — дерево контактів, праворуч — «Ausgewählt:»; «Finden»
/// відкриває пошук контактів (<see cref="ContactFindCreateWindow"/>), знайдений контакт потрапляє у вибрані.
/// </summary>
public sealed class ChooseContactsDialog
{
    private const string WindowId = "wndChooseContacts";

    private readonly Window _owner;

    public ChooseContactsDialog(Window owner)
    {
        _owner = owner;
        Root = Wait.For(() => owner.FindFirstChild(cf => cf.ByAutomationId(WindowId))?.AsWindow(), "вікна «Kontaktzuordnungs-Dialog»");
        FindButton = new ButtonControl("Finden", Locate.ById(Root, "btnSearchWndChooseContacts"));
        OkButton = new ButtonControl("OK", Locate.ById(Root, "btnOkWndChooseContacts"));
        CancelButton = new ButtonControl("Abbrechen", Locate.ById(Root, "btnCloseWndChooseContacts"));
    }

    public ButtonControl FindButton { get; }

    /// <summary>Доступна, коли серед «Ausgewählt:» є контакт.</summary>
    public ButtonControl OkButton { get; }

    public ButtonControl CancelButton { get; }

    public bool IsOpen => _owner.FindAllChildren(cf => cf.ByControlType(ControlType.Window)).Contains(Root);

    private Window Root { get; }

    /// <summary>
    /// «Finden» → у рядку пошуку e-mail <paramref name="email"/> → «Finden» → контакт з цією адресою → OK, потім OK
    /// цього вікна: контакт додається на вкладку «Kontakte» панелі зв'язків.
    /// </summary>
    public void AddFound(string email) => AllureApi.Step($"Додати контакт з e-mail {email}: «Finden» → OK → OK", () =>
    {
        // Якщо натиснути «Finden», поки вікно ще завантажує дерево контактів, ADAICA падає з NullReferenceException
        // (у лозі «btnSearchWndChooseContacts_Click») і пошук не відкриває.
        Wait.Until(
            () => Root.FindFirstDescendant(cf => cf.ByAutomationId("treeViewControl"))?.FindFirstDescendant(cf => cf.ByControlType(ControlType.TreeItem)) is not null,
            "дерева контактів у вікні «Kontaktzuordnungs-Dialog»");
        FindButton.Click();
        var findWindow = new ContactFindCreateWindow(Root);
        findWindow.Email.Fill(email);
        findWindow.ChooseFound(email);
        Wait.Until(() => OkButton.IsEnabled, "доступної кнопки OK у вікні «Kontaktzuordnungs-Dialog»");
        OkButton.Click();
        Wait.Until(() => !IsOpen, "закриття вікна «Kontaktzuordnungs-Dialog»");
    });

    /// <summary>Для прибирання після тесту: закриває вікно, лише якщо воно ще відкрите.</summary>
    public void CancelIfOpen()
    {
        if (!IsOpen)
            return;

        CancelButton.Click();
        Wait.Until(() => !IsOpen, "закриття вікна «Kontaktzuordnungs-Dialog»");
    }
}
