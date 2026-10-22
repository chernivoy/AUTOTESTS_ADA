using ADAICA_AUTOTEST.Controls;
using ADAICA_AUTOTEST.Infrastructure;
using Allure.Net.Commons;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;

namespace ADAICA_AUTOTEST.Pages;

/// <summary>
/// Workflow у навігаторі Dokumentation. Запустити workflow можна лише на документі під вузлом «DMS – Workflows»: меню
/// документа → «Workflow starten» → шаблон (або попередній workflow документа). Під документом з'являється
/// «Ordner» → папка «Workflow - назва - DV1 - WF1 (OPE…)», а в її «Aufgaben» — головне завдання, під яким у
/// «Aufgaben» видно лише поточний крок, а в «Dateien» — копію документа для кроків. Завдань папки workflow дерево не
/// показує: вони є лише у списку вузла «Aufgaben», звідки їх відкриває подвійний клік (<see cref="OpenRecordInFolder"/>).
/// </summary>
public sealed partial class MainPage
{
    /// <summary>Кореневий вузол документів, на яких запускають workflow (назва класу WORKFLOW у базі).</summary>
    public const string WorkflowsNodeName = "DMS – Workflows";

    /// <summary>Вузол опублікованих документів (назва класу PUBLIC у базі).</summary>
    public const string PublicationsNodeName = "DMS – Veröffentlichungen";

    private const string StartWorkflowMenuItemId = "mnuStartWorkflow";

    // Запис зі списку вузла чи нову гілку дерева ADAICA відкриває за 10–15 с.
    private static readonly TimeSpan RecordOpenTimeout = TimeSpan.FromSeconds(40);

    /// <summary>
    /// Пункт «Workflow starten» меню документа <paramref name="documentNode"/>: чи доступний, його підказка (чому
    /// недоступний) і пункти підменю — попередній workflow документа (якщо був) і всі шаблони. Меню закривається.
    /// </summary>
    public StartWorkflowMenu ReadStartWorkflowMenu(string documentNode) =>
        AllureApi.Step($"Прочитати пункт «Workflow starten» меню документа «{documentNode}»", () =>
        {
            var start = OpenStartWorkflowMenu(documentNode);
            var menu = new StartWorkflowMenu(start.IsEnabled, start.HelpText, start.IsEnabled ? StartWorkflowItems(start) : []);
            CloseContextMenu();
            AllureApi.AddAttachment(
                "Workflow starten",
                "text/plain",
                System.Text.Encoding.UTF8.GetBytes($"{menu.IsEnabled} «{menu.Hint}»{Environment.NewLine}{string.Join(Environment.NewLine, menu.Items)}"),
                ".txt");
            return menu;
        });

    /// <summary>
    /// Меню документа <paramref name="documentNode"/> → «Workflow starten» → <paramref name="templateName"/>: вікно
    /// запуску з бланком, заповненим із шаблону (<see cref="WorkflowDialog"/>, дочірнє головного вікна).
    /// </summary>
    public WorkflowDialog OpenStartWorkflow(string documentNode, string templateName) =>
        AllureApi.Step($"Запустити workflow на документі «{documentNode}»: меню → Workflow starten → {templateName}", () =>
        {
            var start = OpenStartWorkflowMenu(documentNode);
            if (!start.IsEnabled)
                throw new InvalidOperationException($"Пункт «Workflow starten» недоступний: «{start.HelpText}».");

            // Пункти підменю мають той самий AutomationId, що й «Workflow starten», тож шукаємо за назвою. Шаблони йдуть
            // останніми: попередній workflow документа, запущений з того самого шаблону, має ту саму назву й стоїть першим.
            new MenuItemControl(templateName, () => Wait.For(
                    () => StartWorkflowItemElements(start).LastOrDefault(item => item.Name == templateName),
                    $"шаблону «{templateName}» у підменю «Workflow starten»"))
                .Click();
            return new WorkflowDialog(Window);
        });

