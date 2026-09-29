using System.Globalization;
using ADAICA_AUTOTEST.Controls;
using ADAICA_AUTOTEST.Infrastructure;
using Allure.Net.Commons;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;

namespace ADAICA_AUTOTEST.Pages;

/// <summary>
/// Вікно «Aufgabenzyklus» (кнопка «Serientyp» у бланку завдання): як повторюється завдання. Це дочірнє вікно
/// вікна завдання без AutomationId, тому впізнаємо його за перемикачем «Täglich». За замовчуванням серія щоденна,
/// кожен день, без кінця, а наступне завдання стає актуальним за добу до свого початку.
/// </summary>
public sealed class TaskSeriesDialog
{
    private const string DailyId = "radioDayRepeatCycleSetting";

    private readonly Window _owner;

    /// <param name="owner">Вікно завдання.</param>
    public TaskSeriesDialog(Window owner)
    {
        _owner = owner;
        Root = Wait.For(
            () => ChildWindows(owner).FirstOrDefault(window => window.FindFirstDescendant(cf => cf.ByAutomationId(DailyId)) is not null),
            "вікна «Aufgabenzyklus»");

        Daily = new RadioButtonControl("Täglich", Locate.ById(Root, DailyId));
        EndsAfter = new RadioButtonControl("Endet nach", Locate.ById(Root, "radioCountCyclingCycleSetting"));
        // Поле кількості завдань не має AutomationId і не вкладене в перемикач «Endet nach»: це перше поле вводу за ним.
        Count = new TextBoxControl("Endet nach … Aufgaben", () => FirstEditAfter(Root, Root.FindByAutomationId("radioCountCyclingCycleSetting")));
        OkButton = new ButtonControl("OK", () => Wait.For(
            () => Root.FindFirstDescendant(cf => cf.ByControlType(ControlType.Button).And(cf.ByName("OK"))),
            "кнопки OK у вікні «Aufgabenzyklus»"));
    }

    public RadioButtonControl Daily { get; }

    public RadioButtonControl EndsAfter { get; }

    public TextBoxControl Count { get; }

    public ButtonControl OkButton { get; }

    private Window Root { get; }

    /// <summary>Щоденна серія, яка закінчується після <paramref name="count"/> завдань.</summary>
    public void SetDaily(int count) => AllureApi.Step($"Задати щоденну серію з {count} завдань", () =>
    {
        Daily.Select();
        EndsAfter.Select();
        Count.Fill(count.ToString(CultureInfo.InvariantCulture));
        OkButton.Click();
        Wait.Until(() => !ChildWindows(_owner).Contains(Root), "закриття вікна «Aufgabenzyklus»");
    });

    // Перше поле вводу після елемента в порядку елементів вікна (зліва направо, згори вниз).
    private static AutomationElement FirstEditAfter(AutomationElement root, AutomationElement element) =>
        root.FindAllDescendants()
            .SkipWhile(descendant => !descendant.Equals(element))
            .FirstOrDefault(descendant => descendant.ControlType == ControlType.Edit)
            ?? throw new InvalidOperationException("Поруч із перемикачем «Endet nach» немає поля кількості завдань.");

    private static IEnumerable<Window> ChildWindows(Window owner) =>
        owner.FindAllChildren(cf => cf.ByControlType(ControlType.Window)).Select(window => window.AsWindow());
}
