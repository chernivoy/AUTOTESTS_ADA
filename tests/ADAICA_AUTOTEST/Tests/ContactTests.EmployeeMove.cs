using ADAICA_AUTOTEST.Infrastructure;
using ADAICA_AUTOTEST.Pages;
using Allure.Net.Commons;
using Allure.Net.Commons.Attributes;

namespace ADAICA_AUTOTEST.Tests;

/// <summary>
/// Функції співробітників і переміщення між відділами фірми («Mitarbeiter umziehen»), зокрема з відділу за
/// замовчуванням «andere Tätigkeiten» і назад. Функції тест бере з довідника Mitarbeiterfunktion
/// (<see cref="CustomerCase.EmployeeFunctions"/>), який доповнює сам.
/// </summary>
public partial class ContactTests
{
    private const string EmployeesNode = "Mitarbeiter";
    private const string DefaultDepartment = "andere Tätigkeiten";
    private const string StandardFunction = "Mitarbeiter";

    /// <summary>
    /// Нова фірма з трьома відділами, у кожному — співробітник зі своєю функцією. Спершу довідник Mitarbeiterfunktion
    /// (кнопка «Konfiguration der Liste von Kommunikationsarten» у картці фірми → вкладка «Firma»): функції тесту
    /// додаються один раз, а коли вони вже є, — лише одна нова. Далі співробітники переходять по колу між відділами:
    /// перший — з двома новими функціями (чинною, нещодавно доданою в довідник, і завершеною), другий — із запланованою,
    /// третій — зі своєю функцією без змін. Після кожного переміщення перевіряються дерево (співробітник у новому відділі
    /// й зник зі старого) і збережені функції («Mitarbeiter bearbeiten»).
    /// </summary>
    [Test]
    [AllureSeverity(SeverityLevel.critical)]
    public void KontakteNavigator_MoveEmployeesBetweenDepartments()
    {
        var mainPage = LogInAndOpenNavigator("Kontakte");
        var customer = CustomerCase.Random();
        var company = customer.NewCompany();
        var departments = new[] { customer.NewDepartment(), customer.NewDepartment(), customer.NewDepartment() };

        var companyForm = CreateCompany(mainPage, company);
        var added = AddEmployeeFunctions(companyForm, customer, addNew: true);

        var employees = departments
            .Select(department =>
            {
                var employee = customer.NewEmployee(customer.PickEmployeeFunction());
                CreateDepartmentWithEmployee(mainPage, company.Name, department, employee);
                return new EmployeeInDepartment(employee, department, [new EmployeeFunctionData(employee.Function)]);
            })
            .ToList();

        // Функцію, щойно додану в довідник, видно у вікні співробітника без перезапуску ADAICA.
        MoveEmployee(mainPage, company.Name, departments, employees[0], departments[1],
            [customer.ActiveFunction(added[^1]), customer.EndedFunction(customer.PickEmployeeFunction())]);
        MoveEmployee(mainPage, company.Name, departments, employees[1], departments[2],
            [customer.PlannedFunction(customer.PickEmployeeFunction())]);
        MoveEmployee(mainPage, company.Name, departments, employees[2], departments[0], []);
    }

    /// <summary>
    /// «Mitarbeiter kopieren»: співробітник переходить в інший відділ з новою функцією, але лишається й у старому зі
    /// старою. У кожному відділі «Mitarbeiter bearbeiten» показує функції саме цього відділу.
    /// </summary>
    [Test]
    [AllureSeverity(SeverityLevel.critical)]
    public void KontakteNavigator_CopyEmployeeToDepartment()
    {
        var mainPage = LogInAndOpenNavigator("Kontakte");
        var customer = CustomerCase.Random();
        var company = customer.NewCompany();
        var departments = new[] { customer.NewDepartment(), customer.NewDepartment() };
        var employee = customer.NewEmployee(customer.PickEmployeeFunction());

        var companyForm = CreateCompany(mainPage, company);
        AddEmployeeFunctions(companyForm, customer, addNew: false);
        CreateDepartmentWithEmployee(mainPage, company.Name, departments[0], employee);
        CreateDepartment(mainPage, company.Name, departments[1]);

        var original = new EmployeeInDepartment(employee, departments[0], [new EmployeeFunctionData(employee.Function)]);
        MoveEmployee(mainPage, company.Name, departments, original, departments[1],
            [customer.ActiveFunction(customer.PickEmployeeFunction())], copy: true);

        EmployeeFunctionsShouldBe(mainPage, company.Name, original);
    }

