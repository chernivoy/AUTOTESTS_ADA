using ADAICA_AUTOTEST.Controls;
using Allure.Net.Commons;
using FlaUI.Core.AutomationElements;

namespace ADAICA_AUTOTEST.Pages;

/// <summary>
/// Вікно «Neue Person/Firma» для нової персони: співробітника відділу чи персони разом із новою фірмою. Це те саме
/// вікно, що й для нової фірми (<see cref="NewCompanyDialog"/>), але з полями персони. Фірму, персону («Nachname, Vorname»),
/// адресу, телефон і e-mail воно бере з рядка пошуку «Finden &amp; Erstellen» і змінити їх не дає (крім індексу);
/// відділ — з вузла, з якого вікно відкрито. Після OK ADAICA закриває обидва вікна й виділяє персону під відділом.
/// </summary>
public sealed class NewPersonDialog : ChildDialog
{
    public const string WindowId = "wndPersonCompanyCreate";

    private static readonly TimeSpan SaveTimeout = TimeSpan.FromSeconds(30);

    private readonly Window _mainWindow;

    /// <param name="mainWindow">Головне вікно ADAICA: повідомлення про помилку збереження з'являється як його дочірнє вікно.</param>
    /// <param name="owner">Вікно «Finden &amp; Erstellen».</param>
    public NewPersonDialog(Window mainWindow, Window owner)
        : base(owner, WindowId, "Neue Person/Firma", "btnWndPCCreateOK", "btnWndPCCreateCancel")
    {
        _mainWindow = mainWindow;

        Company = new TextBoxControl("Firmenname 1. Zeile", Locate.ById(Root, "txtWndPCCreateCompanyA"));
        Person = new TextBoxControl("Person", Locate.ById(Root, "txtWndPCCreatePerson"));
        Phone = new TextBoxControl("Telefon", Locate.ById(Root, "txtWndPCCreatePhone"));
        PhoneType = new ComboBoxControl("Kommunikationsart: Telefon", Locate.ById(Root, "cmbWndPCCreatePhoneType"));
        Email = new TextBoxControl("E-Mail", Locate.ById(Root, "txtWndPCCreateEmail"));
        EmailType = new ComboBoxControl("Kommunikationsart: E-Mail", Locate.ById(Root, "cmbWndPCCreateEmailType"));
        PostalCode = new TextBoxControl("Postleitzahl", Locate.ById(Root, "txtAddressCreatePLZ", "txtBox"));
        City = new TextBoxControl("Ort", Locate.ById(Root, "txtAddressCreateCommunity"));
        District = new TextBoxControl("Ortsteil", Locate.ById(Root, "txtAddressCreateCommunityPart"));
        Street = new TextBoxControl("Straße", Locate.ById(Root, "txtAddressCreateStreet"));
        HouseNumber = new TextBoxControl("Hausnummer", Locate.ById(Root, "txtAddressCreateAddress"));
        Country = new TextBoxControl("Land", Locate.ById(Root, "txtAddressCreateCountry"));
        State = new TextBoxControl("Bundesland", Locate.ById(Root, "txtAddressCreateState"));
        CorrespondencePreference = new ComboBoxControl("Korrespondenz-Präferenz", Locate.ById(Root, "cmbWndPCCreateCorrespondence"));
        ShortName = new TextBoxControl("Kurzname", Locate.ById(Root, "txtWndPCCreateShortName"));
        TradeRegisterNumber = new TextBoxControl("HRG-Nr.", Locate.ById(Root, "txtWndPCCreateRegNumber"));
        Salutation = new ComboBoxControl("Anrede", Locate.ById(Root, "cmbWndPCCreateToAddress"));
        Title = new TextBoxControl("Titel", Locate.ById(Root, "txtWndPCCreateTitle"));
        NameSuffix = new ComboBoxControl("Namenszusatz", Locate.ById(Root, "cmbFullNameSuffixWndFindCreate"));
        Birthday = new DatePickerControl("Geburtstag", Locate.ById(Root, "dateWndPCCreateBirthday"));
        MaidenName = new TextBoxControl("Geburtsname", Locate.ById(Root, "txtWndPCCreateMaidenName"));
        Department = new ComboBoxControl("Organisationseinheit", Locate.ById(Root, "cmbWndPCCreateDepartment"));
        Function = new ComboBoxControl("Funktion", Locate.ById(Root, "cmbWndPCCreateVacancion"));
    }

