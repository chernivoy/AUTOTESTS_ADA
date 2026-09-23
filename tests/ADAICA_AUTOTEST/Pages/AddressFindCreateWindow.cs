using System.Text.RegularExpressions;
using ADAICA_AUTOTEST.Controls;
using ADAICA_AUTOTEST.Infrastructure;
using Allure.Net.Commons;
using Allure.Net.Commons.Attributes;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using FlaUI.Core.Tools;

namespace ADAICA_AUTOTEST.Pages;

/// <summary>
/// Вікно «Finden &amp; Erstellen» на вкладці Adressen («+» біля «Adressen» у картці контакту). Спершу ADAICA шукає
/// введену адресу в довіднику адрес: якщо вона там є, «Neu» недоступна, а знайдену адресу можна вибрати.
/// «Neu» стає доступною, лише коли заповнено всі поля рядка й такої адреси ще немає.
/// Це те саме дочірнє вікно (AutomationId «This»), що й пошук «Finden», тому впізнаємо його за рядком пошуку адрес.
/// </summary>
public sealed partial class AddressFindCreateWindow
{
    private const string SearchLineId = "ctrAddressBox";

    // Доступність «Neu» ADAICA оновлює не одразу після введення, а після перевірки адреси в довіднику.
    private static readonly TimeSpan NewButtonTimeout = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan SearchStartTimeout = TimeSpan.FromSeconds(3);
    private static readonly TimeSpan SearchTimeout = TimeSpan.FromSeconds(30);

    private readonly Window _owner;

    public AddressFindCreateWindow(Window owner)
    {
        _owner = owner;
        Root = Wait.For(
            () => ChildWindows(owner).FirstOrDefault(window => window.FindFirstDescendant(cf => cf.ByAutomationId(SearchLineId)) is not null),
            "вікна «Finden & Erstellen» (Adressen)");

        Country = LineField("Land", "txtBoxCountry");
        State = LineField("Bundesland", "txtBoxState");
        PostalCodeAndCity = LineField("Postleitzahl Ort", "txtBoxPostCode");
        District = LineField("Ortsteil", "txtBoxCommunityPart");
        Street = LineField("Straße", "txtBoxStreet");
        HouseNumber = LineField("Hausnummer", "txtBoxHouseNumber");
        FindButton = new ButtonControl("Finden", Locate.ById(Root, SearchLineId, "btnSearch"));
        StopButton = new ButtonControl("Abbrechen (Suche)", Locate.ById(Root, SearchLineId, "btnStop"));
        NewButton = new ButtonControl("Neu", Locate.ById(Root, SearchLineId, "btnNew"));
        CloseButton = new ButtonControl("Abbrechen", Locate.ById(Root, "btnClose"));
    }

    public TextBoxControl Country { get; }

    public TextBoxControl State { get; }

    /// <summary>Одне поле на поштовий індекс і місто: «50667 Köln».</summary>
    public TextBoxControl PostalCodeAndCity { get; }

    public TextBoxControl District { get; }

    public TextBoxControl Street { get; }

    public TextBoxControl HouseNumber { get; }

    public ButtonControl FindButton { get; }

    /// <summary>«Abbrechen» у рядку пошуку доступна, лише поки триває пошук.</summary>
    public ButtonControl StopButton { get; }

    public ButtonControl NewButton { get; }

    /// <summary>«Abbrechen» унизу вікна: закриває вікно без адреси.</summary>
    public ButtonControl CloseButton { get; }

    // IsAvailable для закритого вікна WPF лишається true, тому перевіряємо, чи вікно ще серед дочірніх.
    public bool IsOpen => ChildWindows(_owner).Contains(Root);

    /// <summary>Підсумок останнього пошуку: «Keine Daten gefunden.» чи «Es wurden 1 Datensätze gefunden.», або null.</summary>
    public string? ResultSummary =>
        Root.FindAllDescendants(cf => cf.ByControlType(ControlType.Text))
            .Select(text => text.Name)
            .FirstOrDefault(text => Summary().IsMatch(text));

    private Window Root { get; }

    /// <summary>
    /// Заповнює рядок пошуку адресою й перевіряє, чи її можна створити.
    /// </summary>
    /// <returns>true, якщо «Neu» доступна; false, якщо така адреса вже є в довіднику.</returns>
    public bool FillNew(AddressData address) => AllureApi.Step($"Ввести адресу «{address}»", () =>
    {
        Country.Fill(address.Country);
        State.Fill(address.State);
        PostalCodeAndCity.Fill($"{address.PostalCode} {address.City}");
        District.Fill(address.District);
        Street.Fill(address.Street);
        HouseNumber.Fill(address.HouseNumber);

        if (Retry.WhileFalse(() => NewButton.IsEnabled, NewButtonTimeout, ignoreException: true).Success)
            return true;

        // «Neu» недоступна: пошук покаже, чи така адреса вже є.
        Search();
        if (ResultSummary is { } summary && summary.StartsWith("Es wurden", StringComparison.Ordinal))
        {
            AllureApi.Step($"Адреса «{address}» уже є в довіднику: {summary}", () => { });
            return false;
        }

        throw new InvalidOperationException(
            $"Кнопка «Neu» недоступна, хоча адресу «{address}» не знайдено ({ResultSummary ?? "без підсумку пошуку"}).");
    });

    [AllureStep("Шукати адресу кнопкою «Finden»")]
    public void Search()
    {
        FindButton.Click();
        // Швидкий пошук може завершитися раніше, ніж ми побачимо «Abbrechen» доступною.
        Retry.WhileFalse(() => StopButton.IsEnabled, SearchStartTimeout, ignoreException: true);
        Wait.Until(() => !StopButton.IsEnabled && ResultSummary is not null, "завершення пошуку адреси", SearchTimeout);
    }

    /// <summary>«Neu»: відкриває вікно «Neue Adresse anlegen», у якому вже стоїть введена адреса.</summary>
    [AllureStep("Відкрити вікно нової адреси кнопкою «Neu»")]
    public NewAddressDialog OpenNewAddressDialog()
    {
        NewButton.Click();
        return new NewAddressDialog(Root);
    }

    /// <summary>Після OK у вікні нової адреси ADAICA сама закриває й це вікно.</summary>
    public void WaitUntilClosed() => Wait.Until(() => !IsOpen, "закриття вікна «Finden & Erstellen» (Adressen)");

    /// <summary>Для прибирання: закриває вікно без адреси, лише якщо воно ще відкрите.</summary>
    public void CloseIfOpen()
    {
        if (!IsOpen)
            return;

        AllureApi.Step("Закрити вікно «Finden & Erstellen» (Adressen) без адреси", () =>
        {
            CloseButton.Click();
            WaitUntilClosed();
        });
    }

    private TextBoxControl LineField(string name, string boxId) => new(name, Locate.ById(Root, SearchLineId, boxId, "txtBox"));

    private static IEnumerable<Window> ChildWindows(Window owner) =>
        owner.FindAllChildren(cf => cf.ByControlType(ControlType.Window)).Select(window => window.AsWindow());

    [GeneratedRegex(@"^(Es wurden \d+ Datensätze gefunden|Keine Daten gefunden)\.$")]
    private static partial Regex Summary();
}
