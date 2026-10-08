using ADAICA_AUTOTEST.Controls;
using ADAICA_AUTOTEST.Infrastructure;
using Allure.Net.Commons;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;

namespace ADAICA_AUTOTEST.Pages;

/// <summary>
/// Вікно «Einstellungen» (Konfiguration → Einstellungen, `wndConfiguration`): вкладки «Allgemeine Einstellungen»,
/// «Adressen», «Kontakte», «Dokumentation», «Objekte», у кожній — дерево розділів і опції вибраного розділу.
/// Поки що тест працює лише з опціями пошти (<see cref="MailOptions"/>). Опції зберігаються в базі: системні — для
/// всіх користувачів, «Anwender» — для поточного; діють після перезапуску ADAICA.
/// </summary>
public sealed partial class OptionsWindow
{
    public const string WindowId = "wndConfiguration";
    private const string RestartMessage = "Änderungen werden erst nach dem Neustart der Anwendung wirksam";

    private static readonly IReadOnlyDictionary<MailRecognitionRule, string> RuleIds = new Dictionary<MailRecognitionRule, string>
    {
        [MailRecognitionRule.Simple] = "radioEmailAddrRecognModeAuto",
        [MailRecognitionRule.SenderAndOneRecipient] = "radioEmailAddrRecognModeFromAndOneTo",
        [MailRecognitionRule.SenderAndAllRecipients] = "radioEmailAddrRecognModeFromAndAllTo",
        [MailRecognitionRule.SenderAllRecipientsAndCopies] = "radioEmailAddrRecognModeFromAndAllToAllCC",
    };

    private static readonly IReadOnlyDictionary<MailMultipleContactsMode, string> MultipleContactsIds = new Dictionary<MailMultipleContactsMode, string>
    {
        [MailMultipleContactsMode.Priority] = "radioEmailAddressLinkPriority",
        [MailMultipleContactsMode.Recipients] = "radioEmailAddressLinkRecipients",
        [MailMultipleContactsMode.All] = "radioEmailAddressLinkAll",
    };

    private readonly Window _owner;

    public OptionsWindow(Window owner)
    {
        _owner = owner;
        Root = Wait.For(() => owner.FindFirstChild(cf => cf.ByAutomationId(WindowId))?.AsWindow(), "вікна «Einstellungen»");
        OkButton = new ButtonControl("OK", Locate.ById(Root, "btnOkWndConfiguration"));
        CancelButton = new ButtonControl("Abbrechen", Locate.ById(Root, "btnCancelWndConfiguration"));
        AddressSuggestionsUser = new CheckBoxControl("Vorschlagsliste E-Mailadressen: Anwender", Locate.ById(Root, "chkIsShowAllEmailsCOManager"));
        AddressSuggestionsSystem = new CheckBoxControl("Vorschlagsliste E-Mailadressen: Systemweit", Locate.ById(Root, "chkDefaultIsShowAllEmailsCOManager"));
        SalutationUser = new CheckBoxControl("Anreden von E-Mail-Empfängern hinzufügen: Anwender", Locate.ById(Root, "chkUseSalutationToAddrCOManager"));
        SalutationSystem = new CheckBoxControl("Anreden von E-Mail-Empfängern hinzufügen: Systemweit", Locate.ById(Root, "chkDefaultUseSalutationToAddrCOManager"));
    }

    public ButtonControl OkButton { get; }

    public ButtonControl CancelButton { get; }

    public CheckBoxControl AddressSuggestionsUser { get; }

    public CheckBoxControl AddressSuggestionsSystem { get; }

    public CheckBoxControl SalutationUser { get; }

    public CheckBoxControl SalutationSystem { get; }

    // IsAvailable для закритого вікна WPF лишається true, тому перевіряємо, чи вікно ще серед дочірніх.
    public bool IsOpen => _owner.FindAllChildren(cf => cf.ByControlType(ControlType.Window)).Contains(Root);

    private Window Root { get; }

    /// <summary>Вкладка «Kontakte» → вузол «E-Mail» дерева розділів. Вузол реагує лише на клік.</summary>
    public OptionsWindow OpenMailOptions() => AllureApi.Step("Відкрити «Kontakte» → «E-Mail» у вікні «Einstellungen»", () =>
    {
        new TabItemControl("Kontakte", Locate.ById(Root, "tiPersonCompanyOptionsWndConfiguration")).Select();
        UiInput.LeftClick(
            () => Wait.For(
                () => Root.FindFirstDescendant(cf => cf.ByAutomationId("treeViewGroup"))?
                    .FindFirstDescendant(cf => cf.ByAutomationId("TextBlock").And(cf.ByName("E-Mail"))),
                "вузла «E-Mail» у дереві розділів"),
            () => Root.FindFirstDescendant(cf => cf.ByAutomationId("grpEmailAddrRecognModeCOManager")) is not null,
            "опцій пошти у вікні «Einstellungen»");
        return this;
    });

    /// <summary>Поточні опції пошти (розділ «E-Mail» має бути відкритий, <see cref="OpenMailOptions"/>).</summary>
    public MailOptions ReadMailOptions() => AllureApi.Step("Прочитати опції пошти", () =>
    {
        var options = new MailOptions(
            RuleIds.Single(pair => Radio(pair.Value).IsSelected).Key,
            MultipleContactsIds.Single(pair => Radio(pair.Value).IsSelected).Key,
            AddressSuggestionsUser.IsChecked,
            AddressSuggestionsSystem.IsChecked,
            SalutationUser.IsChecked,
            SalutationSystem.IsChecked);
        AllureApi.Step($"Опції пошти: {options}");
        return options;
    });

    /// <summary>
    /// Ставить опції пошти й закриває вікно: OK і підтвердження, що зміни діють після перезапуску. Якщо нічого
    /// не змінилося, OK недоступна — тоді «Abbrechen».
    /// </summary>
    public void SetMailOptions(MailOptions options) => AllureApi.Step($"Встановити опції пошти: {options}", () =>
    {
        Radio(RuleIds[options.Rule]).Select();
        Radio(MultipleContactsIds[options.MultipleContacts]).Select();
        AddressSuggestionsUser.SetChecked(options.AddressSuggestionsUser);
        AddressSuggestionsSystem.SetChecked(options.AddressSuggestionsSystem);
        SalutationUser.SetChecked(options.SalutationUser);
        SalutationSystem.SetChecked(options.SalutationSystem);
        ReadMailOptions().ShouldBe(options, "Опції пошти у вікні «Einstellungen»");

        if (!OkButton.IsEnabled)
        {
            Close();
            return;
        }

        OkButton.Click();
        var message = Wait.For(() => MessageBoxDialog.Find(_owner), $"повідомлення «{RestartMessage}»");
        message.Message.ShouldHaveText(RestartMessage);
        message.Button("OK").Click();
        Wait.Until(() => !IsOpen, "закриття вікна «Einstellungen»");
    });

    public void Close() => AllureApi.Step("Закрити вікно «Einstellungen» без змін", () =>
    {
        CancelButton.Click();
        Wait.Until(() => !IsOpen, "закриття вікна «Einstellungen»");
    });

    /// <summary>Для прибирання після тесту: закриває вікно, лише якщо воно ще відкрите.</summary>
    public void CloseIfOpen()
    {
        if (IsOpen)
            Close();
    }

    private RadioButtonControl Radio(string automationId) => new(automationId, Locate.ById(Root, automationId));
}
