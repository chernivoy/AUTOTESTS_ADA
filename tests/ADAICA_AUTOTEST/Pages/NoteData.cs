namespace ADAICA_AUTOTEST.Pages;

/// <summary>Поля інфозапису (Notiz), які користувач може змінити.</summary>
/// <param name="Begin">ADAICA не дозволяє дату, пізнішу за сьогодні: кнопка OK тоді недоступна.</param>
/// <param name="Result">Пункт списку «Ergebnis» або null, якщо нічого не вибирати (у деяких базах список порожній).</param>
public sealed record NoteData(
    string Name,
    string ShortName,
    DateTime Begin,
    string? Result,
    string Remark);