    /// <summary>
    /// Відділ за замовчуванням «andere Tätigkeiten»: співробітник, доданий до фірми без відділу (вузол «Mitarbeiter» →
    /// «Mitarbeiter zur Firma hinzufügen»), потрапляє туди. Звідти він переходить у відділ фірми («Mitarbeiter kopieren»
    /// з «andere Tätigkeiten» недоступне), а «Mitarbeiter aus der Organisationseinheit lösen» повертає його назад:
    /// у списку відділів «Mitarbeiter umziehen» «andere Tätigkeiten» немає.
    /// </summary>
    [Test]
    [AllureSeverity(SeverityLevel.critical)]
    public void KontakteNavigator_MoveEmployeeFromAndToDefaultDepartment()
    {
        var mainPage = LogInAndOpenNavigator("Kontakte");
        var customer = CustomerCase.Random();
        var company = customer.NewCompany();
        var departments = new[] { customer.NewDepartment(), customer.NewDepartment() };
        var employee = customer.NewEmployee(customer.PickEmployeeFunction());

        var companyForm = CreateCompany(mainPage, company);
        AddEmployeeFunctions(companyForm, customer, addNew: false);
        foreach (var department in departments)
            CreateDepartment(mainPage, company.Name, department);

        var inDefault = CreateCompanyEmployee(mainPage, company.Name, employee);

        var moved = MoveEmployee(mainPage, company.Name, departments, inDefault, departments[0], []);

        var question = mainPage.DetachEmployeeFromDepartment(moved.Path(company.Name));
        AllureApi.Step($"Перевірити питання: «Beziehung lösen» з іменем «{employee.LastName} {employee.FirstName}»", () =>
            question.ShouldSatisfyAllConditions(
                () => question.Title.ShouldBe("Beziehung lösen"),
                () => question.Message.Text.ShouldContain($"{employee.LastName} {employee.FirstName}")));
        question.Button("Ja").Click();

        // Функцію відділу ADAICA не переносить: у «andere Tätigkeiten» співробітник знову «Mitarbeiter» (так задумано).
        var detached = moved with { Department = DefaultDepartment, Functions = [new EmployeeFunctionData(StandardFunction)] };
        EmployeeShouldBeIn(mainPage, company.Name, detached);
        EmployeeShouldNotBeIn(mainPage, company.Name, moved);
        EmployeeFunctionsShouldBe(mainPage, company.Name, detached);
    }

