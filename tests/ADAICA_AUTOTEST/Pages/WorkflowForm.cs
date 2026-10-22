using ADAICA_AUTOTEST.Controls;
using ADAICA_AUTOTEST.Infrastructure;
using Allure.Net.Commons;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using FlaUI.Core.WindowsAPI;

namespace ADAICA_AUTOTEST.Pages;

/// <summary>
/// Бланк workflow (`ctrWorkflow`): вкладка «Workflow - назва» (назва workflow і його головного завдання), вкладки
/// кроків «N Schritt - назва - WF1» (Prüfer і назва кроку) і вкладка «+», яка додає крок. Унизу кожної вкладки —
/// «Schritt-Details»: дати, «Annahmeart», «Wichtigkeit», «Priorität». Той самий бланк є у вікні шаблону й запуску
/// workflow на документі (<see cref="WorkflowDialog"/>; у запуску до назв додано « - DV1 - WF1») і в області перегляду
/// під списком «Workflowkonfiguration» — там лише для читання й без «+». Поки на вкладці не заповнено обов'язкове, у її
/// заголовку знак питання, а OK вікна недоступна. Вміст невибраної вкладки WPF у дерево UI Automation не показує, тож
/// поля читаються з вибраної.
/// </summary>
public sealed class WorkflowForm
{
    private const string MainTabType = "DocumentWorkflowVM";
    private const string StepTabType = "TaskWorkflowVM";
    private const string AddStepTabType = "NewWorkflowVM";

    private readonly Func<AutomationElement> _root;
    private readonly Window _window;
    private readonly bool _isReadOnly;

    /// <param name="root">Елемент `ctrWorkflow`.</param>
    /// <param name="window">Вікно бланка: власник меню вкладок і вікна вибору Prüfer.</param>
    /// <param name="isReadOnly">
    /// Бланк лише для читання (область перегляду): вибраного пункту його списків UI Automation не показує — пунктів
    /// немає, поки список не розгорнуто, а розгорнути його не можна.
    /// </param>
    public WorkflowForm(Func<AutomationElement> root, Window window, bool isReadOnly = false)
    {
        _root = root;
        _window = window;
        _isReadOnly = isReadOnly;

        Responsible = new TextBoxControl("Workflow-Verantwortlicher", Field("txtAdminWorkflowDoc"));
        ConvertToPdf = new CheckBoxControl("Nach dem Start in PDF umwandeln", Field("chkConvertPDFWorkflowDoc"));
        Prefix = new TextBoxControl("Workflow-Name: Prefix", Field("txtPrefixNameWorkflowDoc"));
        Name = new TextBoxControl("Workflowname", Field("txtNameWorkflowDoc"));
        DisplayName = new TextBoxControl("Workflow-Name: Anzeigename", Field("txtDisplayNameWorkflowDoc"));
        MainTaskPrefix = new TextBoxControl("Name der Hauptaufgabe: Prefix", Field("txtPrefixStepNameWorkflowDoc"));
        MainTaskName = new TextBoxControl("Name der Workflow-Aufgabe", Field("txtStepNameWorkflowDoc"));
        Version = new TextBoxControl("Workflow-Version", Field("txtVersionWorkflowDoc"));
        DocumentVersion = new TextBoxControl("Dokumentversion", Field("txtDocumentVersionWorkflowDoc"));
        MainTaskDisplayName = new TextBoxControl("Name der Hauptaufgabe: Anzeigename", Field("txtDisplayStepNameWorkflowDoc"));

        StepNumber = new LabelControl("Workflow-Schritt", Field("tbStepNumberWorkflowTask"));
        ChooseReviewersButton = new ButtonControl("Prüfer: ...", Field("btnContactWorkflowTask"));
        StepPrefix = new TextBoxControl("Schritt: Prefix", Field("txtPrefixNameStepWorkflowTask"));
        StepName = new TextBoxControl("Name des Workflow-Schrittes", Field("txtNameStepWorkflowTask"));
        StepDisplayName = new TextBoxControl("Schritt: Anzeigename", Field("txtDisplayNameStepWorkflowTask"));

        Begin = new DateTimePickerControl("Beginnt am", Field("dpBeginDateWorkflow"));
        Due = new DateTimePickerControl("Fällig am", Field("dpEndDateWorkflow"));
        Authority = new ComboBoxControl("Annahmeart", Field("cmbAuthorityDateWorkflow"));
        Significance = new ComboBoxControl("Wichtigkeit", Field("cmbSignificanceWorkflow"));
        Priority = new ComboBoxControl("Priorität", Field("cmbPriorityWorkflow"));
        Publish = new CheckBoxControl("Nach diesem Schritt veröffentlichen und verteilen", Field("chkPublishWorkflowDoc"));
        AsActualVersion = new CheckBoxControl("Veröffentlichen als aktuelle Version einer früheren Veröffentlichung", Field(AsActualVersionId));
        Publication = new WorkflowPublicationPanel(_root, window);
    }

