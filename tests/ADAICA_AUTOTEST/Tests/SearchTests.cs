using System.Text.RegularExpressions;
using ADAICA_AUTOTEST.Pages;
using Allure.Net.Commons;
using Allure.Net.Commons.Attributes;

namespace ADAICA_AUTOTEST.Tests;

/// <summary>
/// Повнотекстовий пошук у вікні «Finden» (System → Finden…, вкладка Dokumentation). Тест створює завдання
/// з природними назвою й приміткою і шукає його за словами лише з одного поля: тоді «Fundstelle» показує саме це поле.
/// Записів зі спільними словами в базі багато, тому своє завдання тест упізнає в результатах за номером.
/// </summary>
[AllureSuite("Пошук")]
public partial class SearchTests : AdaicaTestBase
{
    [Test]
    [AllureSeverity(SeverityLevel.critical)]
    public void Finden_TaskByNameWords()
    {
        var (mainPage, task, number) = CreateTask();

        var search = mainPage.OpenSearchWindow();
        OnCleanup(search.CloseIfOpen);
        search.FindRecords(SearchWords(task.Name, notIn: task.Remark));

        search.WaitForResult(number).ShouldBe(new SearchResult(number, "TASK", task.Name, "Name (Volltextsuche)", task.Remark));
    }

    [Test]
    [AllureSeverity(SeverityLevel.critical)]
    public void Finden_TaskByRemarkWords()
    {
        var (mainPage, task, number) = CreateTask();

        var search = mainPage.OpenSearchWindow();
        OnCleanup(search.CloseIfOpen);
        search.FindRecords(SearchWords(task.Remark, notIn: task.Name));

        search.WaitForResult(number).ShouldBe(new SearchResult(number, "TASK", task.Name, "Bemerkung (Volltextsuche)", task.Remark));
    }

    private (MainPage MainPage, TaskData Task, string Number) CreateTask()
    {
        var mainPage = LogInAndOpenDokumentation();

        var taskDialog = mainPage.OpenNewTaskDialog();
        OnCleanup(taskDialog.CancelIfOpen);
        var task = CustomerCase.Random().NewTask();
        taskDialog.Form.Fill(task);
        var number = taskDialog.Form.Number.Text;
        taskDialog.Save();
        mainPage.WaitForOpenedTask(number);
        return (mainPage, task, number);
    }

    /// <summary>
    /// Два найдовші слова тексту (від 5 літер, тож без службових «für», «mit»), яких немає в іншому полі запису:
    /// так запис може знайтися лише за потрібним полем.
    /// </summary>
    private static string SearchWords(string text, string notIn)
    {
        var otherWords = Words(notIn).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var words = Words(text)
            .Where(word => !otherWords.Contains(word))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderByDescending(word => word.Length)
            .Take(2)
            .ToList();
        return words.Count > 0
            ? string.Join(' ', words)
            : throw new InvalidOperationException($"У тексті «{text}» немає слів, яких нема в «{notIn}».");
    }

    private static IEnumerable<string> Words(string text) => Word().Matches(text).Select(match => match.Value);

    [GeneratedRegex(@"\p{L}{5,}")]
    private static partial Regex Word();
}
