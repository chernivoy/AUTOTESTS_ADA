using System.Globalization;
using ADAICA_AUTOTEST.Controls;
using ADAICA_AUTOTEST.Infrastructure;
using Allure.Net.Commons;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;

namespace ADAICA_AUTOTEST.Pages;

/// <summary>
/// Асистент «Mail-Einstellungen» (Benutzereinstellungen → Maileinstellungen): поштовий обліковий запис користувача
/// ADAICA. Сторінки асистента змінюються в тому самому вікні: «Aktion wählen» → «Einstellungen der Synchronisation
/// mit Microsoft Outlook-Konten» → «Einstellungen der Ordnersynchronisation» → «Abschluss der Konfiguration».
/// </summary>
/// <remarks>
/// У вікна й більшості його елементів немає AutomationId, тому вікно впізнаємо за заголовком, а перемикачі й кнопки —
/// за підписами. Обліковий запис ADAICA зберігає для користувача, і він діє в усіх його сеансах (зокрема у власній
/// ADAICA користувача), тож тест його не видаляє: «Mail-Konto löschen» стирає й закешовані листи.
/// </remarks>
public sealed class MailSettingsWizard
{
    private const string WindowTitle = "Mail-Einstellungen";

    // Після «Beenden» ADAICA створює обліковий запис і запускає синхронізацію.
    private static readonly TimeSpan FinishTimeout = TimeSpan.FromSeconds(60);

    private readonly Window _owner;

    public MailSettingsWizard(Window owner)
    {
        _owner = owner;
        Root = Wait.For(
            () => owner.FindFirstChild(cf => cf.ByControlType(ControlType.Window).And(cf.ByName(WindowTitle)))?.AsWindow(),
            $"вікна «{WindowTitle}»");

        NewAccount = Radio("Neues Mail-Konto einrichten");
        ChangeAccount = Radio("Kontoeinstellungen ändern");
        OutlookAccount = Radio("Neues Microsoft Outlook Konto erstellen");
        ActivateAccountButton = new ButtonControl(">", Locate.ById(Root, "Activate"));
        DefaultFolders = new CheckBoxControl("Standardmäßige wählen", () => Wait.For(
            () => Root.FindFirstDescendant(cf => cf.ByControlType(ControlType.CheckBox).And(cf.ByName("Standardmäßige wählen"))),
            "прапорця «Standardmäßige wählen»"));
        WithinLastDays = new RadioButtonControl("Innerhalb der letzten", Locate.ById(Root, "chkLastDays"));
        // Поле днів — останнє текстове поле в групі перемикачів «Synchronisationstyp bestimmen»; перше там — інтервал.
        LastDays = new TextBoxControl(
            "Innerhalb der letzten … Tage",
            () => Wait.For(
                () => Root.FindFirstDescendant(cf => cf.ByAutomationId("chkLastDays"))?.Parent
                    .FindAllChildren(cf => cf.ByControlType(ControlType.Edit)).LastOrDefault(),
                "поля днів «Innerhalb der letzten»"));
        NextButton = Button("Weiter");
        FinishButton = Button("Beenden");
        CancelButton = Button("Abbrechen");
    }

    /// <summary>«Neues Mail-Konto einrichten» на першій сторінці.</summary>
    public RadioButtonControl NewAccount { get; }

    /// <summary>«Kontoeinstellungen ändern»: доступний, лише коли обліковий запис уже налаштовано.</summary>
    public RadioButtonControl ChangeAccount { get; }

    /// <summary>«Neues Microsoft Outlook Konto erstellen» — «Ein mit Microsoft Outlook eingerichtetes Konto hinzufügen».</summary>
    public RadioButtonControl OutlookAccount { get; }

    /// <summary>«&gt;»: переносить вибраний запис Outlook у «Synchronisierende Konten».</summary>
    public ButtonControl ActivateAccountButton { get; }

    public CheckBoxControl DefaultFolders { get; }

    public RadioButtonControl WithinLastDays { get; }

    public TextBoxControl LastDays { get; }

    public ButtonControl NextButton { get; }

    public ButtonControl FinishButton { get; }

    public ButtonControl CancelButton { get; }

    /// <summary>Чи вже налаштовано поштовий обліковий запис: тоді на першій сторінці доступні інші дії, крім нового запису.</summary>
    public bool HasAccount => ChangeAccount.IsEnabled;

    // IsAvailable для закритого вікна WPF лишається true, тому перевіряємо, чи вікно ще серед дочірніх.
    public bool IsOpen => _owner.FindAllChildren(cf => cf.ByControlType(ControlType.Window)).Contains(Root);

    private Window Root { get; }

