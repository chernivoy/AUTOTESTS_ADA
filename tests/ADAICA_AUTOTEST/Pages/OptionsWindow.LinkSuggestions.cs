using ADAICA_AUTOTEST.Controls;
using ADAICA_AUTOTEST.Infrastructure;
using Allure.Net.Commons;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;

namespace ADAICA_AUTOTEST.Pages;

/// <summary>
/// Розділ «Vorschläge für Verknüpfung der Dokumentation» (вкладка «Dokumentation») → вкладка <see cref="SuggestionTab"/>:
/// <see cref="LinkSuggestionOptions"/>. Кожне налаштування має колонку «Anwender» і «Systemweit». Зміни ADAICA застосовує
/// одразу, хоч і пише, що після перезапуску.
/// </summary>
/// <remarks>
/// У XAML імена груп періоду завдань переплутано: група під підписом «Anwender» має AutomationId
/// <c>TasksDateFilterSystemValue</c>, а «Systemweit» — <c>TasksDateFilterUserValue</c> (прив'язки при цьому правильні).
/// Поки «Aufgaben/Termine vorschlagen» вимкнено, поля періоду недоступні, тож прапорець вмикається першим. Вкладки
/// мають один шаблон: AutomationId елементів (з «Mail» у назві) однакові на всіх вкладках, а в дереві — лише вибрана.
/// </remarks>
public sealed partial class OptionsWindow
{
    private const string SuggestionsNodeName = "Vorschläge für Verknüpfung der Dokumentation";

    private static readonly IReadOnlyDictionary<SuggestionTab, string> TabHeaders = new Dictionary<SuggestionTab, string>
    {
        [SuggestionTab.Mails] = "E-Mails",
        [SuggestionTab.Tasks] = "Aufgaben",
        [SuggestionTab.Appointments] = "Termine",
        [SuggestionTab.Projects] = "Projekte",
        [SuggestionTab.Folders] = "Ordner",
        [SuggestionTab.Notes] = "Notizen",
        [SuggestionTab.Documents] = "Dokumente",
    };

    // Відкрита вкладка розділу пропозицій — для назв кроків.
    private string _suggestionTab = TabHeaders[SuggestionTab.Mails];

    // Пункти списків: показаний текст для вибору й ключ (Name пункту), за яким вибране видно й у згорнутому списку.
    private static readonly IReadOnlyDictionary<TaskStatusFilter, (string Text, string Key)> TaskStatusItems =
        new Dictionary<TaskStatusFilter, (string, string)>
        {
            [TaskStatusFilter.Undone] = ("Unerledigt", "FILTER_TASK_UNDONE"),
            [TaskStatusFilter.All] = ("Erledigt und Unerledigt", "FILTER_TASK_ALL"),
            [TaskStatusFilter.Done] = ("Erledigt", "FILTER_TASK_DONE"),
        };

    private static readonly IReadOnlyDictionary<PeriodUnit, (string Text, string Key)> UnitItems = new Dictionary<PeriodUnit, (string, string)>
    {
        [PeriodUnit.Hours] = ("Stunde(n)", "Hours"),
        [PeriodUnit.Days] = ("Tag(e)", "Days"),
        [PeriodUnit.Weeks] = ("Woche(n)", "Weeks"),
        [PeriodUnit.Months] = ("Monat(e)", "Months"),
        [PeriodUnit.Years] = ("Jahr(e)", "Years"),
    };

    private static readonly IReadOnlyDictionary<AppointmentDateField, (string Text, string Key)> DateFieldItems =
        new Dictionary<AppointmentDateField, (string, string)>
        {
            [AppointmentDateField.Created] = ("Erstelldatum", "FILTER_APPOINT_ENTRY"),
            [AppointmentDateField.Begin] = ("Beginnt", "FILTER_APPOINT_BEGIN"),
            [AppointmentDateField.End] = ("Endet", "FILTER_APPOINT_END"),
        };

    private static readonly IReadOnlyDictionary<AppointmentPeriodMode, string> ModeIds = new Dictionary<AppointmentPeriodMode, string>
    {
        [AppointmentPeriodMode.All] = "radioPeriodAllCtrRecordsDateFilter",
        [AppointmentPeriodMode.Between] = "radioPeriodBetweenCtrRecordsDateFilter",
        [AppointmentPeriodMode.Period] = "radioPeriodPeriodCtrRecordsDateFilter",
    };

