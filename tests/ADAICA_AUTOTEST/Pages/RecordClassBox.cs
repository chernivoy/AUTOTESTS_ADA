using ADAICA_AUTOTEST.Controls;
using ADAICA_AUTOTEST.Infrastructure;
using Allure.Net.Commons;
using FlaUI.Core.AutomationElements;

namespace ADAICA_AUTOTEST.Pages;

/// <summary>
/// «Klasse» / «Unterklasse» на бланку запису (<c>ctrVUBClassSelect</c>): кнопка «Klasse» відкриває
/// <see cref="ClassSelectWindow"/>, а поля праворуч показують назви вибраних класу й підкласу (без коротких назв).
/// Однаковий на бланках проєкту, Ordner, завдання, терміну й інфозапису — і у вікні нового запису, і в огляді, — але
/// AutomationId у кожному бланку свій: <c>ctrVUBClassSelect</c> у проєкту й Ordner, <c>ctrVUBClassSelectRecordTask</c>,
/// <c>…RecordAppoint</c>, <c>…RecordHistory</c>.
/// </summary>
public sealed class RecordClassBox(AutomationElement root, string boxId)
{

    public ButtonControl Button { get; } = new("Klasse", Locate.ById(root, boxId, "btnVUBClassSelectBoxSelectClass"));

    public TextBoxControl ClassName { get; } = new("Klasse", Locate.ById(root, boxId, "txtVUBClassName"));

    public TextBoxControl SubClassName { get; } = new("Unterklasse", Locate.ById(root, boxId, "txtVUBSubClassName"));

    /// <summary>
    /// Вибирає клас і підклас. Якщо у вікні вибору їх немає (у базі ще нема класів чи вони не ввімкнені для цього
    /// типу запису), спершу створює їх через «Konfiguration der Klassenliste».
    /// </summary>
    public void Select(RecordClass recordClass) => AllureApi.Step($"Вибрати клас {recordClass}", () =>
    {
        Button.Click();
        var window = new ClassSelectWindow(MainWindow(root));
        try
        {
            if (!window.Contains(recordClass))
                window.CreateClass(recordClass);
            window.Choose(recordClass);
        }
        finally
        {
            window.CancelIfOpen();
        }

        Wait.Until(() => ClassName.Text == recordClass.Class && SubClassName.Text == recordClass.SubClass,
            $"класу {recordClass} на бланку");
    });

    /// <summary>Перевіряє назви класу й підкласу; якщо <paramref name="expected"/> — null, нічого не перевіряє.</summary>
    public void ShouldHave(RecordClass? expected)
    {
        if (expected is null)
            return;

        AllureApi.Step($"Перевірити клас: {expected}", () => this.ShouldSatisfyAllConditions(
            () => ClassName.ShouldHaveText(expected.Class),
            () => SubClassName.ShouldHaveText(expected.SubClass)));
    }

    // Вікно вибору класу — дочірнє вікна запису, а конфігурація може бути глибше, тож шукаємо від головного вікна.
    private static Window MainWindow(AutomationElement element)
    {
        var walker = element.Automation.TreeWalkerFactory.GetControlViewWalker();
        var desktop = element.Automation.GetDesktop();
        var top = element;
        for (var parent = walker.GetParent(top); parent is not null && !parent.Equals(desktop); parent = walker.GetParent(top))
            top = parent;
        return top.AsWindow();
    }
}