    /// <summary>
    /// «Primärer Briefempfänger» — основний одержувач листів відділу й фірми. Перший співробітник фірми стає ним для
    /// фірми й свого відділу, перший у відділі — для відділу; увімкнений прапорець недоступний, його лише передають
    /// іншому співробітнику («Mitarbeiter bearbeiten» чи «Mitarbeiter umziehen»), і в попереднього він знімається.
    /// Основного одержувача показує «Vorauswahl Korrespondenz → Mitarbeiter» у картках фірми й відділу. Співробітник,
    /// який переходить в інший відділ, лишається основним для фірми; у порожньому відділі він стає основним сам, а з
    /// «Mitarbeiter kopieren» — ще й лишається ним у старому відділі.
    /// </summary>
    [Test]
    [AllureSeverity(SeverityLevel.critical)]
    public void KontakteNavigator_PrimaryLetterRecipient()
    {
        var mainPage = LogInAndOpenNavigator("Kontakte");
        var customer = CustomerCase.Random();
        var company = customer.NewCompany();
        var departments = new[] { customer.NewDepartment(), customer.NewDepartment(), customer.NewDepartment() };
        var (first, second, third) = (customer.NewEmployee(), customer.NewEmployee(), customer.NewEmployee());

        CreateCompany(mainPage, company);
        CreateDepartmentWithEmployee(mainPage, company.Name, departments[0], first);
        CreateDepartmentEmployee(mainPage, company.Name, departments[0], second);
        CreateDepartmentWithEmployee(mainPage, company.Name, departments[1], third);
        CreateDepartment(mainPage, company.Name, departments[2]);

        EmployeeInDepartment In(EmployeeData employee, string department) =>
            new(employee, department, [new EmployeeFunctionData(StandardFunction)]);
        var (firstInA, secondInA, thirdInB) = (In(first, departments[0]), In(second, departments[0]), In(third, departments[1]));

        // Перший співробітник фірми й перший у відділі стають основними одержувачами самі.
        PrimaryRecipientShouldBe(mainPage, company.Name, firstInA, forDepartment: true, forCompany: true);
        PrimaryRecipientShouldBe(mainPage, company.Name, secondInA, forDepartment: false, forCompany: false);
        PrimaryRecipientShouldBe(mainPage, company.Name, thirdInB, forDepartment: true, forCompany: false);
        CompanyPrimaryRecipientShouldBe(mainPage, company.Name, firstInA);
        DepartmentPrimaryRecipientShouldBe(mainPage, company.Name, departments[0], firstInA);
        DepartmentPrimaryRecipientShouldBe(mainPage, company.Name, departments[1], thirdInB);
        DepartmentPrimaryRecipientShouldBe(mainPage, company.Name, departments[2], null);

        // «Mitarbeiter bearbeiten»: роль переходить до іншого співробітника.
        SetPrimaryRecipient(mainPage, company.Name, secondInA, forDepartment: true);
        PrimaryRecipientShouldBe(mainPage, company.Name, secondInA, forDepartment: true, forCompany: false);
        PrimaryRecipientShouldBe(mainPage, company.Name, firstInA, forDepartment: false, forCompany: true);
        DepartmentPrimaryRecipientShouldBe(mainPage, company.Name, departments[0], secondInA);

        SetPrimaryRecipient(mainPage, company.Name, thirdInB, forCompany: true);
        PrimaryRecipientShouldBe(mainPage, company.Name, thirdInB, forDepartment: true, forCompany: true);
        PrimaryRecipientShouldBe(mainPage, company.Name, firstInA, forDepartment: false, forCompany: false);
        CompanyPrimaryRecipientShouldBe(mainPage, company.Name, thirdInB);

        // «Mitarbeiter umziehen» з обома прапорцями: співробітник забирає обидві ролі.
        var firstInB = MoveEmployee(mainPage, company.Name, departments, firstInA, departments[1], [], inWindow: window =>
        {
            window.ShouldHavePrimaryRecipient(forDepartment: false, forCompany: false);
            window.PrimaryForDepartment.SetChecked(true);
            window.PrimaryForCompany.SetChecked(true);
        });
        PrimaryRecipientShouldBe(mainPage, company.Name, firstInB, forDepartment: true, forCompany: true);
        PrimaryRecipientShouldBe(mainPage, company.Name, thirdInB, forDepartment: false, forCompany: false);
        DepartmentPrimaryRecipientShouldBe(mainPage, company.Name, departments[1], firstInB);
        CompanyPrimaryRecipientShouldBe(mainPage, company.Name, firstInB);

        // Без прапорців основним одержувачем відділу лишається той, хто ним був.
        var thirdInA = MoveEmployee(mainPage, company.Name, departments, thirdInB, departments[0], [],
            inWindow: window => window.ShouldHavePrimaryRecipient(forDepartment: false, forCompany: false));
        PrimaryRecipientShouldBe(mainPage, company.Name, thirdInA, forDepartment: false, forCompany: false);
        DepartmentPrimaryRecipientShouldBe(mainPage, company.Name, departments[0], secondInA);

        // Основний одержувач фірми лишається ним і в іншому відділі; у порожньому відділі він стає основним сам.
        var firstInC = MoveEmployee(mainPage, company.Name, departments, firstInB, departments[2], [],
            inWindow: window => window.ShouldHavePrimaryRecipient(forDepartment: true, forCompany: true));
        PrimaryRecipientShouldBe(mainPage, company.Name, firstInC, forDepartment: true, forCompany: true);
        DepartmentPrimaryRecipientShouldBe(mainPage, company.Name, departments[2], firstInC);
        CompanyPrimaryRecipientShouldBe(mainPage, company.Name, firstInC);

        // Копія основного одержувача відділу в порожній відділ: він основний і там, і там.
        var secondInB = MoveEmployee(mainPage, company.Name, departments, secondInA, departments[1], [], copy: true,
            inWindow: window => window.ShouldHavePrimaryRecipient(forDepartment: true, forCompany: false));
        PrimaryRecipientShouldBe(mainPage, company.Name, secondInB, forDepartment: true, forCompany: false);
        PrimaryRecipientShouldBe(mainPage, company.Name, secondInA, forDepartment: true, forCompany: false);
        DepartmentPrimaryRecipientShouldBe(mainPage, company.Name, departments[1], secondInB);
        DepartmentPrimaryRecipientShouldBe(mainPage, company.Name, departments[0], secondInA);
    }

