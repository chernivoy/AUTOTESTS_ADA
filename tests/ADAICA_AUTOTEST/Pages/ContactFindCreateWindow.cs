using ADAICA_AUTOTEST.Controls;
using ADAICA_AUTOTEST.Infrastructure;
using Allure.Net.Commons.Attributes;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;

namespace ADAICA_AUTOTEST.Pages;

/// <summary>
/// Вікно «Finden &amp; Erstellen» на вкладці Kontakte: перш ніж створити контакт, ADAICA пропонує пошукати, чи його ще
/// немає. Це те саме дочірнє вікно (AutomationId «This»), що й пошук «Finden» (<see cref="SearchWindow"/>), тому
/// впізнаємо його за рядком пошуку контактів. Відкривається пунктом «Neue Firma» чи «Neue Person» на вузлі Gesamtliste,
/// а також пунктом «Mitarbeiter zur Organisationseinheit hinzufügen» на вузлі відділу фірми.
/// </summary>
public sealed class ContactFindCreateWindow
{
    private const string SearchLineId = "ctrPACBox";

    private readonly Window _owner;

    public ContactFindCreateWindow(Window owner)
    {
        _owner = owner;
        Root = Wait.For(
            () => ChildWindows(owner).FirstOrDefault(window => window.FindFirstDescendant(cf => cf.ByAutomationId(SearchLineId)) is not null),
            "вікна «Finden & Erstellen» (Kontakte)");

        // Поле, на яке вікно ставить фокус: «Firma» для «Neue Firma», «Vorname Nachname» для співробітника.
        FocusedField = new LabelControl("Fokus", Locate.ById(Root, SearchLineId, "tbFocusOn"));
        Company = new TextBoxControl("Firma", Locate.ById(Root, SearchLineId, "txtBoxCompanyName", "txtBox"));
        Person = new TextBoxControl("Vorname Nachname", Locate.ById(Root, SearchLineId, "txtBoxPersonName", "txtBox"));
        Action = new LabelControl("Aktion", Locate.ById(Root, SearchLineId, "tbActionInfo"));
        NewButton = new ButtonControl("Neu", Locate.ById(Root, "btnNewFindBoxPAC"));
        CloseButton = new ButtonControl("Schließen", Locate.ById(Root, "btnClose"));
    }

    public LabelControl FocusedField { get; }

    public TextBoxControl Company { get; }

    /// <summary>Персона в рядку пошуку: «Vorname Nachname».</summary>
    public TextBoxControl Person { get; }

    /// <summary>Що зробить кнопка «Neu» з уведеними даними, наприклад «Firma erstellen».</summary>
    public LabelControl Action { get; }

    /// <summary>Доступна, лише коли в рядку пошуку щось уведено.</summary>
    public ButtonControl NewButton { get; }

    public ButtonControl CloseButton { get; }

    // IsAvailable для закритого вікна WPF лишається true, тому перевіряємо, чи вікно ще серед дочірніх.
    public bool IsOpen => ChildWindows(_owner).Contains(Root);

    private Window Root { get; }

    /// <summary>«Neu»: відкриває вікно «Neue Person/Firma», у якому вже стоїть назва з поля «Firma».</summary>
    [AllureStep("Відкрити вікно нової фірми кнопкою «Neu»")]
    public NewCompanyDialog OpenNewCompanyDialog()
    {
        NewButton.Click();
        return new NewCompanyDialog(Root);
    }

    /// <summary>«Neu»: відкриває вікно «Neue Person/Firma», у якому вже стоїть персона з рядка пошуку.</summary>
    [AllureStep("Відкрити вікно нової персони кнопкою «Neu»")]
    public NewPersonDialog OpenNewPersonDialog()
    {
        NewButton.Click();
        return new NewPersonDialog(_owner, Root);
    }

    [AllureStep("Закрити вікно «Finden & Erstellen»")]
    public void Close()
    {
        CloseButton.Click();
        Wait.Until(() => !IsOpen, "закриття вікна «Finden & Erstellen»");
    }

    /// <summary>Для прибирання після тесту: закриває вікно, лише якщо воно ще відкрите.</summary>
    public void CloseIfOpen()
    {
        if (IsOpen)
            Close();
    }

    private static IEnumerable<Window> ChildWindows(Window owner) =>
        owner.FindAllChildren(cf => cf.ByControlType(ControlType.Window)).Select(window => window.AsWindow());
}
