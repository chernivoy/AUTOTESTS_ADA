using System.Text.RegularExpressions;
using ADAICA_AUTOTEST.Controls;
using ADAICA_AUTOTEST.Infrastructure;
using Allure.Net.Commons.Attributes;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;

namespace ADAICA_AUTOTEST.Pages;

/// <summary>Робочий простір ADAICA після входу.</summary>
public sealed partial class MainPage : AdaicaWindow
{
    private const string SystemMenuId = "mnuSystemMenuWndMain";
    private const string DemoVersionWindowId = "wndDemoVersion";

    // Така сама смуга вкладок і кнопка «+» є й унизу робочої області, тому верхні шукаємо серед дочірніх елементів вікна.
    private const string NavigatorTabsId = "PART_TabItems";
    private const string AddNavigatorButtonId = "PART_AddTab";

    private const string ProjectsNodeName = "Projekte";
    private const string FoldersNodeName = "Ordner";
    private const string TasksNodeName = "Aufgaben";
    private const string AppointmentsNodeName = "Termine";
    private const string NotesNodeName = "Notizen";
    private const string MailsNodeName = "E-Mails";
    private const string RecycleBinNodeName = "Papierkorb";
    private const string ChatsNodeName = "Meine Chats";
    private const string SaveSentMailQuestion = "Soll diese Nachricht gespeichert werden?";
    private const string ContactsRootNodeName = "Gesamtliste";
    private const string ContactFavoritesNodeName = "Favoriten";
    private const string EmployeesNodeName = "Mitarbeiter";
    private const string NewRecordMenuItemId = "mnuFolderNewRecords";
    private const string NewTaskMenuItemId = "mnuNewTaskDefault";

    // Контекстне меню запису (наприклад, проєкту): «Neu» — підменю з типами дочірніх записів.
    private const string NewChildMenuItemId = "mnuNew";

    // UIA_ScrollPatternNoScroll: не змінювати прокрутку по цій осі.
    private const double NoScroll = -1;

    // Після надсилання з Outlook ADAICA питає про збереження листа, щойно Outlook його відправить.
    private static readonly TimeSpan SentMailTimeout = TimeSpan.FromSeconds(60);

    public MainPage(AppSession session)
        : base(session)
    {
        Element(SystemMenuId);
        CloseDemoVersionWindowIfShown();

        NavigatorTitle = new LabelControl("Navigator", Locate.ById(Window, "tbNameOfNavigator"));
        AddNavigatorButton = new ToggleButtonControl("+", () => Window.FindChildByAutomationId(AddNavigatorButtonId));
    }

    /// <summary>База даних, до якої підключено застосунок (із заголовка «… [БАЗА:СХЕМА] …»).</summary>
    public string Database => ParseTitle().Groups["database"].Value;

    /// <summary>Схема, до якої підключено застосунок.</summary>
    public string Schema => ParseTitle().Groups["schema"].Value;

    /// <summary>
    /// Користувач, під яким увійшли, у тому вигляді, як ADAICA підписує автора запису («Erstellt durch»):
    /// «Прізвище, Ім'я». У заголовку вікна він записаний як «Ім'я Прізвище»: «ADAICA: Igor Chernivoy [БАЗА:СХЕМА]».
    /// </summary>
    public string UserDisplayName
    {
        get
        {
            var fullName = UserInTitle().Match(Title).Groups["user"].Value;
            var lastSpace = fullName.LastIndexOf(' ');
            return lastSpace < 0 ? fullName : $"{fullName[(lastSpace + 1)..]}, {fullName[..lastSpace]}";
        }
    }

    /// <summary>Назва навігатора, відкритого зараз (заголовок над деревом зліва).</summary>
    public LabelControl NavigatorTitle { get; }

    /// <summary>Кнопка «+» праворуч від вкладок: відкриває список навігаторів.</summary>
    public ToggleButtonControl AddNavigatorButton { get; }

    /// <summary>Назви вкладок навігаторів у верхній смузі, зліва направо.</summary>
    public IReadOnlyList<string> NavigatorTabNames => NavigatorTabs.Select(TabName).ToList();

    private AutomationElement[] NavigatorTabs => Window.FindChildByAutomationId(NavigatorTabsId).FindAllChildren();

    public static bool IsShownIn(Window window) =>
        window.FindFirstDescendant(cf => cf.ByAutomationId(SystemMenuId)) is not null;

    [AllureStep("Відкрити навігатор {navigatorName} кнопкою «+»")]
    public MainPage OpenNavigator(string navigatorName)
    {
        var tabCount = NavigatorTabs.Length;

        AddNavigatorButton.SwitchOn();

        AutomationElement? NavigatorItem() => FindInPopups("listView")?.FindFirstChild(cf => cf.ByName(navigatorName));
        var item = Wait.For(NavigatorItem, $"пункту «{navigatorName}» у списку навігаторів");

        // ADAICA відкриває навігатор лише на клік: SelectionItem.Select() тільки виділяє пункт, Enter не діє.
        // Кожне відкриття додає нову вкладку, навіть якщо такий навігатор уже відкритий.
        UiInput.LeftClick(
            () => NavigatorItem() ?? item,
            () => NavigatorTabs.Length == tabCount + 1 && NavigatorTitle.Text == navigatorName,
            $"відкриття навігатора «{navigatorName}»");
        return this;
    }

    /// <summary>
    /// Перемикається на відкриту вкладку навігатора <paramref name="navigatorName"/>. Вкладка — кнопка WPF без
    /// InvokePattern, тож реагує лише на клік.
    /// </summary>
    [AllureStep("Перейти на вкладку навігатора {navigatorName}")]
    public MainPage SwitchToNavigatorTab(string navigatorName)
    {
        AutomationElement Tab() => NavigatorTabs.FirstOrDefault(item => TabName(item) == navigatorName)
            ?? throw new InvalidOperationException($"Вкладки навігатора «{navigatorName}» немає: {string.Join(", ", NavigatorTabNames)}.");
        Tab();
        if (NavigatorTitle.Text == navigatorName)
            return this;

        UiInput.LeftClick(
            () =>
            {
                var tab = Tab();
                return tab.FindFirstDescendantRaw(cf => cf.ByAutomationId("PART_Text")) ?? tab;
            },
            () => NavigatorTitle.Text == navigatorName,
            $"переходу на навігатор «{navigatorName}»");
        return this;
    }

