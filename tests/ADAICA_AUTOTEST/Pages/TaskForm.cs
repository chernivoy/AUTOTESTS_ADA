using ADAICA_AUTOTEST.Controls;
using ADAICA_AUTOTEST.Infrastructure;
using Allure.Net.Commons;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;

namespace ADAICA_AUTOTEST.Pages;

/// <summary>
/// Бланк завдання. Ті самі поля ADAICA показує і у вікні «Neue Aufgabe», і в огляді збереженого завдання.
/// </summary>
/// <param name="root">Вікно завдання або область перегляду.</param>
/// <param name="owner">Вікно, якому належать вікна бланку («Aufgabenzyklus»): вікно завдання або головне вікно.</param>
public class TaskForm(AutomationElement root, Window owner)
{
    public const string NumberId = "txtNumberRecordTask";

    private const string MainTabId = "tiMainRecordTask";
    private const string SeriesTabId = "tiCycleSettingCtrTask";

    private static readonly TimeSpan SaveTimeout = TimeSpan.FromSeconds(60);

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

    public RecordClassBox Class { get; } = new(root, "ctrVUBClassSelectRecordTask");

    /// <summary>«Serientyp»: відкриває вікно «Aufgabenzyklus» (<see cref="TaskSeriesDialog"/>).</summary>
    public ButtonControl SeriesTypeButton { get; } = new("Serientyp", Locate.ById(root, "btnTypeOfReiterationRecordTask"));

    // Кнопки вкладки «Aufgabenzyklus» не мають AutomationId, тож шукаємо їх за підписом.
    /// <summary>З'являється на вкладці «Aufgabenzyklus», коли для завдання задано серію.</summary>
    public ButtonControl StartSeriesButton { get; } = SeriesButton(root, "Serie starten");

    /// <summary>«Bearbeiten» на вкладці «Aufgabenzyklus» запущеної серії: те саме вікно «Aufgabenzyklus».</summary>
    public ButtonControl EditSeriesButton { get; } = SeriesButton(root, "Bearbeiten");

    /// <summary>
    /// OK під оглядом: стає доступною, коли поля змінено, і зберігає зміни. У вікні нового завдання зберігає
    /// <see cref="RecordDialog.Save"/>.
    /// </summary>
    public ButtonControl OkButton { get; } = new("OK", Locate.ById(root, "btnOKCtrMainForm"));

    /// <summary>Відкриває вікно «Aufgabenzyklus» кнопкою «Serientyp» на головній вкладці.</summary>
    public TaskSeriesDialog OpenSeriesType() => AllureApi.Step("Відкрити вікно «Aufgabenzyklus» кнопкою «Serientyp»", () =>
    {
        ShowTab(MainTabId, "Aufgabe");
        SeriesTypeButton.Click();
        return new TaskSeriesDialog(owner);
    });

    /// <summary>Відкриває вікно «Aufgabenzyklus» запущеної серії: вкладка «Aufgabenzyklus» → «Bearbeiten».</summary>
    public TaskSeriesDialog EditSeries() => AllureApi.Step("Відкрити серію: вкладка «Aufgabenzyklus» → Bearbeiten", () =>
    {
        ShowTab(SeriesTabId, "Aufgabenzyklus");
        EditSeriesButton.Click();
        return new TaskSeriesDialog(owner);
    });

    /// <summary>
    /// Запускає задану серію кнопкою «Serie starten» на вкладці «Aufgabenzyklus» і повертається на головну вкладку.
    /// Завдання серії ADAICA створює, коли завдання збережено.
    /// </summary>
    public void StartSeries() => AllureApi.Step("Запустити серію: вкладка «Aufgabenzyklus» → Serie starten", () =>
    {
        ShowTab(SeriesTabId, "Aufgabenzyklus");
        StartSeriesButton.Click();
        // Запущену серію кнопка на тому самому місці вже зупиняє.
        Wait.Until(() => FindButton(root, "Serie stoppen") is not null, "запуску серії («Serie stoppen»)");
        ShowTab(MainTabId, "Aufgabe");
    });