    private const string AsActualVersionId = "chkAddAsVersionWorkflowTask";

    /// <summary>«Workflow-Verantwortlicher»: хто створив шаблон (у запуску — хто запускає), лише для читання.</summary>
    public TextBoxControl Responsible { get; }

    public CheckBoxControl ConvertToPdf { get; }

    public TextBoxControl Prefix { get; }

    /// <summary>«Workflowname»: до 30 символів, унікальний серед шаблонів (з урахуванням регістру).</summary>
    public TextBoxControl Name { get; }

    /// <summary>«Anzeigename» workflow: «Prefix - Workflowname», лише для читання.</summary>
    public TextBoxControl DisplayName { get; }

    public TextBoxControl MainTaskPrefix { get; }

    public TextBoxControl MainTaskName { get; }

    /// <summary>«Workflow-Version»: у шаблоні й запуску з шаблону — «WF1», лише для читання.</summary>
    public TextBoxControl Version { get; }

    /// <summary>«Dokumentversion»: лише у вікні запуску на документі — «DV» і номер актуальної версії документа.</summary>
    public TextBoxControl DocumentVersion { get; }

    public TextBoxControl MainTaskDisplayName { get; }

    /// <summary>Номер кроку поруч із підписом «Workflow-Schritt».</summary>
    public LabelControl StepNumber { get; }

    public ButtonControl ChooseReviewersButton { get; }

    public TextBoxControl StepPrefix { get; }

    public TextBoxControl StepName { get; }

    public TextBoxControl StepDisplayName { get; }

    public DateTimePickerControl Begin { get; }

    public DateTimePickerControl Due { get; }

    public ComboBoxControl Authority { get; }

    public ComboBoxControl Significance { get; }

    public ComboBoxControl Priority { get; }

    /// <summary>
    /// Лише на вкладці кроку; позначити можна тільки один крок шаблону: на інших прапорець недоступний, а підказка
    /// (<see cref="PublishHint"/>) називає крок, на якому публікацію ввімкнено.
    /// </summary>
    public CheckBoxControl Publish { get; }

    /// <summary>Підказка недоступного «Nach diesem Schritt veröffentlichen und verteilen».</summary>
    public string PublishHint => Field("chkPublishWorkflowDoc")().HelpText;

    /// <summary>
    /// «Veröffentlichen als aktuelle Version einer früheren Veröffentlichung»: опублікований файл стане новою версією
    /// документа, опублікованого попереднім workflow цього документа. Доступний лише з «veröffentlichen»; коли шляхом
    /// публікації вибрано документ, ADAICA вмикає його сама й блокує. У вікні запуску його видно, лише якщо документ уже
    /// публікували (<see cref="HasAsActualVersion"/>).
    /// </summary>
    public CheckBoxControl AsActualVersion { get; }

    /// <summary>Чи є на вкладці кроку прапорець «Veröffentlichen als aktuelle Version…».</summary>
    public bool HasAsActualVersion => _root().FindFirstDescendant(cf => cf.ByAutomationId(AsActualVersionId)) is not null;

    /// <summary>Панель публікації під вкладками: її видно на вкладці кроку з «veröffentlichen».</summary>
    public WorkflowPublicationPanel Publication { get; }

    /// <summary>Підписи вкладок зліва направо, без «+».</summary>
    public IReadOnlyList<string> TabNames => Tabs().Where(tab => !IsOfType(tab, AddStepTabType)).Select(TabName).ToList();

    public int StepCount => Tabs().Count(tab => IsOfType(tab, StepTabType));