    /// <summary>
    /// Додає обліковий запис Outlook <paramref name="accountAddress"/> для синхронізації: стандартні теки Outlook
    /// і листи за останні <paramref name="lastDays"/> днів.
    /// </summary>
    public void AddOutlookAccount(string accountAddress, int lastDays) =>
        AllureApi.Step($"Додати обліковий запис Outlook {accountAddress}: стандартні теки, листи за {lastDays} днів", () =>
        {
            NewAccount.Select();
            OutlookAccount.Select();
            NextButton.Click();

            SelectOutlookAccount(accountAddress);
            NextButton.Click();

            DefaultFolders.SetChecked(true);
            WithinLastDays.Select();
            LastDays.Fill(lastDays.ToString(CultureInfo.InvariantCulture));
            AllureApi.Step(
                $"Перевірити теки для синхронізації: {string.Join(", ", CheckedFolders)}",
                () => CheckedFolders.ShouldNotBeEmpty("Теки для синхронізації після «Standardmäßige wählen»"));
            NextButton.Click();

            // Сторінка «Abschluss der Konfiguration» перелічує, що зробить «Beenden»: «адреса Hinzufügen vom Outlook-Konto».
            Wait.Until(
                () => FinishActions.Any(action => action.StartsWith(accountAddress, StringComparison.Ordinal)),
                $"дії з {accountAddress} на сторінці «Abschluss der Konfiguration»");
            AllureApi.Step($"Дії на сторінці «Abschluss der Konfiguration»: {string.Join("; ", FinishActions)}");
            FinishButton.Click();
            Wait.Until(() => !IsOpen, $"закриття вікна «{WindowTitle}» після «Beenden»", FinishTimeout);
        });

    public void Cancel() => AllureApi.Step($"Закрити вікно «{WindowTitle}» без змін", () =>
    {
        CancelButton.Click();
        Wait.Until(() => !IsOpen, $"закриття вікна «{WindowTitle}»");
    });

    /// <summary>Для прибирання після тесту: закриває вікно, лише якщо воно ще відкрите.</summary>
    public void CancelIfOpen()
    {
        if (IsOpen)
            Cancel();
    }

    /// <summary>
    /// Вибирає запис у списку «Outlook-Konten» і переносить його кнопкою «&gt;» у «Synchronisierende Konten».
    /// Рядок списку підписано як «адреса [Exchange via Outlook]», а ім'я елемента — тип моделі, тож шукаємо за текстом.
    /// </summary>
    private void SelectOutlookAccount(string accountAddress) =>
        AllureApi.Step($"Перенести {accountAddress} у «Synchronisierende Konten»", () =>
        {
            AutomationElement? Account(string listId) =>
                Root.FindFirstDescendant(cf => cf.ByAutomationId(listId))?
                    .FindAllChildren(cf => cf.ByControlType(ControlType.ListItem))
                    .FirstOrDefault(item => item.FindFirstChild(cf => cf.ByControlType(ControlType.Text))?.Name
                        .StartsWith(accountAddress + " ", StringComparison.Ordinal) == true);

            var account = Wait.For(() => Account("PART_NonActiveItems"), $"запису {accountAddress} у списку «Outlook-Konten»");
            account.Patterns.SelectionItem.Pattern.Select();
            Wait.Until(() => ActivateAccountButton.IsEnabled, "доступної кнопки «>»");
            ActivateAccountButton.Click();
            Wait.Until(() => Account("PART_ActiveItems") is not null, $"запису {accountAddress} у «Synchronisierende Konten»");
        });

    // Теки з поставленим прапорцем у дереві «Bestimmen Sie die zu synchronisierenden Ordner».
    private IReadOnlyList<string> CheckedFolders =>
        Root.FindFirstDescendant(cf => cf.ByAutomationId("PART_TreeView"))
            .FindAllDescendants(cf => cf.ByControlType(ControlType.TreeItem))
            .Where(folder => folder.FindFirstChild(cf => cf.ByControlType(ControlType.CheckBox))?.Patterns.Toggle.Pattern.ToggleState.Value == ToggleState.On)
            .Select(folder => folder.FindFirstChild(cf => cf.ByControlType(ControlType.Text))?.Name ?? string.Empty)
            .ToList();

    // Рядки «Liste von Einstellungsaktionen», наприклад «адреса Hinzufügen vom Outlook-Konto».
    private IReadOnlyList<string> FinishActions =>
        Root.FindAllDescendants(cf => cf.ByControlType(ControlType.ListItem))
            .Select(item => item.FindFirstChild(cf => cf.ByControlType(ControlType.Text))?.Name ?? string.Empty)
            .ToList();

    private RadioButtonControl Radio(string caption) =>
        new(caption, () => Wait.For(
            () => Root.FindFirstDescendant(cf => cf.ByControlType(ControlType.RadioButton).And(cf.ByName(caption))),
            $"перемикача «{caption}»"));

    private ButtonControl Button(string caption) =>
        new(caption, () => Wait.For(
            () => Root.FindFirstDescendant(cf => cf.ByControlType(ControlType.Button).And(cf.ByName(caption))),
            $"кнопки «{caption}»"));
}