    /// <summary>
    /// Коли основний одержувач листів відділу йде з відділу, роль переходить до співробітника, що лишився. Після
    /// «Mitarbeiter aus der Organisationseinheit lösen» ADAICA так і робить, а після «Mitarbeiter umziehen» — ні:
    /// старий відділ лишається без основного одержувача. Це баг ADAICA, тест падає навмисно (README, «Тести, що падають
    /// навмисно»).
    /// </summary>
    [Test]
    [AllureSeverity(SeverityLevel.critical)]
    public void KontakteNavigator_PrimaryRecipientPassesToRemainingEmployee()
    {
        var mainPage = LogInAndOpenNavigator("Kontakte");
        var customer = CustomerCase.Random();
        var company = customer.NewCompany();
        var departments = new[] { customer.NewDepartment(), customer.NewDepartment() };
        var employees = Enumerable.Range(0, 4).Select(_ => customer.NewEmployee()).ToArray();

        CreateCompany(mainPage, company);
        CreateDepartmentWithEmployee(mainPage, company.Name, departments[0], employees[0]);
        CreateDepartmentEmployee(mainPage, company.Name, departments[0], employees[1]);
        CreateDepartmentWithEmployee(mainPage, company.Name, departments[1], employees[2]);
        CreateDepartmentEmployee(mainPage, company.Name, departments[1], employees[3]);

        EmployeeInDepartment In(EmployeeData employee, string department) =>
            new(employee, department, [new EmployeeFunctionData(StandardFunction)]);
        var (firstInA, secondInA) = (In(employees[0], departments[0]), In(employees[1], departments[0]));
        var (thirdInB, fourthInB) = (In(employees[2], departments[1]), In(employees[3], departments[1]));
        DepartmentPrimaryRecipientShouldBe(mainPage, company.Name, departments[0], firstInA);
        DepartmentPrimaryRecipientShouldBe(mainPage, company.Name, departments[1], thirdInB);

        // «lösen»: основним одержувачем відділу стає той, хто лишився.
        var question = mainPage.DetachEmployeeFromDepartment(thirdInB.Path(company.Name));
        question.Button("Ja").Click();
        EmployeeShouldBeIn(mainPage, company.Name, thirdInB with { Department = DefaultDepartment });
        PrimaryRecipientShouldBe(mainPage, company.Name, fourthInB, forDepartment: true, forCompany: false);
        DepartmentPrimaryRecipientShouldBe(mainPage, company.Name, departments[1], fourthInB);

        // «Mitarbeiter umziehen»: так само має бути й тут, але ADAICA лишає відділ без основного одержувача (баг).
        var firstInB = MoveEmployee(mainPage, company.Name, departments, firstInA, departments[1], []);
        PrimaryRecipientShouldBe(mainPage, company.Name, firstInB, forDepartment: false, forCompany: true);
        PrimaryRecipientShouldBe(mainPage, company.Name, secondInA, forDepartment: true, forCompany: false);
        DepartmentPrimaryRecipientShouldBe(mainPage, company.Name, departments[0], secondInA);
    }