    /// <summary>Видимий підпис вкладки кроку: довший за 50 символів ADAICA обрізає трикрапкою.</summary>
    public string StepTabCaption(int step) => StepTab(step).FindFirstDescendantRaw(cf => cf.ByControlType(ControlType.Text))?.Name ?? string.Empty;

    /// <summary>Чи є вкладка «+» (її немає, коли бланк лише для читання).</summary>
    public bool CanAddSteps => Tabs().Any(tab => IsOfType(tab, AddStepTabType));

    /// <summary>
    /// Чи стоїть у заголовку вкладки знак питання: на ній не заповнено обов'язкове поле (або назва workflow не унікальна).
    /// Вкладка: 0 — «Workflow», далі кроки.
    /// </summary>
    public bool IsTabIncomplete(int index) =>
        ContentTabs()[index].FindFirstDescendantRaw(cf => cf.ByControlType(ControlType.Image)) is { IsOffscreen: false };

    /// <summary>Prüfer вибраного кроку, як їх підписує список (Name пункту — тип даних, підпис — у його TextBlock).</summary>
    public IReadOnlyList<string> Reviewers => ReviewerItems().Select(ReviewerName).ToList();

    /// <summary>
    /// Помилки полів вікна: ADAICA позначає поле червоною « * » поруч, а текст помилки — у її підказці (наприклад,
    /// «Eine gleichnamige Vorlage ist bereits vorhanden» біля «Workflowname»).
    /// </summary>
    public IReadOnlyList<string> FieldErrors =>
        _window.FindAllChildren(cf => cf.ByControlType(ControlType.Text).And(cf.ByName(" * ")))
            .Where(marker => !marker.IsOffscreen)
            .Select(marker => marker.HelpText)
            .ToList();

    public void ShowMainTab() => AllureApi.Step("Відкрити вкладку «Workflow»", () => SelectTab(() => Tabs().Single(tab => IsOfType(tab, MainTabType)), "Workflow"));

    /// <summary>Відкриває вкладку кроку <paramref name="step"/> (з одиниці).</summary>
    public void ShowStep(int step) => AllureApi.Step($"Відкрити вкладку кроку {step}", () =>
    {
        SelectTab(() => StepTab(step), $"{step} Schritt");
        Wait.Until(() => StepNumber.Text == step.ToString(), $"вмісту вкладки кроку {step}");
    });

    /// <summary>Вкладка «+»: додає крок у кінець і відкриває його вкладку. Вкладка реагує лише на клік.</summary>
    public void AddStep() => AllureApi.Step("Додати крок вкладкою «+»", () =>
    {
        var before = StepCount;
        UiInput.LeftClick(
            () => Tabs().Single(tab => IsOfType(tab, AddStepTabType)),
            () => StepCount == before + 1,
            $"вкладки кроку {before + 1}");
        Wait.Until(() => StepNumber.Text == (before + 1).ToString(), $"вмісту вкладки кроку {before + 1}");
    });

    /// <summary>Пункти меню вкладки кроку (правий клік по заголовку). Меню після цього закривається.</summary>
    public IReadOnlyList<(string Name, bool IsEnabled)> StepTabMenuItems(int step) =>
        AllureApi.Step($"Прочитати меню вкладки кроку {step}", () =>
        {
            OpenStepTabMenu(step);
            var items = MenuItems().Select(item => (item.Name, item.IsEnabled)).ToList();
            UiInput.PressKey(() => _window, VirtualKeyShort.ESCAPE, () => !MenuItems().Any(), "закриття меню вкладки");
            return (IReadOnlyList<(string, bool)>)items;
        });

    /// <summary>Меню вкладки кроку → «Löschen». Кроки, що лишилися, ADAICA перенумеровує.</summary>
    public void DeleteStep(int step) => AllureApi.Step($"Видалити крок {step}: меню вкладки → Löschen", () =>
    {
        var before = StepCount;
        OpenStepTabMenu(step);
        new MenuItemControl("Löschen", () => Wait.For(() => MenuItems().FirstOrDefault(item => item.Name == "Löschen"), "пункту «Löschen»")).Click();
        Wait.Until(() => StepCount == before - 1, $"зникнення вкладки кроку {step}");
    });

