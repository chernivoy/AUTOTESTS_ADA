using System.Drawing;
using System.Drawing.Imaging;

namespace ADAICA_AUTOTEST.Infrastructure;

/// <summary>
/// JPG, який тест малює сам (одноколірне тло з підписом) і прибирає після себе.
/// Файли лежать в окремій теці в %TEMP%; якщо попередній прогін не встиг прибрати свій файл, його видаляє наступний.
/// </summary>
public sealed class TestImageFile : IDisposable
{
    private static readonly string Directory = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "ADAICA_AUTOTEST");

    private TestImageFile(string path) => Path = path;

    public string Path { get; }

    public bool Exists => File.Exists(Path);

    /// <summary>Малює JPG 400×300: тло кольору <paramref name="background"/> і білий підпис посередині.</summary>
    public static TestImageFile CreateJpeg(string caption, Color background)
    {
        System.IO.Directory.CreateDirectory(Directory);
        DeleteLeftovers();

        var file = new TestImageFile(System.IO.Path.Combine(Directory, $"{Guid.NewGuid():N}.jpg"));
        using var bitmap = new Bitmap(400, 300);
        using (var graphics = Graphics.FromImage(bitmap))
        using (var font = new Font("Arial", 28, FontStyle.Bold))
        {
            graphics.Clear(background);
            var bounds = new RectangleF(0, 0, bitmap.Width, bitmap.Height);
            using var format = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
            graphics.DrawString(caption, font, Brushes.White, bounds, format);
        }

        bitmap.Save(file.Path, ImageFormat.Jpeg);
        return file;
    }

    /// <summary>Видаляє файл. ADAICA тримає його, поки фото не збережено, тож до того видалення може не вдатися.</summary>
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

    private static void DeleteLeftovers()
    {
        foreach (var leftover in System.IO.Directory.EnumerateFiles(Directory, "*.jpg"))
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
