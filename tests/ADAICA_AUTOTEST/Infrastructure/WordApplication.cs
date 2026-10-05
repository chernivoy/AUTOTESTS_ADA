using System.Diagnostics;
using System.Runtime.InteropServices;
using Allure.Net.Commons;

namespace ADAICA_AUTOTEST.Infrastructure;

/// <summary>
/// Word, у якому ADAICA відкриває документ для редагування («Bearbeiten» у картці документа). Тест керує ним через
/// COM-автоматизацію (Word.Application) — без миші й клавіатури — і чіпає лише свій документ: Word може бути вже
/// відкритий у користувача, тоді ADAICA відкриває документ у ньому ж. Сам Word тест закриває, лише якщо той
/// запустився під час тесту й у ньому не лишилося документів.
/// </summary>
public static class WordApplication
{
    private const string ProcessName = "WINWORD";

    // Word реєструється в таблиці запущених об'єктів не одразу після відкриття документа.
    private static readonly TimeSpan AttachTimeout = TimeSpan.FromSeconds(30);

    /// <summary>Процеси Word, запущені зараз; передайте їх у <see cref="AppendParagraph"/>, щоб той знав, чий Word.</summary>
    public static IReadOnlySet<int> RunningProcessIds() =>
        Process.GetProcessesByName(ProcessName).Select(process => process.Id).ToHashSet();

    /// <summary>
    /// Дописує абзац <paramref name="text"/> у кінець відкритого документа <paramref name="documentName"/> (ім'я файлу
    /// без розширення), зберігає й закриває його. Повертає розмір збереженого файлу в байтах.
    /// </summary>
    /// <param name="wordProcessesBefore">Процеси Word до того, як ADAICA відкрила документ.</param>
    public static long AppendParagraph(string documentName, string text, IReadOnlySet<int> wordProcessesBefore) =>
        AllureApi.Step($"Дописати у Word абзац у «{documentName}», зберегти й закрити документ", () =>
        {
            dynamic word = Attach();
            dynamic document = Wait.For(() => FindDocument(word, documentName), $"документа «{documentName}» у Word", AttachTimeout);

            document.Content.InsertParagraphAfter();
            document.Content.InsertAfter(text);
            document.Save();
            var length = new FileInfo((string)document.FullName).Length;
            document.Close();

            QuitIfStartedByTest(word, wordProcessesBefore);
            return length;
        });

    /// <summary>
    /// Для прибирання після тесту: закриває документ без збереження, якщо він ще відкритий, і Word, якщо його запустив
    /// тест і документів у ньому не лишилося.
    /// </summary>
    public static void CloseWithoutSaving(string documentName, IReadOnlySet<int> wordProcessesBefore)
    {
        if (Process.GetProcessesByName(ProcessName).Length == 0 || TryAttach() is not { } app)
            return;

        dynamic word = app;
        if (FindDocument(word, documentName) is { } document)
            document.Close(false);

        QuitIfStartedByTest(word, wordProcessesBefore);
    }

    private static object Attach() => Wait.For(TryAttach, "запущеного Word (COM, Word.Application)", AttachTimeout);

    private static object? TryAttach()
    {
        try
        {
            CLSIDFromProgID("Word.Application", out var classId);
            GetActiveObject(ref classId, 0, out var application);
            return application;
        }
        catch (COMException)
        {
            return null;
        }
    }

    // Документ, файл якого (без розширення) називається documentName: ADAICA кладе копію з ім'ям документа в свою теку.
    private static object? FindDocument(dynamic word, string documentName)
    {
        int count = word.Documents.Count;
        for (var index = 1; index <= count; index++)
        {
            dynamic document = word.Documents.Item(index);
            if (Path.GetFileNameWithoutExtension((string)document.FullName) == documentName)
                return document;
        }

        return null;
    }

    private static void QuitIfStartedByTest(dynamic word, IReadOnlySet<int> wordProcessesBefore)
    {
        var startedByTest = Process.GetProcessesByName(ProcessName).Any(process => !wordProcessesBefore.Contains(process.Id));
        if (startedByTest && (int)word.Documents.Count == 0)
            word.Quit();
    }

    [DllImport("ole32.dll", CharSet = CharSet.Unicode, PreserveSig = false)]
    private static extern void CLSIDFromProgID(string progId, out Guid classId);

    [DllImport("oleaut32.dll", PreserveSig = false)]
    private static extern void GetActiveObject(ref Guid classId, nint reserved, [MarshalAs(UnmanagedType.IUnknown)] out object application);
}
