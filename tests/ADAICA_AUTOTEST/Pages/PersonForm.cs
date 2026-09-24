using ADAICA_AUTOTEST.Controls;
using Allure.Net.Commons;
using FlaUI.Core.AutomationElements;

namespace ADAICA_AUTOTEST.Pages;

/// <summary>
/// Картка персони в області перегляду: вкладка «Stammdaten» з вкладками Allgemein і Bankverbindungen.
/// Поля невибраної вкладки WPF не показує в дереві UI Automation.
/// </summary>
/// <param name="root">Область перегляду з карткою.</param>
public sealed class PersonForm(AutomationElement root)
{
    public const string NameId = "txtGeneralName";

    /// <summary>Вкладки Allgemein / Bankverbindungen — є в картці персони, хоч яка вкладка відкрита.</summary>
    public const string TabsId = "tabPerson";

    public TabItemControl GeneralTab { get; } = new("Allgemein", Locate.ById(root, TabsId, "tiGeneralPerson"));

    /// <summary>Повне ім'я: «Frau Sabine Hartmann». Саме лише для читання.</summary>
    public TextBoxControl Name { get; } = new("Name", Locate.ById(root, NameId));

    // Briefanrede не перевіряємо: у новому співробітнику ADAICA лишає її шаблоном «Sehr geehrte/r Frau/Herr ,»
    // (імовірний баг, див. README «Співробітник відділу фірми»).
    public void ShouldHaveValues(EmployeeData expected) =>
        AllureApi.Step("Перевірити картку співробітника", () => Name.ShouldHaveText(expected.FullName));
}