    /// <summary>
    /// Відкриває вузол-папку <paramref name="folderName"/> під записом <paramref name="recordNode"/> і подвійним кліком
    /// по рядку <paramref name="name"/> (колонка «Name» чи «Dokumentname») — запис. ADAICA розгортає дерево до запису й
    /// виділяє його (на це йде 10–15 с), а вкладка навігатора бере назву запису.
    /// </summary>
    public MainPage OpenRecordInFolder(string recordNode, string folderName, string name) =>
        AllureApi.Step($"Відкрити «{name}» зі списку вузла {folderName} під «{recordNode}»", () =>
        {
            OpenRecordFolder(recordNode, folderName).Open(
                name,
                () => SelectedNavigatorNodeName is { } selected && WithoutRecordRange(selected) == name);
            return this;
        });

    /// <summary>
    /// Виділяє в дереві папку workflow <paramref name="folderNode"/> («Workflow - … (OPE…)») під документом
    /// <paramref name="documentNode"/>: «DMS – Workflows» → [«Dateien», коли документів кілька] → документ → «Ordner» → папка.
    /// </summary>
    public MainPage SelectWorkflowFolder(string documentNode, string folderNode) =>
        AllureApi.Step($"Вибрати папку workflow «{folderNode}» під документом «{documentNode}»", () =>
        {
            SelectNavigatorNode(WorkflowsNodeName);
            WaitForChildNodes([WorkflowsNodeName]);
            if (!SelectDocumentInPages([], documentNode))
                throw new InvalidOperationException($"Документа «{documentNode}» немає під «{WorkflowsNodeName}».");

            // Документ виділено: далі шлях — від нього.
            List<string> path = [documentNode];
            SelectLoadedChild(path, "Ordner");
            SelectLoadedChild(path, folderNode);
            return this;
        });

    /// <summary>
    /// Нова актуальна версія документа <paramref name="documentNode"/> з файлу: меню документа → версії
    /// (`mnuDocVersions`) → «als aktuelle Version» (`mnuDocVersAddAsActual`) → з файлової системи
    /// (`mnuDocVersAddAsActualFromFile`) → вибір файлу в діалозі Windows. На відміну від «Bearbeiten», Word не потрібен.
    /// </summary>
    public void AddActualVersionFromFile(string documentNode, string filePath) =>
        AllureApi.Step($"Додати документу «{documentNode}» актуальну версію з файлу: меню → Version → als aktuelle Version → aus dem Dateisystem", () =>
        {
            OpenNodeContextMenu(SelectedOrAnyNode(documentNode));
            PopupMenuItem("Versionen", "mnuDocVersions").Expand();
            PopupMenuItem("Als aktuelle Version", "mnuDocVersAddAsActual", "mnuDocVersions").Expand();
            PopupMenuItem("Aus dem Dateisystem", "mnuDocVersAddAsActualFromFile", "mnuDocVersions", "mnuDocVersAddAsActual").Click();
            FileOpenDialog.Open(Window, filePath);
        });

    /// <summary>
    /// Відкриває документ <paramref name="name"/> (колонка «Dokumentname») подвійним кліком у списку вузла
    /// <paramref name="folderName"/> під записом <paramref name="recordNode"/>: ADAICA показує його в області перегляду.
    /// </summary>
    public DocumentForm OpenDocumentInFolder(string recordNode, string folderName, string name) =>
        AllureApi.Step($"Відкрити документ «{name}» зі списку вузла {folderName} під «{recordNode}»", () =>
        {
            OpenRecordFolder(recordNode, folderName).Open(name, () => ShownValue(DocumentForm.NameId) == name, RecordOpenTimeout);
            return WaitForOpenedDocument(name);
        });

    /// <summary>
    /// Виділяє в дереві Ordner публікацій <paramref name="folderNode"/> («назва (OPE…)»): «DMS – Veröffentlichungen» →
    /// «Ordner» → Ordner. Як і під «DMS – Workflows», вузли завантажуються лише після вибору батька.
    /// </summary>
    public MainPage SelectPublicationFolder(string folderNode) =>
        AllureApi.Step($"Вибрати Ordner публікацій «{folderNode}»", () =>
        {
            SelectNavigatorNode(PublicationsNodeName);
            List<string> path = [PublicationsNodeName];
            SelectLoadedChild(path, "Ordner");
            SelectLoadedChild(path, folderNode);
            return this;
        });

