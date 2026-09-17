using System.Globalization;

namespace ADAICA_AUTOTEST.Controls;

/// <summary>Формати, у яких ADAICA показує значення: «18.09.2026», «1500,00».</summary>
public static class AdaicaFormat
{
    public const string DatePattern = "dd.MM.yyyy";

    private static readonly CultureInfo NumberCulture = CultureInfo.GetCultureInfo("de-DE");

    public static string FormatDate(DateTime date) => date.ToString(DatePattern, CultureInfo.InvariantCulture);

    public static DateTime? ParseDate(string text) =>
        string.IsNullOrWhiteSpace(text) ? null : DateTime.ParseExact(text, DatePattern, CultureInfo.InvariantCulture);

    public static string FormatNumber(decimal value) => value.ToString(NumberCulture);

    public static decimal? ParseNumber(string text) =>
        string.IsNullOrWhiteSpace(text) ? null : decimal.Parse(text, NumberStyles.Number, NumberCulture);
}
