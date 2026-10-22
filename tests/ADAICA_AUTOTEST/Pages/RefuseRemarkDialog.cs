using ADAICA_AUTOTEST.Controls;
using ADAICA_AUTOTEST.Infrastructure;
using Allure.Net.Commons;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;

namespace ADAICA_AUTOTEST.Pages;

/// <summary>
/// Вікно причини відмови (`wndRefuseRemark`), яке ADAICA показує посеред збереження кроку workflow, коли Prüfer
/// відхилив документ. Поле причини обов'язкове й заповнене поточною «Bemerkung» кроку; OK доступна, лише коли текст є,
/// а закрити вікно інакше, ніж OK, не можна. Текст замінює «Bemerkung» кроку. У вікна немає AutomationId, тож шукаємо
/// його серед дочірніх вікон власника за полем причини.
/// </summary>
public sealed class RefuseRemarkDialog
{
    private const string RemarkId = "txtEditorWndRefuseRemark";

    private readonly Window _owner;

    public RefuseRemarkDialog(Window owner)
    {
        _owner = owner;
        Root = Wait.For(() => Find(owner), "вікна причини відмови", TimeSpan.FromSeconds(30));
        Caption = new LabelControl("Підпис", Locate.ById(Root, "tbCaptionWndRefuseRemark"));
        Remark = new TextBoxControl("Причина відмови", Locate.ById(Root, RemarkId));
        OkButton = new ButtonControl("OK", Locate.ById(Root, "btnOkWndRefuseRemark"));
    }

    public string Title => Root.Name;

    public LabelControl Caption { get; }

    public TextBoxControl Remark { get; }

    public ButtonControl OkButton { get; }

    public bool IsOpen => Find(_owner) is not null;

    private Window Root { get; }

    /// <summary>Вписує причину й натискає OK; збереження кроку продовжується.</summary>
    public void Confirm(string remark) => AllureApi.Step($"Вказати причину відмови: {remark}", () =>
    {
        Remark.Fill(remark);
        Wait.Until(() => OkButton.IsEnabled, "доступної кнопки OK у вікні причини відмови");
        OkButton.Click();
        Wait.Until(() => !IsOpen, "закриття вікна причини відмови");
    });

    private static Window? Find(Window owner) =>
        owner.FindAllChildren(cf => cf.ByControlType(ControlType.Window))
            .FirstOrDefault(window => window.FindFirstDescendant(cf => cf.ByAutomationId(RemarkId)) is not null)?
            .AsWindow();
}
