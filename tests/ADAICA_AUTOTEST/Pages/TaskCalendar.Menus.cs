using System.Drawing;
using ADAICA_AUTOTEST.Controls;
using ADAICA_AUTOTEST.Infrastructure;
using Allure.Net.Commons;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using VirtualKeyShort = FlaUI.Core.WindowsAPI.VirtualKeyShort;

namespace ADAICA_AUTOTEST.Pages;

/// <summary>Контекстні меню планувальника, подвійний клік по клітинці й кнопка «+» на панелі фільтрів.</summary>
public abstract partial class TaskCalendar
{
    // Пункт, за яким меню клітинки відрізняється від меню смужки, і навпаки.
    private const string CellMenuMarker = "Ansichtwechsel";
    private const string TaskMenuMarker = "Eigenschaften";

    /// <summary>
    /// Меню порожньої клітинки: правий клік унизу планувальника, де немає смужок. Клітинок UI Automation не показує,
    /// тож точку вибираємо серед кількох кандидатів так, щоб вона не влучала в жодну смужку.
    /// </summary>
    public CalendarMenu OpenCellMenu() =>
        AllureApi.Step("Відкрити меню порожньої клітинки планувальника", () => OpenCellMenuAt(EmptyCellPoint(), "порожньої клітинки"));

    /// <summary>Меню смужки завдання <paramref name="task"/> (правий клік). Смужка при цьому стає вибраною.</summary>
    public CalendarMenu OpenTaskMenu(TaskListRow task) => AllureApi.Step($"Відкрити меню смужки «{task}»", () =>
    {
        RemindersPopup.Collapse(Owner);
        UiInput.RightClick(
            () => BarElement(task),
            () => CalendarMenu.Contains(() => PopupWindows, TaskMenuMarker),
            $"меню смужки «{task}»");
        return new CalendarMenu(Root, () => PopupWindows);
    });

    /// <summary>Перемикає вид планувальника: меню клітинки → «Ansichtwechsel» → вид.</summary>
    public void SwitchView(CalendarView view) =>
        AllureApi.Step($"Перемкнути вид на «{view.Label()}»: меню клітинки → Ansichtwechsel", () =>
        {
            var menu = OpenCellMenu();
            menu.Submenu("Ansichtwechsel");
            menu.Click(view.Label());
            Wait.Until(() => View == view, $"виду «{view.Label()}» у планувальнику");
            WaitUntilSettled($"виду «{view.Label()}»");
        });

    /// <summary>Переходить на сьогодні: меню клітинки → «Heute».</summary>
    public void GoToToday() => AllureApi.Step("Перейти на сьогодні: меню клітинки → Heute", () =>
    {
        OpenCellMenu().Click("Heute");
        Wait.Until(() => Contains(Period, DateTime.Today), "періоду із сьогоднішнім днем");
        WaitUntilSettled("переходу на сьогодні");
    });

    /// <summary>Відкриває вікно «Gehe zu Datum»: меню клітинки → «Gehe zu Datum...».</summary>
    public GoToDateDialog OpenGoToDate() => AllureApi.Step("Відкрити «Gehe zu Datum»: меню клітинки → Gehe zu Datum...", () =>
    {
        OpenCellMenu().Click("Gehe zu Datum...");
        return new GoToDateDialog(DialogOwner);
    });

    /// <summary>
    /// Нове завдання з клітинки <paramref name="cell"/>: меню клітинки → «Neu» → «Neue Aufgabe». ADAICA бере «Beginnt am»
    /// і «Fällig am» з меж клітинки.
    /// </summary>
    public TaskDialog NewTaskFromCell(CalendarCell cell) =>
        AllureApi.Step($"Відкрити нове вікно завдання з клітинки {AdaicaFormat.FormatDateTime(cell.Start)}: меню клітинки → Neu → Neue Aufgabe", () =>
        {
            var menu = OpenCellMenuAt(CellPoint(cell), $"клітинки {AdaicaFormat.FormatDateTime(cell.Start)}");
            menu.Submenu("Neu");
            menu.Click("Neue Aufgabe");
            return new TaskDialog(DialogOwner);
        });

    /// <summary>
    /// Нове завдання подвійним кліком по порожній клітинці <paramref name="cell"/>: те саме вікно, що й з меню клітинки,
    /// з датами клітинки. Подвійний клік ADAICA приймає лише активною, тож <see cref="UiInput.DoubleClick"/> спершу
    /// її активує.
    /// </summary>
    public TaskDialog NewTaskByDoubleClick(CalendarCell cell) =>
        AllureApi.Step($"Відкрити нове вікно завдання подвійним кліком по клітинці {AdaicaFormat.FormatDateTime(cell.Start)}", () =>
        {
            var point = CellPoint(cell);
            RemindersPopup.Collapse(Owner);
            UiInput.DoubleClick(
                () => Scheduler,
                () => TaskDialog.IsShownIn(DialogOwner),
                $"вікна нового завдання після подвійного кліку по клітинці {AdaicaFormat.FormatDateTime(cell.Start)}",
                offset: point);
            return new TaskDialog(DialogOwner);
        });

