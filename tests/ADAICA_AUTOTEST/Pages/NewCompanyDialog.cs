using ADAICA_AUTOTEST.Controls;
using ADAICA_AUTOTEST.Infrastructure;
using Allure.Net.Commons;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;

namespace ADAICA_AUTOTEST.Pages;

/// <summary>
/// Вікно «Neue Person/Firma» для нової фірми. Це дочірнє вікно «Finden &amp; Erstellen» (<see cref="ContactFindCreateWindow"/>),
/// а не головного вікна. Назву фірми вікно бере з рядка пошуку. Після OK ADAICA закриває обидва вікна
/// й переходить до нової фірми в навігаторі.
/// </summary>
public sealed class NewCompanyDialog
{
    private const string WindowId = "wndPersonCompanyCreate";

    private static readonly TimeSpan SaveTimeout = TimeSpan.FromSeconds(30);

    private readonly Window _owner;

    /// <param name="owner">Вікно «Finden &amp; Erstellen».</param>
    public NewCompanyDialog(Window owner)
    {
        _owner = owner;
        Root = Wait.For(() => owner.FindFirstChild(cf => cf.ByAutomationId(WindowId))?.AsWindow(), "вікна «Neue Person/Firma»");

        Name = new TextBoxControl("Firmenname 1. Zeile", Locate.ById(Root, "txtWndPCCreateCompanyA"));
        ShortName = new TextBoxControl("Kurzname", Locate.ById(Root, "txtWndPCCreateShortName"));
        OkButton = new ButtonControl("OK", Locate.ById(Root, "btnWndPCCreateOK"));
        CancelButton = new ButtonControl("Abbrechen", Locate.ById(Root, "btnWndPCCreateCancel"));
    }

    public TextBoxControl Name { get; }

    public TextBoxControl ShortName { get; }

    public ButtonControl OkButton { get; }

    public ButtonControl CancelButton { get; }

    // IsAvailable для закритого вікна WPF лишається true, тому перевіряємо, чи вікно ще серед дочірніх.
    // Після OK зникає й вікно «Finden & Erstellen», а з ним і його дочірні вікна.
    public bool IsOpen => ChildWindows(_owner).Contains(Root);

    private Window Root { get; }

    /// <summary>Назва вже стоїть з рядка пошуку, тому заповнюємо лише решту полів.</summary>
    public void Fill(CompanyData company) =>
        AllureApi.Step("Заповнити поля нової фірми", () => ShortName.Fill(company.ShortName));

    /// <summary>Перевіряє всі поля й повідомляє про всі розбіжності разом, а не лише про першу.</summary>
    public void ShouldHaveValues(CompanyData expected) => AllureApi.Step("Перевірити поля нової фірми", () =>
        this.ShouldSatisfyAllConditions(
            () => Name.ShouldHaveText(expected.Name),
            () => ShortName.ShouldHaveText(expected.ShortName)));

    public void Save() => AllureApi.Step("Зберегти фірму", () =>
    {
        OkButton.Click();

        string? error = null;
        Wait.Until(() =>
        {
            if (!IsOpen)
                return true;

            // Якщо збереження не вдалося, ADAICA показує повідомлення й лишає вікно відкритим.
            var message = MessageBoxDialog.Find(Root) ?? MessageBoxDialog.Find(_owner);
            if (message is null)
                return false;

            error = $"{message.Title}: {message.Message.Text}";
            return true;
        }, "збереження фірми", SaveTimeout);

        if (error is not null)
            throw new InvalidOperationException($"Фірму не збережено, ADAICA показала діалог «{error}».");
    });

    /// <summary>Для прибирання після тесту: закриває вікно без збереження, лише якщо воно ще відкрите.</summary>
    public void CancelIfOpen()
    {
        if (!IsOpen)
            return;

        AllureApi.Step("Закрити вікно нової фірми без збереження", () =>
        {
            CancelButton.Click();
            Wait.Until(() => !IsOpen, "закриття вікна нової фірми");
        });
    }

    // Вікно, якого вже немає, може кинути виняток замість порожнього списку.
    private static IEnumerable<Window> ChildWindows(Window owner)
    {
        try
        {
            return owner.FindAllChildren(cf => cf.ByControlType(ControlType.Window)).Select(window => window.AsWindow()).ToList();
        }
        catch (Exception)
        {
            return [];
        }
    }
}