    /// <summary>
    /// Вкладка «Dokumentation» → вузол «Vorschläge für Verknüpfung der Dokumentation» (лише клік) → вкладка
    /// <paramref name="tab"/>. Заголовки вкладок — у шаблоні, тож вкладку беремо за порядком.
    /// </summary>
    public OptionsWindow OpenLinkSuggestions(SuggestionTab tab) => AllureApi.Step($"Відкрити «Dokumentation» → «{SuggestionsNodeName}» → «{TabHeaders[tab]}»", () =>
    {
        new TabItemControl("Dokumentation", Locate.ById(Root, "tiRecordOptionsWndConfiguration")).Select();
        UiInput.LeftClick(
            () => Wait.For(
                () => Root.FindFirstDescendant(cf => cf.ByAutomationId("treeViewGroup"))?
                    .FindFirstDescendant(cf => cf.ByAutomationId("TextBlock").And(cf.ByName(SuggestionsNodeName))),
                $"вузла «{SuggestionsNodeName}» у дереві розділів"),
            () => Root.FindFirstDescendant(cf => cf.ByAutomationId("grpIsLoadTasksCOManager")) is not null,
            $"розділу «{SuggestionsNodeName}»");
        new TabItemControl(TabHeaders[tab], () => Wait.For(
                () => Root.FindFirstDescendant(cf => cf.ByAutomationId("tcRecordLinksSuggestionsGroup"))?
                    .FindAllChildren(cf => cf.ByControlType(ControlType.TabItem)).ElementAtOrDefault((int)tab),
                $"вкладки «{TabHeaders[tab]}»"))
            .Select();
        _suggestionTab = TabHeaders[tab];
        return this;
    });

    /// <summary>Налаштування пропозицій відкритої вкладки в колонці «Anwender» (<paramref name="user"/>) чи «Systemweit».</summary>
    public LinkSuggestionOptions ReadLinkSuggestions(bool user = true) =>
        AllureApi.Step($"Прочитати пропозиції «{_suggestionTab}»: {Side(user)}", () =>
        {
            var options = new LinkSuggestionOptions(ReadTasks(user), ReadAppointments(user));
            AllureApi.Step($"{Side(user)}: {options}");
            return options;
        });

    /// <summary>Ставить налаштування пропозицій відкритої вкладки в колонці «Anwender». Зберігає їх <see cref="SaveChanges"/>.</summary>
    public void SetLinkSuggestions(LinkSuggestionOptions options) =>
        AllureApi.Step($"Встановити пропозиції «{_suggestionTab}» (Anwender): {options}", () =>
        {
            SetTasks(options.Tasks, user: true);
            SetAppointments(options.Appointments, user: true);

            // Дати «Zwischen» лишаються в полях і тоді, коли режим інший; якщо тест їх не задав, не порівнюємо.
            var read = ReadLinkSuggestions();
            if (options.Appointments.From is null)
                read = read with { Appointments = read.Appointments with { From = null, To = null } };
            read.ShouldBe(options, $"Пропозиції «{_suggestionTab}» у колонці «Anwender»");
        });

    /// <summary>«Aufgaben vorschlagen» у колонці «Systemweit». Якщо «Anwender» збігався із системним, він змінюється теж.</summary>
    public void SetSystemTaskSuggestions(bool enabled) =>
        CheckBox("Aufgaben vorschlagen: Systemweit", "chkTasksMailSaveLoadAddSystemValue").SetChecked(enabled);

    /// <summary>
    /// Повертає колонці «Anwender» стан до тесту <paramref name="user"/>. Завдання й терміни окремо: якщо значення
    /// користувача збігалося із системним <paramref name="system"/>, воно знову має його успадковувати, а ADAICA вважає
    /// значення успадкованим, лише коли воно стає рівним системному після зміни. Тому таке налаштування спершу
    /// змінюється, а потім ставиться рівним системному (дати «Zwischen» для термінів ADAICA теж порівнює). Власне значення
    /// користувача ставиться як було.
    /// </summary>
    public void RestoreUserLinkSuggestions(LinkSuggestionOptions system, LinkSuggestionOptions user) =>
        AllureApi.Step($"Повернути пропозиції «{_suggestionTab}» (Anwender): {user}", () =>
        {
            if (user.Tasks == system.Tasks)
                ResyncUserTasks(system.Tasks);
            else
                SetTasks(user.Tasks, user: true);

            if (user.Appointments == system.Appointments)
                ResyncUserAppointments(system.Appointments);
            else
                SetAppointments(user.Appointments, user: true);

            ReadLinkSuggestions().ShouldBe(user, $"Пропозиції «{_suggestionTab}» у колонці «Anwender»");
        });