    /// <summary>
    /// Нове завдання з клітинки дня в «Monatsansicht»: меню клітинки дня завдання <paramref name="task"/> під його
    /// смужкою → «Neu» → «Neue Aufgabe». Клітинка тут — цілий день, і смужки займають лише її верх.
    /// </summary>
    public TaskDialog NewTaskInDayOf(TaskListRow task) =>
        AllureApi.Step($"Відкрити нове вікно завдання з клітинки дня {AdaicaFormat.FormatDate(task.Begin)}: меню клітинки → Neu → Neue Aufgabe", () =>
        {
            var menu = OpenCellMenuAt(DayCellPoint(task), $"клітинки дня {AdaicaFormat.FormatDate(task.Begin)}");
            menu.Submenu("Neu");
            menu.Click("Neue Aufgabe");
            return new TaskDialog(DialogOwner);
        });

    /// <summary>
    /// Нове завдання кнопкою «+» на панелі фільтрів → «Neue Aufgabe». Дати ADAICA бере з виділеної клітинки, а без неї — з
    /// півгодини, у яку показано планувальник.
    /// </summary>
    public TaskDialog NewTaskFromAddButton() => AllureApi.Step("Відкрити нове вікно завдання: «+» → Neue Aufgabe", () =>
    {
        AddButtonMenuItem("mnuNewTaskDefault", "Neue Aufgabe").Click();
        return new TaskDialog(DialogOwner);
    });

    /// <summary>Показані пункти меню кнопки «+» (WPF ContextMenu): меню відкривається й закривається клавішею Esc.</summary>
    public IReadOnlyList<string> AddButtonMenuItems() => AllureApi.Step("Прочитати пункти меню кнопки «+»", () =>
    {
        var items = OpenAddButtonMenu()
            .FindAllDescendants(cf => cf.ByControlType(ControlType.MenuItem))
            .Where(item => !item.IsOffscreen)
            .Select(item => item.Name)
            .ToList();
        UiInput.PressKey(() => Root, VirtualKeyShort.ESCAPE, () => AddButtonPopupMenu is null, "закриття меню кнопки «+»");
        return (IReadOnlyList<string>)items;
    });

    /// <summary>«Aufgabenvorlagen» з меню клітинки: «Neu» → «Vorlage erstellen/bearbeiten».</summary>
    public TaskTemplatesWindow OpenTemplatesFromCell() =>
        AllureApi.Step("Відкрити «Aufgabenvorlagen»: меню клітинки → Neu → Vorlage erstellen/bearbeiten", () =>
        {
            var menu = OpenCellMenu();
            menu.Submenu("Neu");
            menu.Click("Vorlage erstellen/bearbeiten");
            return new TaskTemplatesWindow(Owner);
        });

    /// <summary>«Aufgabenvorlagen» з кнопки «+» → «Vorlage erstellen/bearbeiten».</summary>
    public TaskTemplatesWindow OpenTemplatesFromAddButton() =>
        AllureApi.Step("Відкрити «Aufgabenvorlagen»: «+» → Vorlage erstellen/bearbeiten", () =>
        {
            AddButtonMenuItem("mnuMoreTaskTemplates", "Vorlage erstellen/bearbeiten").Click();
            return new TaskTemplatesWindow(Owner);
        });

    /// <summary>Меню смужки → «Öffnen» → «Im Dokumentation-Navigator öffnen»: завдання відкривається в навігаторі Dokumentation.</summary>
    public void OpenInDokumentation(TaskListRow task) =>
        AllureApi.Step($"Відкрити «{task}» у навігаторі Dokumentation: меню смужки → Öffnen → Im Dokumentation-Navigator öffnen", () =>
        {
            var menu = OpenTaskMenu(task);
            menu.Submenu("Öffnen");
            menu.Click("Im Dokumentation-Navigator öffnen");
        });

    /// <summary>Меню смужки → «Öffnen» → «In einem neuen Fenster öffnen»: окреме вікно завдання з номером <paramref name="number"/>.</summary>
    public TaskEditWindow OpenInNewWindow(TaskListRow task, string number) =>
        AllureApi.Step($"Відкрити «{task}» в окремому вікні: меню смужки → Öffnen → In einem neuen Fenster öffnen", () =>
        {
            var menu = OpenTaskMenu(task);
            menu.Submenu("Öffnen");
            menu.Click("In einem neuen Fenster öffnen");
            return new TaskEditWindow(DialogOwner, number);
        });

