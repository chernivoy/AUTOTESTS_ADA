using ADAICA_AUTOTEST.Controls;
using Allure.Net.Commons;
using FlaUI.Core.AutomationElements;

namespace ADAICA_AUTOTEST.Pages;

/// <summary>
/// Бланк завдання. Ті самі поля ADAICA показує і у вікні «Neue Aufgabe», і в огляді збереженого завдання.
/// </summary>
public class TaskForm(AutomationElement root)
{
    public const string NumberId = "txtNumberRecordTask";

    public TextBoxControl Name { get; } = new("Name", Locate.ById(root, "txtRecordName"));

    public TextBoxControl ShortName { get; } = new("Kurzname", Locate.ById(root, "txtShortNameRecordTask"));

    /// <summary>Номер, який ADAICA призначає завданню сама (наприклад, TAS0000001).</summary>
    public TextBoxControl Number { get; } = new("Nummer", Locate.ById(root, NumberId));

    /// <summary>Дата й час створення («18.09.2026 08:48»), які ADAICA заповнює сама.</summary>
    public TextBoxControl Created { get; } = new("Erstelldatum", Locate.ById(root, "txtEntryDateRecordTask"));

    public DateTimePickerControl Begin { get; } = new("Beginnt am", Locate.ById(root, "dpBeginDateRecordTask"));

    public DateTimePickerControl Due { get; } = new("Fällig am", Locate.ById(root, "dpEndDateRecordTask"));

    public DateTimePickerControl Done { get; } = new("Erledigt am", Locate.ById(root, "dpDoneDateRecordTask"));

    public DatePickerControl ClosedOn { get; } = new("Geschlossen an", Locate.ById(root, "dpCloseDateRecordTask"));

    public ComboBoxControl AcceptanceType { get; } = new("Annahmeart", Locate.ById(root, "cmbAuthorityRecordTask"));

    public ComboBoxControl Priority { get; } = new("Priorität", Locate.ById(root, "cmbPriorityRecordTask"));

    public ComboBoxControl Importance { get; } = new("Wichtigkeit", Locate.ById(root, "cmbSignificanceRecordTask"));

    public NumberBoxControl EstimatedDuration { get; } = new("Dauer (gesch.)", Locate.ById(root, "ctrEstimateTextBox"));

    public ComboBoxControl EstimatedDurationUnit { get; } = new("Dauer (gesch.): Einheit", Locate.ById(root, "ctrEstimateComboBox"));

    public NumberBoxControl ActualDuration { get; } = new("Dauer (real)", Locate.ById(root, "ctrUsedTimeTextBox"));

    public ComboBoxControl ActualDurationUnit { get; } = new("Dauer (real): Einheit", Locate.ById(root, "ctrUsedTimeComboBox"));

    public TextBoxControl OrderNumber { get; } = new("Auftragsnummer", Locate.ById(root, "txtOrederNoRecordTask"));

    public TextBoxControl PurchaseOrderNumber { get; } = new("Bestell-Nr.", Locate.ById(root, "txtBookingNoRecordTask"));

    public NumberBoxControl PercentDone { get; } = new("% erledigt", Locate.ById(root, "ctrExecutedRecordTask", "txtUpDown"));

    // Поля з довідника: у кожному контейнері однакові AutomationId, тому шлях іде через контейнер.
    public ComboBoxControl Location { get; } = new("Standort", Locate.ById(root, "ctrPlaceRecordTask", "comboBoxVurCatalog"));

    public TextBoxControl LocationText { get; } = new("Standort: Text", Locate.ById(root, "ctrPlaceRecordTask", "textBoxVurCatalog"));

    public ComboBoxControl CustomerBenefit { get; } = new("Kundennutzen", Locate.ById(root, "ctrCustomerValueRecordTask", "comboBoxVurCatalog"));

    public TextBoxControl CustomerBenefitText { get; } = new("Kundennutzen: Text", Locate.ById(root, "ctrCustomerValueRecordTask", "textBoxVurCatalog"));

