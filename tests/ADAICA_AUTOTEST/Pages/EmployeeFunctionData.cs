using ADAICA_AUTOTEST.Controls;

namespace ADAICA_AUTOTEST.Pages;

/// <summary>
/// Функція співробітника у відділі («Funktion hinzufügen»). Обидві дати необов'язкові, статус ADAICA виводить із них.
/// </summary>
/// <param name="Function">Mitarbeiterfunktion з довідника (Konfiguration der Vorgabewerte → Firma → Mitarbeiterfunktion).</param>
/// <param name="ValidFrom">«Gültig vom» або null.</param>
/// <param name="ValidTo">«Gültig bis» або null.</param>
public sealed record EmployeeFunctionData(string Function, DateTime? ValidFrom = null, DateTime? ValidTo = null)
{
    /// <summary>
    /// Статус, який ADAICA показує для дат: без дат — «Unbekannt», початок у майбутньому — «Geplant», кінець у минулому —
    /// «Beendet», інакше — «Aktiv».
    /// </summary>
    public string Status => (ValidFrom, ValidTo) switch
    {
        (null, null) => "Unbekannt",
        ({ } from, _) when from.Date > DateTime.Today => "Geplant",
        (_, { } to) when to.Date < DateTime.Today => "Beendet",
        _ => "Aktiv",
    };

    /// <summary>
    /// Рядок таблиці функцій, як його читає <see cref="EmployeeFunctionsGrid"/>: «Teamleitung | 01.10.2026 |  | Aktiv».
    /// Нову функцію (ще не збережену) ADAICA позначає «*».
    /// </summary>
    public string Row(bool isNew = false) => FormatRow(Function, ValidFrom, ValidTo, Status, isNew);

    /// <summary>Рядок таблиці функцій зі статусом, який показує сама таблиця.</summary>
    public static string FormatRow(string function, DateTime? validFrom, DateTime? validTo, string status, bool isNew) =>
        $"{function} | {FormatDate(validFrom)} | {FormatDate(validTo)} | {status}{(isNew ? " | *" : string.Empty)}";

    public override string ToString() => Row();

    private static string FormatDate(DateTime? date) => date is { } value ? AdaicaFormat.FormatDate(value) : string.Empty;
}
