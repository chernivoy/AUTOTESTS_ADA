using ADAICA_AUTOTEST.Pages;
using Allure.Net.Commons;

namespace ADAICA_AUTOTEST.Tests;

/// <summary>
/// Спільне для тестів планувальника завдань (<see cref="TaskCalendar"/>): вікна «Kalender» у Dashboard і календаря
/// вузла «Aufgaben» під фірмою.
/// </summary>
public abstract partial class AdaicaTestBase
{
    /// <summary>Смужка завдання в планувальнику: назва й день початку.</summary>
    protected static TaskListRow Row(DashboardTaskData task) => new(task.Name, task.Begin.Date);

    /// <summary>Клітинка на пів години нижче за смужку завдання <paramref name="task"/> того самого дня.</summary>
    protected static CalendarCell CellBelow(DashboardTaskData task) =>
        new(Row(task), task.Begin, task.Due!.Value, task.Due.Value.AddMinutes(30));

    protected static void ShouldBeFound(TaskCalendar calendar, TaskListRow task, bool expected, string search) =>
        AllureApi.Step($"Перевірити, що «{task}» {(expected ? "видно" : "не видно")} у планувальнику: {search}", () =>
            calendar.IsShown(task).ShouldBe(expected, $"«{task}» у планувальнику ({search})"));

    /// <summary>Період має <paramref name="days"/> днів, і день <paramref name="day"/> у ньому.</summary>
    protected static void ShouldSpan((DateTime From, DateTime To) period, int days, DateTime day) =>
        period.ShouldSatisfyAllConditions(
            () => (period.To - period.From).TotalDays.ShouldBe(days, $"Днів у періоді {period.From:dd.MM.yyyy}–{period.To:dd.MM.yyyy}"),
            () => day.ShouldBeInRange(period.From, period.To.AddTicks(-1), "День у періоді"));

    /// <summary>Початок півгодинної клітинки планувальника, у яку припадає <paramref name="value"/>.</summary>
    protected static DateTime HalfHourStart(DateTime value) =>
        value.Date.AddMinutes(Math.Floor((value - value.Date).TotalMinutes / 30) * 30);
}
