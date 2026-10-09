using ADAICA_AUTOTEST.Pages;
using Allure.Net.Commons;

namespace ADAICA_AUTOTEST.Tests;

/// <summary>
/// Спільне для тестів пропозицій завдань і термінів («Vorschläge für Verknüpfung der Dokumentation»): записи під
/// контактом з потрібними датами, налаштування вкладок <see cref="SuggestionTab"/> з поверненням після тесту і перевірка
/// пропозицій на вкладці «Dokumentation» панелі зв'язків (вікно збереження листа чи нового запису).
/// </summary>
public abstract partial class AdaicaTestBase
{
    // Пропозиції ADAICA додає у фоні, коли контакт з'являється на вкладці «Kontakte», тож перший прочитаний список
    // може бути ще неповним.
    private static readonly TimeSpan SuggestionsSettle = TimeSpan.FromSeconds(3);

    // Налаштування вкладок до тесту («Systemweit» і «Anwender»), які поверне прибирання (див. UseLinkSuggestions).
    private readonly Dictionary<SuggestionTab, (LinkSuggestionOptions System, LinkSuggestionOptions User)> _savedSuggestions = [];

    /// <summary>
    /// Нова персона з адресою <paramref name="email"/>: Kontakte → Gesamtliste → «Neue Person» → «Finden &amp; Erstellen»
    /// (ім'я й e-mail) → «Neu» → «Neue Person/Firma» → OK. ADAICA виділяє її в «Aktuelle». Повертає її Briefanrede.
    /// </summary>
    protected string CreatePersonWithEmail(MainPage mainPage, EmployeeData person, string email) =>
        AllureApi.Step($"Створити персону {person.SortName} з e-mail {email}", () =>
        {
            var findCreateWindow = mainPage.OpenNewPersonWindow();
            OnCleanup(findCreateWindow.CloseIfOpen);
            findCreateWindow.Person.Fill(person.PrefilledName());
            findCreateWindow.Email.Fill(email);
            findCreateWindow.Action.WaitForText("Person erstellen");

            var personDialog = findCreateWindow.OpenNewPersonDialog();
            OnCleanup(personDialog.CancelIfOpen);
            personDialog.Salutation.Select(person.Salutation);
            var letterSalutation = personDialog.LetterSalutation.Text;
            personDialog.Save();
            findCreateWindow.WaitUntilClosed();
            return letterSalutation;
        });

    /// <summary>
    /// Співробітник нової фірми з адресою: Gesamtliste → «Neue Person» → у рядку пошуку персона, фірма й e-mail →
    /// «Neue Person/Firma» → OK. ADAICA кладе його у відділ «andere Tätigkeiten» і виділяє в «Aktuelle → Фірма → …».
    /// </summary>
    protected (EmployeeData Employee, string Email, string Company) CreateEmployeeWithEmail(MainPage mainPage, CustomerCase customer)
    {
        var employee = customer.NewMailPerson();
        var email = CustomerCase.NewMailAddress(employee);
        var company = customer.NewCompany().Name;
        return AllureApi.Step($"Створити співробітника {employee.SortName} <{email}> нової фірми {company}", () =>
        {
            var findCreateWindow = mainPage.OpenNewPersonWindow();
            OnCleanup(findCreateWindow.CloseIfOpen);
            findCreateWindow.Person.Fill(employee.PrefilledName());
            findCreateWindow.Company.Fill(company);
            findCreateWindow.Email.Fill(email);
            findCreateWindow.Action.WaitForText("Firma erstellen, Person erstellen, Mitarbeiter zur Firma hinzufügen");

            var personDialog = findCreateWindow.OpenNewPersonDialog();
            OnCleanup(personDialog.CancelIfOpen);
            personDialog.Salutation.Select(employee.Salutation);
            personDialog.Save();
            findCreateWindow.WaitUntilClosed();
            return (employee, email, company);
        });
    }

    /// <summary>Завдання під контактом <paramref name="parentNode"/> (меню вузла «Neu» → «Aufgabe»). Повертає його назву.</summary>
    protected string CreateContactTask(
        MainPage mainPage, CustomerCase customer, string parentNode, DateTime begin, DateTime? done = null, DateTime? closedOn = null)
    {
        var dialog = mainPage.OpenNewChildTaskDialog(parentNode);
        OnCleanup(dialog.CancelIfOpen);
        var task = customer.NewContactTask(begin, done, closedOn);
        dialog.Form.Fill(task);
        var number = dialog.Form.Number.Text;
        dialog.Save();
        mainPage.WaitForOpenedTask(number);
        return task.Name;
    }

