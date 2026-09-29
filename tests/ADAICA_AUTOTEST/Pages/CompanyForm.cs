using ADAICA_AUTOTEST.Controls;
using ADAICA_AUTOTEST.Infrastructure;
using Allure.Net.Commons;
using Allure.Net.Commons.Attributes;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;

namespace ADAICA_AUTOTEST.Pages;

/// <summary>
/// Картка фірми в області перегляду: вкладка «Stammdaten» з вкладками Allgemein, Details і Bankverbindungen.
/// Поля невибраної вкладки WPF не показує в дереві UI Automation, тому перед роботою з полями вкладку відкриваємо.
/// Перша ж зміна переводить картку в режим «Element in Bearbeitung»; зміни зберігає кнопка OK внизу картки.
/// </summary>
/// <param name="owner">Головне вікно ADAICA: вікна й меню картки — його дочірні вікна.</param>
/// <param name="root">Область перегляду з карткою.</param>
public class CompanyForm(Window owner, AutomationElement root)
{
    public const string NameId = "txtFullNameCompany";

    /// <summary>Вкладки Allgemein / Details / Bankverbindungen — є в картці фірми, хоч яка вкладка відкрита.</summary>
    public const string TabsId = "tabCompany";

    private const string EditingStatus = "Element in Bearbeitung";
    private const string BankAccountsListId = "lvBankDataViewCtrBankDataBox";

    public TabItemControl GeneralTab { get; } = new("Allgemein", Locate.ById(root, TabsId, "tiGeneralCompany"));

    public TabItemControl DetailsTab { get; } = new("Details", Locate.ById(root, TabsId, "tiCompanyOtherCtrCompanyForm"));

    public TabItemControl BankTab { get; } = new("Bankverbindungen", Locate.ById(root, TabsId, "tiBankDataCtrCompanyForm"));

    // Allgemein

    /// <summary>Повна назва: рядки назви з вікна «Firmendetails» через пробіл. Сама лише для читання.</summary>
    public TextBoxControl Name { get; } = new("Firmenname", Locate.ById(root, NameId));

    public TextBoxControl ShortName { get; } = new("Kurzname", Locate.ById(root, "txtCompanyShortName"));

    public ComboBoxControl CompanyType { get; } = new("Firmentyp", Locate.ById(root, "cmbCompanyType"));

    public TextBoxControl TradeRegister { get; } = new("Handelsregister", Locate.ById(root, "txtTradeRegisterNameCompany"));

    public TextBoxControl TradeRegisterNumber { get; } = new("HRG-Nr.", Locate.ById(root, "txtTradeRegisterAccountCompany"));

    public TextBoxControl TaxNumber { get; } = new("Steuernummer", Locate.ById(root, "txtControlNumberCtrCompanyGeneral"));

    public TextBoxControl VatNumber { get; } = new("USt-IdNr.", Locate.ById(root, "txtEGNumberCtrCompanyGeneral"));

    public CommunicationsList Communications { get; } = new(owner, root);

    public TextBoxControl Remark { get; } = new("Bemerkung", Locate.ById(root, "txtRemark"));

    public ComboBoxControl CorrespondencePreference { get; } = new("Korrespondenz-Präferenz", Locate.ById(root, "cmbCorrespondencePreference"));

    public ComboBoxControl ElectronicContact { get; } =
        new("Bevorzugt elektronische Kontaktaufnahme", Locate.ById(root, "cmbEmailPreferenceCtrCompanyGeneral"));

    public CheckBoxControl NoAdvertisingEmails { get; } = new("Keine Werbemails", Locate.ById(root, "chkAdvertisingEmailCtrCompanyGeneral"));

    public CheckBoxControl NoAdvertisingLetters { get; } = new("Keine Werbebriefe", Locate.ById(root, "chkAdvertisingLetterCtrCompanyGeneral"));

    public CheckBoxControl NoAdvertisingCalls { get; } = new("Keine Werbeanrufe", Locate.ById(root, "chkNoAdvertisingCallsCtrCompanyGeneral"));

    public CheckBoxControl PrivacyStatementOk { get; } = new("Datenschutzerklärung ok", Locate.ById(root, "chkPrivacyStatementOkCtrCompanyGeneral"));

