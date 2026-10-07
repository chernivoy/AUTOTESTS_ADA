using ADAICA_AUTOTEST.Controls;
using ADAICA_AUTOTEST.Infrastructure;
using Allure.Net.Commons;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;

namespace ADAICA_AUTOTEST.Pages;

/// <summary>
/// Лист в області перегляду (вкладка «Übersicht»): заголовок «Von», «An», «Versanddatum», «Betreff» і текст.
/// Поля заголовка — текстові поля без AutomationId, кожне йде за своїм підписом, тож шукаємо їх за підписом.
/// Текст листа показує вбудований переглядач, якого UI Automation не бачить.
/// </summary>
public sealed class MailView(AutomationElement view)
{
    public const string HeaderId = "InfoHeaderControl";

    public string From => Field("Von");

    public string To => Field("An");

    public string Subject => Field("Betreff");

    /// <summary>«05.10.2026 16:09», як ADAICA показує дату надсилання.</summary>
    public string SentAt => Field("Versanddatum");

    /// <summary>Значення поля за підписом або null, якщо такого поля в заголовку немає.</summary>
    public static string? Field(AutomationElement view, string label)
    {
        var parts = view.FindFirstDescendant(cf => cf.ByAutomationId(HeaderId))?.FindAllChildren();
        if (parts is null)
            return null;

        var index = Array.FindIndex(parts, part => part.ControlType == ControlType.Text && part.Name == label);
        return index < 0
            ? null
            : parts.Skip(index + 1).FirstOrDefault(part => part.ControlType == ControlType.Edit)?.Patterns.Value.Pattern.Value.Value;
    }

    /// <summary>
    /// Перевіряє заголовок листа: відправник — обліковий запис Outlook <paramref name="accountAddress"/>, отримувач —
    /// створена персона з адресою листа, тема, дата надсилання — між <paramref name="sentFrom"/> і <paramref name="sentTo"/>.
    /// </summary>
    public void ShouldHaveValues(MailData expected, string accountAddress, DateTime sentFrom, DateTime sentTo) =>
        AllureApi.Step("Перевірити заголовок листа", () =>
        {
            var fromMinute = new DateTime(sentFrom.Ticks - sentFrom.Ticks % TimeSpan.TicksPerMinute, sentFrom.Kind);
            var sentAt = SentAt.Split(' ');
            this.ShouldSatisfyAllConditions(
                () => From.ShouldEndWith($"<{accountAddress}>", Case.Sensitive, "Поле «Von»"),
                () => To.ShouldBe(expected.RecipientLine, "Поле «An»"),
                () => Subject.ShouldBe(expected.Subject, "Поле «Betreff»"),
                () => (AdaicaFormat.ParseDate(sentAt[0])!.Value + AdaicaFormat.ParseTime(sentAt.ElementAtOrDefault(1) ?? string.Empty)!.Value)
                    .ShouldBeInRange(fromMinute, sentTo, "Поле «Versanddatum»"));
        });

    /// <summary>
    /// Вкладка «Beziehungen» → «Zugriffspfade»: записи, контакти й об'єкти, з якими зв'язано лист. Після читання
    /// повертає вкладку «Übersicht», бо ADAICA відкриває наступний лист на вкладці, яку відкривали останньою.
    /// </summary>
    public RecordAccessPaths ReadAccessPaths() => AllureApi.Step("Прочитати «Beziehungen» → «Zugriffspfade» листа", () =>
    {
        SelectTab(() => view.FindFirstDescendant(cf => cf.ByControlType(ControlType.TabItem).And(cf.ByName("Beziehungen"))), "Beziehungen");
        SelectTab(() => view.FindFirstDescendant(cf => cf.ByAutomationId("tiAscendingHierarchy")), "Zugriffspfade");
        try
        {
            var tree = Wait.For(() => view.FindFirstDescendant(cf => cf.ByAutomationId("RecordAncestorTreeListView")), "дерева «Zugriffspfade»");
            // Дані ADAICA завантажує після відкриття вкладки: лист завжди лежить хоча б в одному записі.
            Wait.Until(() => tree.FindFirstChild(cf => cf.ByControlType(ControlType.TreeItem)) is not null, "записів у «Zugriffspfade»");

            var paths = new RecordAccessPaths(
                tree.FindAllChildren(cf => cf.ByControlType(ControlType.TreeItem)).Select(Chain).ToList(),
                ListNames("ContactsListView"),
                ListNames("UtObjectsListView"));
            AllureApi.AddAttachment("Zugriffspfade", "text/plain", System.Text.Encoding.UTF8.GetBytes(paths.ToString()), ".txt");
            return paths;
        }
        finally
        {
            SelectTab(() => view.FindFirstDescendant(cf => cf.ByControlType(ControlType.TabItem).And(cf.ByName("Übersicht"))), "Übersicht");
        }
    });

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

    private IReadOnlyList<string> ListNames(string listId) =>
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

    private string Field(string label) =>
        Field(view, label) ?? throw new InvalidOperationException($"У заголовку листа немає поля «{label}».");
}
