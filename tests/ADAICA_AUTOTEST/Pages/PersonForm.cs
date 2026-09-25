using ADAICA_AUTOTEST.Controls;
using ADAICA_AUTOTEST.Infrastructure;
using Allure.Net.Commons;
using FlaUI.Core.AutomationElements;

namespace ADAICA_AUTOTEST.Pages;

/// <summary>
/// Картка персони в області перегляду: вкладка «Stammdaten» з вкладками Allgemein і Bankverbindungen.
/// Поля невибраної вкладки WPF не показує в дереві UI Automation.
/// </summary>
/// <param name="owner">Головне вікно ADAICA: меню картки — його дочірні вікна.</param>
/// <param name="root">Область перегляду з карткою.</param>
public sealed class PersonForm(Window owner, AutomationElement root)
{
    public const string NameId = "txtGeneralName";

    /// <summary>Вкладки Allgemein / Bankverbindungen — є в картці персони, хоч яка вкладка відкрита.</summary>
    public const string TabsId = "tabPerson";

    public TabItemControl GeneralTab { get; } = new("Allgemein", Locate.ById(root, TabsId, "tiGeneralPerson"));

    /// <summary>Повне ім'я: «Frau Dr. Claudia von Gerber». Саме лише для читання.</summary>
    public TextBoxControl Name { get; } = new("Name", Locate.ById(root, NameId));

    public DatePickerControl Birthday { get; } = new("Geburtstag", Locate.ById(root, "dateBirthday"));

    public TextBoxControl MaidenName { get; } = new("Geburtsname", Locate.ById(root, "txtBoxNameOfBirth"));

    public CommunicationsList Communications { get; } = new(owner, root, "communications");

    /// <summary>
    /// Рядки списку «Adressen» (див. <see cref="AddressData.ListRow"/>). Адресу фірми персона показує з назвою фірми
    /// в категорії: «Straßenanschrift (Hotel Seeblick …)».
    /// </summary>
    public IReadOnlyList<string> AddressRows => CompanyForm.AddressRowsIn(root.FindByAutomationId("addresses"));

    // Briefanrede не перевіряємо: у новій персоні ADAICA лишає її шаблоном «Sehr geehrte/r Frau/Herr ,»
    // (імовірний баг, див. README «Співробітник відділу фірми»).
    public void ShouldHaveValues(EmployeeData expected) =>
        AllureApi.Step("Перевірити картку співробітника", () => Name.ShouldHaveText(expected.FullName));

    /// <summary>Перевіряє персону, створену разом із фірмою: телефон і e-mail — її, адреса — фірми.</summary>
    public void ShouldHaveValues(NewContactData expected) => AllureApi.Step("Перевірити картку персони", () =>
    {
        var person = expected.Person;
        var address = expected.Address with { Category = $"{expected.Address.Category} ({expected.Company.Name})" };
        this.ShouldSatisfyAllConditions(
            () => Name.ShouldHaveText(person.FullName),
            () => Birthday.Value.ShouldBe(person.Birthday?.Date, "Поле «Geburtstag»"),
            () => MaidenName.ShouldHaveText(person.MaidenName),
            () => Communications.ShouldHaveItems([expected.Phone, expected.Email]),
            () => AllureApi.Step($"Перевірити «Adressen»: {address.ListRow}",
                () => AddressRows.ShouldBe(new[] { address.ListRow }, "Adressen")));
    });
}