    /// <summary>
    /// Закриває останні вкладки навігаторів, доки їх не залишиться <paramref name="keep"/>.
    /// Відкриті вкладки ADAICA запам'ятовує між сеансами, тому тести прибирають за собою.
    /// Назва вкладки змінюється разом із вибраним вузлом, тому орієнтуємося на кількість, а не на назву:
    /// нові вкладки завжди додаються в кінець.
    /// </summary>
    [AllureStep("Закрити останні вкладки навігаторів, залишити {keep}")]
    public MainPage CloseLastNavigatorTabs(int keep)
    {
        for (var tabs = NavigatorTabs; tabs.Length > keep; tabs = NavigatorTabs)
        {
            tabs[^1].FindByAutomationId("PART_CloseButton").AsButton().Invoke();
            Wait.Until(() => NavigatorTabs.Length == tabs.Length - 1, "закриття вкладки навігатора");
        }

        return this;
    }

    /// <summary>
    /// Вузол у дереві активного навігатора. До назви ADAICA може додавати діапазон записів,
    /// наприклад «Projekte (1-25)», тому порівнюємо лише саму назву.
    /// </summary>
    public AutomationElement NavigatorNode(string nodeName) =>
        Wait.For(
            () => RealizedNode(nodeName) ?? ScrolledToNode(nodeName),
            $"вузла «{nodeName}» у дереві навігатора");

    private AutomationElement? RealizedNode(string nodeName) =>
        NavigatorTree.FindAllChildren()
            .FirstOrDefault(node => NodeText(node) is { } text && WithoutRecordRange(text.Name) == nodeName);

    /// <summary>
    /// Дерево віртуалізоване: вузлів поза видимою областю в UI Automation немає. Тому гортаємо дерево від початку,
    /// доки вузол не з'явиться. Після цього він у видимій області.
    /// </summary>
    private AutomationElement? ScrolledToNode(string nodeName)
    {
        var scroll = NavigatorTree.Patterns.Scroll.PatternOrDefault;
        if (scroll?.VerticallyScrollable.Value != true)
            return null;

        scroll.SetScrollPercent(NoScroll, 0);
        while (true)
        {
            if (RealizedNode(nodeName) is { } node)
                return node;

            var before = scroll.VerticalScrollPercent.Value;
            scroll.Scroll(ScrollAmount.NoAmount, ScrollAmount.LargeIncrement);
            if (scroll.VerticalScrollPercent.Value <= before)
                return null;
        }
    }

    /// <summary>Назва вузла, виділеного в дереві навігатора, або null, якщо нічого не виділено.</summary>
    public string? SelectedNavigatorNodeName =>
        NavigatorTree.Patterns.Selection.Pattern.Selection.Value
            .Select(node => NodeText(node)?.Name.TrimEnd())
            .FirstOrDefault();

    /// <summary>
    /// Шлях до виділеного вузла: назви його предків від кореня дерева й назва самого вузла, без діапазонів записів.
    /// </summary>
    public IReadOnlyList<string> SelectedNavigatorNodePath =>
        SelectedNodeWithAncestors().Select(node => WithoutRecordRange(NodeText(node)!.Name)).ToList();

    [AllureStep("Дочекатися відкриття проєкту {number}")]
    public ProjectForm WaitForOpenedProject(string number) =>
        WaitForOpenedRecord(ProjectForm.NumberId, number, view => new ProjectForm(view));

    [AllureStep("Дочекатися відкриття Ordner {number}")]
    public ProjectForm WaitForOpenedFolder(string number) =>
        WaitForOpenedRecord(ProjectForm.NumberId, number, view => new ProjectForm(view, "Ordner"));

    [AllureStep("Дочекатися відкриття завдання {number}")]
    public TaskForm WaitForOpenedTask(string number) =>
        WaitForOpenedRecord(TaskForm.NumberId, number, view => new TaskForm(view));

    [AllureStep("Дочекатися відкриття терміну {number}")]
    public AppointmentForm WaitForOpenedAppointment(string number) =>
        WaitForOpenedRecord(AppointmentForm.NumberId, number, view => new AppointmentForm(view));

    /// <summary>
    /// Номера в інфозапису немає, тому його впізнаємо за текстом у полі «Bemerkung»,
    /// який відрізняється від прогону до прогону (тест додає туди час запису).
    /// </summary>
    [AllureStep("Дочекатися відкриття інфозапису з Bemerkung {remark}")]
    public NoteForm WaitForOpenedNote(string remark) =>
        WaitForOpenedRecord(NoteForm.RemarkId, remark, view => new NoteForm(view));

    /// <summary>
    /// Фірму впізнаємо за назвою: тест дає кожній фірмі унікальну назву, а номера в картці фірми немає.
    /// Картка відкривається на вкладці, яку відкривали останньою, а поле назви є лише на Allgemein, тож її відкриваємо.
    /// </summary>
    [AllureStep("Дочекатися відкриття фірми {name}")]
    public CompanyForm WaitForOpenedCompany(string name)
    {
        var view = Wait.For(
            () => Window.FindFirstDescendant(cf => cf.ByClassName("InformationView")) is { } information
                && information.FindFirstDescendant(cf => cf.ByAutomationId(CompanyForm.TabsId)) is not null
                    ? information
                    : null,
            "картки фірми в області перегляду");
        new CompanyForm(Window, view).GeneralTab.Select();
        return WaitForOpenedRecord(CompanyForm.NameId, name, information => new CompanyForm(Window, information));
    }

    [AllureStep("Дочекатися відкриття відділу {name}")]
    public DepartmentForm WaitForOpenedDepartment(string name) =>
        WaitForOpenedRecord(DepartmentForm.NameId, name, view => new DepartmentForm(view));