    /// <summary>
    /// OK і підтвердження «Änderungen werden erst nach dem Neustart der Anwendung wirksam». Якщо нічого не змінилося,
    /// OK недоступна — тоді «Abbrechen».
    /// </summary>
    public void SaveChanges() => AllureApi.Step("Зберегти налаштування: OK", () =>
    {
        if (!OkButton.IsEnabled)
        {
            Close();
            return;
        }

        OkButton.Click();
        var message = Wait.For(() => MessageBoxDialog.Find(_owner), $"повідомлення «{RestartMessage}»");
        message.Message.ShouldHaveText(RestartMessage);
        message.Button("OK").Click();
        Wait.Until(() => !IsOpen, "закриття вікна «Einstellungen»");
    });

    private void ResyncUserTasks(TaskSuggestionOptions system)
    {
        var other = system.Status == TaskStatusFilter.Undone ? TaskStatusFilter.All : TaskStatusFilter.Undone;
        SetTasks(system with { Enabled = true, Status = other, CompanyRecords = !system.CompanyRecords }, user: true);
        SetTasks(system with { Enabled = true }, user: true);
        var enabled = CheckBox("Aufgaben vorschlagen: Anwender", "chkTasksMailSaveLoadAddUserValue");
        enabled.SetChecked(!system.Enabled);
        enabled.SetChecked(system.Enabled);
    }

    private void ResyncUserAppointments(AppointmentSuggestionOptions system)
    {
        var otherField = system.Field == AppointmentDateField.Created ? AppointmentDateField.Begin : AppointmentDateField.Created;
        SetAppointments(
            system with { Enabled = true, Mode = AppointmentPeriodMode.Between, Field = otherField, CompanyRecords = !system.CompanyRecords },
            user: true);
        SetAppointments(system with { Enabled = true }, user: true);
        var enabled = CheckBox("Termine vorschlagen: Anwender", "chkAppointsMailSaveLoadAddUserValue");
        enabled.SetChecked(!system.Enabled);
        enabled.SetChecked(system.Enabled);
    }

    private TaskSuggestionOptions ReadTasks(bool user)
    {
        var side = SideSuffix(user);
        var filter = TasksFilter(user);
        return new TaskSuggestionOptions(
            CheckBox("Aufgaben vorschlagen", $"chkTasksMailSaveLoadAdd{side}").IsChecked,
            Selected(TaskStatusItems, Combo(filter, "Art des Datums", "cmbPeriodDateFieldCtrTasksDateFilter")),
            ReadPeriod(filter, "Tasks", "Previous"),
            ReadPeriod(filter, "Tasks", "Next"),
            CheckBox("Aufgaben der Firma", $"chkTasksMailSaveAddEmployeeCompany{side}").IsChecked);
    }

    private AppointmentSuggestionOptions ReadAppointments(bool user)
    {
        var side = SideSuffix(user);
        var filter = AppointmentsFilter(user);
        return new AppointmentSuggestionOptions(
            CheckBox("Termine vorschlagen", $"chkAppointsMailSaveLoadAdd{side}").IsChecked,
            Selected(DateFieldItems, Combo(filter, "Art des Datums", "cmbPeriodDateFieldCtrRecordsDateFilter")),
            ModeIds.Single(pair => Radio(filter, pair.Value).IsSelected).Key,
            ReadPeriod(filter, "Records", "Previous"),
            ReadPeriod(filter, "Records", "Next"),
            CheckBox("Termine der Firma", $"chkAppointsMailSaveAddEmployeeCompany{side}").IsChecked,
            DatePicker(filter, "von", "dpPeriodDateFromCtrRecordsDateFilter").Value,
            DatePicker(filter, "bis", "dpPeriodDateToCtrRecordsDateFilter").Value);
    }

    private void SetTasks(TaskSuggestionOptions tasks, bool user)
    {
        var side = SideSuffix(user);
        var enabled = CheckBox($"Aufgaben vorschlagen: {Side(user)}", $"chkTasksMailSaveLoadAdd{side}");
        enabled.SetChecked(true);
        var filter = TasksFilter(user);
        Combo(filter, "Art des Datums", "cmbPeriodDateFieldCtrTasksDateFilter").Select(TaskStatusItems[tasks.Status].Text);
        SetPeriods(filter, "Tasks", tasks.Previous, tasks.Next);
        CheckBox($"Aufgaben der Firma: {Side(user)}", $"chkTasksMailSaveAddEmployeeCompany{side}").SetChecked(tasks.CompanyRecords);
        enabled.SetChecked(tasks.Enabled);
    }

