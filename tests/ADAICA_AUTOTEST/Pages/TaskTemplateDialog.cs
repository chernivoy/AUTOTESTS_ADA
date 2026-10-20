using ADAICA_AUTOTEST.Controls;
using FlaUI.Core.AutomationElements;

namespace ADAICA_AUTOTEST.Pages;

/// <summary>
/// Вікно шаблону завдання: «Neue Aufgabenvorlage» («Als Aufgabenvorlage speichern» у вікні нового завдання, «Neu» чи
/// «Duplizieren» у «Aufgabenvorlagen») і «Aufgabenvorlage bearbeiten: назва» («Bearbeiten» і «Anzeigen» у
/// «Aufgabenvorlagen»). Це той самий бланк завдання (<see cref="TaskForm"/>) з обов'язковим полем «Name der Vorlage»
/// над ним, але без «Weitere Eigenschaften», «Aufgabenzyklus» і панелі зв'язків; «Beginnt am», «Erinnerung» і
/// «Serientyp» недоступні. Вікно — дочірнє вікна, з якого його відкрито.
/// </summary>
public sealed class TaskTemplateDialog : RecordDialog
{
    public const string NewTitle = "Neue Aufgabenvorlage";

    private const string MainTabId = "tiMainRecordTask";

    /// <param name="owner">Вікно нового завдання або «Aufgabenvorlagen».</param>
    public TaskTemplateDialog(Window owner)
        : base(owner, MainTabId, "шаблон завдання")
    {
        Form = new TaskForm(Root, Root);
        TemplateName = new TextBoxControl("Name der Vorlage", Locate.ById(Root, "txtTemplateNameRow"));
    }

    public TaskForm Form { get; }

    /// <summary>
    /// «Name der Vorlage»: так шаблон підписано в «Aufgabenvorlagen» і в меню «Neu» → «Aufgabe». Поки поле порожнє або
    /// назва вже є в іншого шаблону (без урахування регістру), OK недоступна.
    /// </summary>
    public TextBoxControl TemplateName { get; }

    /// <summary>
    /// Заголовок вікна наявного шаблону. «Anzeigen» відкриває вікно з тим самим заголовком, що й «Bearbeiten», але лише
    /// для читання.
    /// </summary>
    public static string EditTitle(string templateName) => $"Aufgabenvorlage bearbeiten: {templateName}";
}