    public ComboBoxControl Hint { get; } = new("Hinweis", Locate.ById(root, "ctrReferenceRecordTask", "comboBoxVurCatalog"));

    public TextBoxControl HintText { get; } = new("Hinweis: Text", Locate.ById(root, "ctrReferenceRecordTask", "textBoxVurCatalog"));

    /// <summary>Вкладка простої примітки; за замовчуванням ADAICA може відкрити форматований редактор.</summary>
    public TabItemControl SimpleRemarkTab { get; } = new("Bemerkung (einfach)", Locate.ById(root, "tiSimpleRemark"));

    public TextBoxControl Remark { get; } = new("Bemerkung", Locate.ById(root, "ctrSimpleRemark", "txtRemark"));

    public void Fill(TaskData task) => AllureApi.Step("Заповнити поля завдання", () =>
    {
        Name.Fill(task.Name);
        ShortName.Fill(task.ShortName);
        Begin.Fill(task.Begin);
        Due.Fill(task.Due);
        Done.Fill(task.Done);
        ClosedOn.Fill(task.ClosedOn);
        AcceptanceType.Select(task.AcceptanceType);
        Priority.Select(task.Priority);
        Importance.Select(task.Importance);
        EstimatedDuration.Fill(task.EstimatedDuration);
        EstimatedDurationUnit.Select(task.EstimatedDurationUnit);
        ActualDuration.Fill(task.ActualDuration);
        ActualDurationUnit.Select(task.ActualDurationUnit);
        OrderNumber.Fill(task.OrderNumber);
        PurchaseOrderNumber.Fill(task.PurchaseOrderNumber);
        PercentDone.Fill(task.PercentDone);
        Location.Select(task.Location);
        LocationText.Fill(task.LocationText);
        CustomerBenefit.Select(task.CustomerBenefit);
        CustomerBenefitText.Fill(task.CustomerBenefitText);
        Hint.Select(task.Hint);
        HintText.Fill(task.HintText);
        SimpleRemarkTab.Select();
        Remark.Fill(task.Remark);
    });

    /// <summary>Перевіряє всі поля й повідомляє про всі розбіжності разом, а не лише про першу.</summary>
    public void ShouldHaveValues(TaskData expected) => AllureApi.Step("Перевірити поля завдання", () =>
    {
        SimpleRemarkTab.Select();
        this.ShouldSatisfyAllConditions(
            () => Name.ShouldHaveText(expected.Name),
            () => ShortName.ShouldHaveText(expected.ShortName),
            () => Begin.ShouldHaveValue(expected.Begin),
            () => Due.ShouldHaveValue(expected.Due),
            () => Done.ShouldHaveValue(expected.Done),
            () => ClosedOn.ShouldHaveValue(expected.ClosedOn),
            () => AcceptanceType.ShouldHaveSelected(expected.AcceptanceType),
            () => Priority.ShouldHaveSelected(expected.Priority),
            () => Importance.ShouldHaveSelected(expected.Importance),
            () => EstimatedDuration.ShouldHaveValue(expected.EstimatedDuration),
            () => EstimatedDurationUnit.ShouldHaveSelected(expected.EstimatedDurationUnit),
            () => ActualDuration.ShouldHaveValue(expected.ActualDuration),
            () => ActualDurationUnit.ShouldHaveSelected(expected.ActualDurationUnit),
            () => OrderNumber.ShouldHaveText(expected.OrderNumber),
            () => PurchaseOrderNumber.ShouldHaveText(expected.PurchaseOrderNumber),
            () => PercentDone.ShouldHaveValue(expected.PercentDone),
            () => Location.ShouldHaveSelected(expected.Location),
            () => LocationText.ShouldHaveText(expected.LocationText),
            () => CustomerBenefit.ShouldHaveSelected(expected.CustomerBenefit),
            () => CustomerBenefitText.ShouldHaveText(expected.CustomerBenefitText),
            () => Hint.ShouldHaveSelected(expected.Hint),
            () => HintText.ShouldHaveText(expected.HintText),
            () => Remark.ShouldHaveText(expected.Remark));
    });
}