    /// <summary>
    /// Персону впізнаємо за повним ім'ям: номера в картці персони немає, а тест створює її під новою фірмою.
    /// Картка відкривається на вкладці, яку відкривали останньою, а поле імені є лише на Allgemein, тож її відкриваємо.
    /// </summary>
    [AllureStep("Дочекатися відкриття персони {fullName}")]
    public PersonForm WaitForOpenedPerson(string fullName)
    {
        var view = Wait.For(
            () => Window.FindFirstDescendant(cf => cf.ByClassName("InformationView")) is { } information
                && information.FindFirstDescendant(cf => cf.ByAutomationId(PersonForm.TabsId)) is not null
                    ? information
                    : null,
            "картки персони в області перегляду");
        new PersonForm(Window, view).GeneralTab.Select();
        return WaitForOpenedRecord(PersonForm.NameId, fullName, information => new PersonForm(Window, information));
    }

    /// <summary>
    /// Перевідкриває фірму з дерева навігатора Kontakte, щоб картка показала збережені значення, а не те, що
    /// лишилося в ній після редагування. Вузол «Favoriten» показує список без картки, тож видно, що картку закрито.
    /// </summary>
    /// <param name="nodeName">Вузол фірми в дереві.</param>
    /// <param name="name">Повна назва фірми в картці.</param>
    [AllureStep("Перевідкрити фірму {nodeName}")]
    public CompanyForm ReopenCompany(string nodeName, string name)
    {
        SelectNavigatorNode(ContactFavoritesNodeName);
        Wait.Until(
            () => Window.FindFirstDescendant(cf => cf.ByAutomationId(CompanyForm.TabsId)) is null,
            "закриття картки фірми");
        SelectNavigatorNode(nodeName);
        return WaitForOpenedCompany(name);
    }

    /// <summary>Виділяє вузол у дереві навігатора. Дерево реагує лише на клік.</summary>
    [AllureStep("Вибрати вузол {nodeName} у дереві навігатора")]
    public MainPage SelectNavigatorNode(string nodeName)
    {
        UiInput.LeftClick(
            () =>
            {
                var node = NavigatorNode(nodeName);
                return NodeText(ScrolledIntoView(node)) ?? node;
            },
            () => SelectedNavigatorNodeName is { } selected && WithoutRecordRange(selected) == nodeName,
            $"виділення вузла «{nodeName}»");
        return this;
    }

    /// <summary>
    /// Вибирає в навігаторі Dashboard вузол залогіненого користувача — корінь дерева («Chernivoy, Igor»). Праворуч
    /// ADAICA показує його день: «Meine Aufgaben», «Meine Termine», «Meine Mails».
    /// </summary>
    [AllureStep("Вибрати вузол користувача в навігаторі Dashboard")]
    public DashboardTaskList OpenUserTaskList()
    {
        var root = Wait.For(() => NavigatorTree.FindFirstChild(), "кореня дерева навігатора");
        var userName = WithoutRecordRange(NodeText(root)!.Name);
        SelectNavigatorNode(userName);
        return new DashboardTaskList(Window, userName);
    }

    /// <summary>
    /// Вибирає в навігаторі Dashboard вузол «Meine Chats» під користувачем: праворуч список його чатів.
    /// </summary>
    [AllureStep("Вибрати вузол «Meine Chats» у навігаторі Dashboard")]
    public ChatList OpenUserChats()
    {
        SelectNavigatorNode(ChatsNodeName);
        return new ChatList(Window);
    }

    /// <summary>Іконка чатів з лічильником нових повідомлень праворуч угорі.</summary>
    public ChatNotifications ChatNotifications => new(Window);

    [AllureStep("Відкрити вікно пошуку: System → Finden…")]
    public SearchWindow OpenSearchWindow()
    {
        new MenuItemControl("System", Locate.ById(Window, SystemMenuId)).Expand();
        PopupMenuItem("Finden…", "mnuSystemSearchWndMain").Click();
        return new SearchWindow(Window);
    }

    /// <summary>
    /// System → «Finden &amp; Erstellen» на вкладці «Kontakte»: пошук контакту з будь-якого навігатора.
    /// </summary>
    [AllureStep("Відкрити пошук контактів: System → Finden & Erstellen → Kontakte")]
    public ContactFindCreateWindow OpenContactSearch()
    {
        new MenuItemControl("System", Locate.ById(Window, SystemMenuId)).Expand();
        PopupMenuItem("Finden & Erstellen", "mnuSystemFindCreate").Click();
        new TabItemControl("Kontakte", () => Wait.FindByAutomationId(Window, "tiPersonsAndCompanies")).Select();
        return new ContactFindCreateWindow(Window);
    }

    [AllureStep("Відкрити налаштування: Konfiguration → Einstellungen")]
    public OptionsWindow OpenOptions()
    {
        new MenuItemControl("Konfiguration", Locate.ById(Window, "mnuConfigurationWndMain")).Expand();
        PopupMenuItem("Einstellungen", "mnuOptionsWndMain").Click();
        return new OptionsWindow(Window);
    }

    [AllureStep("Відкрити асистент пошти: Benutzereinstellungen → Maileinstellungen")]
    public MailSettingsWizard OpenMailSettings()
    {
        new MenuItemControl("Benutzereinstellungen", Locate.ById(Window, "mnuUserConfiguration")).Expand();
        PopupMenuItem("Maileinstellungen", "mnuMailConfigurationSettings").Click();
        return new MailSettingsWizard(Window);
    }

    /// <summary>
    /// Новий лист: вузол «E-Mails» → контекстне меню → «Neu». ADAICA відкриває вікно нового листа в Outlook
    /// (див. <see cref="OutlookApplication.WaitForNewMail"/>), а не власне вікно.
    /// </summary>
    [AllureStep("Відкрити новий лист в Outlook: вузол E-Mails → контекстне меню → Neu")]
    public void OpenNewMail()
    {
        OpenNodeContextMenu(MailsNodeName);
        PopupMenuItem("Neu", NewRecordMenuItemId).Click();
    }