    /// <summary>
    /// Чекає, доки область перегляду покаже завдання <paramref name="name"/>, і повертає його бланк. Бланк завдання ADAICA
    /// відкриває на вкладці, яку відкривали останньою (після кроку workflow — «Workflow»), а поле назви є в дереві лише на
    /// «Aufgabe», тож її відкриваємо.
    /// </summary>
    public TaskForm WaitForOpenedTaskNamed(string name) => AllureApi.Step($"Дочекатися відкриття завдання «{name}»", () =>
        Wait.For(
            () =>
            {
                if (InformationView() is not { } view || view.FindFirstDescendant(cf => cf.ByAutomationId("tcRecordTask")) is null)
                    return null;

                var form = new TaskForm(view, Window);
                if (ShownValue("txtRecordName") is null)
                    form.ShowMainTab();
                return ShownValue("txtRecordName") == name ? form : null;
            },
            $"завдання «{name}» в області перегляду",
            RecordOpenTimeout));

    // Вузли під «DMS – Workflows» ADAICA завантажує, лише коли батьківський вузол вибрано: доти трикутника в нього немає.
    private IReadOnlyList<string> WaitForChildNodes(List<string> path) =>
        Wait.For(
            () => ChildNodeNames([.. path]) is { Count: > 0 } children ? children : null,
            $"вузлів під «{path[^1]}»",
            RecordOpenTimeout);

    private void SelectLoadedChild(List<string> path, string child)
    {
        Wait.Until(
            () =>
            {
                if (ChildNodeNames([.. path]).Contains(child))
                    return true;

                ScrollParentToTop(path);
                return false;
            },
            $"вузла «{child}» під «{path[^1]}»",
            RecordOpenTimeout);
        path.Add(child);
        SelectNavigatorNodeAt([.. path]);
    }

    private void ScrollParentToTop(List<string> path) => ScrollToTop(NodeAt([.. path]));

    // Дерево віртуалізоване: дочірні вузли нижче видимої області в UI Automation з'являються, лише коли до них
    // прогорнути (після кількох запусків workflow гілки DMS довгі). Гортаємо по рядку (один рядок — один крок), доки
    // вузол не опиниться вгорі.
    private void ScrollToTop(AutomationElement node)
    {
        if (NavigatorTree.Patterns.Scroll.PatternOrDefault is not { } scroll || !scroll.VerticallyScrollable.Value)
            return;

        // Вузол дерева — рядок плаского списку, тож його висота — висота рядка.
        var rect = node.BoundingRectangle;
        if (rect.Height <= 0)
            return;

        var rows = (int)((rect.Top - NavigatorTree.BoundingRectangle.Top) / rect.Height) - 2;
        for (var row = 0; row < rows && scroll.VerticalScrollPercent.Value < 100; row++)
            scroll.Scroll(ScrollAmount.NoAmount, ScrollAmount.SmallIncrement);
    }

    /// <summary>
    /// Шукає й виділяє документ під «DMS – Workflows». Коли документів багато, ADAICA кладе їх у вузли-сторінки:
    /// «Dateien (1-29)» → «Dateien (1-25)», «Dateien (26-29)» → документи. Сторінки однаково називаються «Dateien», тож
    /// розрізняємо їх за повним підписом (<paramref name="pagePath"/> — підписи сторінок від кореня вниз). Документи
    /// сторінки ADAICA завантажує, лише коли її вибрано.
    /// </summary>
    private bool SelectDocumentInPages(List<string> pagePath, string documentNode)
    {
        bool IsDocument(AutomationElement node) => WithoutRecordRange(FullText(node)) == documentNode;

        var children = LoadedChildren(pagePath);
        if (children.Any(IsDocument))
        {
            AutomationElement Document() => Wait.For(
                () => DirectChildren(PageNode(pagePath)).FirstOrDefault(IsDocument),
                $"документа «{documentNode}»");
            UiInput.LeftClick(
                () => NodeText(ScrolledIntoView(Document())) ?? Document(),
                () => SelectedNavigatorNodeName is { } selected && WithoutRecordRange(selected) == documentNode,
                $"виділення документа «{documentNode}»");
            return true;
        }

        foreach (var page in children.Select(FullText).Where(text => WithoutRecordRange(text) == "Dateien").ToList())
        {
            List<string> next = [.. pagePath, page];
            UiInput.LeftClick(
                () => NodeText(ScrolledIntoView(PageNode(next))) ?? PageNode(next),
                () => SelectedNavigatorNodeName == page,
                $"виділення вузла «{page}»");
            if (SelectDocumentInPages(next, documentNode))
                return true;
        }

        return false;
    }

