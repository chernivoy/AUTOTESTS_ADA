using ADAICA_AUTOTEST.Controls;
using ADAICA_AUTOTEST.Infrastructure;
using Allure.Net.Commons;
using FlaUI.Core.AutomationElements;
using static ADAICA_AUTOTEST.Pages.TaskListFilter;

namespace ADAICA_AUTOTEST.Pages;

/// <summary>
/// Вид «Kalender» вузла «Aufgaben» під записом (наприклад, під фірмою в навігаторі Kontakte): кнопка «Kalender» над
/// списком (<see cref="RecordList.OpenTaskCalendar"/>) показує в області перегляду замість списку планувальник
/// (<see cref="TaskCalendar"/>) із завданнями цього запису, а «Listenansicht» повертає список. Кнопок стану менше, ніж у
/// Dashboard (<see cref="StateFilters"/>). Під планувальником — бланк вибраного завдання з OK і «Übernehmen». Вікна, що
/// відкривають меню й кнопки планувальника, належать головному вікну. Вид ADAICA не запам'ятовує: після повторного
/// вибору вузла знову список.
/// </summary>
public sealed class RecordTaskCalendar : TaskCalendar
{
    private const string ViewClass = "GantFilterBarControl";

    /// <summary>Кнопки стану на панелі фільтрів: без «Ungelesen», «Akzeptieren», «Angenommen» і «Abgelehnt».</summary>
    public static readonly IReadOnlyList<TaskListFilter> StateFilters = [Done, Undone, Open, Closed, Overdue, Actual, Planned];

    /// <param name="owner">Головне вікно ADAICA.</param>
    public RecordTaskCalendar(Window owner)
        : base(owner, Wait.For(() => FindView(owner), "календаря вузла «Aufgaben»", LoadTimeout), "у календарі вузла «Aufgaben»", StateFilters)
    {
    }

    /// <summary>Чи показує область перегляду планувальник, а не список.</summary>
    public bool IsOpen => FindView(Owner) is not null;

    /// <summary>Бланк вибраного завдання в панелі під планувальником.</summary>
    public override TaskForm Details =>
        new(Wait.For(() => Tabs(Owner)?.FindFirstChild(cf => cf.ByClassName("MainForm")), "бланка вибраного завдання під планувальником"), Owner);

    protected override Window DialogOwner => Owner;

    protected override IEnumerable<AutomationElement> PopupWindows => Popups.Of(Owner);

    /// <summary>«Listenansicht»: область перегляду знову показує список завдань запису.</summary>
    public RecordList ShowList() => AllureApi.Step("Повернутися до списку кнопкою «Listenansicht»", () =>
    {
        new ButtonControl("Listenansicht", Locate.ById(Root, "btnReturnFromGantToList")).Click();
        Wait.Until(() => !IsOpen, "списку завдань замість календаря");
        return new RecordList(Owner);
    });

    /// <summary>Згортає панель з бланком вибраного завдання (кнопка «_» над нею), якщо її розгорнуто.</summary>
    protected override void CollapseDetails()
    {
        AutomationElement? Minimize() => Tabs(Owner)?.FindFirstChild(cf => cf.ByAutomationId("PART_MinimizeButton"));
        if (Minimize() is not { IsEnabled: true })
            return;

        AllureApi.Step("Згорнути панель з бланком завдання під планувальником", () =>
        {
            // Кнопка-перемикач реагує лише на клік.
            UiInput.LeftClick(() => Minimize()!, () => Minimize() is not { IsEnabled: true }, "згортання панелі з бланком завдання");
            WaitUntilSettled("згортання панелі з бланком завдання");
        });
    }

    // Вкладки області перегляду: планувальник, а коли вибрано завдання, — ще бланк і кнопки його панелі.
    private static AutomationElement? Tabs(Window owner) =>
        owner.FindFirstDescendant(cf => cf.ByClassName("InformationView"))?.FindFirstChild(cf => cf.ByAutomationId("tbBase"));

    private static AutomationElement? FindView(Window owner) => Tabs(owner)?.FindFirstChild(cf => cf.ByClassName(ViewClass));
}
