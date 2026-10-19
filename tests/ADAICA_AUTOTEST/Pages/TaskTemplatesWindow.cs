using ADAICA_AUTOTEST.Controls;
using ADAICA_AUTOTEST.Infrastructure;
using Allure.Net.Commons;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;

namespace ADAICA_AUTOTEST.Pages;

/// <summary>
/// Вікно «Aufgabenvorlagen» («Vorlage erstellen/bearbeiten» у меню «Neu» планувальника або кнопки «+» вікна
/// «Kalender»): шаблони завдань, кнопки «Neu», «Ansehen», «Bearbeiten», «Löschen» (картинки без підпису), швидкий пошук,
/// «Verwenden» і «Schließen». У вікна немає AutomationId, тож шукаємо його за заголовком серед дочірніх вікон
/// головного вікна ADAICA.
/// </summary>
public sealed class TaskTemplatesWindow
{
    public const string Title = "Aufgabenvorlagen";

    private readonly Window _owner;

    /// <param name="owner">Головне вікно ADAICA.</param>
    public TaskTemplatesWindow(Window owner)
    {
        _owner = owner;
        Root = Wait.For(() => Find(owner), $"вікна «{Title}»");
        NewButton = new ButtonControl("Neu", Locate.ById(Root, "btnNew"));
        ViewButton = new ButtonControl("Ansehen", Locate.ById(Root, "btnView"));
        EditButton = new ButtonControl("Bearbeiten", Locate.ById(Root, "btnEdit"));
        DeleteButton = new ButtonControl("Löschen", Locate.ById(Root, "btnDelete"));
        UseButton = new ButtonControl("Verwenden", Locate.ById(Root, "btnSelect"));
        CloseButton = new ButtonControl("Schließen", Locate.ById(Root, "btnClose"));
        QuickSearch = new TextBoxControl("Suchbegriff eingeben...", Locate.ById(Root, "searchFilterControl", "txtQuickSearch"));
    }

    public ButtonControl NewButton { get; }

    public ButtonControl ViewButton { get; }

    public ButtonControl EditButton { get; }

    public ButtonControl DeleteButton { get; }

    public ButtonControl UseButton { get; }

    public ButtonControl CloseButton { get; }

    public TextBoxControl QuickSearch { get; }

    /// <summary>Підписи колонок списку шаблонів.</summary>
    public IReadOnlyList<string> ColumnNames =>
        Root.FindByAutomationId("lstTemplates")
            .FindAllDescendants(cf => cf.ByControlType(ControlType.HeaderItem))
            .Select(header => header.FindFirstChild(cf => cf.ByAutomationId("PART_Content"))?.Name)
            .OfType<string>()
            .ToList();

    public bool IsOpen => Find(_owner) is not null;

    private Window Root { get; }

    public void Close() => AllureApi.Step($"Закрити вікно «{Title}» кнопкою «Schließen»", () =>
    {
        CloseButton.Click();
        Wait.Until(() => !IsOpen, $"закриття вікна «{Title}»");
    });

    /// <summary>Для прибирання після тесту: закриває вікно, лише якщо воно ще відкрите.</summary>
    public void CloseIfOpen()
    {
        if (IsOpen)
            Close();
    }

    private static Window? Find(Window owner) =>
        owner.FindFirstChild(cf => cf.ByControlType(ControlType.Window).And(cf.ByName(Title)))?.AsWindow();
}
