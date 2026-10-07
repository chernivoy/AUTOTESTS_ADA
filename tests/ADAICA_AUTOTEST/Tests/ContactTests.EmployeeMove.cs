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

        EmployeeFunctionsShouldBe(mainPage, original);
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

        var question = mainPage.DetachEmployeeFromDepartment(moved.Node);
        AllureApi.Step($"Перевірити питання: «Beziehung lösen» з іменем «{employee.LastName} {employee.FirstName}»", () =>
            question.ShouldSatisfyAllConditions(
                () => question.Title.ShouldBe("Beziehung lösen"),
                () => question.Message.Text.ShouldContain($"{employee.LastName} {employee.FirstName}")));
        question.Button("Ja").Click();

        // Функцію відділу ADAICA не переносить: у «andere Tätigkeiten» співробітник знову «Mitarbeiter» (поведінку ще
        // має підтвердити розробник, див. README «Переміщення співробітників»).
        var detached = moved with { Department = DefaultDepartment, Functions = [new EmployeeFunctionData(StandardFunction)] };
        EmployeeShouldBeIn(mainPage, company.Name, detached);
        EmployeeShouldNotBeIn(mainPage, company.Name, moved);
        EmployeeFunctionsShouldBe(mainPage, detached);
    }

    /// <summary>Співробітник у відділі та його функції там.</summary>
    private sealed record EmployeeInDepartment(EmployeeData Employee, string Department, IReadOnlyList<EmployeeFunctionData> Functions)
    {
        /// <summary>Вузол співробітника під відділом.</summary>
        public string Node => Employee.NodeNameWith(Functions.Select(function => function.Function));
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
    private EmployeeInDepartment MoveEmployee(
        MainPage mainPage,
        string companyName,
        IReadOnlyList<string> departments,
        EmployeeInDepartment source,
        string to,
        IReadOnlyList<EmployeeFunctionData> functions,
        bool copy = false) =>
        AllureApi.Step($"Перемістити «{source.Node}» з «{source.Department}» у «{to}»{(copy ? " (копія)" : string.Empty)}", () =>
        {
            var window = mainPage.OpenEmployeeMoveWindow(source.Node);
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
            window.Confirm();

            var moved = source with { Department = to, Functions = expected };
            SelectedEmployeeShouldBe(mainPage, companyName, moved);
            EmployeeShouldBeIn(mainPage, companyName, moved);
            if (copy)
                EmployeeShouldBeIn(mainPage, companyName, source);
            else
                EmployeeShouldNotBeIn(mainPage, companyName, source);
            EmployeeFunctionsShouldBe(mainPage, moved);
            return moved;
        });

    /// <summary>«Mitarbeiter bearbeiten» показує функції співробітника в його відділі; вікно закривається без змін.</summary>
    private void EmployeeFunctionsShouldBe(MainPage mainPage, EmployeeInDepartment employee) =>
        AllureApi.Step($"Перевірити функції «{employee.Node}» у «{employee.Department}»", () =>
        {
            var window = mainPage.OpenEmployeeEditWindow(employee.Node);
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