    /// <summary>Чи чекає вставки завдання, скопійоване «Duplizieren» (червоне вікно праворуч угорі головного вікна).</summary>
    public bool HasPendingPaste => FindPendingPaste() is not null;

    /// <summary>
    /// Меню смужки → «Duplizieren»: ADAICA нічого не зберігає, а запам'ятовує завдання для «Einfügen als Duplikat» у меню
    /// клітинки й показує праворуч угорі головного вікна червоне вікно «Aus dem Zwischenspeicher einfügen» з назвою
    /// завдання, «Abbrechen» і «Mehrfachverwendung».
    /// </summary>
    public void Duplicate(TaskListRow task) => AllureApi.Step($"Скопіювати «{task}»: меню смужки → Duplizieren", () =>
    {
        OpenTaskMenu(task).Click("Duplizieren");
        Wait.Until(() => HasPendingPaste, "вікна «Aus dem Zwischenspeicher einfügen»");
    });

    /// <summary>
    /// Меню клітинки <paramref name="cell"/> → «Einfügen als Duplikat»: ADAICA питає у вікні «Festlegungen für das
    /// Duplikat», що перенести в копію, а після OK зберігає копію в цій клітинці.
    /// </summary>
    public DuplicateOptionsDialog PasteDuplicate(CalendarCell cell) =>
        AllureApi.Step($"Вставити копію в клітинку {AdaicaFormat.FormatDateTime(cell.Start)}: меню клітинки → Einfügen als Duplikat", () =>
        {
            OpenCellMenuAt(CellPoint(cell), $"клітинки {AdaicaFormat.FormatDateTime(cell.Start)}").Click("Einfügen als Duplikat");
            return new DuplicateOptionsDialog(Owner);
        });

    /// <summary>Для прибирання після тесту: скасовує вставку скопійованого завдання, якщо вона ще чекає.</summary>
    public void CancelPendingPaste()
    {
        if (FindPendingPaste() is not { } window)
            return;

        AllureApi.Step("Скасувати вставку скопійованого завдання: «Aus dem Zwischenspeicher einfügen» → Abbrechen", () =>
        {
            window.FindByAutomationId("btnCancelWndDocTemplatePasteShowProcess").Patterns.Invoke.Pattern.Invoke();
            Wait.Until(() => !HasPendingPaste, "закриття вікна «Aus dem Zwischenspeicher einfügen»");
        });
    }

    // AutomationId у вікна немає, а Name — його x:Name.
    private AutomationElement? FindPendingPaste() =>
        Owner.FindFirstChild(cf => cf.ByControlType(ControlType.Window).And(cf.ByName("wndCopyPasteShowProcess"))) is { IsOffscreen: false } window
            ? window
            : null;

    /// <summary>Меню смужки → «Löschen». ADAICA питає, чи видалити; питання повертаємо тесту.</summary>
    public MessageBoxDialog Delete(TaskListRow task) => AllureApi.Step($"Видалити «{task}»: меню смужки → Löschen", () =>
    {
        OpenTaskMenu(task).Click("Löschen");
        return Wait.For(() => MessageBoxDialog.Find(DialogOwner), "питання «Löschen»");
    });

    /// <summary>Меню смужки → «Eigenschaften»: вікно властивостей (дочірнє вікно головного вікна).</summary>
    public PropertiesWindow OpenProperties(TaskListRow task) =>
        AllureApi.Step($"Відкрити властивості «{task}»: меню смужки → Eigenschaften", () =>
        {
            OpenTaskMenu(task).Click("Eigenschaften");
            return new PropertiesWindow(Owner);
        });

    /// <summary>Перезавантажує завдання з бази кнопкою оновлення праворуч від «+».</summary>
    public void Refresh() => AllureApi.Step($"Оновити планувальник {Place} кнопкою оновлення", () =>
    {
        Root.FindByAutomationId("btnRefreshList").Patterns.Invoke.Pattern.Invoke();
        WaitUntilSettled($"оновлення планувальника {Place}");
    });

    /// <summary>Згортає панель з бланком вибраного завдання під планувальником, якщо її розгорнуто: з нею сітка часу коротка.</summary>
    protected abstract void CollapseDetails();

    private AutomationElement? AddButtonPopupMenu =>
        PopupWindows.Select(popup => popup.FindFirstDescendant(cf => cf.ByAutomationId("contextMenuAddNewTask"))).FirstOrDefault(menu => menu is not null);

