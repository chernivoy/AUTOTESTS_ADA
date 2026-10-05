using System.Globalization;

namespace ADAICA_AUTOTEST.Controls;

/// <summary>Формати, у яких ADAICA показує значення: «18.09.2026», «09:00», «1500,00».</summary>
public static class AdaicaFormat
{
    public const string DatePattern = "dd.MM.yyyy";
    public const string TimePattern = "HH:mm";

    private static readonly CultureInfo NumberCulture = CultureInfo.GetCultureInfo("de-DE");

    public static string FormatDate(DateTime date) => date.ToString(DatePattern, CultureInfo.InvariantCulture);

    public static DateTime? ParseDate(string text) =>
        string.IsNullOrWhiteSpace(text) ? null : DateTime.ParseExact(text, DatePattern, CultureInfo.InvariantCulture);

    public static string FormatTime(DateTime time) => time.ToString(TimePattern, CultureInfo.InvariantCulture);

    public static TimeSpan? ParseTime(string text) =>
        string.IsNullOrWhiteSpace(text)
            ? null
            : DateTime.ParseExact(text, TimePattern, CultureInfo.InvariantCulture).TimeOfDay;

    public static string FormatDateTime(DateTime value) => $"{FormatDate(value)} {FormatTime(value)}";

    public static string FormatNumber(decimal value) => value.ToString(NumberCulture);

    /// <summary>
    /// Розмір файлу, як його показує ADAICA (csFunctions.ConvertFileSizeToString): менше кілобайта — ціле число байтів
    /// («512 b»), інакше три знаки після коми в найбільшій одиниці, де число не менше одиниці («1,026 KB»).
    /// </summary>
    public static string FormatFileSize(long bytes)
    {
        string[] units = [" b", " KB", " MB", " GB", " TB"];
        var size = (double)bytes;
        var unit = 0;
        for (; size >= 1024 && unit < units.Length - 1; unit++)
            size /= 1024;

        return unit == 0 ? $"{bytes}{units[0]}" : size.ToString("F3", NumberCulture) + units[unit];
    }

    public static decimal? ParseNumber(string text) =>
        string.IsNullOrWhiteSpace(text) ? null : decimal.Parse(text, NumberStyles.Number, NumberCulture);
}
