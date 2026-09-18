using System.Globalization;
using ADAICA_AUTOTEST.Pages;

namespace ADAICA_AUTOTEST.Tests;

/// <summary>
/// Справа одного клієнта, з якої тести беруть дані записів. Назви й примітки звучать так, наче їх увела людина
/// (німецькою, як працюють користувачі ADAICA), і щоразу інші: у базі накопичуються різні записи,
/// на яких можна перевіряти повнотекстовий пошук за назвами й примітками. Усі записи однієї справи
/// стосуються того самого клієнта й теми, як у реальній роботі. Вибрані значення видно в кроках звіту Allure.
/// Дати — різні дні в межах 60 днів від сьогодні в минуле й майбутнє, час — робочі години з кроком 15 хвилин.
/// </summary>
/// <remarks>
/// Назви можуть повторитися в іншому прогоні, тому свій запис тест і далі впізнає за номером.
/// Kurzname «MAT» у всіх записах однаковий: за ним тестові записи можна відрізнити від уведених людьми.
/// </remarks>
public sealed class CustomerCase
{
    private const string ShortName = "MAT";

    // Межа дат записів у днях від сьогодні, в минуле й майбутнє.
    private const int DateRangeDays = 60;

    // Дні, уже зайняті датами записів у цьому прогоні тестів, щоб дати різних тестів теж не збігалися.
    private static readonly HashSet<DateTime> DaysUsedInRun = [];

    private static readonly Customer[] Customers =
    [
        new("Keller Treuhand AG", "Winterthur", new Person("Keller", IsWoman: true)),
        new("Bäckerei Huber GmbH", "St. Gallen", new Person("Huber", IsWoman: false)),
        new("Schreinerei Brunner", "Luzern", new Person("Brunner", IsWoman: false)),
        new("Praxis Dr. Meier", "Zürich", new Person("Dr. Meier", IsWoman: true)),
        new("Gemeindeverwaltung Wil", "Wil", new Person("Fässler", IsWoman: false)),
        new("Garage Steiner AG", "Chur", new Person("Steiner", IsWoman: false)),
        new("Hotel Seeblick", "Thun", new Person("Gerber", IsWoman: true)),
        new("Müller Logistik AG", "Basel", new Person("Müller", IsWoman: true)),
    ];

    private static readonly Topic[] Topics =
    [
        new("Umbau Empfangsbereich", "den Umbau des Empfangsbereichs"),
        new("Einführung Zeiterfassung", "die Einführung der Zeiterfassung"),
        new("Umzug ins neue Lager", "den Umzug ins neue Lager"),
        new("Neue Website", "die neue Website"),
        new("Jahresabschluss 2026", "den Jahresabschluss 2026"),
        new("Sanierung Heizung", "die Sanierung der Heizung"),
        new("Wechsel der Buchhaltungssoftware", "den Wechsel der Buchhaltungssoftware"),
    ];

    private readonly Customer _customer;
    private readonly Topic _topic;
    private readonly HashSet<DateTime> _usedDays = [];

    private CustomerCase(Customer customer, Topic topic)
    {
        _customer = customer;
        _topic = topic;
    }

    public static CustomerCase Random() => new(Pick(Customers), Pick(Topics));

    public ProjectData NewProject()
    {
        var days = UniqueDays(2);
        return new ProjectData(
            Name: $"{_topic.Title} – {_customer.Company}",
            ShortName: ShortName,
            BeginDate: days[0],
            EndDate: days[1],
            Cost: 1500m,
            CostCurrency: "USD",
            Outlay: 8m,
            OutlayUnit: "Tag(e)",
            Comment: Pick(
                $"Auftrag von {Contact.Dative} am Telefon bestätigt. Budget und Termine sind mit der Geschäftsleitung abgestimmt.",
                $"Ansprechperson: {Contact.Nominative}, {_customer.Company}. Rechnungen bitte immer an die Buchhaltung senden.",
                "Der Kunde wünscht jeden Monat einen kurzen Statusbericht per E-Mail."));
    }

