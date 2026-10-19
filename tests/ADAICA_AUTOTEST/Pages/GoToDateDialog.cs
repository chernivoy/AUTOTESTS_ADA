using System.Globalization;
using ADAICA_AUTOTEST.Controls;
using ADAICA_AUTOTEST.Infrastructure;
using Allure.Net.Commons;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;

namespace ADAICA_AUTOTEST.Pages;

/// <summary>
/// Вікно DevExpress «Gehe zu Datum» (меню клітинки планувальника → «Gehe zu Datum...»): дата («Datum») і вид, у якому
/// її показати («Zeige In»). Обидва поля прив'язані до EditValue редакторів, а ValuePattern DevExpress у них різний:
/// DateEdit записує рядок в EditValue (прив'язка перетворює його на дату незалежно від культури, тож пишемо ISO), а
/// ComboBoxEdit — лише в показаний текст, і вибраний вид не змінюється. Тому вид вибираємо пунктом розгорнутого списку
/// (SelectionItem пункту ставить SelectedItem). Кнопка OK не має InvokePattern, тож реагує лише на клік.
/// </summary>
public sealed class GoToDateDialog
{
    public const string Title = "Gehe zu Datum";

    private readonly Window _owner;

    /// <param name="owner">Вікно «Kalender».</param>
    public GoToDateDialog(Window owner)
    {
        _owner = owner;
        Root = Wait.For(() => Find(owner), $"вікна «{Title}»");
    }

    /// <summary>Показана дата, наприклад «16.10.2026».</summary>
    public string DateText => TextInput.Read(DateEditor.FindByAutomationId("PART_Editor"));

    /// <summary>Показаний вид, наприклад «Wochenansicht».</summary>
    public string ViewText => TextInput.Read(ViewEditor);

    public bool IsOpen => Find(_owner) is not null;

    private Window Root { get; }

    private AutomationElement DateEditor => Root.FindByAutomationId("editorDate");

    private AutomationElement ViewEditor => Root.FindByAutomationId("editorView");

    // EditValue редактора дати — DateTime, ValuePattern показує його як «30.11.2026 00:00:00».
    private DateTime? DateValue => AdaicaFormat.ParseDate(TextInput.Read(DateEditor).Split(' ')[0]);

    /// <summary>Вписує дату й (якщо задано) вибирає вид, потім натискає OK.</summary>
    public void GoTo(DateTime date, CalendarView? view = null) =>
        AllureApi.Step($"Перейти на {AdaicaFormat.FormatDate(date)}{(view is { } chosen ? $" у виді «{chosen.Label()}»" : string.Empty)}", () =>
        {
            TextInput.Write(DateEditor, "Datum", date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
            Wait.Until(() => DateValue == date.Date, $"дати {AdaicaFormat.FormatDate(date)} у вікні «{Title}»");
            if (view is { } target)
                SelectView(target);

            UiInput.LeftClick(
                () => Root.FindByAutomationId("PART_OKDialogButton"),
                () => !IsOpen,
                $"закриття вікна «{Title}» кнопкою OK");
        });

    private void SelectView(CalendarView view)
    {
        var label = view.Label();
        var editor = ViewEditor.Patterns.ExpandCollapse.Pattern;
        try
        {
            editor.Expand();
            Wait.For(
                    () => ViewEditor.FindFirstChild(cf => cf.ByControlType(ControlType.ListItem).And(cf.ByName(label))),
                    $"пункту «{label}» у списку «Zeige In»")
                .Patterns.SelectionItem.Pattern.Select();
        }
        finally
        {
            if (editor.ExpandCollapseState.Value == ExpandCollapseState.Expanded)
                editor.Collapse();
        }

        Wait.Until(() => ViewText == label, $"виду «{label}» у вікні «{Title}»");
    }

    private static Window? Find(Window owner) =>
        owner.FindFirstChild(cf => cf.ByControlType(ControlType.Window).And(cf.ByName(Title)))?.AsWindow();
}