    /// <summary>Співробітник у відділі та його функції там.</summary>
    private sealed record EmployeeInDepartment(EmployeeData Employee, string Department, IReadOnlyList<EmployeeFunctionData> Functions)
    {
        /// <summary>Вузол співробітника під відділом.</summary>
        public string Node => Employee.NodeNameWith(Functions.Select(function => function.Function));

        /// <summary>Шлях до вузла співробітника в дереві: «Фірма», «Mitarbeiter», «Відділ», «Співробітник».</summary>
        public string[] Path(string companyName) => [companyName, EmployeesNode, Department, Node];
    }

    /// <summary>
    /// Довідник Mitarbeiterfunktion (картка фірми → «Konfiguration der Liste von Kommunikationsarten» → «Firma»):
    /// додає функції <see cref="CustomerCase.EmployeeFunctions"/>, яких ще немає. Якщо всі вже є і
    /// <paramref name="addNew"/>, додає одну нову (<see cref="CustomerCase.NewEmployeeFunction"/>). Перезапуск ADAICA,
    /// про який вона пише після збереження, не потрібен.
    /// </summary>
    /// <returns>Додані функції.</returns>
    private IReadOnlyList<string> AddEmployeeFunctions(CompanyForm companyForm, CustomerCase customer, bool addNew) =>
        AllureApi.Step("Доповнити довідник Mitarbeiterfunktion", () =>
        {
            var window = companyForm.OpenContactDefaults();
            OnCleanup(window.CancelIfOpen);
            window.OpenCompanyList("Mitarbeiterfunktion");

            var existing = window.CompanyItems;
            var missing = CustomerCase.EmployeeFunctions.Except(existing).ToList();
            IReadOnlyList<string> added = missing.Count > 0 ? missing : addNew ? [customer.NewEmployeeFunction()] : [];
            if (added.Count == 0)
            {
                window.CancelIfOpen();
                return added;
            }

            foreach (var function in added)
                window.AddCompanyItem(function);
            AllureApi.Step("Перевірити функції в довіднику", () =>
                window.CompanyItems.ShouldBe(existing.Concat(added), ignoreOrder: true, "Mitarbeiterfunktion"));
            window.Save();
            return added;
        });

    /// <summary>
    /// Співробітник фірми без відділу: вузол «Mitarbeiter» → «Mitarbeiter zur Firma hinzufügen» → «Finden &amp; Erstellen»
    /// → «Neue Person/Firma» з відділом «andere Tätigkeiten».
    /// </summary>
    private EmployeeInDepartment CreateCompanyEmployee(MainPage mainPage, string companyName, EmployeeData employee)
    {
        var findCreateWindow = mainPage.OpenNewCompanyEmployeeWindow(companyName);
        OnCleanup(findCreateWindow.CloseIfOpen);

        findCreateWindow.Person.Fill($"{employee.FirstName} {employee.LastName}");
        findCreateWindow.Action.WaitForText("Person erstellen");

        var personDialog = findCreateWindow.OpenNewPersonDialog();
        OnCleanup(personDialog.CancelIfOpen);
        personDialog.Fill(employee);
        personDialog.ShouldHaveValues(employee, companyName, DefaultDepartment);
        personDialog.Save();

        findCreateWindow.WaitUntilClosed();
        mainPage.WaitForOpenedPerson(employee.FullName).ShouldHaveValues(employee);
        var created = new EmployeeInDepartment(employee, DefaultDepartment, [new EmployeeFunctionData(employee.Function)]);
        SelectedEmployeeShouldBe(mainPage, companyName, created);
        return created;
    }

