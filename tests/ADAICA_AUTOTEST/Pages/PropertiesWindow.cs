using ADAICA_AUTOTEST.Controls;
using ADAICA_AUTOTEST.Infrastructure;
using Allure.Net.Commons;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;

namespace ADAICA_AUTOTEST.Pages;

/// <summary>
/// Вікно «Eigenschaften (Назва)» (`wndProperties`) з контекстного меню запису: на вкладці «Stammdaten» — внутрішній ID
/// запису («261008105135PROJ604332») і Klassentyp («PROJECT»). Номера запису (PRO…) Papierkorb не показує, тож свій
/// запис тест упізнає в ньому за цим ID. Поля ID і Klassentyp не мають AutomationId: це два поля редагування після
/// підписів, у порядку «Klassentyp», «ID».
/// </summary>
public sealed class PropertiesWindow
{
    private const string WindowId = "wndProperties";

    private readonly Window _owner;

    public PropertiesWindow(Window owner)
    {
        _owner = owner;
        Root = Wait.For(() => Find(owner), "вікна «Eigenschaften»");
        CancelButton = new ButtonControl("Abbrechen", Locate.ById(Root, "btnCancelWndProperties"));
    }

    public ButtonControl CancelButton { get; }

    /// <summary>Klassentyp запису: PROJECT, OPERATION (Ordner), TASK, APPOINT, HISTORY (інфозапис), BLOB (документ).</summary>
    public string ClassType => Field(0);

    /// <summary>Внутрішній ID запису.</summary>
    public string Id => Field(1);

    private Window Root { get; }

    public bool IsOpen => Find(_owner) is not null;

    public void Close() => AllureApi.Step("Закрити вікно «Eigenschaften»", () =>
    {
        CancelButton.Click();
        Wait.Until(() => !IsOpen, "закриття вікна «Eigenschaften»");
    });

    // Поле «Name» (txtControlInstances) — теж Edit, тому беремо поля без AutomationId.
    private string Field(int index) =>
        Wait.For(
            () => Root.FindByAutomationId("ctrIconProperty")
                .FindAllChildren(cf => cf.ByControlType(ControlType.Edit))
                .Where(edit => string.IsNullOrEmpty(edit.AutomationId))
                .ElementAtOrDefault(index)?.Patterns.Value.Pattern.Value.Value is { Length: > 0 } value
                    ? value
                    : null,
            "поля у вікні «Eigenschaften»");

    private static Window? Find(Window owner) =>
        owner.FindFirstChild(cf => cf.ByAutomationId(WindowId))?.AsWindow();
}