    /// <summary>
    /// «...» біля «Prüfer» → прапорець користувача <paramref name="user"/> у вікні вибору контактів → «>>» → OK. Кожен
    /// вибір додається до наявних Prüfer. Виділення вузла без прапорця «>>» тут не переносить.
    /// </summary>
    public void AddReviewer(string user) => AllureApi.Step($"Додати Prüfer «{user}»", () =>
    {
        var before = Reviewers.Count;
        ChooseReviewersButton.Click();
        var dialog = new ChatParticipantsDialog(_window);
        dialog.Choose(user);
        dialog.Confirm();
        Wait.Until(() => Reviewers.Count == before + 1, $"Prüfer «{user}» у списку кроку");
    });

    /// <summary>Виділяє Prüfer у списку кроку й видаляє його з меню списку («Löschen»).</summary>
    public void RemoveReviewer(string reviewer) => AllureApi.Step($"Прибрати Prüfer «{reviewer}»: меню списку → Löschen", () =>
    {
        AutomationElement Item() => Wait.For(
            () => ReviewerItems().FirstOrDefault(item => ReviewerName(item).StartsWith(reviewer, StringComparison.Ordinal)),
            $"Prüfer «{reviewer}» у списку кроку");
        Item().Patterns.SelectionItem.Pattern.Select();
        UiInput.RightClick(Item, () => MenuItems().Any(), "меню списку Prüfer");
        new MenuItemControl("Löschen", () => Wait.For(() => MenuItems().FirstOrDefault(item => item.Name == "Löschen"), "пункту «Löschen»")).Click();
        Wait.Until(() => !Reviewers.Any(name => name.StartsWith(reviewer, StringComparison.Ordinal)), $"зникнення Prüfer «{reviewer}»");
    });

    /// <summary>
    /// Чи є біля назв перемикачі «Name des übergeordneten Elements vorausfüllen»: їх видно лише у вікні запуску на
    /// документі (шукаються на відкритій вкладці).
    /// </summary>
    public bool HasDocumentNameSwitches =>
        DocumentNameSwitchIds.Any(id => _root().FindFirstDescendant(cf => cf.ByAutomationId(id)) is { IsOffscreen: false });

    /// <summary>
    /// Перемикач «Name des übergeordneten Elements vorausfüllen» на відкритій вкладці: біля «Workflowname» і назви
    /// головного завдання (вкладка «Workflow») чи біля назви кроку. Увімкнений підставляє замість назви з шаблону назву
    /// документа, вимкнений повертає назву з шаблону. Реагує лише на клік.
    /// </summary>
    public void UseDocumentName(DocumentNameField field, bool on) =>
        AllureApi.Step($"{(on ? "Увімкнути" : "Вимкнути")} «Name des übergeordneten Elements vorausfüllen» біля {field}", () =>
        {
            var id = DocumentNameSwitchIds[(int)field];
            UiInput.LeftClick(
                Field(id),
                () => (Field(id)().Patterns.Toggle.Pattern.ToggleState.Value == ToggleState.On) == on,
                $"перемикача біля {field} ({(on ? "увімкнено" : "вимкнено")})");
        });

    // У порядку DocumentNameField.
    private static readonly string[] DocumentNameSwitchIds =
        ["btnApplyTemplateNameWorkflowDoc", "btnApplyMainTaskTemplateNameWorkflowDoc", "btnApplyTemplateTaskNameWorkflowDoc"];

    /// <summary>
    /// Заповнює вкладку «Workflow» і кроки: кроків, яких бракує, додає вкладкою «+», а Prüfer, яких ще немає, —
    /// вікном вибору контактів. Зайвих кроків і Prüfer не прибирає.
    /// </summary>
    public void Fill(WorkflowTemplateData data) => AllureApi.Step($"Заповнити бланк workflow {data}", () =>
    {
        ShowMainTab();
        FillIfChanged(Prefix, data.Prefix);
        FillIfChanged(Name, data.Name);
        FillIfChanged(MainTaskPrefix, data.MainTaskPrefix);
        FillIfChanged(MainTaskName, data.MainTaskName);
        if (ConvertToPdf.IsChecked != data.ConvertToPdf)
            ConvertToPdf.SetChecked(data.ConvertToPdf);

        for (var step = 1; step <= data.Steps.Count; step++)
        {
            if (step > StepCount)
                AddStep();
            else
                ShowStep(step);
            FillStep(data.Steps[step - 1]);
        }
    });

