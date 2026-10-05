using System.Globalization;
using ADAICA_AUTOTEST.Controls;
using ADAICA_AUTOTEST.Infrastructure;
using Allure.Net.Commons;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;

namespace ADAICA_AUTOTEST.Pages;

/// <summary>
/// Огляд документа (вкладка «Übersicht» → «Dokument»): дані файлу, які ADAICA заповнює сама, і поля запису. Нижче
/// ADAICA показує вміст документа (DevExpress RichEdit), але його тексту в дереві UI Automation немає.
/// </summary>
public sealed class DocumentForm(AutomationElement root, Window mainWindow)
{
    public const string NameId = "txtNameRecordDocument";

    // Нову версію ADAICA зберігає, коли Word закриє файл, а це трохи пізніше за закриття документа.
    private static readonly TimeSpan NewVersionTimeout = TimeSpan.FromSeconds(30);

    /// <summary>
    /// «Bearbeiten»: ADAICA кладе копію файлу у свою теку, відкриває її у Word і стежить за нею. Коли Word збереже й
    /// закриє файл, ADAICA сама зберігає його як нову версію.
    /// </summary>
    public ButtonControl EditButton { get; } = new("Bearbeiten", Locate.ById(root, "btnEditDocumentCRecMainFormBase"));

    /// <summary>«Version»: відкриває вікно версій документа.</summary>
    public ButtonControl VersionsButton { get; } = new("Version", Locate.ById(root, "btnViewVersionRecordDocument"));

    public TextBoxControl Name { get; } = new("Dokumentname", Locate.ById(root, NameId));

    public TextBoxControl ShortName { get; } = new("Kurzname", Locate.ById(root, "txtShortNameRecordDocument"));

    public TextBoxControl FileType { get; } = new("Dateityp", Locate.ById(root, "txtFileExtentionRecordDocument"));

    public TextBoxControl FileSize { get; } = new("Dateigröße", Locate.ById(root, "txtBlobSizeRecordDocument"));

    /// <summary>«Erstelldatum (Aktuelle Version)»: дата й час до хвилини.</summary>
    public TextBoxControl EntryDate { get; } = new("Erstelldatum (Aktuelle Version)", Locate.ById(root, "txtEntryDateRecordDocument"));

    public TextBoxControl Remark { get; } = new("Bemerkung", Locate.ById(root, "txtCommentRecordDocument"));

    public ComboBoxControl Status { get; } = new("Status", Locate.ById(root, "cmbStatusRecordDocument"));

    public RecordClassBox Class { get; } = new(root, "ctrVUBClassSelectRecordDocument");

    /// <summary>OK під оглядом: стає доступною, коли поля змінено, і зберігає зміни.</summary>
    public ButtonControl OkButton { get; } = new("OK", Locate.ById(root, "btnOKCtrMainForm"));

    private ButtonControl CancelButton { get; } = new("Abbrechen", Locate.ById(root, "btnCancelCtrMainForm"));

    /// <summary>«Version: 1 / 1» — поточна версія й кількість версій, два окремі написи.</summary>
    public string Version =>
        $"{new LabelControl("Version", Locate.ById(root, "tbViewActualVersionRecordDocument")).Text} / "
        + new LabelControl("Versionen", Locate.ById(root, "tbViewCountVersionRecordDocument")).Text;

    /// <summary>«Erstellt durch» — список користувачів.</summary>
    public IReadOnlyList<string> Creators =>
        root.FindByAutomationId("lbCreatorRecordDocument")
            .FindAllChildren(cf => cf.ByControlType(ControlType.ListItem))
            .Select(item => item.Name)
            .ToList();

    /// <summary>Чекає, доки ADAICA збереже нову версію: кількість версій стане <paramref name="count"/>.</summary>
    public void WaitForVersionCount(int count) =>
        AllureApi.Step($"Дочекатися версії {count} документа", () =>
            Wait.Until(
                () => new LabelControl("Versionen", Locate.ById(root, "tbViewCountVersionRecordDocument")).Text == count.ToString(CultureInfo.InvariantCulture),
                $"{count} версій документа",
                NewVersionTimeout));

    /// <summary>Кнопка «Version» → вікно версій документа.</summary>
    public DocumentVersionsWindow OpenVersions()
    {
        VersionsButton.Click();
        return new DocumentVersionsWindow(mainWindow);
    }

    /// <summary>
    /// Вибирає клас документа в огляді й зберігає кнопкою OK. Вибраний клас ADAICA підставляє в «Kurzname» («KUAKKORR»),
    /// тож після нього тест вводить свою коротку назву <paramref name="shortName"/>.
    /// </summary>
    public void SetClass(RecordClass recordClass, string shortName) =>
        AllureApi.Step($"Задати документу клас {recordClass} і зберегти", () =>
        {
            Class.Select(recordClass);
            ShortName.Fill(shortName);
            OkButton.Click();
            Wait.Until(() => !OkButton.IsEnabled, "збереження документа", TimeSpan.FromSeconds(30));
        });

    /// <summary>Для прибирання: відкидає незбережені зміни огляду кнопкою «Abbrechen».</summary>
    public void CancelIfEditing()
    {
        if (CancelButton.IsEnabled)
            CancelButton.Click();
    }

    /// <summary>Перевіряє всі поля й повідомляє про всі розбіжності разом, а не лише про першу.</summary>
    public void ShouldHaveValues(DocumentData expected) => AllureApi.Step("Перевірити поля документа", () =>
        this.ShouldSatisfyAllConditions(
            () => Name.ShouldHaveText(expected.Name),
            () => ShortName.ShouldHaveText(expected.ShortName),
            () => FileType.ShouldHaveText(expected.FileType),
            () => FileSize.ShouldHaveText(expected.FileSize),
            () => Creators.ShouldBe([expected.Creator], "Erstellt durch"),
            () => Version.ShouldBe(expected.Version, "Version"),
            () => Status.ShouldHaveSelected(expected.Status),
            () => Remark.ShouldHaveText(expected.Remark),
            () => Class.ShouldHave(expected.Class)));

    /// <summary>
    /// Дата створення лежить між <paramref name="from"/> і <paramref name="to"/>. ADAICA показує її до хвилини,
    /// тож нижню межу обрізаємо до хвилини.
    /// </summary>
    public void ShouldBeCreatedBetween(DateTime from, DateTime to) =>
        AllureApi.Step($"Перевірити «{EntryDate.Name}»: між {AdaicaFormat.FormatDateTime(from)} і {AdaicaFormat.FormatDateTime(to)}", () =>
        {
            var parts = EntryDate.Text.Split(' ');
            var created = AdaicaFormat.ParseDate(parts[0])!.Value + AdaicaFormat.ParseTime(parts[1])!.Value;
            var fromMinute = new DateTime(from.Ticks - from.Ticks % TimeSpan.TicksPerMinute, from.Kind);
            created.ShouldBeInRange(fromMinute, to, $"Поле «{EntryDate.Name}»");
        });
}