    /// <summary>Фото фірми праворуч угорі.</summary>
    public ImagePickerControl Photo { get; } = new("Foto", Locate.ById(root, "imageCompany"));

    /// <summary>Категорії фірми під фото (лише для читання; змінюються у вікні «Kategorie»).</summary>
    public IReadOnlyList<string> Categories =>
        root.FindByAutomationId("lstCategories").FindAllChildren(cf => cf.ByControlType(ControlType.ListItem)).Select(item => item.Name).ToList();

    // Details

    public NumberBoxControl Turnover { get; } = new("Umsatz", Locate.ById(root, "txtTurnoverCtrCompanyOther"));

    public ComboBoxControl TurnoverCurrency { get; } = new("Umsatz: Währung", Locate.ById(root, "cmbTurnoverCtrCompanyOther"));

    public ComboBoxControl Creditworthiness { get; } = new("Bonität", Locate.ById(root, "cmbCreditworthinessCtrCompanyOther"));

    // Кнопки картки

    public ButtonControl OkButton { get; } = new("OK", Locate.ById(root, "btnOKCtrMainForm"));

    /// <summary>Картка в режимі редагування: є незбережені зміни.</summary>
    public bool IsEditing =>
        root.FindFirstDescendant(cf => cf.ByControlType(ControlType.Text).And(cf.ByName(EditingStatus))) is not null;

    [AllureStep("Відкрити вікно «Firmendetails» кнопкою «...» біля назви")]
    public CompanyNameDialog OpenNameDialog()
    {
        GeneralTab.Select();
        new ButtonControl("...", Locate.ById(root, NameId, "PART_Button")).Click();
        return new CompanyNameDialog(owner);
    }

    [AllureStep("Відкрити вікно «Kategorie» кнопкою «...» під категоріями")]
    public CategoriesDialog OpenCategoriesDialog()
    {
        GeneralTab.Select();
        new ButtonControl("...", Locate.ById(root, "categoriesCompany", "btnMoreCategories")).Click();
        return new CategoriesDialog(owner);
    }

    [AllureStep("Відкрити вікно нового банківського рахунку кнопкою «+»")]
    public BankAccountDialog OpenNewBankAccountDialog()
    {
        BankTab.Select();
        new ButtonControl("+", Locate.ById(root, "btnAddCtrBankDataBox")).Click();
        return new BankAccountDialog(owner);
    }

    /// <summary>Рядки списку «Adressen» (див. <see cref="AddressRowsIn"/>).</summary>
    public IReadOnlyList<string> AddressRows => AddressRowsIn(AddressBox);

    /// <summary>Які з колонок адреси список «Adressen» показує (див. <see cref="AddressColumnsIn"/>).</summary>
    public IReadOnlyList<string> AddressColumns => AddressColumnsIn(AddressBox);

    private AutomationElement AddressBox => root.FindByAutomationId("addressesCompany");

    /// <summary>
    /// Рядки списку «Adressen» картки контакту (фірми чи персони) у форматі <see cref="AddressData.ListRowIn"/>: лише
    /// колонки з <see cref="AddressData.ListColumns"/>, які список показує, у цьому порядку. Колонки користувач може
    /// переставляти й ховати (в AUTOTEST у фірми немає «Ortsteil», а в персони «Straße» стоїть перед «Ortsteil»),
    /// тому комірки беремо за заголовками.
    /// </summary>
    /// <param name="addressBox">Контрол AddressBox зі списком адрес.</param>
    public static IReadOnlyList<string> AddressRowsIn(AutomationElement addressBox)
    {
        var list = addressBox.FindByAutomationId("listView");
        var columns = ColumnNames(list);
        return list.FindAllChildren(cf => cf.ByControlType(ControlType.DataItem))
            .Select(row =>
            {
                var cells = row.FindAllChildren();
                return string.Join(" | ", AddressData.ListColumns.Where(columns.Contains)
                    .Select(column => cells.ElementAtOrDefault(columns.IndexOf(column))
                        ?.FindFirstDescendant(cf => cf.ByControlType(ControlType.Text))?.Name ?? string.Empty));
            })
            .ToList();
    }

    /// <summary>Колонки з <see cref="AddressData.ListColumns"/>, які показує список «Adressen» картки контакту.</summary>
    public static IReadOnlyList<string> AddressColumnsIn(AutomationElement addressBox) =>
        AddressData.ListColumns.Where(ColumnNames(addressBox.FindByAutomationId("listView")).Contains).ToList();