    /// <summary>
    /// «Mitarbeiter umziehen» з відділу <paramref name="source"/> у <paramref name="to"/>. У списку відділів — усі
    /// <paramref name="departments"/>, крім поточного (і без «andere Tätigkeiten»). Таблиця пропонує поточні функції як
    /// нові; кожна з <paramref name="functions"/> додається з перевіркою статусу (перша замінює стандартну «Mitarbeiter»). Після OK
    /// співробітник має бути виділений у новому відділі, зникнути зі старого (або лишитися там, якщо
    /// <paramref name="copy"/>), а «Mitarbeiter bearbeiten» — показати збережені функції.
    /// </summary>
    /// <param name="inWindow">Що ще зробити у вікні перед OK, наприклад перевірити чи ввімкнути «Primärer Briefempfänger…».</param>
    private EmployeeInDepartment MoveEmployee(
        MainPage mainPage,
        string companyName,
        IReadOnlyList<string> departments,
        EmployeeInDepartment source,
        string to,
        IReadOnlyList<EmployeeFunctionData> functions,
        bool copy = false,
        Action<EmployeeMoveWindow>? inWindow = null) =>
        AllureApi.Step($"Перемістити «{source.Node}» з «{source.Department}» у «{to}»{(copy ? " (копія)" : string.Empty)}", () =>
        {
            var window = mainPage.OpenEmployeeMoveWindow(source.Path(companyName));
            OnCleanup(window.CancelIfOpen);

            window.SourceDepartment.ShouldHaveText(source.Department);
            var targets = departments.Where(department => department != source.Department).ToList();
            AllureApi.Step($"Перевірити список «in Organisationseinheit»: {string.Join(", ", targets)}", () =>
                window.TargetDepartment.Items.ShouldBe(targets, ignoreOrder: true, "Список «in Organisationseinheit»"));
            window.TargetDepartment.Select(to);

            if (source.Department == DefaultDepartment)
                window.CopyEmployee.ShouldBeDisabled();

            window.Functions.ShouldHaveFunctions(source.Functions, isNew: true);
            foreach (var function in functions)
                window.Functions.Add(function);
            // Стандартну «Mitarbeiter» перша додана функція замінює, інші запропоновані функції лишаються.
            var expected = functions.Count > 0
                ? source.Functions.Where(function => function.Function != StandardFunction).Concat(functions).ToList()
                : source.Functions;
            window.Functions.ShouldHaveFunctions(expected, isNew: true);

            window.CopyEmployee.SetChecked(copy);
            inWindow?.Invoke(window);
            window.Confirm();

            var moved = source with { Department = to, Functions = expected };
            SelectedEmployeeShouldBe(mainPage, companyName, moved);
            EmployeeShouldBeIn(mainPage, companyName, moved);
            if (copy)
                EmployeeShouldBeIn(mainPage, companyName, source);
            else
                EmployeeShouldNotBeIn(mainPage, companyName, source);
            EmployeeFunctionsShouldBe(mainPage, companyName, moved);
            return moved;
        });

    /// <summary>«Mitarbeiter bearbeiten» → прапорці «Primärer Briefempfänger…» співробітника; вікно закривається без змін.</summary>
    private void PrimaryRecipientShouldBe(MainPage mainPage, string companyName, EmployeeInDepartment employee, bool forDepartment, bool forCompany) =>
        AllureApi.Step($"Перевірити «Primärer Briefempfänger» «{employee.Node}» у «{employee.Department}»", () =>
        {
            var window = mainPage.OpenEmployeeEditWindow(employee.Path(companyName));
            OnCleanup(window.CancelIfOpen);
            window.ShouldHavePrimaryRecipient(forDepartment, forCompany);
            window.Cancel();
        });