    /// <summary>
    /// Після надсилання листа, створеного з ADAICA, вона питає «Soll diese Nachricht gespeichert werden?»; «Ja»
    /// відкриває асистент збереження листа.
    /// </summary>
    [AllureStep("Зберегти надісланий лист в ADAICA: «Soll diese Nachricht gespeichert werden?» → Ja")]
    public MailSaveDialog ConfirmSaveSentMail()
    {
        var question = Wait.For(
            () => FindMessageBox() is { } dialog && dialog.Message.Text == SaveSentMailQuestion ? dialog : null,
            $"питання «{SaveSentMailQuestion}»",
            SentMailTimeout);
        question.Button("Ja").Click();
        return new MailSaveDialog(Window);
    }

    /// <summary>Лист в області перегляду; свій лист тест упізнає за унікальною адресою отримувача.</summary>
    [AllureStep("Дочекатися відкриття листа до {recipientAddress}")]
    public MailView WaitForOpenedMail(string recipientAddress) =>
        Wait.For(
            () => InformationView() is { } view
                && MailView.Field(view, "An")?.Contains($"<{recipientAddress}>", StringComparison.Ordinal) == true
                    ? new MailView(view)
                    : null,
            $"листа до {recipientAddress} в області перегляду");

    /// <summary>
    /// «Beziehungen» → «Zugriffspfade» запису, відкритого в області перегляду (після <c>WaitForOpened…</c>): записи, під
    /// якими він лежить, і його контакти.
    /// </summary>
    public RecordAccessPaths ReadOpenedRecordAccessPaths() =>
        RecordAccessPaths.Read(Wait.For(InformationView, "області перегляду"), "запису");

    /// <summary>
    /// Відкриває вузол-папку <paramref name="folderName"/> («Kontakte», «Notizen» тощо) під записом
    /// <paramref name="recordNode"/> (розгортає запис, якщо треба) і повертає її список в області перегляду.
    /// Запис шукаємо серед виділеного вузла та його предків, як і в OpenNewChildMenu.
    /// </summary>
    [AllureStep("Відкрити вузол {folderName} під записом {recordNode}")]
    public RecordList OpenRecordFolder(string recordNode, string folderName)
    {
        var record = SelectedOrAnyNode(recordNode);
        var folder = ChildNode(record, folderName);
        UiInput.LeftClick(
            () => NodeText(ScrolledIntoView(folder)) ?? folder,
            () => SelectedNodeWithAncestors() is { Count: >= 2 } path
                && WithoutRecordRange(NodeText(path[^1])!.Name) == folderName
                && WithoutRecordRange(NodeText(path[^2])!.Name) == recordNode,
            $"виділення вузла «{folderName}» під «{recordNode}»");
        return new RecordList(Window);
    }

    /// <summary>Вузол «Papierkorb» навігатора Dokumentation: видалені записи.</summary>
    [AllureStep("Відкрити вузол Papierkorb")]
    public RecycleBin OpenRecycleBin()
    {
        SelectNavigatorNode(RecycleBinNodeName);
        return new RecycleBin(Window);
    }

    /// <summary>
    /// Внутрішній ID запису <paramref name="recordNode"/> з вікна «Eigenschaften» (контекстне меню запису). Запис
    /// шукаємо серед виділеного вузла та його предків, як і в OpenNewChildMenu.
    /// </summary>
    [AllureStep("Прочитати ID запису {recordNode}: контекстне меню → Eigenschaften")]
    public string ReadRecordId(string recordNode)
    {
        OpenNodeContextMenu(SelectedOrAnyNode(recordNode));
        PopupMenuItem("Eigenschaften", "mnuProperties").Click();
        var properties = new PropertiesWindow(Window);
        var id = properties.Id;
        properties.Close();
        return id;
    }

    /// <summary>
    /// «Löschen» з контекстного меню запису <paramref name="recordNode"/> (шукаємо серед виділеного вузла та його предків).
    /// ADAICA питає «Folgende Elemente werden gelöscht: …»; питання повертаємо тесту. «Ja» переносить запис разом із
    /// дочірніми, не пов'язаними з іншими записами, у Papierkorb.
    /// </summary>
    [AllureStep("Видалити запис {recordNode}: контекстне меню → Löschen")]
    public MessageBoxDialog DeleteRecord(string recordNode)
    {
        OpenNodeContextMenu(SelectedOrAnyNode(recordNode));
        PopupMenuItem("Löschen", "mnuRecordDeleteItem").Click();
        return Wait.For(FindMessageBox, "питання «Löschen»");
    }

    [AllureStep("Відкрити нове вікно проєкту: вузол Projekte → контекстне меню → Neu")]
    public ProjectDialog OpenNewProjectDialog()
    {
        OpenNodeContextMenu(ProjectsNodeName);
        PopupMenuItem("Neu", NewRecordMenuItemId).Click();
        return new ProjectDialog(Window);
    }

    [AllureStep("Відкрити нове вікно Ordner: вузол Ordner → контекстне меню → Neu")]
    public FolderDialog OpenNewFolderDialog()
    {
        OpenNodeContextMenu(FoldersNodeName);
        PopupMenuItem("Neu", NewRecordMenuItemId).Click();
        return new FolderDialog(Window);
    }

    [AllureStep("Відкрити нове вікно завдання: вузол Aufgaben → контекстне меню → Neu → Neue Aufgabe")]
    public TaskDialog OpenNewTaskDialog()
    {
        OpenNodeContextMenu(TasksNodeName);
        // На вузлі Aufgaben «Neu» — підменю з варіантами створення.
        PopupMenuItem("Neu", NewRecordMenuItemId).Expand();
        PopupMenuItem("Neue Aufgabe", NewTaskMenuItemId, NewRecordMenuItemId).Click();
        return new TaskDialog(Window);
    }