    /// <summary>Двогодинний термін у день <paramref name="day"/> під контактом <paramref name="parentNode"/>.</summary>
    protected ContactAppointment CreateContactAppointment(MainPage mainPage, CustomerCase customer, string parentNode, DateTime day)
    {
        var dialog = mainPage.OpenNewChildAppointmentDialog(parentNode);
        OnCleanup(dialog.CancelIfOpen);
        var random = customer.NewAppointment();
        var begin = day.Date + random.Begin.TimeOfDay;
        var appointment = random with { Begin = begin, End = begin + (random.End - random.Begin) };
        dialog.Form.Fill(appointment);
        var number = dialog.Form.Number.Text;
        dialog.Save();
        mainPage.WaitForOpenedAppointment(number);
        return new ContactAppointment(AppointmentNode(appointment), appointment.Begin);
    }

    /// <summary>
    /// Ставить налаштування пропозицій вкладки <paramref name="tab"/> у колонці «Anwender» (і, якщо задано
    /// <paramref name="systemTasks"/>, «Aufgaben vorschlagen» у колонці «Systemweit» — до користувацьких).
    /// </summary>
    protected void UseLinkSuggestions(MainPage mainPage, SuggestionTab tab, LinkSuggestionOptions options, bool? systemTasks = null) =>
        UseLinkSuggestions(mainPage, new Dictionary<SuggestionTab, LinkSuggestionOptions> { [tab] = options }, systemTasks);

    /// <summary>
    /// Ставить налаштування пропозицій кількох вкладок за одне відкриття «Einstellungen». Кожну вкладку вперше
    /// запам'ятовує (обидві колонки): після тесту прибирання повертає системний прапорець «Aufgaben vorschlagen», а
    /// «Anwender» — власне значення користувача або, якщо він збігався із системним, знову успадкування від «Systemweit».
    /// </summary>
    protected void UseLinkSuggestions(
        MainPage mainPage, IReadOnlyDictionary<SuggestionTab, LinkSuggestionOptions> options, bool? systemTasks = null)
    {
        var window = mainPage.OpenOptions();
        OnCleanup(window.CloseIfOpen);
        foreach (var (tab, tabOptions) in options)
        {
            window.OpenLinkSuggestions(tab);
            if (!_savedSuggestions.ContainsKey(tab))
            {
                if (_savedSuggestions.Count == 0)
                    OnCleanup(RestoreLinkSuggestions);
                _savedSuggestions[tab] = (window.ReadLinkSuggestions(user: false), window.ReadLinkSuggestions(user: true));
            }

            if (systemTasks is { } enabled)
                window.SetSystemTaskSuggestions(enabled);
            window.SetLinkSuggestions(tabOptions);
        }

        window.SaveChanges();
    }

    /// <summary>
    /// Перевіряє записи на вкладці «Dokumentation» панелі <paramref name="links"/> з рядка <paramref name="fromRow"/>:
    /// рівно <paramref name="expected"/> (підписи, як у дереві), у будь-якому порядку. Повертає їх у порядку рядків. Якщо
    /// список ще не такий, читає його ще раз за кілька секунд: ADAICA додає й прибирає пропозиції не одразу.
    /// </summary>
    protected static IReadOnlyList<string> SuggestionsShouldBe(RecordLinksPanel links, int fromRow, params string[] expected) =>
        AllureApi.Step($"Перевірити пропозиції на вкладці «Dokumentation»: {(expected.Length == 0 ? "немає" : string.Join("; ", expected))}", () =>
        {
            var rows = links.RecordRowLabels(fromRow);
            if (!rows.Order().SequenceEqual(expected.Order()))
            {
                Thread.Sleep(SuggestionsSettle);
                rows = links.RecordRowLabels(fromRow);
            }

            rows.ShouldBe(expected, ignoreOrder: true, "Записи на вкладці «Dokumentation»");
            return rows;
        });

    private void RestoreLinkSuggestions()
    {
        var saved = _savedSuggestions.ToList();
        _savedSuggestions.Clear();
        var window = new MainPage(Session).OpenOptions();
        foreach (var (tab, (system, user)) in saved)
        {
            window.OpenLinkSuggestions(tab);
            window.SetSystemTaskSuggestions(system.Tasks.Enabled);
            window.RestoreUserLinkSuggestions(system, user);
        }

        window.SaveChanges();
    }

    /// <summary>Термін під контактом: підпис у дереві й на вкладці «Dokumentation» і початок.</summary>
    protected sealed record ContactAppointment(string Label, DateTime Begin);
}