    public TextBoxControl Company { get; }

    public TextBoxControl Person { get; }

    public TextBoxControl Phone { get; }

    public ComboBoxControl PhoneType { get; }

    public TextBoxControl Email { get; }

    public ComboBoxControl EmailType { get; }

    /// <summary>Єдине поле адреси, яке можна змінити: решту частин ADAICA бере з рядка пошуку.</summary>
    public TextBoxControl PostalCode { get; }

    public TextBoxControl City { get; }

    public TextBoxControl District { get; }

    public TextBoxControl Street { get; }

    public TextBoxControl HouseNumber { get; }

    /// <summary>Land і Bundesland ADAICA визначає з індексу й міста.</summary>
    public TextBoxControl Country { get; }

    public TextBoxControl State { get; }

    public ComboBoxControl CorrespondencePreference { get; }

    public TextBoxControl ShortName { get; }

    public TextBoxControl TradeRegisterNumber { get; }

    /// <summary>ADAICA сама вгадує Anrede за ім'ям, тому тест вибирає її явно.</summary>
    public ComboBoxControl Salutation { get; }

    /// <summary>Лише для читання: титул вибирають у вікні «Titel» (кнопка «...»).</summary>
    public TextBoxControl Title { get; }

    public ComboBoxControl NameSuffix { get; }

    public DatePickerControl Birthday { get; }

    public TextBoxControl MaidenName { get; }

    /// <summary>Недоступний для зміни, якщо вікно відкрито з вузла відділу.</summary>
    public ComboBoxControl Department { get; }

    public ComboBoxControl Function { get; }

    protected override IEnumerable<Window> MessageOwners => base.MessageOwners.Append(_mainWindow);

    /// <summary>Персона вже стоїть з рядка пошуку, тому заповнюємо лише решту полів.</summary>
    public void Fill(EmployeeData employee) => AllureApi.Step("Заповнити поля нового співробітника", () =>
    {
        Salutation.Select(employee.Salutation);
        ShortName.Fill(employee.ShortName);
        Function.Select(employee.Function);
    });

    /// <summary>Перевіряє всі поля й повідомляє про всі розбіжності разом, а не лише про першу.</summary>
    public void ShouldHaveValues(EmployeeData expected, string company, string department) =>
        AllureApi.Step("Перевірити поля нового співробітника", () =>
            this.ShouldSatisfyAllConditions(
                () => Company.ShouldHaveText(company),
                () => Person.ShouldHaveText(expected.SortName),
                () => ShortName.ShouldHaveText(expected.ShortName),
                () => Salutation.ShouldHaveSelected(expected.Salutation),
                () => Department.Text.ShouldBe(department, "Список «Organisationseinheit»"),
                () => Function.ShouldHaveSelected(expected.Function)));

    /// <summary>Перевіряє, що вікно взяло з рядка пошуку фірму, персону, адресу, телефон і e-mail.</summary>
    public void ShouldHaveSearchLineValues(NewContactData expected) =>
        AllureApi.Step("Перевірити поля, взяті з рядка пошуку", () =>
            this.ShouldSatisfyAllConditions(
                () => Company.ShouldHaveText(expected.Company.Name),
                () => Person.ShouldHaveText(expected.Person.SortName),
                () => Phone.ShouldHaveText(expected.Phone.Value),
                () => Email.ShouldHaveText(expected.Email.Value),
                () => PostalCode.ShouldHaveText(expected.Address.PostalCode),
                () => City.ShouldHaveText(expected.Address.City),
                () => District.ShouldHaveText(expected.Address.District),
                () => Street.ShouldHaveText(expected.Address.Street),
                () => HouseNumber.ShouldHaveText(expected.Address.HouseNumber),
                () => Country.ShouldHaveText(expected.Address.Country),
                () => State.ShouldHaveText(expected.Address.State)));