    [AllureStep("Відкрити нове вікно терміну: вузол Termine → контекстне меню → Neu")]
    public AppointmentDialog OpenNewAppointmentDialog()
    {
        OpenNodeContextMenu(AppointmentsNodeName);
        PopupMenuItem("Neu", NewRecordMenuItemId).Click();
        return new AppointmentDialog(Window);
    }

    [AllureStep("Відкрити нове вікно інфозапису: вузол Notizen → контекстне меню → Neu")]
    public NoteDialog OpenNewNoteDialog()
    {
        OpenNodeContextMenu(NotesNodeName);
        PopupMenuItem("Neu", NewRecordMenuItemId).Click();
        return new NoteDialog(Window);
    }

    /// <summary>
    /// У навігаторі Kontakte нову фірму створюють через вікно «Finden &amp; Erstellen»: спершу пошук, чи її ще немає.
    /// </summary>
    [AllureStep("Відкрити вікно нової фірми: вузол Gesamtliste → контекстне меню → Neue Firma")]
    public ContactFindCreateWindow OpenNewCompanyWindow()
    {
        OpenNodeContextMenu(ContactsRootNodeName);
        PopupMenuItem("Neue Firma", "MnuCompanyNew").Click();
        return new ContactFindCreateWindow(Window);
    }

    /// <summary>
    /// Нова організаційна одиниця (відділ) фірми: з контекстного меню вузла «Mitarbeiter» фірми. Вузол фірми
    /// розгортаємо, якщо він згорнутий. Фірму шукаємо серед виділеного вузла та його предків, як і в OpenNewChildMenu.
    /// </summary>
    [AllureStep("Відкрити вікно нового відділу: вузол {companyNode} → Mitarbeiter → контекстне меню → Neue Organisationseinheit hinzufügen")]
    public DepartmentDialog OpenNewDepartmentDialog(string companyNode)
    {
        OpenNodeContextMenu(ChildNode(SelectedOrAnyNode(companyNode), EmployeesNodeName));
        PopupMenuItem("Neue Organisationseinheit hinzufügen", "MnuEmployeesDepartmentNew").Click();
        return new DepartmentDialog(Window);
    }

    /// <summary>
    /// Новий співробітник відділу: ADAICA спершу відкриває пошук персони «Finden &amp; Erstellen», щоб не створити її двічі.
    /// Відділи різних фірм можуть називатися однаково, тому відділ шукаємо серед виділеного вузла та його предків.
    /// </summary>
    [AllureStep("Відкрити вікно нового співробітника: вузол {departmentNode} → контекстне меню → Mitarbeiter zur Organisationseinheit hinzufügen")]
    public ContactFindCreateWindow OpenNewDepartmentEmployeeWindow(string departmentNode)
    {
        OpenNodeContextMenu(SelectedOrAnyNode(departmentNode));
        PopupMenuItem("Mitarbeiter zur Organisationseinheit hinzufügen", "MnuDepartmentEmployeeNew").Click();
        return new ContactFindCreateWindow(Window);
    }

    /// <summary>
    /// Новий співробітник фірми без відділу: вузол «Mitarbeiter» фірми → «Mitarbeiter zur Firma hinzufügen». ADAICA
    /// відкриває той самий пошук «Finden &amp; Erstellen», а у «Neue Person/Firma» відділ — «andere Tätigkeiten» (його
    /// можна змінити). Фірму шукаємо серед виділеного вузла та його предків.
    /// </summary>
    [AllureStep("Відкрити вікно нового співробітника фірми: вузол {companyNode} → Mitarbeiter → контекстне меню → Mitarbeiter zur Firma hinzufügen")]
    public ContactFindCreateWindow OpenNewCompanyEmployeeWindow(string companyNode)
    {
        OpenNodeContextMenu(ChildNode(SelectedOrAnyNode(companyNode), EmployeesNodeName));
        PopupMenuItem("Mitarbeiter zur Firma hinzufügen", "MnuEmployeesEmployeeNew").Click();
        return new ContactFindCreateWindow(Window);
    }

    /// <summary>
    /// Вузол співробітника за шляхом «Фірма», «Mitarbeiter», «Відділ», «Співробітник» (див. <see cref="NodeAt"/>): після
    /// «Mitarbeiter kopieren» той самий співробітник є у двох відділах → «Mitarbeiter umziehen».
    /// </summary>
    [AllureStep("Відкрити вікно «Mitarbeiter umziehen»: вузол {employeePath} → контекстне меню → Mitarbeiter umziehen")]
    public EmployeeMoveWindow OpenEmployeeMoveWindow(params string[] employeePath)
    {
        OpenNodeContextMenu(NodeAt(employeePath));
        PopupMenuItem("Mitarbeiter umziehen", "MnuEmployeeMove").Click();
        return new EmployeeMoveWindow(Window);
    }

    [AllureStep("Відкрити вікно «Mitarbeiter bearbeiten»: вузол {employeePath} → контекстне меню → Mitarbeiter bearbeiten")]
    public EmployeeEditWindow OpenEmployeeEditWindow(params string[] employeePath)
    {
        OpenNodeContextMenu(NodeAt(employeePath));
        PopupMenuItem("Mitarbeiter bearbeiten", "MnuEmployeeEdit").Click();
        return new EmployeeEditWindow(Window);
    }

    /// <summary>
    /// Вузол співробітника → «Mitarbeiter aus der Organisationseinheit lösen». ADAICA питає «Verbindung zu … lösen?»
    /// (вікно «Beziehung lösen»); питання повертаємо тесту. У співробітника з «andere Tätigkeiten» цього пункту немає.
    /// </summary>
    [AllureStep("Відв'язати співробітника від відділу: вузол {employeePath} → контекстне меню → Mitarbeiter aus der Organisationseinheit lösen")]
    public MessageBoxDialog DetachEmployeeFromDepartment(params string[] employeePath)
    {
        OpenNodeContextMenu(NodeAt(employeePath));
        PopupMenuItem("Mitarbeiter aus der Organisationseinheit lösen", "MnuEmployeeDeleteFromDepartment").Click();
        return Wait.For(FindMessageBox, "питання «Verbindung zu … lösen?»");
    }

