using System.IO.Compression;
using System.Security;
using System.Text;

namespace ADAICA_AUTOTEST.Infrastructure;

/// <summary>
/// Word-документ (.docx), який тест складає сам: заголовок і абзаци тексту. Без Word і сторонніх бібліотек — .docx
/// це ZIP із кількома XML-файлами (Office Open XML), найменший робочий набір яких тут і записується.
/// Файли лежать в окремій теці в %TEMP%; якщо попередній прогін не встиг прибрати свій файл, його видаляє наступний.
/// </summary>
public sealed class TestWordFile : IDisposable
{
    private static readonly string Directory = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "ADAICA_AUTOTEST");

    private const string ContentTypes =
        """
        <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
        <Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types">
          <Default Extension="rels" ContentType="application/vnd.openxmlformats-package.relationships+xml"/>
          <Default Extension="xml" ContentType="application/xml"/>
          <Override PartName="/word/document.xml" ContentType="application/vnd.openxmlformats-officedocument.wordprocessingml.document.main+xml"/>
        </Types>
        """;

    private const string PackageRelationships =
        """
        <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
        <Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">
          <Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument" Target="word/document.xml"/>
        </Relationships>
        """;

    private TestWordFile(string path) => Path = path;

    public string Path { get; }

    /// <summary>Ім'я файлу без теки й розширення — так ADAICA називає документ із нього.</summary>
    public string Name => System.IO.Path.GetFileNameWithoutExtension(Path);

    /// <summary>Ім'я файлу з розширенням — так документ підписано в дереві навігатора.</summary>
    public string FileName => System.IO.Path.GetFileName(Path);

    /// <summary>Розмір файлу в байтах.</summary>
    public long Length => new FileInfo(Path).Length;

    public bool Exists => File.Exists(Path);

    /// <summary>Складає .docx за <paramref name="content"/>: ім'я файлу, жирний заголовок і абзаци.</summary>
    public static TestWordFile CreateDocx(WordFileContent content) =>
        CreateDocx(content.FileName, content.Title, content.Paragraphs);

    /// <summary>
    /// Складає .docx з файлом <paramref name="fileName"/> (без розширення): жирний заголовок <paramref name="title"/>
    /// і абзаци <paramref name="paragraphs"/>. Ім'я файлу має бути унікальним, бо інший прогін міг лишити такий самий.
    /// </summary>
    public static TestWordFile CreateDocx(string fileName, string title, IEnumerable<string> paragraphs)
    {
        System.IO.Directory.CreateDirectory(Directory);
        DeleteLeftovers();

        var file = new TestWordFile(System.IO.Path.Combine(Directory, $"{fileName}.docx"));
        using var zip = ZipFile.Open(file.Path, ZipArchiveMode.Create);
        Write(zip, "[Content_Types].xml", ContentTypes);
        Write(zip, "_rels/.rels", PackageRelationships);
        Write(zip, "word/document.xml", Document(title, paragraphs));
        return file;
    }

    /// <summary>Видаляє файл. ADAICA може тримати його, поки документ не збережено, тож до того видалення може не вдатися.</summary>
    public void Delete()
    {
        if (Exists)
            File.Delete(Path);
    }

    /// <summary>Для прибирання після тесту: видаляє файл, а якщо його ще тримає застосунок, лишає наступному прогону.</summary>
    public void Dispose()
    {
        try
        {
            Delete();
        }
        catch (IOException ex)
        {
            TestContext.Out.WriteLine($"Не вдалося видалити {Path}, його прибере наступний прогін: {ex.Message}");
        }
    }

    private static string Document(string title, IEnumerable<string> paragraphs)
    {
        var body = new StringBuilder();
        body.Append($"<w:p><w:r><w:rPr><w:b/><w:sz w:val=\"32\"/></w:rPr><w:t>{SecurityElement.Escape(title)}</w:t></w:r></w:p>");
        foreach (var paragraph in paragraphs)
            body.Append($"<w:p><w:r><w:t xml:space=\"preserve\">{SecurityElement.Escape(paragraph)}</w:t></w:r></w:p>");

        return "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>"
            + "<w:document xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\">"
            + $"<w:body>{body}</w:body></w:document>";
    }

    private static void Write(ZipArchive zip, string entryName, string content)
    {
        using var writer = new StreamWriter(zip.CreateEntry(entryName).Open(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        writer.Write(content);
    }

    private static void DeleteLeftovers()
    {
        foreach (var leftover in System.IO.Directory.EnumerateFiles(Directory, "*.docx"))
        {
            try
            {
                File.Delete(leftover);
            }
            catch (IOException)
            {
                // Файл ще тримає інший процес — спробуємо наступного разу.
            }
        }
    }
}

/// <summary>Вміст Word-файлу: ім'я (без розширення), заголовок і абзаци тексту.</summary>
public sealed record WordFileContent(string FileName, string Title, IReadOnlyList<string> Paragraphs);