    /// <summary>Заповнює відкриту вкладку кроку.</summary>
    public void FillStep(WorkflowStepData step) => AllureApi.Step($"Заповнити крок «{step.Name}»", () =>
    {
        FillIfChanged(StepPrefix, step.Prefix);
        FillIfChanged(StepName, step.Name);
        foreach (var reviewer in step.Reviewers.Where(reviewer => !Reviewers.Any(name => name.StartsWith(reviewer, StringComparison.Ordinal))))
            AddReviewer(reviewer);
        if (Authority.SelectedText != step.Authority)
            Authority.Select(step.Authority);

        Publish.SetChecked(step.Publication is not null);
        if (step.Publication is { } publication)
        {
            Publication.Fill(publication);
            // Коли шляхом вибрано документ, ADAICA сама вмикає й блокує «als aktuelle Version».
            if (AsActualVersion.IsEnabled)
                AsActualVersion.SetChecked(publication.AsActualVersion);
        }
    });

    /// <summary>
    /// Перевіряє всі вкладки бланка: назви вкладок, поля «Workflow» і кожного кроку. Повідомляє про всі розбіжності
    /// одразу. Prüfer порівнюються за початком підпису: до користувача з контактом фірми ADAICA дописує фірму.
    /// </summary>
    /// <param name="documentVersion">
    /// Для вікна запуску на документі — номер актуальної версії документа: тоді назви мають « - DV{версія} - WF1», а
    /// «Dokumentversion» — «DV{версія}».
    /// </param>
    public void ShouldHaveValues(WorkflowTemplateData expected, string responsible, int? documentVersion = null) =>
        AllureApi.Step($"Перевірити бланк workflow {expected}", () =>
        {
            var expectedTabs = new[] { expected.DisplayName(documentVersion) }
                .Concat(expected.Steps.Select((step, index) => step.DisplayName(index + 1, documentVersion)))
                .ToList();
            TabNames.ShouldBe(expectedTabs, ignoreOrder: false, "Вкладки бланка");

            ShowMainTab();
            this.ShouldSatisfyAllConditions(
                () => Responsible.Text.ShouldBe(responsible, $"Поле «{Responsible.Name}»"),
                () => ConvertToPdf.IsChecked.ShouldBe(expected.ConvertToPdf, $"Прапорець «{ConvertToPdf.Name}»"),
                () => Prefix.Text.ShouldBe(expected.Prefix, $"Поле «{Prefix.Name}»"),
                () => Name.Text.ShouldBe(expected.Name, $"Поле «{Name.Name}»"),
                () => DisplayName.Text.ShouldBe(expected.DisplayName(documentVersion), $"Поле «{DisplayName.Name}»"),
                () => MainTaskPrefix.Text.ShouldBe(expected.MainTaskPrefix, $"Поле «{MainTaskPrefix.Name}»"),
                () => MainTaskName.Text.ShouldBe(expected.MainTaskName, $"Поле «{MainTaskName.Name}»"),
                () => Version.Text.ShouldBe(WorkflowTemplateData.Version, $"Поле «{Version.Name}»"),
                () =>
                {
                    if (documentVersion is { } version)
                        DocumentVersion.Text.ShouldBe($"DV{version}", $"Поле «{DocumentVersion.Name}»");
                },
                () => MainTaskDisplayName.Text.ShouldBe(expected.MainTaskDisplayName(documentVersion), $"Поле «{MainTaskDisplayName.Name}»"));

            for (var step = 1; step <= expected.Steps.Count; step++)
            {
                var data = expected.Steps[step - 1];
                ShowStep(step);
                var reviewers = Reviewers;
                this.ShouldSatisfyAllConditions(
                    () => StepPrefix.Text.ShouldBe(data.Prefix, $"Крок {step}: поле «{StepPrefix.Name}»"),
                    () => StepName.Text.ShouldBe(data.Name, $"Крок {step}: поле «{StepName.Name}»"),
                    () => StepDisplayName.Text.ShouldBe(data.DisplayName(step, documentVersion), $"Крок {step}: поле «{StepDisplayName.Name}»"),
                    () =>
                    {
                        if (!_isReadOnly)
                            Authority.SelectedText.ShouldBe(data.Authority, $"Крок {step}: поле «{Authority.Name}»");
                    },
                    () => reviewers.Count.ShouldBe(data.Reviewers.Count, $"Крок {step}: Prüfer {string.Join("; ", reviewers)}"),
                    () => data.Reviewers.ShouldAllBe(
                        reviewer => reviewers.Any(name => name.StartsWith(reviewer, StringComparison.Ordinal)),
                        $"Крок {step}: Prüfer {string.Join("; ", reviewers)}"),
                    () => Publish.IsChecked.ShouldBe(data.Publication is not null, $"Крок {step}: прапорець «{Publish.Name}»"),
                    () =>
                    {
                        // У вікні запуску прапорця немає, поки документ не публікували.
                        if (data.Publication is { } publication && HasAsActualVersion)
                            AsActualVersion.IsChecked.ShouldBe(publication.AsActualVersion, $"Крок {step}: прапорець «{AsActualVersion.Name}»");
                    },
                    () =>
                    {
                        if (data.Publication is { } publication)
                            Publication.ShouldHaveValues(publication);
                        else
                            Publication.IsShown.ShouldBeFalse($"Крок {step}: панель публікації без «{Publish.Name}»");
                    });
            }
        });