    /// <summary>Виділяє вузол за шляхом (див. <see cref="NodeAt"/>), наприклад відділ: «Фірма», «Mitarbeiter», «Відділ».</summary>
    [AllureStep("Вибрати вузол {path} у дереві навігатора")]
    public MainPage SelectNavigatorNodeAt(params string[] path)
    {
        UiInput.LeftClick(
            () =>
            {
                var node = NodeAt(path);
                return NodeText(ScrolledIntoView(node)) ?? node;
            },
            () => SelectedNodeWithAncestors() is { Count: > 0 } selected
                && selected.Select(node => WithoutRecordRange(NodeText(node)!.Name)).TakeLast(path.Length).SequenceEqual(path),
            $"виділення вузла «{string.Join(" → ", path)}»");
        return this;
    }

    /// <summary>
    /// Назви дочірніх вузлів (без вкладених глибше) вузла за шляхом <paramref name="path"/> (див. <see cref="NodeAt"/>).
    /// Наприклад, співробітники відділу: «Фірма», «Mitarbeiter», «Відділ».
    /// </summary>
    public IReadOnlyList<string> ChildNodeNames(params string[] path)
    {
        var node = NodeAt(path);

        // Вузла без дочірніх (наприклад, порожнього відділу) трикутник не має.
        if (node.FindFirstChild(cf => cf.ByAutomationId("ToggleButton")) is null)
            return [];

        Expand(node);
        var children = ChildNodes(node);
        var indent = children.Select(child => NodeText(child)!.BoundingRectangle.Left).DefaultIfEmpty().Min();
        return children
            .Where(child => NodeText(child)!.BoundingRectangle.Left == indent)
            .Select(child => WithoutRecordRange(NodeText(child)!.Name))
            .ToList();
    }

    /// <summary>
    /// Нова персона в навігаторі Kontakte: як і для фірми, ADAICA спершу відкриває пошук «Finden &amp; Erstellen».
    /// Якщо в рядку пошуку є й фірма, «Neu» створює персону разом із фірмою.
    /// </summary>
    [AllureStep("Відкрити вікно нової персони: вузол Gesamtliste → контекстне меню → Neue Person")]
    public ContactFindCreateWindow OpenNewPersonWindow()
    {
        OpenNodeContextMenu(ContactsRootNodeName);
        PopupMenuItem("Neue Person", "MnuPersonNew").Click();
        return new ContactFindCreateWindow(Window);
    }

    [AllureStep("Відкрити нове вікно підпроєкту: вузол {parentNode} → контекстне меню → Neu → Projekt")]
    public ProjectDialog OpenNewChildProjectDialog(string parentNode)
    {
        OpenNewChildMenu(parentNode);
        PopupMenuItem("Projekt", "mnuNewProject", NewChildMenuItemId).Click();
        return new ProjectDialog(Window);
    }

    [AllureStep("Відкрити нове вікно дочірнього Ordner: вузол {parentNode} → контекстне меню → Neu → Ordner")]
    public FolderDialog OpenNewChildFolderDialog(string parentNode)
    {
        OpenNewChildMenu(parentNode);
        PopupMenuItem("Ordner", "mnuNewProcess", NewChildMenuItemId).Click();
        return new FolderDialog(Window);
    }

    [AllureStep("Відкрити нове вікно дочірнього завдання: вузол {parentNode} → контекстне меню → Neu → Aufgabe → Neue Aufgabe")]
    public TaskDialog OpenNewChildTaskDialog(string parentNode)
    {
        OpenNewChildMenu(parentNode);
        PopupMenuItem("Aufgabe", "mnuNewTask", NewChildMenuItemId).Expand();
        PopupMenuItem("Neue Aufgabe", NewTaskMenuItemId, NewChildMenuItemId, "mnuNewTask").Click();
        return new TaskDialog(Window);
    }

    [AllureStep("Відкрити нове вікно дочірнього терміну: вузол {parentNode} → контекстне меню → Neu → Termin")]
    public AppointmentDialog OpenNewChildAppointmentDialog(string parentNode)
    {
        OpenNewChildMenu(parentNode);
        PopupMenuItem("Termin", "mnuNewAppoint", NewChildMenuItemId).Click();
        return new AppointmentDialog(Window);
    }

    [AllureStep("Відкрити нове вікно дочірнього інфозапису: вузол {parentNode} → контекстне меню → Neu → Notiz")]
    public NoteDialog OpenNewChildNoteDialog(string parentNode)
    {
        OpenNewChildMenu(parentNode);
        PopupMenuItem("Notiz", "mnuNewHistory", NewChildMenuItemId).Click();
        return new NoteDialog(Window);
    }

    /// <summary>
    /// Вікно нового документа у вбудованому редакторі: контекстне меню вузла <paramref name="parentNode"/> → «Neu» →
    /// «Dokument» → «Editor». На відміну від «Aus dem Dateisystem», тут є панель зв'язків.
    /// </summary>
    [AllureStep("Відкрити вікно нового документа: вузол {parentNode} → контекстне меню → Neu → Dokument → Editor")]
    public DocumentEditorDialog OpenNewChildDocumentEditor(string parentNode)
    {
        OpenNewChildMenu(parentNode);
        PopupMenuItem("Dokument", "mnuNewDocument", NewChildMenuItemId).Expand();
        PopupMenuItem("Editor", "mnuNewDocumentFromEditor", NewChildMenuItemId, "mnuNewDocument").Click();
        return new DocumentEditorDialog(Window);
    }

    /// <summary>
    /// Створює під вузлом <paramref name="parentNode"/> документ із файлу: контекстне меню → «Neu» → «Dokument» →
    /// «Aus dem Dateisystem» → вибір файлу в діалозі Windows. Вікна редагування ADAICA не показує: документ одразу
    /// зберігається й відкривається в області перегляду (<see cref="WaitForOpenedDocument"/>).
    /// </summary>
    [AllureStep("Створити документ з файлу: вузол {parentNode} → контекстне меню → Neu → Dokument → Aus dem Dateisystem")]
    public void NewChildDocumentFromFile(string parentNode, string filePath)
    {
        OpenNewChildMenu(parentNode);
        PopupMenuItem("Dokument", "mnuNewDocument", NewChildMenuItemId).Expand();
        PopupMenuItem("Aus dem Dateisystem", "mnuNewDocumentFromFile", NewChildMenuItemId, "mnuNewDocument").Click();
        FileOpenDialog.Open(Window, filePath);
    }

