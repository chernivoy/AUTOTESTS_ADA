using ADAICA_AUTOTEST.Controls;
using Allure.Net.Commons;
using FlaUI.Core.AutomationElements;

namespace ADAICA_AUTOTEST.Pages;

/// <summary>
/// Бланк інфозапису (Notiz). Ті самі поля ADAICA показує і у вікні «Neue Notiz», і в огляді збереженого запису.
/// Номера в інфозапису немає.
/// </summary>
public class NoteForm(AutomationElement root)
{
    public const string RemarkId = "txtCommentRecordHistory";

    public TextBoxControl Name { get; } = new("Name", Locate.ById(root, "txtRecordName"));

    public TextBoxControl ShortName { get; } = new("Kurzname", Locate.ById(root, "txtShortNameRecordHistory"));

    public DateTimePickerControl Begin { get; } = new("Beginnt", Locate.ById(root, "dpBeginDateRecordHistory"));

    public ComboBoxControl Result { get; } = new("Ergebnis", Locate.ById(root, "cmbResultRecordHistory"));

    public TextBoxControl Remark { get; } = new("Bemerkung", Locate.ById(root, RemarkId));

    public void Fill(NoteData note) => AllureApi.Step("Заповнити поля інфозапису", () =>
    {
        Name.Fill(note.Name);
        ShortName.Fill(note.ShortName);
        Begin.Fill(note.Begin);
        if (note.Result is not null)
            Result.Select(note.Result);
        Remark.Fill(note.Remark);
    });

    /// <summary>Перевіряє всі поля й повідомляє про всі розбіжності разом, а не лише про першу.</summary>
    public void ShouldHaveValues(NoteData expected) => AllureApi.Step("Перевірити поля інфозапису", () =>
        this.ShouldSatisfyAllConditions(
            () => Name.ShouldHaveText(expected.Name),
            () => ShortName.ShouldHaveText(expected.ShortName),
            () => Begin.ShouldHaveValue(expected.Begin),
            () => Result.ShouldHaveSelected(expected.Result),
            () => Remark.ShouldHaveText(expected.Remark)));
}
