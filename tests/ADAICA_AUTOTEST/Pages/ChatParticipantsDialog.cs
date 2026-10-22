using System.Drawing;
using ADAICA_AUTOTEST.Controls;
using ADAICA_AUTOTEST.Infrastructure;
using Allure.Net.Commons;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;

namespace ADAICA_AUTOTEST.Pages;

/// <summary>
/// «Kontaktzuordnungs-Dialog» (<c>wndChooseContacts</c>) для учасників чату й Prüfer кроку workflow
/// (<see cref="WorkflowForm.AddReviewer"/>): ліворуч дерево «Benutzer» / «Gruppen»
/// з прапорцями, «&gt;&gt;» переносить позначених у «Ausgewählt:», «Löschen» прибирає виділеного, «Alles löschen» — усіх;
/// OK доступна, коли «Ausgewählt:» не порожній. У дереві є й ті, хто вже в чаті: повторно їх ADAICA мовчки не додає.
/// </summary>
/// <remarks>
/// Те саме вікно, що й <see cref="ChooseContactsDialog"/> панелі зв'язків, але без пошуку контактів. Прапорця вузла
/// UI Automation не показує (TreeItem без дочірніх елементів), тож клікаємо в його точку: зліва від значка, на
/// середині висоти рядка. Відступ залежить від масштабу екрана, тому рахуємо його від висоти рядка
/// (рядок 20 DIP, прапорець — на 47 DIP від лівого краю рядка).
/// </remarks>
public sealed class ChatParticipantsDialog
{
    private const string WindowId = "wndChooseContacts";

    private readonly Window _owner;

    public ChatParticipantsDialog(Window owner)
    {
        _owner = owner;
        Root = Wait.For(() => owner.FindFirstChild(cf => cf.ByAutomationId(WindowId))?.AsWindow(), "вікна «Kontaktzuordnungs-Dialog»");
        AddButton = new ButtonControl(">>", Locate.ById(Root, "btnAddInSelectedNodeWndChooseContacts"));
        DeleteButton = new ButtonControl("Löschen", Locate.ById(Root, "btnDeleteWndChooseContacts"));
        DeleteAllButton = new ButtonControl("Alles löschen", Locate.ById(Root, "btnDeleteAllWndChooseContacts"));
        OkButton = new ButtonControl("OK", Locate.ById(Root, "btnOkWndChooseContacts"));
        CancelButton = new ButtonControl("Abbrechen", Locate.ById(Root, "btnCloseWndChooseContacts"));
    }

    public ButtonControl AddButton { get; }

    public ButtonControl DeleteButton { get; }

    public ButtonControl DeleteAllButton { get; }

    public ButtonControl OkButton { get; }

    public ButtonControl CancelButton { get; }

    public bool IsOpen => _owner.FindAllChildren(cf => cf.ByControlType(ControlType.Window)).Contains(Root);

    /// <summary>Вузли дерева ліворуч зверху вниз: «Benutzer», користувачі, «Gruppen».</summary>
    public IReadOnlyList<string> Users =>
        Root.FindByAutomationId("treeViewControl").FindAllDescendants(cf => cf.ByControlType(ControlType.TreeItem))
            .Select(item => item.Name)
            .ToList();

    /// <summary>Список «Ausgewählt:».</summary>
    public IReadOnlyList<string> Selected =>
        Root.FindByAutomationId("treeView").FindAllDescendants(cf => cf.ByAutomationId("TextBlock"))
            .Select(text => text.Name)
            .ToList();

    private Window Root { get; }

    /// <summary>
    /// Позначає прапорець користувача <paramref name="user"/> і переносить його в «Ausgewählt:» кнопкою «&gt;&gt;».
    /// Чи позначено прапорець, видно лише після «&gt;&gt;», тож перевірка натискає її сама (зайве натискання нічого
    /// не дублює). «&gt;&gt;» скидає прапорці.
    /// </summary>
    public void Choose(string user) => AllureApi.Step($"Вибрати користувача {user} і перенести «>>»", () =>
    {
        // Поки вікно завантажує дерево, вузлів у ньому немає.
        var item = Wait.For(() => UserItem(user), $"користувача «{user}» у вікні «Kontaktzuordnungs-Dialog»");
        UiInput.LeftClick(
            () => UserItem(user) ?? item,
            () =>
            {
                Root.FindByAutomationId("btnAddInSelectedNodeWndChooseContacts").Patterns.Invoke.Pattern.Invoke();
                return Selected.Any(name => IsUser(name, user));
            },
            $"користувача «{user}» у списку «Ausgewählt:»",
            offset: CheckBoxPoint(item));
    });

    /// <summary>OK: вибрані користувачі стають учасниками, вікно закривається.</summary>
    public void Confirm() => AllureApi.Step("Підтвердити вибір учасників (OK)", () =>
    {
        OkButton.Click();
        Wait.Until(() => !IsOpen, "закриття вікна «Kontaktzuordnungs-Dialog»");
    });

    /// <summary>Для прибирання після тесту: закриває вікно, лише якщо воно ще відкрите.</summary>
    public void CancelIfOpen()
    {
        if (!IsOpen)
            return;

        CancelButton.Click();
        Wait.Until(() => !IsOpen, "закриття вікна «Kontaktzuordnungs-Dialog»");
    }

    private AutomationElement? UserItem(string user) =>
        Root.FindByAutomationId("treeViewControl").FindAllDescendants(cf => cf.ByControlType(ControlType.TreeItem))
            .FirstOrDefault(item => IsUser(item.Name, user));

    // Для Prüfer workflow ADAICA дописує до користувача з контактом фірми саму фірму: «van de Goldun, Anna Dr. # Hotel Seeblick …».
    private static bool IsUser(string node, string user) =>
        node == user || node.StartsWith(user + " # ", StringComparison.Ordinal);

    private static Point CheckBoxPoint(AutomationElement item)
    {
        var height = item.BoundingRectangle.Height;
        return new Point(height * 47 / 20, height / 2);
    }
}