    // Заголовок колонки — ContentPresenter «PART_Content» із назвою; у першій колонці значок без назви.
    private static List<string> ColumnNames(AutomationElement list) =>
        list.FindFirstChild(cf => cf.ByControlType(ControlType.Header))
            ?.FindAllChildren()
            .Select(header => header.FindFirstChild(cf => cf.ByAutomationId("PART_Content"))?.Name ?? header.Name ?? string.Empty)
            .ToList()
        ?? [];

    /// <summary>
    /// «+» біля «Adressen» → категорія адреси: відкриває вікно «Finden &amp; Erstellen» на вкладці Adressen.
    /// Кнопка відкриває меню лише на клік мишею: InvokePattern вона не підтримує.
    /// </summary>
    [AllureStep("Відкрити вікно нової адреси: «+» біля «Adressen» → {category}")]
    public AddressFindCreateWindow OpenNewAddressWindow(string category)
    {
        GeneralTab.Select();
        AtomicMouse.LeftClick(root.FindByAutomationId("tbtnAddSpecifyCompanyAddress"));
        new MenuItemControl(
                category,
                () => Wait.For(
                    () => owner.FindAllChildren(cf => cf.ByClassName("Popup"))
                        .Select(popup => popup.FindFirstDescendant(cf => cf.ByControlType(ControlType.MenuItem).And(cf.ByName(category))))
                        .FirstOrDefault(item => item is not null),
                    $"пункту «{category}» у меню «Adressen»"))
            .Click();
        return new AddressFindCreateWindow(owner);
    }

    /// <summary>
    /// Створює нову адресу й додає її до фірми. Якщо таку адресу вже створив попередній прогін, бере іншу
    /// з <paramref name="anotherAddress"/>.
    /// </summary>
    /// <returns>Адреса, яку справді створено.</returns>
    public AddressData AddAddress(AddressData address, Func<AddressData, AddressData> anotherAddress) =>
        AllureApi.Step($"Додати адресу «{address.Category}»", () =>
        {
            var window = OpenNewAddressWindow(address.Category);
            try
            {
                const int attempts = 5;
                for (var attempt = 1; !window.FillNew(address); attempt++)
                {
                    if (attempt == attempts)
                        throw new InvalidOperationException($"За {attempts} спроби не знайшлося адреси, якої ще немає в довіднику.");

                    address = anotherAddress(address);
                }

                var dialog = window.OpenNewAddressDialog();
                dialog.ShouldHaveValues(address);
                dialog.Confirm();
                window.WaitUntilClosed();
            }
            finally
            {
                window.CloseIfOpen();
            }

            AllureApi.Step($"Перевірити, що адреса «{address}» з'явилася в списку «Adressen»",
                () => Wait.Until(() => AddressRows.Contains(address.ListRowIn(AddressColumns)), $"адреси «{address}» у списку «Adressen»"));
            return address;
        });

    /// <summary>Рахунки на вкладці Bankverbindungen у порядку показу.</summary>
    public IReadOnlyList<BankAccountData> BankAccounts =>
        root.FindByAutomationId(BankAccountsListId).FindByAutomationId("listView")
            .FindAllChildren(cf => cf.ByControlType(ControlType.DataItem))
            .Select(row => row.FindAllDescendants(cf => cf.ByControlType(ControlType.Text)).Select(text => text.Name).ToArray())
            // Колонки: Kontoinhaber, IBAN, Name der Bank, SWIFT-BIC, Hauptkonto («+» або «-»), BLZ, Kontonummer.
            .Select(cells => new BankAccountData(
                Holder: cells[0],
                Iban: cells[1],
                BankCode: cells[5],
                BankName: cells[2],
                Bic: cells[3],
                AccountNumber: cells[6],
                IsPrimary: cells[4] == "+"))
            .ToList();