    /// <summary>Етап того самого проєкту, для підпроєкту.</summary>
    public ProjectData NewSubproject() =>
        NewProject() with
        {
            Name = $"{_topic.Title}: {Pick("Planung", "Umsetzung", "Abnahme", "Schulung der Mitarbeitenden")}",
            Comment = Pick(
                "Die Abrechnung läuft über das Hauptprojekt.",
                $"Etappe mit {Contact.Dative} abgesprochen, Start nach Freigabe des Budgets."),
        };

    public TaskData NewTask()
    {
        // Початок — найраніший день; виконання раніше за закриття; термін «Fällig am» — будь-коли після початку.
        var days = UniqueDays(4);
        var due = Pick(1, 2, 3);
        var (done, closed) = due switch { 1 => (2, 3), 2 => (1, 3), _ => (1, 2) };
        return new TaskData(
            Name: Pick(
                $"Offerte für {_topic.Object} erstellen",
                $"Kosten für {_topic.Object} mit {Contact.Dative} besprechen",
                $"Unterlagen für {_topic.Object} zusammenstellen",
                $"Terminplan für {_topic.Object} aktualisieren"),
            ShortName: ShortName,
            Begin: days[0] + WorkingTime(),
            Due: days[due] + WorkingTime(),
            // Дати виконання й закриття переводять завдання в статус «Aufgabe erledigt».
            Done: days[done] + WorkingTime(),
            ClosedOn: days[closed],
            AcceptanceType: "Automatisch",
            Priority: "hoch",
            Importance: "mittel",
            EstimatedDuration: 2m,
            EstimatedDurationUnit: "Tag(e)",
            ActualDuration: 6m,
            ActualDurationUnit: "Stunde(n)",
            OrderNumber: $"A-{days[0].Year}-{System.Random.Shared.Next(100, 1000)}",
            PurchaseOrderNumber: $"B-{System.Random.Shared.Next(10000, 100000)}",
            PercentDone: 50m,
            Location: "Unbekannt",
            LocationText: Pick($"Büro {_customer.City}", "Homeoffice"),
            CustomerBenefit: "Unbekannt",
            CustomerBenefitText: Pick("Weniger Aufwand im Alltag", "Schnellere Abläufe", "Klare Kosten von Anfang an"),
            Hint: "WF starten",
            HintText: Pick($"Rücksprache mit {Contact.Dative}", "Unterschrift der Geschäftsleitung nötig", "Dringend, der Kunde wartet"),
            Remark: Pick(
                $"{Contact.Nominative} braucht die Unterlagen bis Ende Monat. Bitte zwei Varianten rechnen: mit und ohne Montage.",
                "Vorlage vom letzten Projekt verwenden und die Preise für das neue Jahr anpassen.",
                "Vor dem Versand kurz mit der Projektleitung abstimmen."));
    }

    public AppointmentData NewAppointment()
    {
        // Термін триває 2 години й закінчується того самого дня.
        var begin = UniqueDays(1)[0] + WorkingTime(latestHour: 16);
        return new AppointmentData(
            Name: Pick(
                $"Kick-off-Meeting {_topic.Title}",
                $"Besprechung mit {Contact.Dative}",
                $"Begehung vor Ort in {_customer.City}",
                $"Zwischenstand {_topic.Title}"),
            ShortName: ShortName,
            Begin: begin,
            End: begin.AddHours(2),
            AllDay: false,
            // Трудовитрати дорівнюють тривалості терміну, тому ADAICA не показує попередження.
            Effort: 120m,
            EffortUnit: "Minute(n)",
            Importance: "hoch",
            Location: Pick($"{_customer.City}, Sitzungszimmer 2", $"{_customer.Company}, {_customer.City}", "Hauptsitz, grosses Sitzungszimmer"),
            Remark: Pick(
                $"Treffpunkt beim Haupteingang in {_customer.City}. Pläne und Muster mitnehmen.",
                $"Parkplätze sind knapp, besser mit dem Zug anreisen. Das Protokoll schreibt {Contact.Nominative}.",
                "Laptop und Beamer sind vor Ort, die Präsentation vorher per E-Mail schicken."));
    }