    /// <summary>
    /// Відкриває головну вкладку «Aufgabe»: бланк збереженого завдання ADAICA показує на вкладці, яку відкривали
    /// останньою, а поля головної вкладки є в дереві, лише коли вона вибрана.
    /// </summary>
    public void ShowMainTab() => ShowTab(MainTabId, "Aufgabe");

    /// <summary>Для прибирання: відкидає незбережені зміни огляду кнопкою «Abbrechen».</summary>
    public void CancelIfEditing()
    {
        var cancel = new ButtonControl("Abbrechen", Locate.ById(root, "btnCancelCtrMainForm"));
        if (cancel.IsEnabled)
            cancel.Click();
    }

    /// <summary>Зберігає зміни завдання в огляді кнопкою OK і чекає, доки ADAICA їх збереже (OK знову недоступна).</summary>
    public void SaveChanges() => AllureApi.Step("Зберегти зміни завдання кнопкою OK під оглядом", () =>
    {
        OkButton.Click();
        Wait.Until(() => !OkButton.IsEnabled, "збереження завдання", SaveTimeout);
    });

    /// <summary>
    /// Перевіряє «Beginnt am» і «Fällig am» (null — поле порожнє). Поля є лише на головній вкладці, тож її відкриваємо:
    /// після вікна «Aufgabenzyklus» ADAICA може показувати вкладку серії.
    /// </summary>
    public void ShouldHaveDates(DateTime? begin, DateTime? due) => AllureApi.Step(
        $"Перевірити дати завдання: Beginnt am {Format(begin)}, Fällig am {Format(due)}",
        () =>
        {
            ShowTab(MainTabId, "Aufgabe");
            this.ShouldSatisfyAllConditions(
                () => Begin.Value.ShouldBe(begin, $"Поле «{Begin.Name}»"),
                () => Due.Value.ShouldBe(due, $"Поле «{Due.Name}»"));
        });

    public void Fill(TaskData task) => AllureApi.Step("Заповнити поля завдання", () =>
    {
        // Клас — першим: ADAICA підставляє в «Kurzname» короткі назви класу й підкласу («KUAKKORR»).
        if (task.Class is { } recordClass)
            Class.Select(recordClass);
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

    /// <summary>Заповнює поля завдання для списку «Meine Aufgaben»; порожні дати не чіпає.</summary>
    public void Fill(DashboardTaskData task) => AllureApi.Step("Заповнити поля завдання", () =>
    {
        Name.Fill(task.Name);
        ShortName.Fill(task.ShortName);
        Begin.Fill(task.Begin);
        if (task.Due is { } due)
            Due.Fill(due);
        if (task.Done is { } done)
            Done.Fill(done);
        if (task.ClosedOn is { } closedOn)
            ClosedOn.Fill(closedOn);
        AcceptanceType.Select(task.AcceptanceType);
        SimpleRemarkTab.Select();
        Remark.Fill(task.Remark);
    });

    /// <summary>Перевіряє всі поля й повідомляє про всі розбіжності разом, а не лише про першу.</summary>
    public void ShouldHaveValues(TaskData expected) => AllureApi.Step("Перевірити поля завдання", () =>
    {
        SimpleRemarkTab.Select();
        this.ShouldSatisfyAllConditions(
            () => Class.ShouldHave(expected.Class),
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

    private static string Format(DateTime? value) => value is { } date ? AdaicaFormat.FormatDateTime(date) : "порожнє";

    private static ButtonControl SeriesButton(AutomationElement root, string name) =>
        new(name, () => Wait.For(() => FindButton(root, name), $"кнопки «{name}»"));

    private static AutomationElement? FindButton(AutomationElement root, string name) =>
        root.FindFirstDescendant(cf => cf.ByControlType(ControlType.Button).And(cf.ByName(name)));

    // Вкладки бланку завдання перемикаємо кліком, як користувач.
    private void ShowTab(string automationId, string name) => AllureApi.Step($"Відкрити вкладку «{name}»", () =>
    {
        var tab = Locate.ById(root, automationId);
        if (IsSelected(tab()))
            return;

        UiInput.LeftClick(tab, () => IsSelected(tab()), $"вибору вкладки «{name}»");
    });

    private static bool IsSelected(AutomationElement tab) => tab.Patterns.SelectionItem.Pattern.IsSelected.Value;
}
