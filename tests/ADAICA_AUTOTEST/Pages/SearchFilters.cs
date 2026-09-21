using ADAICA_AUTOTEST.Controls;
using ADAICA_AUTOTEST.Infrastructure;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;

namespace ADAICA_AUTOTEST.Pages;

/// <summary>
/// Фільтри вкладки Dokumentation у вікні «Finden» (розділ «Filtern»): «Suchen nach (Klassentypen)» —
/// які типи записів шукати, і «Suchen in (Suchkriterien)» — у яких полях. Обидва — дерева з прапорцем
/// у кожному пункті; у пунктів немає AutomationId, тому прапорець шукаємо за підписом пункту.
/// </summary>
public sealed class SearchFilters(AutomationElement root)
{
    public const string Tasks = "Aufgaben";
    public const string Projects = "Projekte";
    public const string Folders = "Ordner";
    public const string Appointments = "Termine";
    public const string Notes = "Notizen";

    public const string Name = "Name (Volltextsuche)";
    public const string Remark = "Bemerkung (Volltextsuche)";
    public const string Comment = "Kommentar (Volltextsuche)";
    public const string Number = "Nummer";
    public const string ShortName = "Kurzname";

    /// <summary>Тип запису в «Suchen nach (Klassentypen)», наприклад <see cref="Tasks"/>.</summary>
    public CheckBoxControl ClassType(string name) => Item(name);

    /// <summary>Поле в «Suchen in (Suchkriterien)», наприклад <see cref="Remark"/>.</summary>
    public CheckBoxControl Field(string name) => Item(name);

    /// <summary>Підписи всіх пунктів обох дерев, у яких знято прапорець.</summary>
    public IReadOnlyList<string> Unchecked =>
        root.FindAllDescendants(cf => cf.ByControlType(ControlType.TreeItem))
            .Select(item => (Label: Label(item), Box: item.FindFirstChild(cf => cf.ByControlType(ControlType.CheckBox))))
            .Where(item => item.Label is not null && item.Box?.Patterns.Toggle.Pattern.ToggleState.Value == ToggleState.Off)
            .Select(item => item.Label!)
            .ToList();

    private CheckBoxControl Item(string label) => new(label, () => Wait.For(
        () => root.FindAllDescendants(cf => cf.ByControlType(ControlType.TreeItem))
            .FirstOrDefault(item => Label(item) == label)
            ?.FindFirstChild(cf => cf.ByControlType(ControlType.CheckBox)),
        $"прапорця «{label}» у фільтрах пошуку"));

    // Пункт дерева: прапорець, картинка й TextBlock з підписом.
    private static string? Label(AutomationElement item) => item.FindFirstChild(cf => cf.ByControlType(ControlType.Text))?.Name;
}