    /// <summary>«Mitarbeiter bearbeiten» → увімкнути прапорець «Primärer Briefempfänger…» → OK.</summary>
    private void SetPrimaryRecipient(
        MainPage mainPage, string companyName, EmployeeInDepartment employee, bool forDepartment = false, bool forCompany = false) =>
        AllureApi.Step($"Зробити «{employee.Node}» основним одержувачем {(forDepartment ? "відділу" : "фірми")}", () =>
        {
            var window = mainPage.OpenEmployeeEditWindow(employee.Path(companyName));
            OnCleanup(window.CancelIfOpen);
            if (forDepartment)
                window.PrimaryForDepartment.SetChecked(true);
            if (forCompany)
                window.PrimaryForCompany.SetChecked(true);
            window.Confirm();
        });

    /// <summary>Картка фірми: «Vorauswahl Korrespondenz → Mitarbeiter» — основний одержувач листів фірми.</summary>
    private static void CompanyPrimaryRecipientShouldBe(MainPage mainPage, string companyName, EmployeeInDepartment employee) =>
        AllureApi.Step($"Перевірити, що основний одержувач фірми — «{employee.Node}»", () =>
        {
            mainPage.SelectNavigatorNode(companyName);
            mainPage.WaitForOpenedCompany(companyName).CorrespondenceEmployee.ShouldHaveSelected(employee.Node);
        });

    /// <summary>Картка відділу: «Vorauswahl Korrespondenz → Mitarbeiter»; null — основного одержувача немає.</summary>
    private static void DepartmentPrimaryRecipientShouldBe(MainPage mainPage, string companyName, string department, EmployeeInDepartment? employee) =>
        AllureApi.Step($"Перевірити, що основний одержувач відділу «{department}» — «{employee?.Node ?? "немає"}»", () =>
        {
            mainPage.SelectNavigatorNodeAt(companyName, EmployeesNode, department);
            mainPage.WaitForOpenedDepartment(department).CorrespondenceEmployee.ShouldHaveSelected(employee?.Node);
        });

    /// <summary>«Mitarbeiter bearbeiten» показує функції співробітника в його відділі; вікно закривається без змін.</summary>
    private void EmployeeFunctionsShouldBe(MainPage mainPage, string companyName, EmployeeInDepartment employee) =>
        AllureApi.Step($"Перевірити функції «{employee.Node}» у «{employee.Department}»", () =>
        {
            var window = mainPage.OpenEmployeeEditWindow(employee.Path(companyName));
            OnCleanup(window.CancelIfOpen);
            window.Functions.ShouldHaveFunctions(employee.Functions);
            window.Cancel();
        });

    private static void SelectedEmployeeShouldBe(MainPage mainPage, string companyName, EmployeeInDepartment employee)
    {
        var path = new[] { companyName, EmployeesNode, employee.Department, employee.Node };
        AllureApi.Step($"Перевірити, що в дереві виділено «{string.Join(" → ", path)}»",
            () => mainPage.SelectedNavigatorNodePath.TakeLast(4).ShouldBe(path));
    }

    // Дерево ADAICA оновлює не одразу після збереження, тому чекаємо.
    private static void EmployeeShouldBeIn(MainPage mainPage, string companyName, EmployeeInDepartment employee) =>
        AllureApi.Step($"Перевірити, що «{employee.Node}» є у відділі «{employee.Department}»", () =>
            Wait.Until(
                () => mainPage.ChildNodeNames(companyName, EmployeesNode, employee.Department).Contains(employee.Node),
                $"«{employee.Node}» у відділі «{employee.Department}»"));

    private static void EmployeeShouldNotBeIn(MainPage mainPage, string companyName, EmployeeInDepartment employee) =>
        AllureApi.Step($"Перевірити, що «{employee.Employee.SortName}» немає у відділі «{employee.Department}»", () =>
            Wait.Until(
                () => !mainPage.ChildNodeNames(companyName, EmployeesNode, employee.Department)
                    .Any(node => node.StartsWith($"{employee.Employee.SortName} (", StringComparison.Ordinal)),
                $"зникнення «{employee.Employee.SortName}» з відділу «{employee.Department}»"));
}
