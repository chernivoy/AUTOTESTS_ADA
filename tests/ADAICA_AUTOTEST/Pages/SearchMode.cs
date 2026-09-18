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
