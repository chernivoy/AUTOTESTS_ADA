using ADAICA_AUTOTEST.Controls;
using ADAICA_AUTOTEST.Infrastructure;
using Allure.Net.Commons;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;

namespace ADAICA_AUTOTEST.Pages;

/// <summary>
/// «Vorlage umbenennen» (меню рядка «Aufgabenvorlagen» → «Umbenennen»): «Name der Vorlage» з поточною назвою, OK і
/// «Abbrechen». OK доступна, лише коли назва непорожня, відрізняється від поточної й не збігається з назвою іншого
/// шаблону (без урахування регістру). У вікна немає AutomationId, тож шукаємо його за заголовком.
/// </summary>
public sealed class TemplateRenameDialog
{
    public const string Title = "Vorlage umbenennen";

    private readonly Window _owner;

    /// <param name="owner">Вікно «Aufgabenvorlagen».</param>
    public TemplateRenameDialog(Window owner)
    {
        _owner = owner;
        Root = Wait.For(() => Find(owner), $"вікна «{Title}»");
        TemplateName = new TextBoxControl("Name der Vorlage", Locate.ById(Root, "txtTemplateName"));
        OkButton = new ButtonControl("OK", Locate.ById(Root, "btnOk"));
        CancelButton = new ButtonControl("Abbrechen", () => Wait.For(
            () => Root.FindFirstChild(cf => cf.ByControlType(ControlType.Button).And(cf.ByName("Abbrechen"))),
            $"кнопки «Abbrechen» у вікні «{Title}»"));
    }

    public TextBoxControl TemplateName { get; }

    public ButtonControl OkButton { get; }

    public ButtonControl CancelButton { get; }

    public bool IsOpen => Find(_owner) is not null;

    private Window Root { get; }

    /// <summary>Вписує нову назву й зберігає її кнопкою OK.</summary>
    public void Rename(string newName) => AllureApi.Step($"Перейменувати шаблон на «{newName}»", () =>
    {
        TemplateName.Fill(newName);
        Wait.Until(() => OkButton.IsEnabled, $"доступної кнопки OK у вікні «{Title}»");
        OkButton.Click();
        Wait.Until(() => !IsOpen, $"закриття вікна «{Title}»");
    });

    public void Cancel() => AllureApi.Step($"Закрити вікно «{Title}» кнопкою «Abbrechen»", () =>
    {
        CancelButton.Click();
        Wait.Until(() => !IsOpen, $"закриття вікна «{Title}»");
    });

    /// <summary>Для прибирання після тесту: закриває вікно, лише якщо воно ще відкрите.</summary>
    public void CancelIfOpen()
    {
        if (IsOpen)
            Cancel();
    }

    private static Window? Find(Window owner) =>
        owner.FindFirstChild(cf => cf.ByControlType(ControlType.Window).And(cf.ByName(Title)))?.AsWindow();
}