    private void SetAppointments(AppointmentSuggestionOptions appointments, bool user)
    {
        var side = SideSuffix(user);
        var enabled = CheckBox($"Termine vorschlagen: {Side(user)}", $"chkAppointsMailSaveLoadAdd{side}");
        enabled.SetChecked(true);
        var filter = AppointmentsFilter(user);
        Combo(filter, "Art des Datums", "cmbPeriodDateFieldCtrRecordsDateFilter").Select(DateFieldItems[appointments.Field].Text);

        // Поля дат доступні лише в режимі «Zwischen», а періоду — лише в «Zeitraum».
        if (appointments.From is { } from && appointments.To is { } to)
        {
            Radio(filter, ModeIds[AppointmentPeriodMode.Between]).Select();
            DatePicker(filter, "Zwischen: von", "dpPeriodDateFromCtrRecordsDateFilter").Fill(from);
            DatePicker(filter, "Zwischen: bis", "dpPeriodDateToCtrRecordsDateFilter").Fill(to);
        }

        Radio(filter, ModeIds[AppointmentPeriodMode.Period]).Select();
        SetPeriods(filter, "Records", appointments.Previous, appointments.Next);
        Radio(filter, ModeIds[appointments.Mode]).Select();
        CheckBox($"Termine der Firma: {Side(user)}", $"chkAppointsMailSaveAddEmployeeCompany{side}").SetChecked(appointments.CompanyRecords);
        enabled.SetChecked(appointments.Enabled);
    }

    // Увімкнений період ставиться першим: якщо зняти прапорець, коли інший уже знято, ADAICA вмикає інший сама.
    private void SetPeriods(AutomationElement filter, string kind, SuggestionPeriod previous, SuggestionPeriod next)
    {
        if (!previous.IsOn && !next.IsOn)
            throw new ArgumentException("ADAICA не дозволяє вимкнути обидва періоди «Letzte(n)» і «Nächste(n)».");

        var periods = new[] { (Prefix: "Previous", Period: previous), (Prefix: "Next", Period: next) }.OrderByDescending(item => item.Period.IsOn);
        foreach (var (prefix, period) in periods)
        {
            var label = prefix == "Previous" ? "Letzte(n)" : "Nächste(n)";
            CheckBox(label, $"chk{prefix}PeriodCtr{kind}DateFilter", filter).SetChecked(true);
            new TextBoxControl($"{label}: Anzahl", Locate.ById(filter, $"txtCount{prefix}PeriodCtr{kind}DateFilter")).Fill(period.Count.ToString());
            Combo(filter, $"{label}: Einheit", $"cmbUnit{prefix}PeriodCtr{kind}DateFilter").Select(UnitItems[period.Unit].Text);
        }

        foreach (var (prefix, period) in periods.Where(item => !item.Period.IsOn))
            CheckBox(prefix == "Previous" ? "Letzte(n)" : "Nächste(n)", $"chk{prefix}PeriodCtr{kind}DateFilter", filter).SetChecked(false);
    }

    private SuggestionPeriod ReadPeriod(AutomationElement filter, string kind, string prefix) =>
        new(
            int.TryParse(new TextBoxControl(prefix, Locate.ById(filter, $"txtCount{prefix}PeriodCtr{kind}DateFilter")).Text, out var count) ? count : 0,
            Selected(UnitItems, Combo(filter, prefix, $"cmbUnit{prefix}PeriodCtr{kind}DateFilter")),
            CheckBox(prefix, $"chk{prefix}PeriodCtr{kind}DateFilter", filter).IsChecked);

    // Група періоду завдань під підписом «Anwender» має AutomationId …SystemValue (див. remarks).
    private AutomationElement TasksFilter(bool user) =>
        Wait.FindByAutomationId(Root, user ? "TasksDateFilterSystemValue" : "TasksDateFilterUserValue");

    private AutomationElement AppointmentsFilter(bool user) =>
        Wait.FindByAutomationId(Root, user ? "RecordsDateFilterUserValue" : "RecordsDateFilterSystemValue");

    private CheckBoxControl CheckBox(string name, string automationId, AutomationElement? scope = null) =>
        new(name, Locate.ById(scope ?? Root, automationId));

    private static T Selected<T>(IReadOnlyDictionary<T, (string Text, string Key)> items, ComboBoxControl comboBox)
        where T : notnull
    {
        var key = comboBox.SelectedItemName;
        return items.FirstOrDefault(pair => pair.Value.Key == key) is { Value.Key: not null } found
            ? found.Key
            : throw new InvalidOperationException($"У списку «{comboBox.Name}» вибрано невідомий пункт «{key}».");
    }

    private static ComboBoxControl Combo(AutomationElement scope, string name, string automationId) =>
        new(name, Locate.ById(scope, automationId));

    private static RadioButtonControl Radio(AutomationElement scope, string automationId) =>
        new(automationId, Locate.ById(scope, automationId));

    private static DatePickerControl DatePicker(AutomationElement scope, string name, string automationId) =>
        new(name, Locate.ById(scope, automationId));

    private static string SideSuffix(bool user) => user ? "UserValue" : "SystemValue";

    private static string Side(bool user) => user ? "Anwender" : "Systemweit";
}