    private static void FillIfChanged(TextBoxControl field, string text)
    {
        if (field.Text != text)
            field.Fill(text);
    }

    private Func<AutomationElement> Field(string automationId) => () => _root().FindByAutomationId(automationId);

    private AutomationElement[] ReviewerItems() =>
        Field("lstContactWorkflowTask")().FindAllChildren(cf => cf.ByControlType(ControlType.ListItem));

    private static string ReviewerName(AutomationElement item) =>
        item.FindFirstChild(cf => cf.ByControlType(ControlType.Text))?.Name ?? item.Name;

    private AutomationElement TabControl => _root().FindFirstChild(cf => cf.ByControlType(ControlType.Tab));

    private AutomationElement[] Tabs() => TabControl.FindAllChildren(cf => cf.ByControlType(ControlType.TabItem));

    // Вкладки «Workflow» і кроків, без «+».
    private List<AutomationElement> ContentTabs() => Tabs().Where(tab => !IsOfType(tab, AddStepTabType)).ToList();

    private AutomationElement StepTab(int step) =>
        Tabs().Where(tab => IsOfType(tab, StepTabType)).ElementAtOrDefault(step - 1)
            ?? throw new InvalidOperationException($"Кроку {step} у шаблоні немає, кроків: {StepCount}.");

    // Name вкладки — тип моделі («…ViewModels.TaskWorkflowVM»), підпис — TextBlock заголовка (видно лише в raw-дереві).
    private static bool IsOfType(AutomationElement tab, string type) => tab.Name.EndsWith("." + type, StringComparison.Ordinal);

    private static string TabName(AutomationElement tab)
    {
        var text = tab.FindFirstDescendantRaw(cf => cf.ByControlType(ControlType.Text));
        // Довгий підпис ADAICA обрізає до 50 символів, повний — у підказці.
        return text is null ? string.Empty : string.IsNullOrEmpty(text.HelpText) ? text.Name : text.HelpText;
    }

    // Вкладки реагують лише на клік. В області перегляду їх може не бути видно, якщо її прокручено до панелі публікації.
    private static void SelectTab(Func<AutomationElement> tab, string name)
    {
        Scrolling.IntoView(tab);
        UiInput.LeftClick(tab, () => tab().Patterns.SelectionItem.Pattern.IsSelected.Value, $"вибору вкладки «{name}»");
    }

    private void OpenStepTabMenu(int step) =>
        UiInput.RightClick(() => StepTab(step), () => MenuItems().Any(), $"меню вкладки кроку {step}");

    // Меню — WPF Popup вікна бланка (див. Popups).
    private IReadOnlyList<AutomationElement> MenuItems() =>
        Popups.Of(_window)
            .SelectMany(popup => popup.FindAllDescendants(cf => cf.ByControlType(ControlType.MenuItem)))
            .ToList();
}

/// <summary>Назва, біля якої стоїть перемикач «Name des übergeordneten Elements vorausfüllen» (<see cref="WorkflowForm.UseDocumentName"/>).</summary>
public enum DocumentNameField
{
    /// <summary>«Workflowname» на вкладці «Workflow».</summary>
    Workflow,

    /// <summary>«Name der Workflow-Aufgabe» на вкладці «Workflow».</summary>
    MainTask,

    /// <summary>«Name des Workflow-Schrittes» на вкладці кроку.</summary>
    Step,
}
