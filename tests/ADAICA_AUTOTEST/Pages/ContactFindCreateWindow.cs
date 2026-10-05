using ADAICA_AUTOTEST.Controls;
using ADAICA_AUTOTEST.Infrastructure;
using Allure.Net.Commons;
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

    // «Nein» у вікні «Meinten Sie vielleicht…» (wndExistObjects).
    private const string ExistingPersonsCancelId = "btnCancelWndExistObjects";

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
        Country = new TextBoxControl("Land", Locate.ById(Root, SearchLineId, "txtBoxCountry", "txtBox"));
        State = new TextBoxControl("Bundesland", Locate.ById(Root, SearchLineId, "txtBoxState", "txtBox"));
        PostalCodeAndCity = new TextBoxControl("Postleitzahl Ort", Locate.ById(Root, SearchLineId, "txtBoxPostCode", "txtBox"));
        District = new TextBoxControl("Ortsteil", Locate.ById(Root, SearchLineId, "txtBoxCommunityPart", "txtBox"));
        Street = new TextBoxControl("Straße", Locate.ById(Root, SearchLineId, "txtBoxStreet", "txtBox"));
        HouseNumber = new TextBoxControl("Hausnummer", Locate.ById(Root, SearchLineId, "txtBoxHouseNumber", "txtBox"));
        Phone = new TextBoxControl("Telefon", Locate.ById(Root, SearchLineId, "txtBoxPACPhone", "txtBox"));
        Email = new TextBoxControl("E-Mail", Locate.ById(Root, SearchLineId, "txtBoxPACEmail", "txtBox"));
        Action = new LabelControl("Aktion", Locate.ById(Root, SearchLineId, "tbActionInfo"));
        NewButton = new ButtonControl("Neu", Locate.ById(Root, "btnNewFindBoxPAC"));
        CloseButton = new ButtonControl("Schließen", Locate.ById(Root, "btnClose"));
    }

    public LabelControl FocusedField { get; }

    public TextBoxControl Company { get; }

    /// <summary>Персона в рядку пошуку: «Vorname Nachname».</summary>
    public TextBoxControl Person { get; }

    /// <summary>Land і Bundesland рядок показує лише після кнопки з глобусом (<see cref="ShowCountryAndState"/>).</summary>
    public TextBoxControl Country { get; }

    public TextBoxControl State { get; }

    /// <summary>Одне поле на поштовий індекс і місто: «80331 München».</summary>
    public TextBoxControl PostalCodeAndCity { get; }

    public TextBoxControl District { get; }

    public TextBoxControl Street { get; }

    public TextBoxControl HouseNumber { get; }

    public TextBoxControl Phone { get; }

    public TextBoxControl Email { get; }

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

    /// <summary>Заповнює рядок пошуку персоною, фірмою, адресою фірми, телефоном і e-mail.</summary>
    public void Fill(NewContactData contact) => AllureApi.Step("Заповнити рядок пошуку персоною, фірмою, адресою й комунікаціями", () =>
    {
        Person.Fill($"{contact.Person.FirstName} {contact.Person.LastName}");
        Company.Fill(contact.Company.Name);
        // Без Bundesland ADAICA не збереже адресу: «Unvollständige Angaben … Es fehlen Angaben für Bundesland».
        ShowCountryAndState();
        Country.Fill(contact.Address.Country);
        State.Fill(contact.Address.State);
        PostalCodeAndCity.Fill($"{contact.Address.PostalCode} {contact.Address.City}");
        District.Fill(contact.Address.District);
        Street.Fill(contact.Address.Street);
        HouseNumber.Fill(contact.Address.HouseNumber);
        Phone.Fill(contact.Phone.Value);
        Email.Fill(contact.Email.Value);
    });

    /// <summary>
    /// Кнопка з глобусом додає в рядок пошуку поля Land і Bundesland. Вона реагує лише на клік, а стан
    /// ADAICA між сеансами не запам'ятовує: у новому вікні поля сховані. Bundesland з'являється, лише якщо ввімкнено
    /// «Bundesland berücksichtigen» (<see cref="OptionalLineFields"/>).
    /// </summary>
    public void ShowCountryAndState() => AllureApi.Step("Показати в рядку пошуку Land і Bundesland", () =>
    {
        if (CountryBox is null)
        {
            UiInput.LeftClick(
                () => Root.FindByAutomationId("chkCountryStateHide"),
                () => CountryBox is not null,
                "полів Land і Bundesland у рядку пошуку");
        }

        OptionalLineFields.Show(Root);
    });

    /// <summary>
    /// «Neu»: відкриває вікно «Neue Person/Firma», у якому вже стоїть персона з рядка пошуку. Якщо персона з таким
    /// ім'ям уже є (імена тестових персон повторюються між прогонами), ADAICA спершу показує вікно «Eine gleichnamige
    /// Person ist bereits vorhanden. Meinten Sie vielleicht…» (wndExistObjects): «Ja» бере вибрану наявну персону,
    /// «Nein» продовжує створення нової. Тест створює нову, тож натискає «Nein».
    /// </summary>
    [AllureStep("Відкрити вікно нової персони кнопкою «Neu»")]
    public NewPersonDialog OpenNewPersonDialog()
    {
        NewButton.Click();

        AutomationElement? PersonDialog() => Root.FindFirstChild(cf => cf.ByAutomationId(NewPersonDialog.WindowId));
        AutomationElement? CreateAnywayButton() =>
            ChildWindows(Root).Concat(ChildWindows(_owner))
                .Select(window => window.FindFirstDescendant(cf => cf.ByAutomationId(ExistingPersonsCancelId)))
                .FirstOrDefault(button => button is not null);

        Wait.Until(() => PersonDialog() is not null || CreateAnywayButton() is not null, "вікна «Neue Person/Firma»");
        if (PersonDialog() is null && CreateAnywayButton() is { } createAnyway)
        {
            AllureApi.Step("ADAICA знайшла персону з таким самим ім'ям («Meinten Sie vielleicht…») — «Nein», створити нову", () =>
                new ButtonControl("Nein", () => createAnyway).Click());
        }

        return new NewPersonDialog(_owner, Root);
    }

    /// <summary>Після OK у «Neue Person/Firma» ADAICA закриває й це вікно, але трохи пізніше.</summary>
    [AllureStep("Дочекатися закриття вікна «Finden & Erstellen»")]
    public void WaitUntilClosed() => Wait.Until(() => !IsOpen, "закриття вікна «Finden & Erstellen» після збереження");

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

    private AutomationElement? CountryBox => Root.FindFirstDescendant(cf => cf.ByAutomationId("txtBoxCountry"));

    private static IEnumerable<Window> ChildWindows(Window owner) =>
        owner.FindAllChildren(cf => cf.ByControlType(ControlType.Window)).Select(window => window.AsWindow());
}