    /// <summary>
    /// Номера в інфозапису немає, тож тест знаходить його за «Bemerkung». Щоб текст відрізнявся
    /// від прогону до прогону, примітка закінчується часом запису, як це часто пишуть люди.
    /// </summary>
    public NoteData NewNote()
    {
        var now = DateTime.Now;
        return new NoteData(
            Name: Pick(
                $"Telefonat mit {Contact.Dative}",
                $"Rückfrage von {Contact.Dative}",
                $"E-Mail von {Contact.Dative}",
                $"Gesprächsnotiz {_topic.Title}"),
            ShortName: ShortName,
            // Дата інфозапису не може бути пізнішою за сьогодні, тому лише минулі дні.
            Begin: UniqueDays(1, toDay: -1)[0] + WorkingTime(),
            // У BLANK_IGOR список «Ergebnis» порожній.
            Result: null,
            Remark: Pick(
                    $"{Contact.Nominative} hat angerufen: Der Termin passt, die Unterlagen bitte vorab per E-Mail schicken.",
                    "Der Kunde ist mit dem Angebot grundsätzlich einverstanden, wünscht aber 5 % Rabatt.",
                    $"Die Lieferung verzögert sich um eine Woche, {Contact.Nominative} ist informiert.")
                + $" Notiert am {now.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture)} um {now.ToString("HH:mm", CultureInfo.InvariantCulture)} Uhr.");
    }

    private Person Contact => _customer.Contact;

    /// <summary>
    /// <paramref name="count"/> різних днів за зростанням, від <paramref name="fromDay"/> до <paramref name="toDay"/>
    /// днів від сьогодні. Дні не повторюються в межах справи, а поки вільних днів вистачає, — і в межах прогону.
    /// </summary>
    private DateTime[] UniqueDays(int count, int fromDay = -DateRangeDays, int toDay = DateRangeDays)
    {
        var candidates = Enumerable.Range(fromDay, toDay - fromDay + 1)
            .Select(offset => DateTime.Today.AddDays(offset))
            .Where(day => !_usedDays.Contains(day))
            .ToArray();

        lock (DaysUsedInRun)
        {
            var fresh = candidates.Where(day => !DaysUsedInRun.Contains(day)).ToArray();
            var pool = fresh.Length >= count ? fresh : candidates;
            if (pool.Length < count)
                throw new InvalidOperationException($"Бракує вільних днів для {count} дат справи «{_customer.Company}».");

            System.Random.Shared.Shuffle(pool);
            var days = pool.Take(count).Order().ToArray();
            DaysUsedInRun.UnionWith(days);
            _usedDays.UnionWith(days);
            return days;
        }
    }

    // Час, який людина вибрала б сама: робочі години з 07:00, кратно 15 хвилинам.
    private static TimeSpan WorkingTime(int latestHour = 17) =>
        TimeSpan.FromMinutes(System.Random.Shared.Next(7 * 4, latestHour * 4 + 1) * 15);

    private static T Pick<T>(params T[] items) => items[System.Random.Shared.Next(items.Length)];

    private sealed record Customer(string Company, string City, Person Contact);

    /// <param name="Object">Тема в знахідному відмінку, для «Offerte für …».</param>
    private sealed record Topic(string Title, string Object);

    private sealed record Person(string Surname, bool IsWoman)
    {
        public string Nominative => $"{(IsWoman ? "Frau" : "Herr")} {Surname}";

        /// <summary>Після «mit», «von»: «Herrn Huber».</summary>
        public string Dative => $"{(IsWoman ? "Frau" : "Herrn")} {Surname}";
    }
}
