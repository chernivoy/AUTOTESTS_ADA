using System.Globalization;

namespace ADAICA_AUTOTEST.Controls;

/// <summary>Формати, у яких ADAICA показує значення: «18.09.2026», «09:00», «1500,00».</summary>
public static class AdaicaFormat
{
    public const string DatePattern = "dd.MM.yyyy";
    public const string TimePattern = "HH:mm";

    private static readonly CultureInfo GermanCulture = CultureInfo.GetCultureInfo("de-DE");

    public static string FormatDate(DateTime date) => date.ToString(DatePattern, CultureInfo.InvariantCulture);

    public static DateTime? ParseDate(string text) =>
        string.IsNullOrWhiteSpace(text) ? null : DateTime.ParseExact(text, DatePattern, CultureInfo.InvariantCulture);

    public static string FormatTime(DateTime time) => time.ToString(TimePattern, CultureInfo.InvariantCulture);

    public static TimeSpan? ParseTime(string text) =>
        string.IsNullOrWhiteSpace(text)
            ? null
            : DateTime.ParseExact(text, TimePattern, CultureInfo.InvariantCulture).TimeOfDay;

    public static string FormatDateTime(DateTime value) => $"{FormatDate(value)} {FormatTime(value)}";

    /// <summary>Дата з часом у форматі «18.09.2026 09:00» або null, якщо текст порожній чи не в цьому форматі.</summary>
    public static DateTime? ParseDateTime(string? text) =>
        DateTime.TryParseExact(text, $"{DatePattern} {TimePattern}", CultureInfo.InvariantCulture, DateTimeStyles.None, out var value)
            ? value
            : null;

    /// <summary>Назва дня тижня, як її пише ADAICA: «Montag».</summary>
    public static string FormatDayOfWeek(DayOfWeek day) => GermanCulture.DateTimeFormat.GetDayName(day);

    /// <summary>Назва місяця, як її пише ADAICA: «März».</summary>
    public static string FormatMonth(int month) => GermanCulture.DateTimeFormat.GetMonthName(month);

    public static string FormatNumber(decimal value) => value.ToString(GermanCulture);

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

        return unit == 0 ? $"{bytes}{units[0]}" : size.ToString("F3", GermanCulture) + units[unit];
    }

    public static decimal? ParseNumber(string text) =>
        string.IsNullOrWhiteSpace(text) ? null : decimal.Parse(text, NumberStyles.Number, GermanCulture);
}
