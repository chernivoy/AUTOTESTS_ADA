using ADAICA_AUTOTEST.Controls;
using Allure.Net.Commons;
using FlaUI.Core.AutomationElements;

namespace ADAICA_AUTOTEST.Pages;

/// <summary>
/// Вікно «Neue Person/Firma» для нового співробітника відділу. Це те саме вікно, що й для нової фірми
/// (<see cref="NewCompanyDialog"/>), але з полями персони. Фірму й відділ ADAICA підставляє з вузла, з якого
/// вікно відкрито, і змінити їх не дає, а персону — з рядка пошуку «Finden &amp; Erstellen» у вигляді «Nachname, Vorname».
/// Після OK ADAICA закриває обидва вікна й виділяє нового співробітника під відділом.
/// </summary>
public sealed class NewPersonDialog : ChildDialog
{
    private static readonly TimeSpan SaveTimeout = TimeSpan.FromSeconds(30);

    private readonly Window _mainWindow;

    /// <param name="mainWindow">Головне вікно ADAICA: повідомлення про помилку збереження з'являється як його дочірнє вікно.</param>
    /// <param name="owner">Вікно «Finden &amp; Erstellen».</param>
    public NewPersonDialog(Window mainWindow, Window owner)
        : base(owner, "wndPersonCompanyCreate", "Neue Person/Firma", "btnWndPCCreateOK", "btnWndPCCreateCancel")
    {
        _mainWindow = mainWindow;

        Company = new TextBoxControl("Firmenname 1. Zeile", Locate.ById(Root, "txtWndPCCreateCompanyA"));
        Person = new TextBoxControl("Person", Locate.ById(Root, "txtWndPCCreatePerson"));
        ShortName = new TextBoxControl("Kurzname", Locate.ById(Root, "txtWndPCCreateShortName"));
        Salutation = new ComboBoxControl("Anrede", Locate.ById(Root, "cmbWndPCCreateToAddress"));
        Department = new ComboBoxControl("Organisationseinheit", Locate.ById(Root, "cmbWndPCCreateDepartment"));
        Function = new ComboBoxControl("Funktion", Locate.ById(Root, "cmbWndPCCreateVacancion"));
    }

    public TextBoxControl Company { get; }

    public TextBoxControl Person { get; }

    public TextBoxControl ShortName { get; }

    /// <summary>ADAICA сама вгадує Anrede за ім'ям, тому тест вибирає її явно.</summary>
    public ComboBoxControl Salutation { get; }

    /// <summary>Недоступний для зміни: відділ, з вузла якого відкрито вікно.</summary>
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

    public void Save() => AllureApi.Step("Зберегти співробітника", () => Confirm(SaveTimeout));
}
