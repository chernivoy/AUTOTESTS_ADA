namespace ADAICA_AUTOTEST.Pages;

/// <summary>
/// Режим пошуку «Finden» для кількох слів (список праворуч від джокерів; для одного слова він недоступний).
/// Значення — позиції пунктів у списку: за текстом пункт не вибрати, бо «Еinzelnes Wort» починається з кириличної «Е».
/// </summary>
public enum SearchMode
{
    /// <summary>«Alle Wörter»: запис має містити всі слова, причому всі — в одному полі.</summary>
    AllWords = 0,

    /// <summary>«Exakte Suche»: слова стоять поруч і в тому самому порядку.</summary>
    ExactPhrase = 1,

    /// <summary>«Einzelnes Wort»: вистачає будь-якого зі слів.</summary>
    SingleWord = 2,
}

public static class SearchModeLabels
{
    /// <summary>Підпис режиму латиницею, як його читає людина (у списку «Einzelnes Wort» починається з кириличної «Е»).</summary>
    public static string Label(this SearchMode mode) => mode switch
    {
        SearchMode.AllWords => "Alle Wörter",
        SearchMode.ExactPhrase => "Exakte Suche",
        SearchMode.SingleWord => "Einzelnes Wort",
        _ => mode.ToString(),
    };
}
