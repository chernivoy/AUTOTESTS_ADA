using System.Drawing;
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

    private static readonly Lock CustomerNumberLock = new();

    // Останній номер клієнта, виданий у цьому прогоні (див. NewCompany).
    private static long _lastCustomerNumber;

    private static readonly Customer[] Customers =
    [
        new("Keller Treuhand AG", "Winterthur", new Person("Keller", IsWoman: true), "Treuhand und Steuerberatung", "Zürich", "52", "8400", "keller-treuhand.ch"),
        new("Bäckerei Huber GmbH", "St. Gallen", new Person("Huber", IsWoman: false), "Bäckerei und Konditorei", "St. Gallen", "71", "9000", "baeckerei-huber.ch"),
        new("Schreinerei Brunner", "Luzern", new Person("Brunner", IsWoman: false), "Möbel und Innenausbau", "Luzern", "41", "6003", "schreinerei-brunner.ch"),
        new("Praxis Dr. Meier", "Zürich", new Person("Dr. Meier", IsWoman: true), "Allgemeinmedizin", "Zürich", "44", "8001", "praxis-meier.ch"),
        new("Gemeindeverwaltung Wil", "Wil", new Person("Fässler", IsWoman: false), "Einwohnerdienste und Steuern", "St. Gallen", "71", "9500", "stadtwil.ch"),
        new("Garage Steiner AG", "Chur", new Person("Steiner", IsWoman: false), "Autowerkstatt und Carrosserie", "Graubünden", "81", "7000", "garage-steiner.ch"),
        new("Hotel Seeblick", "Thun", new Person("Gerber", IsWoman: true), "Hotel und Restaurant", "Bern", "33", "3600", "seeblick-thun.ch"),
        new("Müller Logistik AG", "Basel", new Person("Müller", IsWoman: true), "Transport und Lagerung", "Basel-Stadt", "61", "4051", "mueller-logistik.ch"),
    ];

    // Німецькі міста для адрес фірми: у довіднику адрес тестової бази є лише Deutschland, тож фірма зі Швейцарії
    // має тут філію чи адресу доставки. Вулиці — справжні вулиці цих районів.
    private static readonly GermanPlace[] GermanPlaces =
    [
        new("Bayern", "80331", "München", "Altstadt", ["Sendlinger Straße", "Rosenstraße", "Tal"]),
        new("Baden-Württemberg", "70173", "Stuttgart", "Mitte", ["Königstraße", "Calwer Straße", "Eberhardstraße"]),
        new("Hessen", "60311", "Frankfurt am Main", "Innenstadt", ["Zeil", "Töngesgasse", "Fahrgasse"]),
        new("Nordrhein-Westfalen", "50667", "Köln", "Altstadt-Nord", ["Hohe Straße", "Schildergasse", "Breite Straße"]),
        new("Hamburg", "20095", "Hamburg", "Altstadt", ["Mönckebergstraße", "Spitalerstraße", "Rathausstraße"]),
        new("Sachsen", "01067", "Dresden", "Innere Altstadt", ["Wilsdruffer Straße", "Schloßstraße", "Seestraße"]),
    ];

    // Банки з довідника ADAICA (німецький, за BLZ): назва й SWIFT-BIC такі, як їх заповнює вибір банку.
    private static readonly (string Code, string Name, string Bic)[] Banks =
    [
        ("37040044", "Commerzbank", "COBADEFFXXX"),
        ("39060180", "Aachener Bank", "GENODED1AAC"),
        ("70120600", "Airbus Bank", "AGBMDEMMXXX"),
        ("10030400", "ABK Allgemeine Beamten Bank", "ABKBDEB1XXX"),
    ];

    // Відділи, які бувають у невеликої фірми.
    private static readonly string[] Departments =
        ["Buchhaltung", "Geschäftsleitung", "Verkauf", "Einkauf", "Personalwesen", "Kundendienst", "Technik"];

    private static readonly string[] WomenFirstNames = ["Sabine", "Andrea", "Claudia", "Nicole", "Petra", "Sandra", "Monika"];

    private static readonly string[] MenFirstNames = ["Thomas", "Daniel", "Markus", "Stefan", "Andreas", "Peter", "Martin"];

    private static readonly string[] Surnames =
        ["Hartmann", "Brandt", "Schneider", "Fischer", "Weber", "Zimmermann", "Baumann", "Frei", "Graf", "Lehmann"];

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
    private readonly HashSet<string> _usedNames = [];

    private CustomerCase(Customer customer, Topic topic)
    {
        _customer = customer;
        _topic = topic;
    }

    public static CustomerCase Random() => new(Pick(Customers), Pick(Topics));

    /// <summary>
    /// Фірма клієнта для навігатора Kontakte. Назва завжди унікальна, щоб потім знаходити саме цю фірму пошуком:
    /// до назви фірми додається номер клієнта з дати й часу створення, як його дописують у CRM («Keller Treuhand AG 260923161021»).
    /// </summary>
    public CompanyData NewCompany() => new(
        Name: $"{_customer.Company} {NewCustomerNumber()}",
        ShortName: ShortName);

    /// <summary>Відділ (Organisationseinheit) фірми клієнта.</summary>
    public string NewDepartment() => PickUnused(Departments);

    /// <summary>
    /// Співробітник фірми клієнта. Ім'я й Anrede узгоджені, бо ADAICA вгадує Anrede за ім'ям. Прізвище не збігається
    /// з контактною особою фірми, щоб співробітник був іншою людиною.
    /// </summary>
    public EmployeeData NewEmployee()
    {
        var isWoman = System.Random.Shared.Next(2) == 0;
        return new EmployeeData(
            FirstName: Pick(isWoman ? WomenFirstNames : MenFirstNames),
            LastName: PickUnused(Surnames.Where(surname => surname != Contact.Surname).ToArray()),
            Salutation: isWoman ? "Frau" : "Herr",
            ShortName: ShortName,
            Function: "Mitarbeiter");
    }

    /// <summary>
    /// Дані, якими тест заповнює картку фірми після створення: усе про ту саму фірму клієнта — її діяльність, місто,
    /// кантон, контактну особу. Номери (телефон, UID, рахунок) щоразу інші.
    /// </summary>
    public CompanyEditData EditCompany()
    {
        var uid = $"CHE-{Digits(3)}.{Digits(3)}.{Digits(3)}";
        var bank = Pick(Banks);
        var accountNumber = Digits(10);
        return new CompanyEditData(
            new CompanyGeneralData(
                NameLine2: _customer.Trade,
                NameLine3: Pick($"Filiale {_customer.City}", $"Hauptsitz {_customer.City}"),
                AddressLine: $"{_customer.Company}, z. H. {Contact.Dative}",
                Salutation: $"{(Contact.IsWoman ? "Sehr geehrte Frau" : "Sehr geehrter Herr")} {Contact.Surname},",
                CompanyType: "Firma",
                TradeRegister: $"Handelsregister des Kantons {_customer.Canton}",
                TradeRegisterNumber: uid,
                TaxNumber: $"{Digits(3)}.{Digits(3)}.{Digits(3)}",
                VatNumber: $"{uid} MWST",
                Communications:
                [
                    new("Telefon", PhoneNumber(), "Telefon Zentrale", Pick("Zentrale, Mo–Fr 8–17 Uhr", $"Empfang, {Contact.Nominative}")),
                    new("Fax", PhoneNumber(), "Fax Zentrale", string.Empty),
                    new("E-Mail", $"info@{_customer.Domain}", "E-Mail", "Allgemeine Anfragen"),
                    new("Webseite", $"www.{_customer.Domain}", "Website", string.Empty),
                    new("Sonstige", $"Postfach {System.Random.Shared.Next(100, 1000)}, {_customer.PostalCode} {_customer.City}", "Unbekannt", string.Empty),
                ],
                Address: NewAddress(),
                Remark: Pick(
                    $"Stammkunde, Ansprechperson ist {Contact.Nominative}. Rechnungen bitte per E-Mail.",
                    $"Rahmenvertrag bis Ende Jahr. Termine immer mit {Contact.Dative} abstimmen.",
                    "Zahlt zuverlässig innert 30 Tagen, Skonto wird nicht gewährt."),
                Category: Pick("Eigentümer", "Wartungsfirma", "Kostenträger"),
                // Логотип: назва фірми на кольоровому тлі. Кольори насичені, щоб не сплутати їх із сірим тлом картки.
                Photo: new PhotoData(_customer.Company, Pick(Color.SteelBlue, Color.SeaGreen, Color.DarkOrange, Color.Crimson)),
                CorrespondencePreference: "Firma",
                ElectronicContact: Pick("Ja", "Nein"),
                NoAdvertisingEmails: true,
                NoAdvertisingLetters: true,
                NoAdvertisingCalls: true,
                PrivacyStatementOk: true),
            new CompanyDetailsData(
                Turnover: System.Random.Shared.Next(5, 500) * 10000m,
                TurnoverCurrency: "USD",
                Creditworthiness: "Unbekannt"),
            new BankAccountData(
                Holder: _customer.Company,
                Iban: GermanIban(bank.Code, accountNumber),
                BankCode: bank.Code,
                BankName: bank.Name,
                Bic: bank.Bic,
                AccountNumber: accountNumber,
                IsPrimary: true));
    }

    /// <summary>Філія або адреса доставки фірми в Німеччині.</summary>
    public static AddressData NewAddress()
    {
        var place = Pick(GermanPlaces);
        return new AddressData(
            Category: Pick("Zweigstelle", "Lieferanschrift"),
            Country: "Deutschland",
            State: place.State,
            PostalCode: place.PostalCode,
            City: place.City,
            District: place.District,
            Street: Pick(place.Streets),
            HouseNumber: HouseNumber());
    }

    /// <summary>
    /// Та сама адреса з іншим номером будинку — коли задану вже створив попередній прогін
    /// (адреси лишаються в довіднику, а нову ADAICA дає створити лише таку, якої там ще немає).
    /// </summary>
    public static AddressData AnotherAddress(AddressData address)
    {
        var another = address;
        while (another.HouseNumber == address.HouseNumber)
            another = address with { HouseNumber = HouseNumber() };
        return another;
    }

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

    /// <summary>
    /// Ordner для документів справи. Бланк у Ordner той самий, що й у проєкту, тому й дані — <see cref="ProjectData"/>.
    /// </summary>
    public ProjectData NewFolder()
    {
        var days = UniqueDays(2);
        (string Name, string Comment)[] folders =
        [
            ($"Korrespondenz {_customer.Company}", $"E-Mails und Briefe von {Contact.Dative}, neueste zuerst."),
            ($"Pläne {_topic.Title}", "Nur freigegebene Pläne ablegen, Entwürfe bleiben im Projektordner."),
            ($"Verträge {_customer.Company}", $"Unterschriebene Verträge als PDF, die Originale liegen im Archiv in {_customer.City}."),
            ("Sitzungsprotokolle", "Protokolle der Projektsitzungen, Ablage nach Datum."),
        ];
        var (name, comment) = PickUnused(folder => folder.Name, folders);
        return new ProjectData(
            Name: name,
            ShortName: ShortName,
            BeginDate: days[0],
            EndDate: days[1],
            Cost: 480m,
            CostCurrency: "USD",
            Outlay: 3m,
            OutlayUnit: "Tag(e)",
            Comment: comment);
    }

    public TaskData NewTask()
    {
        // Початок — найраніший день; виконання раніше за закриття; термін «Fällig am» — будь-коли після початку.
        var days = UniqueDays(4);
        var due = Pick(1, 2, 3);
        var (done, closed) = due switch { 1 => (2, 3), 2 => (1, 3), _ => (1, 2) };
        return new TaskData(
            Name: PickUnused(
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
            Name: PickUnused(
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
            Name: PickUnused(
                $"Telefonat mit {Contact.Dative}",
                $"Rückfrage von {Contact.Dative}",
                $"E-Mail von {Contact.Dative}",
                $"Gesprächsnotiz {_topic.Title}"),
            ShortName: ShortName,
            // Дата інфозапису не може бути пізнішою за сьогодні, тому лише минулі дні.
            Begin: UniqueDays(1, toDay: -1)[0] + WorkingTime(),
            // У BLANK_IGOR список «Ergebnis» порожній.
            Result: null,
            Remark: PickUnused(
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

    /// <summary>
    /// Номер ррММддГГххсс з поточного часу. Якщо в ту саму секунду вже видано номер, береться наступний,
    /// тож у межах прогону номери не повторюються, а між прогонами їх розводить час.
    /// </summary>
    private static string NewCustomerNumber()
    {
        var fromClock = long.Parse(DateTime.Now.ToString("yyMMddHHmmss", CultureInfo.InvariantCulture), CultureInfo.InvariantCulture);
        lock (CustomerNumberLock)
        {
            _lastCustomerNumber = Math.Max(fromClock, _lastCustomerNumber + 1);
            return _lastCustomerNumber.ToString(CultureInfo.InvariantCulture);
        }
    }

    // Швейцарський номер у місті фірми: «+41 81 252 34 17».
    private string PhoneNumber() => $"+41 {_customer.AreaCode} {Digits(3)} {Digits(2)} {Digits(2)}";

    // Номер будинку, як його пишуть у Німеччині: зазвичай число, іноді з літерою.
    private static string HouseNumber() =>
        $"{System.Random.Shared.Next(1, 200)}{Pick("", "", "", "a", "b")}";

    private static string Digits(int count) =>
        string.Concat(Enumerable.Range(0, count).Select(_ => System.Random.Shared.Next(10).ToString(CultureInfo.InvariantCulture)));

    /// <summary>Правильний німецький IBAN: «DE», контрольні цифри за ISO 7064 (mod 97), BLZ і номер рахунку.</summary>
    private static string GermanIban(string bankCode, string accountNumber)
    {
        // «DE00» переноситься в кінець, а літери стають числами: D = 13, E = 14.
        var remainder = $"{bankCode}{accountNumber}131400".Aggregate(0, (current, digit) => (current * 10 + (digit - '0')) % 97);
        return $"DE{98 - remainder:00}{bankCode}{accountNumber}";
    }

    // Час, який людина вибрала б сама: робочі години з 07:00, кратно 15 хвилинам.
    private static TimeSpan WorkingTime(int latestHour = 17) =>
        TimeSpan.FromMinutes(System.Random.Shared.Next(7 * 4, latestHour * 4 + 1) * 15);

    private static T Pick<T>(params T[] items) => items[System.Random.Shared.Next(items.Length)];

    /// <summary>
    /// Назва, якої в цій справі ще не було, поки є з чого вибирати: дочірній запис не має збігатися з батьківським,
    /// інакше в дереві не видно, де батько, а де дочірній запис.
    /// </summary>
    private string PickUnused(params string[] names) => PickUnused(name => name, names);

    private T PickUnused<T>(Func<T, string> name, T[] candidates)
    {
        var unused = candidates.Where(candidate => !_usedNames.Contains(name(candidate))).ToArray();
        var picked = Pick(unused.Length > 0 ? unused : candidates);
        _usedNames.Add(name(picked));
        return picked;
    }

    private sealed record GermanPlace(string State, string PostalCode, string City, string District, string[] Streets);

    /// <param name="Trade">Діяльність фірми — другий рядок її назви.</param>
    /// <param name="AreaCode">Телефонний код міста без нуля: «+41 81 …».</param>
    private sealed record Customer(
        string Company, string City, Person Contact, string Trade, string Canton, string AreaCode, string PostalCode, string Domain);

    /// <param name="Object">Тема в знахідному відмінку, для «Offerte für …».</param>
    private sealed record Topic(string Title, string Object);

    private sealed record Person(string Surname, bool IsWoman)
    {
        public string Nominative => $"{(IsWoman ? "Frau" : "Herr")} {Surname}";

        /// <summary>Після «mit», «von»: «Herrn Huber».</summary>
        public string Dative => $"{(IsWoman ? "Frau" : "Herrn")} {Surname}";
    }
}
