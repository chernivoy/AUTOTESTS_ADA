using ADAICA_AUTOTEST.Controls;
using ADAICA_AUTOTEST.Infrastructure;
using ADAICA_AUTOTEST.Pages;
using Allure.Net.Commons;
using Allure.Net.Commons.Attributes;

namespace ADAICA_AUTOTEST.Tests;

/// <summary>
/// Створення контактів через навігатор Kontakte. Збережені контакти лишаються в базі. Назва фірми щоразу унікальна
/// (<see cref="CustomerCase.NewCompany"/>), тож за нею фірму можна знайти пошуком і відрізнити від створених раніше.
/// </summary>
[AllureSuite("Навігатори")]
public class ContactTests : AdaicaTestBase
{
    [Test]
    [Category("Smoke")]
    [AllureSeverity(SeverityLevel.critical)]
    public void KontakteNavigator_NewCompany()
    {
        var mainPage = LogInAndOpenNavigator("Kontakte");
        var company = CustomerCase.Random().NewCompany();

        var savedCompany = CreateCompany(mainPage, company);

        savedCompany.ShouldHaveValues(company);
    }

    /// <summary>
    /// Нова фірма, у картці якої змінено всі поля вкладок Allgemein, Details і Bankverbindungen, які ADAICA дає
    /// редагувати в тестовій базі. Після збереження фірму перевідкрито з дерева, тож перевіряються збережені значення.
    /// </summary>
    [Test]
    [AllureSeverity(SeverityLevel.critical)]
    public void KontakteNavigator_EditCompany()
    {
        var mainPage = LogInAndOpenNavigator("Kontakte");
        var customer = CustomerCase.Random();
        var company = customer.NewCompany();
        var edit = customer.EditCompany();

        // Фото тест малює сам; файл прибирається, щойно фото збережено, а якщо тест упаде раніше, — після тесту.
        var photo = TestImageFile.CreateJpeg(edit.General.Photo.Caption, edit.General.Photo.Background);
        OnCleanup(photo.Dispose);

        var savedCompany = CreateCompany(mainPage, company);
        // Якщо тест упаде посеред редагування, незбережені зміни буде відкинуто.
        OnCleanup(savedCompany.CancelIfEditing);
        edit = savedCompany.Edit(edit, CustomerCase.AnotherAddress, photo.Path);

        // Фото зберігається в ADAICA, а не посиланням на файл: після видалення файлу воно лишається у фірмі.
        AllureApi.Step($"Видалити файл фото {photo.Path}", () =>
        {
            photo.Delete();
            photo.Exists.ShouldBeFalse($"Файл фото {photo.Path} не видалено");
        });

        // Вузол фірми в дереві — повна назва й категорія в дужках.
        var fullName = CompanyForm.FullName(company.Name, edit.General);
        var node = $"{fullName} ({edit.General.Category})";
        AllureApi.Step($"Перевірити, що в дереві виділено «Aktuelle → {node}»",
            () => mainPage.SelectedNavigatorNodePath.ShouldBe(new[] { "Aktuelle", node }));

        var reopenedCompany = mainPage.ReopenCompany(node, fullName);
        reopenedCompany.ShouldHaveValues(company.Name, company.ShortName, edit);
    }

    /// <summary>
    /// Нова фірма, а під нею з її контекстного меню («Neu») — проєкт, Ordner, завдання, термін та інфозапис.
    /// Усі записи стосуються тієї самої справи клієнта, що й фірма.
    /// </summary>
    [Test]
    [AllureSeverity(SeverityLevel.critical)]
    public void KontakteNavigator_NewCompanyWithChildRecords()
    {
        var mainPage = LogInAndOpenNavigator("Kontakte");
        var customer = CustomerCase.Random();
        var company = customer.NewCompany();

        CreateCompany(mainPage, company);

        CreateChildRecords(mainPage, customer, company.Name);
    }

    /// <summary>
    /// Нова фірма, під її вузлом «Mitarbeiter» — новий відділ (Organisationseinheit), а у відділі — співробітник,
    /// створений як нова персона через «Finden &amp; Erstellen» → «Neue Person/Firma».
    /// </summary>
    [Test]
    [AllureSeverity(SeverityLevel.critical)]
    public void KontakteNavigator_NewCompanyDepartmentWithEmployee()
    {
        var mainPage = LogInAndOpenNavigator("Kontakte");
        var customer = CustomerCase.Random();
        var company = customer.NewCompany();
        var department = customer.NewDepartment();
        var employee = customer.NewEmployee();

        CreateCompany(mainPage, company);

        CreateDepartmentWithEmployee(mainPage, company.Name, department, employee);
    }