    /// <summary>
    /// Доповнює поля, яких рядок пошуку не заповнив: порожні текстові поля заповнює, у списках вибирає потрібне значення.
    /// Поля, які вже заповнено, не чіпає — їх перевіряє <see cref="ShouldHaveValues(NewContactData)"/>.
    /// </summary>
    public void FillMissing(NewContactData contact) => AllureApi.Step("Доповнити порожні поля нової фірми й персони", () =>
    {
        var person = contact.Person;
        PhoneType.Select(contact.Phone.Type);
        EmailType.Select(contact.Email.Type);
        CorrespondencePreference.Select(contact.CorrespondencePreference);
        FillIfEmpty(ShortName, contact.Company.ShortName);
        FillIfEmpty(TradeRegisterNumber, contact.TradeRegisterNumber);
        Salutation.Select(person.Salutation);
        if (Title.Text.Length == 0 && person.Title.Length > 0)
        {
            var titles = OpenTitlesDialog();
            titles.Check(person.Title);
            titles.Confirm();
        }

        if (person.NameSuffix.Length > 0)
            NameSuffix.Select(person.NameSuffix);
        if (Birthday.Value is null && person.Birthday is { } birthday)
            Birthday.Fill(birthday);
        FillIfEmpty(MaidenName, person.MaidenName);
        Function.Select(person.Function);
    });

    /// <summary>Перевіряє всі поля вікна й повідомляє про всі розбіжності разом, а не лише про першу.</summary>
    public void ShouldHaveValues(NewContactData expected) => AllureApi.Step("Перевірити всі поля нової фірми й персони", () =>
    {
        var person = expected.Person;
        this.ShouldSatisfyAllConditions(
            () => ShouldHaveSearchLineValues(expected),
            () => PhoneType.ShouldHaveSelected(expected.Phone.Type),
            () => EmailType.ShouldHaveSelected(expected.Email.Type),
            () => CorrespondencePreference.ShouldHaveSelected(expected.CorrespondencePreference),
            () => ShortName.ShouldHaveText(expected.Company.ShortName),
            () => TradeRegisterNumber.ShouldHaveText(expected.TradeRegisterNumber),
            () => Salutation.ShouldHaveSelected(person.Salutation),
            () => Title.ShouldHaveText(person.Title),
            () => NameSuffix.ShouldHaveSelected(person.NameSuffix.Length > 0 ? person.NameSuffix : null),
            () => Birthday.Value.ShouldBe(person.Birthday?.Date, "Поле «Geburtstag»"),
            () => MaidenName.ShouldHaveText(person.MaidenName),
            () => Department.ShouldHaveSelected(NewContactData.Department),
            () => Function.ShouldHaveSelected(person.Function));
    });

    public void Save() => AllureApi.Step("Зберегти вікно «Neue Person/Firma»", () => Confirm(SaveTimeout));

    /// <summary>Кнопка «...» біля поля «Titel».</summary>
    private TitlesDialog OpenTitlesDialog() => AllureApi.Step("Відкрити вікно «Titel» кнопкою «...»", () =>
    {
        new ButtonControl("...", Locate.ById(Root, "txtWndPCCreateTitle", "PART_Button")).Click();
        return new TitlesDialog(Root);
    });

    private static void FillIfEmpty(TextBoxControl field, string value)
    {
        if (field.Text.Length == 0)
            field.Fill(value);
    }
}
