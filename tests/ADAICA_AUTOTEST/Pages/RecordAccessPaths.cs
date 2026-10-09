using ADAICA_AUTOTEST.Infrastructure;
using Allure.Net.Commons;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;

namespace ADAICA_AUTOTEST.Pages;

/// <summary>
/// «Beziehungen» → «Zugriffspfade» запису: де в ADAICA лежить запис. Для листа тут видно все, що задано у вікні
/// збереження: записи Dokumentation, під якими лист (кожен — ланцюжком від запису до кореня, наприклад
/// «Назва завдання» ← «ROOT TASK»; вузол «E-Mails» — «ROOT EMAIL»), контакти й об'єкти. Для нового запису — так
/// само записи, під якими його створено чи до яких зв'язано у вікні нового запису, і контакти (батько-контакт тут, а
/// не серед записів).
/// </summary>
/// <param name="Records">Ланцюжки записів: назва запису, далі його предки до кореня.</param>
/// <param name="Contacts">Контакти: «Прізвище, Ім'я».</param>
/// <param name="Objects">Об'єкти: назви.</param>
public sealed record RecordAccessPaths(
    IReadOnlyList<IReadOnlyList<string>> Records,
    IReadOnlyList<string> Contacts,
    IReadOnlyList<string> Objects)
{
    /// <summary>
    /// Читає «Beziehungen» → «Zugriffspfade» запису в області перегляду <paramref name="view"/>. Після читання
    /// повертає вкладку «Übersicht», бо ADAICA відкриває наступний запис на вкладці, яку відкривали останньою.
    /// </summary>
    /// <param name="what">Чий шлях («листа», «запису») — для назви кроку.</param>
    public static RecordAccessPaths Read(AutomationElement view, string what) =>
        AllureApi.Step($"Прочитати «Beziehungen» → «Zugriffspfade» {what}", () =>
        {
            SelectTab(() => view.FindFirstDescendant(cf => cf.ByControlType(ControlType.TabItem).And(cf.ByName("Beziehungen"))), "Beziehungen");
            SelectTab(() => view.FindFirstDescendant(cf => cf.ByAutomationId("tiAscendingHierarchy")), "Zugriffspfade");
            try
            {
                var tree = Wait.For(() => view.FindFirstDescendant(cf => cf.ByAutomationId("RecordAncestorTreeListView")), "дерева «Zugriffspfade»");
                // Дані ADAICA завантажує після відкриття вкладки: лист чи запис завжди лежить хоча б в одному записі.
                Wait.Until(() => tree.FindFirstChild(cf => cf.ByControlType(ControlType.TreeItem)) is not null, "записів у «Zugriffspfade»");

                var paths = new RecordAccessPaths(
                    tree.FindAllChildren(cf => cf.ByControlType(ControlType.TreeItem)).Select(Chain).ToList(),
                    ListNames(view, "ContactsListView"),
                    ListNames(view, "UtObjectsListView"));
                AllureApi.AddAttachment("Zugriffspfade", "text/plain", System.Text.Encoding.UTF8.GetBytes(paths.ToString()), ".txt");
                return paths;
            }
            finally
            {
                SelectTab(() => view.FindFirstDescendant(cf => cf.ByControlType(ControlType.TabItem).And(cf.ByName("Übersicht"))), "Übersicht");
            }
        });

    public override string ToString() =>
        $"Dokumentation: {string.Join("; ", Records.Select(chain => string.Join(" ← ", chain)))}"
        + $"{Environment.NewLine}Kontakte: {string.Join("; ", Contacts)}"
        + $"{Environment.NewLine}Objekte: {string.Join("; ", Objects)}";

    // Запис і його предки: у дереві кожен предок — дочірній вузол попереднього.
    private static IReadOnlyList<string> Chain(AutomationElement item)
    {
        var chain = new List<string>();
        for (var current = item; current is not null; current = current.FindFirstChild(cf => cf.ByControlType(ControlType.TreeItem)))
        {
            chain.Add(current.FindFirstChild(cf => cf.ByControlType(ControlType.Text))?.Name ?? string.Empty);
            if (current.Patterns.ExpandCollapse.TryGetPattern(out var expand)
                && expand.ExpandCollapseState.Value == ExpandCollapseState.Collapsed)
                expand.Expand();
        }

        return chain;
    }

    private static IReadOnlyList<string> ListNames(AutomationElement view, string listId) =>
        view.FindFirstDescendant(cf => cf.ByAutomationId(listId))?
            .FindAllChildren(cf => cf.ByControlType(ControlType.DataItem))
            .Select(row => row.FindFirstDescendant(cf => cf.ByAutomationId("TextElement"))?.Name ?? string.Empty)
            .ToList() ?? [];

    private static void SelectTab(Func<AutomationElement?> find, string name)
    {
        var tab = Wait.For(find, $"вкладки «{name}»");
        if (!tab.Patterns.SelectionItem.Pattern.IsSelected.Value)
            tab.Patterns.SelectionItem.Pattern.Select();
        Wait.Until(() => find()?.Patterns.SelectionItem.Pattern.IsSelected.Value == true, $"вибору вкладки «{name}»");
    }
}