    /// <summary>
    /// Нова фірма, у ній новий відділ і новий співробітник, а під співробітником з його контекстного меню («Neu») —
    /// проєкт, Ordner, завдання, термін та інфозапис. Усі записи стосуються тієї самої справи клієнта, що й фірма.
    /// </summary>
    [Test]
    [AllureSeverity(SeverityLevel.critical)]
    public void KontakteNavigator_NewEmployeeWithChildRecords()
    {
        var mainPage = LogInAndOpenNavigator("Kontakte");
        var customer = CustomerCase.Random();
        var company = customer.NewCompany();
        var department = customer.NewDepartment();
        var employee = customer.NewEmployee();

        CreateCompany(mainPage, company);
        CreateDepartmentWithEmployee(mainPage, company.Name, department, employee);

        CreateChildRecords(mainPage, customer, employee.NodeName);
    }

    /// <summary>
    /// Документ із Word-файлу під фірмою: контекстне меню фірми → «Neu» → «Dokument» → «Aus dem Dateisystem» → .docx,
    /// який тест складає сам (пропозиція чи протокол у справі клієнта). Вікна редагування ADAICA не показує: документ
    /// одразу зберігається й відкривається. Назва — ім'я файлу без розширення, тип «docx», розмір як у файлу,
    /// версія 1 з 1, статус «aktuell», автор — поточний користувач; у дереві документ — під фірмою у вузлі «Dateien».
    /// </summary>
    [Test]
    [AllureSeverity(SeverityLevel.critical)]
    public void KontakteNavigator_NewDocumentFromWordFile()
    {
        var mainPage = LogInAndOpenNavigator("Kontakte");
        var customer = CustomerCase.Random();
        var company = customer.NewCompany();
        CreateCompany(mainPage, company);

        var file = TestWordFile.CreateDocx(customer.NewWordFile(company.Name));
        OnCleanup(file.Dispose);
        AllureApi.AddAttachment($"{file.FileName}", "application/vnd.openxmlformats-officedocument.wordprocessingml.document", file.Path);

        var createdFrom = DateTime.Now;
        mainPage.NewChildDocumentFromFile(company.Name, file.Path);
        var document = mainPage.WaitForOpenedDocument(file.Name);
        var createdTo = DateTime.Now;

        document.ShouldHaveValues(new DocumentData(
            Name: file.Name,
            ShortName: string.Empty,
            FileType: "docx",
            FileSize: AdaicaFormat.FormatFileSize(file.Length),
            Creator: mainPage.UserDisplayName,
            Version: "1 / 1",
            Status: "aktuell",
            Remark: string.Empty));
        document.ShouldBeCreatedBetween(createdFrom, createdTo);
        AllureApi.Step($"Перевірити, що в дереві виділено «{company.Name} → Dateien → {file.FileName}»",
            () => mainPage.SelectedNavigatorNodePath.TakeLast(3).ShouldBe(new[] { company.Name, "Dateien", file.FileName }));
    }

    /// <summary>
    /// Нова персона й нова фірма разом: у рядку пошуку «Finden &amp; Erstellen» — персона, фірма, адреса фірми, телефон
    /// і e-mail. Вікно «Neue Person/Firma» має взяти все це з рядка пошуку; решту порожніх полів тест доповнює
    /// (Kurzname, HRG-Nr., Titel, Namenszusatz, Geburtstag, Geburtsname, Funktion тощо). Після збереження персона —
    /// співробітник фірми у відділі «andere Tätigkeiten»; перевіряються обидві картки.
    /// </summary>
    [Test]
    [AllureSeverity(SeverityLevel.critical)]
    public void KontakteNavigator_NewPersonWithNewCompany()
    {
        var mainPage = LogInAndOpenNavigator("Kontakte");
        var contact = CustomerCase.Random().NewContact();
        var person = contact.Person;

        var findCreateWindow = mainPage.OpenNewPersonWindow();
        // Якщо тест упаде до збереження, вікна закриються без створення фірми й персони.
        OnCleanup(findCreateWindow.CloseIfOpen);

        findCreateWindow.FocusedField.ShouldHaveText("Vorname Nachname");
        findCreateWindow.Fill(contact);
        // Дію під рядком пошуку ADAICA визначає за введеними даними, не одразу після введення.
        findCreateWindow.Action.WaitForText("Firma erstellen, Person erstellen, Mitarbeiter zur Firma hinzufügen");

        var dialog = findCreateWindow.OpenNewPersonDialog();
        OnCleanup(dialog.CancelIfOpen);

        dialog.ShouldHaveSearchLineValues(contact);
        dialog.FillMissing(contact);
        dialog.ShouldHaveValues(contact);

        dialog.Save();

        findCreateWindow.WaitUntilClosed();
        mainPage.WaitForOpenedPerson(person.FullName).ShouldHaveValues(contact);
        var personPath = new[] { "Aktuelle", contact.Company.Name, "Mitarbeiter", NewContactData.Department, person.NodeName };
        AllureApi.Step($"Перевірити, що в дереві виділено «{string.Join(" → ", personPath)}»",
            () => mainPage.SelectedNavigatorNodePath.ShouldBe(personPath));

        mainPage.SelectNavigatorNode(contact.Company.Name);
        mainPage.WaitForOpenedCompany(contact.Company.Name).ShouldHaveValues(contact);
    }

