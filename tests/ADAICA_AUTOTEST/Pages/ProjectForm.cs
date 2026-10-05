using ADAICA_AUTOTEST.Controls;
using Allure.Net.Commons;
using FlaUI.Core.AutomationElements;

namespace ADAICA_AUTOTEST.Pages;

/// <summary>
/// Бланк проєкту. Ті самі поля ADAICA показує і у вікні «Neues Projekt», і в огляді збереженого проєкту,
/// тому бланк працює в межах переданого кореневого елемента. Такий самий бланк і в Ordner (<see cref="FolderDialog"/>).
/// </summary>
/// <param name="root">Вікно нового запису або область перегляду.</param>
/// <param name="recordName">Запис у родовому відмінку для кроків звіту: «Заповнити поля проєкту».</param>
public class ProjectForm(AutomationElement root, string recordName = "проєкту")
{
    public const string NumberId = "txtRecordFolderNumber";

    public TextBoxControl Name { get; } = new("Name", Locate.ById(root, "txtRecordName"));

    public TextBoxControl ShortName { get; } = new("Kurzname", Locate.ById(root, "txtRecordFolderShortName"));

    /// <summary>Номер, який ADAICA призначає проєкту сама (наприклад, PRO0000016).</summary>
    public TextBoxControl Number { get; } = new("Nummer", Locate.ById(root, NumberId));

    public DatePickerControl BeginDate { get; } = new("Datum von", Locate.ById(root, "dateBeginRecordFolder"));

    public DatePickerControl EndDate { get; } = new("Datum bis", Locate.ById(root, "dateEndRecordFolder"));

    public NumberBoxControl Cost { get; } = new("Kosten", Locate.ById(root, "txtRecordFolderCost"));

    public ComboBoxControl CostCurrency { get; } = new("Kosten: Währung", Locate.ById(root, "cmbUnitCostRecordFolder"));

    public NumberBoxControl Outlay { get; } = new("Aufwand", Locate.ById(root, "txtRecordFolderOutlay"));

    public ComboBoxControl OutlayUnit { get; } = new("Aufwand: Einheit", Locate.ById(root, "cmbRecordFolderUnitJob"));

    public TextBoxControl Comment { get; } = new("Bemerkung", Locate.ById(root, "txtRecordFolderComment"));

    public RecordClassBox Class { get; } = new(root, "ctrVUBClassSelect");

    public void Fill(ProjectData project) => AllureApi.Step($"Заповнити поля {recordName}", () =>
    {
        // Клас — першим: ADAICA підставляє в «Kurzname» короткі назви класу й підкласу («KUAKKORR»).
        if (project.Class is { } recordClass)
            Class.Select(recordClass);
        Name.Fill(project.Name);
        ShortName.Fill(project.ShortName);
        BeginDate.Fill(project.BeginDate);
        EndDate.Fill(project.EndDate);
        Cost.Fill(project.Cost);
        CostCurrency.Select(project.CostCurrency);
        Outlay.Fill(project.Outlay);
        OutlayUnit.Select(project.OutlayUnit);
        Comment.Fill(project.Comment);
    });

    /// <summary>Перевіряє всі поля й повідомляє про всі розбіжності разом, а не лише про першу.</summary>
    public void ShouldHaveValues(ProjectData expected) => AllureApi.Step($"Перевірити поля {recordName}", () =>
        this.ShouldSatisfyAllConditions(
            () => Class.ShouldHave(expected.Class),
            () => Name.ShouldHaveText(expected.Name),
            () => ShortName.ShouldHaveText(expected.ShortName),
            () => BeginDate.ShouldHaveValue(expected.BeginDate),
            () => EndDate.ShouldHaveValue(expected.EndDate),
            () => Cost.ShouldHaveValue(expected.Cost),
            () => CostCurrency.ShouldHaveSelected(expected.CostCurrency),
            () => Outlay.ShouldHaveValue(expected.Outlay),
            () => OutlayUnit.ShouldHaveSelected(expected.OutlayUnit),
            () => Comment.ShouldHaveText(expected.Comment)));
}
