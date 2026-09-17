using System.Globalization;
using ADAICA_AUTOTEST.Infrastructure;
using Allure.Net.Commons.Attributes;
using FlaUI.Core.AutomationElements;

namespace ADAICA_AUTOTEST.Pages;

/// <summary>
/// Бланк проєкту. Ті самі поля ADAICA показує і у вікні «Neues Projekt», і в огляді збереженого проєкту,
/// тому бланк працює в межах переданого кореневого елемента.
/// </summary>
public class ProjectForm(AutomationElement root)
{
    public const string NumberId = "txtRecordFolderNumber";

    // ADAICA показує дати й числа в німецькому форматі: «18.09.2026», «1500,00».
    private const string DateFormat = "dd.MM.yyyy";
    private static readonly CultureInfo AdaicaCulture = CultureInfo.GetCultureInfo("de-DE");

    public TextBox NameTextBox => TextBox("txtRecordName");

    public TextBox ShortNameTextBox => TextBox("txtRecordFolderShortName");

    /// <summary>Номер, який ADAICA призначає проєкту сама (наприклад, PRO0000016).</summary>
    public string Number => TextBox(NumberId).Text;

    public TextBox CostTextBox => TextBox("txtRecordFolderCost");

    public ComboBox CostCurrencyComboBox => Element("cmbUnitCostRecordFolder").AsComboBox();

    public TextBox OutlayTextBox => TextBox("txtRecordFolderOutlay");

    public ComboBox OutlayUnitComboBox => Element("cmbRecordFolderUnitJob").AsComboBox();

    public TextBox CommentTextBox => TextBox("txtRecordFolderComment");

    [AllureStep("Заповнити поля проєкту")]
    public void Fill(ProjectData project)
    {
        NameTextBox.Text = project.Name;
        ShortNameTextBox.Text = project.ShortName;
        SetDate("dateBeginRecordFolder", project.BeginDate);
        SetDate("dateEndRecordFolder", project.EndDate);
        CostTextBox.Text = project.Cost.ToString(AdaicaCulture);
        CostCurrencyComboBox.SelectByText(project.CostCurrency);
        OutlayTextBox.Text = project.Outlay.ToString(AdaicaCulture);
        OutlayUnitComboBox.SelectByText(project.OutlayUnit);
        CommentTextBox.Text = project.Comment;
    }

    /// <summary>Значення, які зараз показує бланк, у тому ж вигляді, що й <see cref="ProjectData"/>.</summary>
    public ProjectData ReadValues() => new(
        NameTextBox.Text,
        ShortNameTextBox.Text,
        GetDate("dateBeginRecordFolder"),
        GetDate("dateEndRecordFolder"),
        decimal.Parse(CostTextBox.Text, NumberStyles.Number, AdaicaCulture),
        CostCurrencyComboBox.SelectedItem?.Text ?? string.Empty,
        decimal.Parse(OutlayTextBox.Text, NumberStyles.Number, AdaicaCulture),
        OutlayUnitComboBox.SelectedItem?.Text ?? string.Empty,
        CommentTextBox.Text);

    protected AutomationElement Element(string automationId) => root.FindByAutomationId(automationId);

    private TextBox TextBox(string automationId) => Element(automationId).AsTextBox();

    // DatePicker3 приймає дату через ValuePattern у форматі поля; показане значення лежить у PART_TextBox1.
    private void SetDate(string datePickerId, DateTime date) =>
        Element(datePickerId).Patterns.Value.Pattern.SetValue(date.ToString(DateFormat, CultureInfo.InvariantCulture));

    private DateTime GetDate(string datePickerId)
    {
        var text = Element(datePickerId).FindByAutomationId("PART_TextBox1").AsTextBox().Text;
        return DateTime.ParseExact(text, DateFormat, CultureInfo.InvariantCulture);
    }
}
