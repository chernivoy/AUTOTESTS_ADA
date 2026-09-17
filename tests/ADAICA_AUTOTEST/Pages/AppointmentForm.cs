using ADAICA_AUTOTEST.Controls;
using Allure.Net.Commons;
using FlaUI.Core.AutomationElements;

namespace ADAICA_AUTOTEST.Pages;

/// <summary>
/// Бланк терміну. Ті самі поля ADAICA показує і у вікні «Neuer Termin», і в огляді збереженого терміну.
/// </summary>
public class AppointmentForm(AutomationElement root)
{
    public const string NumberId = "txtNumberRecordAppoint";

    public TextBoxControl Name { get; } = new("Name", Locate.ById(root, "txtRecordName"));

    public TextBoxControl ShortName { get; } = new("Kurzname", Locate.ById(root, "txtShortNameRecordAppoint"));

    /// <summary>Номер, який ADAICA призначає терміну сама (наприклад, APP0000001).</summary>
    public TextBoxControl Number { get; } = new("Nummer", Locate.ById(root, NumberId));

    public DateTimePickerControl Begin { get; } = new("Beginn", Locate.ById(root, "dpBeginDateRecordAppoint"));

    public DateTimePickerControl End { get; } = new("Ende", Locate.ById(root, "dpEndDateRecordAppoint"));

    public CheckBoxControl AllDay { get; } = new("Ganztägig", Locate.ById(root, "chkRecordAppointFullTime"));

    /// <summary>Прапорець «Aufwand»: лише коли він увімкнений, поле трудовитрат можна редагувати.</summary>
    public CheckBoxControl EffortEnabled { get; } = new("Aufwand (z.B. Arbeitszeit)", Locate.ById(root, "radioDateByDurationRecordAppoint"));

    public NumberBoxControl Effort { get; } = new("Aufwand", Locate.ById(root, "ctrDurationRecordAppoint", "txtDuration"));

    public ComboBoxControl EffortUnit { get; } = new("Aufwand: Einheit", Locate.ById(root, "ctrDurationRecordAppoint", "cmbDuration"));

    public ComboBoxControl Importance { get; } = new("Wichtigkeit", Locate.ById(root, "cmbSignificanceRecordAppoint"));

    public TextBoxControl Location { get; } = new("Ort", Locate.ById(root, "txtLocationRecordAppoint", "modTxtBox"));

    public TextBoxControl Remark { get; } = new("Bemerkung", Locate.ById(root, "txtCommentRecordAppoint"));

    public void Fill(AppointmentData appointment) => AllureApi.Step("Заповнити поля терміну", () =>
    {
        Name.Fill(appointment.Name);
        ShortName.Fill(appointment.ShortName);
        Begin.Fill(appointment.Begin);
        End.Fill(appointment.End);
        AllDay.SetChecked(appointment.AllDay);
        EffortEnabled.SetChecked(appointment.Effort is not null);
        EffortUnit.Select(appointment.EffortUnit);
        if (appointment.Effort is { } effort)
            Effort.Fill(effort);
        Importance.Select(appointment.Importance);
        Location.Fill(appointment.Location);
        Remark.Fill(appointment.Remark);
    });

    /// <summary>Перевіряє всі поля й повідомляє про всі розбіжності разом, а не лише про першу.</summary>
    public void ShouldHaveValues(AppointmentData expected) => AllureApi.Step("Перевірити поля терміну", () =>
    {
        List<Action> conditions =
        [
            () => Name.ShouldHaveText(expected.Name),
            () => ShortName.ShouldHaveText(expected.ShortName),
            () => Begin.ShouldHaveValue(expected.Begin),
            () => End.ShouldHaveValue(expected.End),
            () => AllDay.ShouldBeChecked(expected.AllDay),
            () => EffortEnabled.ShouldBeChecked(expected.Effort is not null),
            () => EffortUnit.ShouldHaveSelected(expected.EffortUnit),
            () => Importance.ShouldHaveSelected(expected.Importance),
            () => Location.ShouldHaveText(expected.Location),
            () => Remark.ShouldHaveText(expected.Remark),
        ];

        // Без прапорця «Aufwand» поле показує розраховану тривалість, а не введене значення.
        if (expected.Effort is { } effort)
            conditions.Add(() => Effort.ShouldHaveValue(effort));

        this.ShouldSatisfyAllConditions(conditions.ToArray());
    });
}