    /// <summary>
    /// Змінює всі поля трьох вкладок, які ADAICA дає редагувати в тестовій базі, і зберігає картку кнопкою OK.
    /// </summary>
    /// <param name="anotherAddress">Інша адреса, якщо задана вже є в довіднику адрес.</param>
    /// <param name="photoFile">JPG для фото фірми, намальований за <see cref="CompanyGeneralData.Photo"/>.</param>
    /// <returns>Збережені дані: адреса може відрізнятися від заданої.</returns>
    public CompanyEditData Edit(CompanyEditData data, Func<AddressData, AddressData> anotherAddress, string photoFile) =>
        AllureApi.Step("Змінити поля фірми", () =>
        {
            var general = EditGeneral(data.General, anotherAddress, photoFile);
            EditDetails(data.Details);
            AddBankAccount(data.BankAccount);
            Save();
            return data with { General = general };
        });

    /// <returns>Збережені дані вкладки: адреса може відрізнятися від заданої.</returns>
    public CompanyGeneralData EditGeneral(CompanyGeneralData general, Func<AddressData, AddressData> anotherAddress, string photoFile) =>
        AllureApi.Step("Змінити поля на вкладці Allgemein", () =>
    {
        GeneralTab.Select();

        var nameDialog = OpenNameDialog();
        nameDialog.Fill(general);
        nameDialog.Confirm();

        CompanyType.Select(general.CompanyType);
        TradeRegister.Fill(general.TradeRegister);
        TradeRegisterNumber.Fill(general.TradeRegisterNumber);
        TaxNumber.Fill(general.TaxNumber);
        VatNumber.Fill(general.VatNumber);

        foreach (var communication in general.Communications)
            Communications.Add(communication);

        var address = AddAddress(general.Address, anotherAddress);

        Remark.Fill(general.Remark);

        var categoriesDialog = OpenCategoriesDialog();
        categoriesDialog.Check(general.Category);
        categoriesDialog.Confirm();

        Photo.AddFromFile(photoFile);

        CorrespondencePreference.Select(general.CorrespondencePreference);
        ElectronicContact.Select(general.ElectronicContact);
        NoAdvertisingEmails.SetChecked(general.NoAdvertisingEmails);
        NoAdvertisingLetters.SetChecked(general.NoAdvertisingLetters);
        NoAdvertisingCalls.SetChecked(general.NoAdvertisingCalls);
        PrivacyStatementOk.SetChecked(general.PrivacyStatementOk);
        return general with { Address = address };
    });

    public void EditDetails(CompanyDetailsData details) => AllureApi.Step("Змінити поля на вкладці Details", () =>
    {
        DetailsTab.Select();
        Turnover.Fill(details.Turnover);
        TurnoverCurrency.Select(details.TurnoverCurrency);
        Creditworthiness.Select(details.Creditworthiness);
    });

    public void AddBankAccount(BankAccountData account) => AllureApi.Step("Додати рахунок на вкладці Bankverbindungen", () =>
    {
        var dialog = OpenNewBankAccountDialog();
        dialog.Fill(account);
        dialog.Confirm();
    });

    /// <summary>Зберігає зміни кнопкою OK і чекає, доки картка вийде з режиму редагування.</summary>
    public void Save() => AllureApi.Step("Зберегти фірму кнопкою OK", () =>
    {
        OkButton.Click();
        Wait.Until(() => !IsEditing && !OkButton.IsEnabled, "збереження фірми", TimeSpan.FromSeconds(30));
    });

    /// <summary>Для прибирання після тесту: відкидає незбережені зміни кнопкою «Abbrechen», якщо картка ще редагується.</summary>
    public void CancelIfEditing()
    {
        if (!IsEditing)
            return;

        AllureApi.Step("Відкинути незбережені зміни фірми", () =>
        {
            new ButtonControl("Abbrechen", Locate.ById(root, "btnCancelCtrMainForm")).Click();
            Wait.Until(() => !IsEditing, "виходу картки фірми з режиму редагування");
        });
    }