    /// <summary>
    /// Чекає документ <paramref name="name"/> в області перегляду. Якщо такий файл (за вмістом чи назвою) уже є
    /// в ADAICA, вона натомість показує вікно «Festlegung von Aktionen für den Import von doppelten Dateien» —
    /// тоді тест падає одразу з поясненням, а не за таймаутом.
    /// </summary>
    [AllureStep("Дочекатися відкриття документа {name}")]
    public DocumentForm WaitForOpenedDocument(string name)
    {
        Wait.Until(
            () => DuplicateImportWindow() is not null || ShownValue(DocumentForm.NameId) == name,
            $"документа «{name}» в області перегляду");
        if (DuplicateImportWindow() is not null)
        {
            throw new InvalidOperationException(
                $"ADAICA вважає файл документа «{name}» дублікатом і показує вікно «Festlegung von Aktionen für den Import von doppelten Dateien».");
        }

        return WaitForOpenedRecord(DocumentForm.NameId, name, view => new DocumentForm(view, Window));
    }

    // Вікно вибору дій для файлів, які вже є в ADAICA: його впізнаємо за списком файлів, що імпортуються.
    private AutomationElement? DuplicateImportWindow() =>
        Window.FindAllChildren(cf => cf.ByControlType(ControlType.Window))
            .FirstOrDefault(window => window.FindFirstDescendant(cf => cf.ByAutomationId("lvImportFiles")) is not null);

    /// <summary>
    /// Чекає, доки в області перегляду відкриється запис, у якому поле <paramref name="keyFieldId"/>
    /// має значення <paramref name="keyValue"/> (номер запису або інша унікальна ознака).
    /// Шукаємо лише в області перегляду: у відкритому діалозі запису є поля з тими самими AutomationId.
    /// </summary>
    private TForm WaitForOpenedRecord<TForm>(string keyFieldId, string keyValue, Func<AutomationElement, TForm> createForm)
        where TForm : class =>
        Wait.For(
            () => ShownValue(keyFieldId) == keyValue ? createForm(InformationView()!) : null,
            $"запису «{keyValue}» в області перегляду");

    private AutomationElement? InformationView() => Window.FindFirstDescendant(cf => cf.ByClassName("InformationView"));

    // Значення поля в області перегляду або null, якщо такого поля там немає.
    private string? ShownValue(string fieldId) =>
        InformationView()?.FindFirstDescendant(cf => cf.ByAutomationId(fieldId))?.AsTextBox().Text;

    // Контекстне меню вузла відкривається лише правим кліком: ні ShowContextMenu з UIA, ні Shift+F10 не діють.
    private void OpenNodeContextMenu(string nodeName) => OpenNodeContextMenu(NavigatorNode(nodeName));

    private void OpenNodeContextMenu(AutomationElement node) =>
        UiInput.RightClick(
            () => NodeText(ScrolledIntoView(node)) ?? node,
            () => Popups.FindDescendant(Window, cf => cf.ByControlType(ControlType.Menu)) is not null,
            $"контекстного меню вузла «{NodeText(node)?.Name.TrimEnd()}»");

    /// <summary>
    /// Прокручує дерево до вузла: у довгому дереві (фірма з відділами, співробітниками й записами) вузол може бути
    /// поза видимою областю, і клік туди не влучив би.
    /// </summary>
    private static AutomationElement ScrolledIntoView(AutomationElement node)
    {
        if (node.IsOffscreen && node.Patterns.ScrollItem.TryGetPattern(out var scrollItem))
            scrollItem.ScrollIntoView();
        return node;
    }

    /// <summary>
    /// Відкриває підменю «Neu» батьківського запису. Назви записів можуть повторюватися (у завдання й інфозапису
    /// немає номера), тому батька шукаємо серед виділеного вузла та його предків: після збереження ADAICA виділяє
    /// новий запис, тож і сам батько, і його дочірні записи лежать на цьому ланцюжку.
    /// </summary>
    private void OpenNewChildMenu(string parentNode)
    {
        OpenNodeContextMenu(SelectedOrAnyNode(parentNode));
        PopupMenuItem("Neu", NewChildMenuItemId).Expand();
    }

    // Вузол з такою назвою серед виділеного вузла та його предків, а якщо там його немає, — перший у дереві.
    private AutomationElement SelectedOrAnyNode(string nodeName) =>
        SelectedNodeWithAncestors().LastOrDefault(node => WithoutRecordRange(NodeText(node)!.Name) == nodeName)
            ?? NavigatorNode(nodeName);

    /// <summary>
    /// Дочірній вузол <paramref name="parent"/> з назвою <paramref name="nodeName"/>. Згорнутий батьківський вузол
    /// розгортає кліком по трикутнику. Дочірні вузли — ті, що йдуть за батьком і мають більший відступ.
    /// </summary>
    private AutomationElement ChildNode(AutomationElement parent, string nodeName)
    {
        Expand(parent);
        return Wait.For(
            () => ChildNodes(parent).FirstOrDefault(node => WithoutRecordRange(NodeText(node)!.Name) == nodeName),
            $"вузла «{nodeName}» під «{WithoutRecordRange(NodeText(parent)!.Name)}»");
    }

    /// <summary>
    /// Вузол за шляхом: перший вузол шукаємо серед виділеного та його предків, як у <see cref="OpenNewDepartmentDialog"/>,
    /// наступні — серед дочірніх попереднього. Згорнуті вузли шляху розгортає.
    /// </summary>
    private AutomationElement NodeAt(IReadOnlyList<string> path)
    {
        var node = SelectedOrAnyNode(path[0]);
        foreach (var name in path.Skip(1))
            node = ChildNode(node, name);
        return node;
    }