    /// <summary>
    /// Під фірмою створює відділ (вузол «Mitarbeiter» → «Neue Organisationseinheit hinzufügen»), а в ньому —
    /// співробітника як нову персону (меню відділу → «Finden &amp; Erstellen» → «Neue Person/Firma»).
    /// Після збереження ADAICA виділяє співробітника в дереві під відділом і відкриває його картку.
    /// </summary>
    private void CreateDepartmentWithEmployee(MainPage mainPage, string companyName, string department, EmployeeData employee)
    {
        var departmentDialog = mainPage.OpenNewDepartmentDialog(companyName);
        OnCleanup(departmentDialog.CancelIfOpen);
        departmentDialog.Name.Fill(department);
        departmentDialog.Confirm();

        mainPage.WaitForOpenedDepartment(department);
        AllureApi.Step($"Перевірити, що в дереві виділено «{companyName} → Mitarbeiter → {department}»",
            () => mainPage.SelectedNavigatorNodePath.TakeLast(3).ShouldBe(new[] { companyName, "Mitarbeiter", department }));

        var findCreateWindow = mainPage.OpenNewDepartmentEmployeeWindow(department);
        // Якщо тест упаде до збереження, вікна закриються без створення персони.
        OnCleanup(findCreateWindow.CloseIfOpen);

        findCreateWindow.Person.Fill($"{employee.FirstName} {employee.LastName}");
        // Дію під рядком пошуку ADAICA визначає за введеними даними, не одразу після введення.
        findCreateWindow.Action.WaitForText("Person erstellen");

        var personDialog = findCreateWindow.OpenNewPersonDialog();
        OnCleanup(personDialog.CancelIfOpen);

        personDialog.Fill(employee);
        personDialog.ShouldHaveValues(employee, companyName, department);

        personDialog.Save();

        findCreateWindow.WaitUntilClosed();
        mainPage.WaitForOpenedPerson(employee.FullName).ShouldHaveValues(employee);
        AllureApi.Step($"Перевірити, що в дереві виділено «{companyName} → Mitarbeiter → {department} → {employee.NodeName}»",
            () => mainPage.SelectedNavigatorNodePath.TakeLast(4)
                .ShouldBe(new[] { companyName, "Mitarbeiter", department, employee.NodeName }));
    }

    /// <summary>
    /// Створює фірму: Gesamtliste → «Neue Firma» → «Finden &amp; Erstellen» → «Neue Person/Firma» → OK.
    /// Після збереження ADAICA сама відкриває фірму у вузлі Aktuelle.
    /// </summary>
    private CompanyForm CreateCompany(MainPage mainPage, CompanyData company)
    {
        var findCreateWindow = mainPage.OpenNewCompanyWindow();
        // Якщо тест упаде до збереження, вікна закриються без створення фірми.
        OnCleanup(findCreateWindow.CloseIfOpen);

        findCreateWindow.FocusedField.ShouldHaveText("Firma");
        findCreateWindow.Company.Fill(company.Name);
        // Дію під рядком пошуку ADAICA визначає за введеними даними, не одразу після введення.
        findCreateWindow.Action.WaitForText("Firma erstellen");

        var companyDialog = findCreateWindow.OpenNewCompanyDialog();
        OnCleanup(companyDialog.CancelIfOpen);

        companyDialog.Fill(company);
        companyDialog.ShouldHaveValues(company);

        companyDialog.Save();

        findCreateWindow.WaitUntilClosed();
        var savedCompany = mainPage.WaitForOpenedCompany(company.Name);
        AllureApi.Step($"Перевірити, що в дереві виділено «Aktuelle → {company.Name}»",
            () => mainPage.SelectedNavigatorNodePath.ShouldBe(new[] { "Aktuelle", company.Name }));
        return savedCompany;
    }
}