    // Кнопка «+» реагує лише на клік і відкриває WPF ContextMenu з AutomationId пунктів.
    private AutomationElement OpenAddButtonMenu()
    {
        UiInput.LeftClickOpeningMenu(
            () => Root.FindByAutomationId("btnAddNewTask"),
            () => AddButtonPopupMenu is not null,
            "меню кнопки «+»");
        return Wait.For(() => AddButtonPopupMenu, "меню кнопки «+»");
    }

    private MenuItemControl AddButtonMenuItem(string automationId, string name)
    {
        var menu = OpenAddButtonMenu();
        return new MenuItemControl(name, () => Wait.For(
            () => menu.FindFirstDescendant(cf => cf.ByAutomationId(automationId)),
            $"пункту «{name}» у меню кнопки «+»"));
    }

    // Вікно нагадувань про терміни праворуч унизу може закрити частину планувальника, тож перед кліком його згортаємо.
    private CalendarMenu OpenCellMenuAt(Point offset, string place)
    {
        RemindersPopup.Collapse(Owner);
        UiInput.RightClick(
            () => Scheduler,
            () => CalendarMenu.Contains(() => PopupWindows, CellMenuMarker),
            $"меню {place}",
            offset: offset);
        return new CalendarMenu(Root, () => PopupWindows);
    }

    private Point EmptyCellPoint()
    {
        var scheduler = Scheduler.BoundingRectangle;
        var bars = BarElements().Select(bar => bar.BoundingRectangle).Where(bar => bar.Width > 0).ToList();
        foreach (var fromBottom in new[] { 40, 100, 160 })
        {
            foreach (var fraction in new[] { 0.5, 0.35, 0.65, 0.2, 0.8 })
            {
                var point = new Point(scheduler.X + (int)(scheduler.Width * fraction), scheduler.Bottom - fromBottom);
                if (!bars.Any(bar => bar.Contains(point)))
                    return Offset(point, scheduler);
            }
        }

        throw new InvalidOperationException("Унизу планувальника немає клітинки без смужок.");
    }

    /// <summary>
    /// Точка клітинки <paramref name="cell"/> відносно планувальника. У сітці часу смужка займає рівно свій час, тож
    /// висоту хвилини беремо зі смужки завдання <see cref="CalendarCell.Task"/>, а стовпець — її стовпець. Перед цим
    /// згортаємо панель із бланком вибраного завдання: з нею сітка часу коротка.
    /// </summary>
    private Point CellPoint(CalendarCell cell)
    {
        CollapseDetails();
        var bar = BarElement(cell.Task).BoundingRectangle;
        var minute = bar.Height / (cell.TaskEnd - cell.TaskBegin).TotalMinutes;
        // На 5 хвилин глибше за початок клітинки — щоб не влучити в межу між клітинками.
        var point = new Point(bar.Left + bar.Width / 2, bar.Top + (int)Math.Round(((cell.Start - cell.TaskBegin).TotalMinutes + 5) * minute));
        return InsideFreeCell(point, $"Клітинка {AdaicaFormat.FormatDateTime(cell.Start)}");
    }

    /// <summary>
    /// Точка в клітинці дня завдання <paramref name="task"/> у «Monatsansicht»: на дві висоти смужки нижче за найнижчу
    /// смужку цього дня.
    /// </summary>
    private Point DayCellPoint(TaskListRow task)
    {
        CollapseDetails();
        var bar = BarElement(task).BoundingRectangle;
        var lowest = BarElements()
            .Select(element => element.BoundingRectangle)
            .Where(other => other.Width > 0 && other.Left < bar.Right && other.Right > bar.Left)
            .Max(other => other.Bottom);
        var point = new Point(bar.Left + bar.Width / 2, lowest + bar.Height * 2);
        return InsideFreeCell(point, $"Клітинка дня {AdaicaFormat.FormatDate(task.Begin)}");
    }

    private Point InsideFreeCell(Point point, string cell)
    {
        var scheduler = Scheduler.BoundingRectangle;
        if (!scheduler.Contains(point))
            throw new InvalidOperationException($"{cell} поза видимою частиною планувальника.");
        if (BarElements().Any(other => other.BoundingRectangle.Contains(point)))
            throw new InvalidOperationException($"{cell}: точку займає смужка іншого завдання.");
        return Offset(point, scheduler);
    }

    private static bool Contains((DateTime From, DateTime To) period, DateTime day) => day >= period.From && day < period.To;
}

/// <summary>
/// Клітинка сітки часу, що починається о <paramref name="Start"/>, у стовпці дня завдання <paramref name="Task"/>, яке
/// триває від <paramref name="TaskBegin"/> до <paramref name="TaskEnd"/> (у межах однієї доби). Клітинок UI Automation
/// не показує, тож координату рахуємо від смужки цього завдання.
/// </summary>
public sealed record CalendarCell(TaskListRow Task, DateTime TaskBegin, DateTime TaskEnd, DateTime Start);
