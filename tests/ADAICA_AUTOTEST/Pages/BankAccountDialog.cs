using ADAICA_AUTOTEST.Controls;
using ADAICA_AUTOTEST.Infrastructure;
using Allure.Net.Commons;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;

namespace ADAICA_AUTOTEST.Pages;

/// <summary>
/// Вікно «Bankkonto hinzufügen/bearbeiten» (кнопка «+» на вкладці Bankverbindungen). Назва банку, SWIFT-BIC і BLZ
/// лише для читання: їх заповнює вибір банку з довідника внизу вікна. Довідник великий, тому спершу його
/// фільтруємо рядком пошуку.
/// </summary>
public sealed class BankAccountDialog(Window owner)
    : ChildDialog(owner, "wndBankAccountEdit", "Bankkonto hinzufügen/bearbeiten", "btnOKWndUTObjectNewRelation", "btnCancelWndUTObjectNewRelation")
{
    // Колонка BLZ у довіднику банків: Name der Bank, SWIFT-BIC, Bankleitzahl (BLZ), PLZ, Ort.
    private const int BankCodeColumn = 2;

    public TextBoxControl Holder => new("Kontoinhaber", Locate.ById(Root, "txtAccountHolderWndBankAccountEdit"));

    public TextBoxControl Iban => new("IBAN", Locate.ById(Root, "txtIbanWndBankAccountEdit"));

    public CheckBoxControl IsPrimary => new("Hauptkonto", Locate.ById(Root, "chkPrimaryYnWndBankAccountEdit"));

    public TextBoxControl BankName => new("Name der Bank", Locate.ById(Root, "txtBankNameWndBankAccountEdit"));

    public TextBoxControl Bic => new("SWIFT-BIC", Locate.ById(Root, "txtSwiftBicWndBankAccountEdit"));

    public TextBoxControl BankCode => new("Bankleitzahl (BLZ)", Locate.ById(Root, "txtBankCodeWndBankAccountEdit"));

    public TextBoxControl AccountNumber => new("Kontonummer", Locate.ById(Root, "txtAccountNumberWndBankAccountEdit"));

    public TextBoxControl BankSearch => new("Suchbegriff (Banken)", Locate.ById(Root, "searchWndBankAccountEdit", "txtQuickSearch"));

    public void Fill(BankAccountData account) => AllureApi.Step("Заповнити банківський рахунок", () =>
    {
        SelectBank(account.BankCode);
        Holder.Fill(account.Holder);
        Iban.Fill(account.Iban);
        AccountNumber.Fill(account.AccountNumber);
        IsPrimary.SetChecked(account.IsPrimary);
    });

    /// <summary>Вибирає банк із довідника за BLZ; ADAICA заповнює назву банку, SWIFT-BIC і BLZ.</summary>
    public void SelectBank(string bankCode) => AllureApi.Step($"Вибрати банк з BLZ {bankCode}", () =>
    {
        BankSearch.Fill(bankCode);
        // Кнопка фільтра реагує лише на клік мишею: InvokePattern вона не підтримує.
        AtomicMouse.LeftClick(Root.FindByAutomationId("btnQuickSearchApplyFilter"));

        var bank = Wait.For(
            () =>
            {
                var rows = BankRows();
                return rows.All(row => CellText(row, BankCodeColumn) == bankCode) ? rows.FirstOrDefault() : null;
            },
            $"банку з BLZ {bankCode} у відфільтрованому довіднику");
        bank.Patterns.SelectionItem.Pattern.Select();
        Wait.Until(() => BankCode.Text == bankCode, $"BLZ {bankCode} у полі «{BankCode.Name}»");
    });

    private AutomationElement[] BankRows() =>
        Root.FindByAutomationId("lvBankDataWndBankAccountEdit").FindAllChildren(cf => cf.ByControlType(ControlType.DataItem));

    private static string? CellText(AutomationElement row, int column) =>
        row.FindAllDescendants(cf => cf.ByControlType(ControlType.Text)).ElementAtOrDefault(column)?.Name;
}