    // Згорнутий вузол розгортає кліком по трикутнику.
    private void Expand(AutomationElement parent)
    {
        var parentName = WithoutRecordRange(NodeText(parent)!.Name);
        var toggle = parent.FindFirstChild(cf => cf.ByAutomationId("ToggleButton"))
            ?? throw new InvalidOperationException($"Вузол «{parentName}» не має дочірніх вузлів.");
        if (toggle.Patterns.Toggle.Pattern.ToggleState.Value == ToggleState.On)
            return;

        UiInput.LeftClick(
            () => ScrolledIntoView(parent).FindFirstChild(cf => cf.ByAutomationId("ToggleButton")) ?? toggle,
            () => toggle.Patterns.Toggle.Pattern.ToggleState.Value == ToggleState.On,
            $"розгортання вузла «{parentName}»");
    }

    // Усі вкладені вузли: ті, що йдуть за батьком і мають більший відступ.
    private List<AutomationElement> ChildNodes(AutomationElement parent)
    {
        var nodes = NavigatorTree.FindAllChildren();
        var index = Array.IndexOf(nodes, parent);
        var indent = NodeText(parent)!.BoundingRectangle.Left;
        return nodes.Skip(index + 1)
            .TakeWhile(node => NodeText(node) is { } text && text.BoundingRectangle.Left > indent)
            .ToList();
    }

    /// <summary>
    /// Виділений вузол дерева та його предки, від кореня. У UI Automation дерево — плаский список, тому предка
    /// визначаємо за відступом: це найближчий вузол вище, текст якого починається лівіше.
    /// </summary>
    private List<AutomationElement> SelectedNodeWithAncestors()
    {
        var nodes = NavigatorTree.FindAllChildren();
        var selected = Array.FindLastIndex(nodes, node => node.Patterns.SelectionItem.Pattern.IsSelected.Value);

        var chain = new List<AutomationElement>();
        var indent = int.MaxValue;
        for (var i = selected; i >= 0; i--)
        {
            var text = NodeText(nodes[i]);
            if (text is null || text.BoundingRectangle.Left >= indent)
                continue;

            chain.Insert(0, nodes[i]);
            indent = text.BoundingRectangle.Left;
        }

        return chain;
    }

    // Дерево навігатора — ListBox «treeView». Той самий AutomationId має TreeView категорій у картці контакту.
    private AutomationElement NavigatorTree =>
        Wait.For(
            () => Window.FindFirstDescendant(cf => cf.ByAutomationId("treeView").And(cf.ByControlType(ControlType.List))),
            "дерева навігатора");

    /// <summary>
    /// Пункт контекстного меню. Якщо він у підменю, <paramref name="submenuIds"/> — підменю від зовнішнього до
    /// внутрішнього: поки пункту немає, тест знову розгортає згорнуті з них. WPF згортає підменю, коли справжній
    /// курсор, що опинився над меню, наводить на сусідній пункт, а клік тесту йде повідомленням, без курсора.
    /// </summary>
    private MenuItemControl PopupMenuItem(string name, string automationId, params string[] submenuIds) =>
        new(name, () => Wait.For(
            () => FindInPopups(automationId) ?? ExpandCollapsedSubmenus(submenuIds),
            $"пункту «{name}» у контекстному меню"));

    private AutomationElement? ExpandCollapsedSubmenus(IEnumerable<string> submenuIds)
    {
        foreach (var submenuId in submenuIds)
        {
            if (FindInPopups(submenuId) is not { } submenu || !submenu.Patterns.ExpandCollapse.TryGetPattern(out var expandCollapse))
                return null;
            if (expandCollapse.ExpandCollapseState.ValueOrDefault != ExpandCollapseState.Expanded)
                expandCollapse.Expand();
        }

        return null;
    }

    private static AutomationElement? NodeText(AutomationElement node) =>
        node.FindFirstChild(cf => cf.ByAutomationId("TextBlock"));

    // Вузли контактів ADAICA підписує з пробілами в кінці: «Keller Treuhand AG  ».
    private static string WithoutRecordRange(string nodeText) => RecordRange().Replace(nodeText.TrimEnd(), string.Empty);

    // Списки й контекстні меню ADAICA відкриваються у WPF Popup (див. Popups).
    private AutomationElement? FindInPopups(string automationId) =>
        Popups.FindDescendant(Window, cf => cf.ByAutomationId(automationId));

    // Заголовок вкладки — TextBlock із шаблону, тому його видно лише в raw-дереві.
    private static string TabName(AutomationElement tab) =>
        tab.FindFirstDescendantRaw(cf => cf.ByAutomationId("PART_Text"))?.Name ?? string.Empty;

    private void CloseDemoVersionWindowIfShown()
    {
        // Без ліцензії («Lizenziert für: Not Registered») після входу завжди відкривається вікно демоверсії.
        if (!Title.Contains("Not Registered", StringComparison.Ordinal))
            return;

        Element(DemoVersionWindowId).FindByAutomationId("btnExitWndDemoVersion").AsButton().Invoke();
        Wait.Until(
            () => Window.FindFirstChild(cf => cf.ByAutomationId(DemoVersionWindowId)) is null,
            "закриття вікна демоверсії");
    }

    private Match ParseTitle()
    {
        var match = ConnectionInTitle().Match(Title);
        return match.Success
            ? match
            : throw new InvalidOperationException($"У заголовку вікна немає бази й схеми: «{Title}».");
    }

    [GeneratedRegex(@"\[(?<database>[^:\]]+):(?<schema>[^\]]+)\]")]
    private static partial Regex ConnectionInTitle();

    [GeneratedRegex(@"^ADAICA: (?<user>.+?) \[")]
    private static partial Regex UserInTitle();

    // Діапазон записів, який ADAICA додає до назви вузла: «Projekte (1-25)».
    [GeneratedRegex(@" \(\d+-\d+\)$")]
    private static partial Regex RecordRange();
}