    // Дочірні вузли сторінки, коли ADAICA їх завантажила (з'явився трикутник); сторінку розгортаємо й гортаємо вгору,
    // щоб її вузли потрапили у видиму область.
    private List<AutomationElement> LoadedChildren(IReadOnlyList<string> pagePath) =>
        Wait.For(
            () =>
            {
                var page = PageNode(pagePath);
                if (page.FindFirstChild(cf => cf.ByAutomationId("ToggleButton")) is null)
                    return null;

                Expand(page);
                ScrollToTop(PageNode(pagePath));
                return DirectChildren(PageNode(pagePath)) is { Count: > 0 } children ? children : null;
            },
            $"вузлів під «{(pagePath.Count == 0 ? WorkflowsNodeName : pagePath[^1])}»",
            RecordOpenTimeout);

    // Вузол-сторінка за підписами від «DMS – Workflows» униз (порожній шлях — сам корінь).
    private AutomationElement PageNode(IReadOnlyList<string> pagePath)
    {
        var node = NavigatorNode(WorkflowsNodeName);
        foreach (var page in pagePath)
        {
            var parent = node;
            node = Wait.For(
                () =>
                {
                    if (DirectChildren(parent).FirstOrDefault(child => FullText(child) == page) is { } found)
                        return found;

                    ScrollToTop(parent);
                    return null;
                },
                $"вузла «{page}»");
        }

        return node;
    }

    private static string FullText(AutomationElement node) => NodeText(node)!.Name.TrimEnd();

    // Безпосередні дочірні вузли: найменший відступ серед вкладених (як у ChildNodeNames, але самі вузли).
    private List<AutomationElement> DirectChildren(AutomationElement parent)
    {
        var children = ChildNodes(parent);
        var indent = children.Select(child => NodeText(child)!.BoundingRectangle.Left).DefaultIfEmpty().Min();
        return children.Where(child => NodeText(child)!.BoundingRectangle.Left == indent).ToList();
    }

    private AutomationElement OpenStartWorkflowMenu(string documentNode)
    {
        OpenNodeContextMenu(SelectedOrAnyNode(documentNode));
        return Wait.For(
            () => Popups.Of(Window)
                .SelectMany(popup => popup.FindAllDescendants(cf => cf.ByAutomationId(StartWorkflowMenuItemId)))
                .FirstOrDefault(item => item.Patterns.ExpandCollapse.IsSupported),
            "пункту «Workflow starten» у меню документа");
    }

    private IReadOnlyList<string> StartWorkflowItems(AutomationElement start) =>
        Wait.For(() => StartWorkflowItemElements(start) is { Count: > 0 } items ? items : null, "пунктів підменю «Workflow starten»")
            .Select(item => item.Name)
            .ToList();

    // Підменю розгортається лише через ExpandCollapse. Його Popup UI Automation показує і окремо, і всередині меню
    // документа, тож кожен пункт трапляється двічі. Попередній workflow і шаблон, з якого його запущено, мають однакову
    // назву, тож різні пункти відрізняємо за положенням. Пункти підменю (на відміну від «Workflow starten») не розгортаються.
    private List<AutomationElement> StartWorkflowItemElements(AutomationElement start)
    {
        if (start.Patterns.ExpandCollapse.Pattern.ExpandCollapseState.Value != ExpandCollapseState.Expanded)
            start.Patterns.ExpandCollapse.Pattern.Expand();
        return Popups.Of(Window)
            .SelectMany(popup => popup.FindAllDescendants(cf => cf.ByAutomationId(StartWorkflowMenuItemId)))
            .Where(item => !item.Patterns.ExpandCollapse.IsSupported && !item.BoundingRectangle.IsEmpty)
            .DistinctBy(item => (item.Name, item.BoundingRectangle.Top))
            .OrderBy(item => item.BoundingRectangle.Top)
            .ToList();
    }
}

/// <summary>
/// Пункт «Workflow starten» меню документа: доступність, підказка (для недоступного — чому, наприклад «Workflow für
/// dieses Dokument ist bereits gestartet») і пункти підменю — попередній workflow документа й шаблони.
/// </summary>
public sealed record StartWorkflowMenu(bool IsEnabled, string Hint, IReadOnlyList<string> Items);