    /// <summary>Перевіряє поля трьох вкладок і вікна «Firmendetails»; повідомляє про всі розбіжності вкладки разом.</summary>
    /// <param name="name">Назва фірми (перший рядок), з якою її створено.</param>
    public void ShouldHaveValues(string name, string shortName, CompanyEditData expected) => AllureApi.Step("Перевірити поля фірми", () =>
    {
        var general = expected.General;
        AllureApi.Step("Перевірити вкладку Allgemein", () =>
        {
            GeneralTab.Select();
            this.ShouldSatisfyAllConditions(
                () => Name.ShouldHaveText(FullName(name, general)),
                () => ShortName.ShouldHaveText(shortName),
                () => CompanyType.ShouldHaveSelected(general.CompanyType),
                () => TradeRegister.ShouldHaveText(general.TradeRegister),
                () => TradeRegisterNumber.ShouldHaveText(general.TradeRegisterNumber),
                () => TaxNumber.ShouldHaveText(general.TaxNumber),
                () => VatNumber.ShouldHaveText(general.VatNumber),
                () => Communications.ShouldHaveItems(general.Communications),
                () => AllureApi.Step($"Перевірити «Adressen»: {general.Address.ListRow}",
                    () => AddressRows.ShouldBe(new[] { general.Address.ListRowIn(AddressColumns) }, "Adressen")),
                () => Remark.ShouldHaveText(general.Remark),
                () => AllureApi.Step($"Перевірити «Kategorie»: {general.Category}",
                    () => Categories.ShouldBe(new[] { general.Category }, "Kategorie")),
                () => Photo.ShouldHaveImage(),
                () => Photo.ShouldShowImageWithBackground(general.Photo.Background),
                () => CorrespondencePreference.ShouldHaveSelected(general.CorrespondencePreference),
                () => ElectronicContact.ShouldHaveSelected(general.ElectronicContact),
                () => NoAdvertisingEmails.ShouldBeChecked(general.NoAdvertisingEmails),
                () => NoAdvertisingLetters.ShouldBeChecked(general.NoAdvertisingLetters),
                () => NoAdvertisingCalls.ShouldBeChecked(general.NoAdvertisingCalls),
                () => PrivacyStatementOk.ShouldBeChecked(general.PrivacyStatementOk));

            var nameDialog = OpenNameDialog();
            try
            {
                nameDialog.ShouldHaveValues(name, general);
            }
            finally
            {
                nameDialog.CancelIfOpen();
            }
        });

        AllureApi.Step("Перевірити вкладку Details", () =>
        {
            DetailsTab.Select();
            this.ShouldSatisfyAllConditions(
                () => Turnover.ShouldHaveValue(expected.Details.Turnover),
                () => TurnoverCurrency.ShouldHaveSelected(expected.Details.TurnoverCurrency),
                () => Creditworthiness.ShouldHaveSelected(expected.Details.Creditworthiness));
        });

        AllureApi.Step($"Перевірити вкладку Bankverbindungen: рахунок {expected.BankAccount.Iban}", () =>
        {
            BankTab.Select();
            BankAccounts.ShouldBe(new[] { expected.BankAccount }, "Bankverbindungen");
        });
    });

    /// <summary>Назва, яку ADAICA показує в картці: рядки назви через пробіл.</summary>
    public static string FullName(string name, CompanyGeneralData general) => $"{name} {general.NameLine2} {general.NameLine3}";

    /// <summary>
    /// Перевіряє фірму, створену разом із персоною. Телефон і e-mail ADAICA записує персоні, тож «Verbindung» фірми
    /// порожній, а адреса — фірми.
    /// </summary>
    public void ShouldHaveValues(NewContactData expected) => AllureApi.Step("Перевірити поля фірми", () =>
    {
        GeneralTab.Select();
        this.ShouldSatisfyAllConditions(
            () => Name.ShouldHaveText(expected.Company.Name),
            () => ShortName.ShouldHaveText(expected.Company.ShortName),
            () => TradeRegisterNumber.ShouldHaveText(expected.TradeRegisterNumber),
            () => CorrespondencePreference.ShouldHaveSelected(expected.CorrespondencePreference),
            () => Communications.ShouldHaveItems([]),
            () => AllureApi.Step($"Перевірити «Adressen»: {expected.Address.ListRow}",
                () => AddressRows.ShouldBe(new[] { expected.Address.ListRowIn(AddressColumns) }, "Adressen")));
    });

    /// <summary>Перевіряє поля нової фірми, заданої у вікні «Neue Person/Firma».</summary>
    public void ShouldHaveValues(CompanyData expected) => AllureApi.Step("Перевірити поля фірми", () =>
    {
        GeneralTab.Select();
        this.ShouldSatisfyAllConditions(
            () => Name.ShouldHaveText(expected.Name),
            () => ShortName.ShouldHaveText(expected.ShortName));
    });
}
