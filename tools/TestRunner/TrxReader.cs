using System.Globalization;
using System.Xml.Linq;

namespace TestRunner;

/// <summary>Результат одного випадку тесту з TRX-файлу dotnet test.</summary>
public sealed record TrxResult(string Name, string Outcome, TimeSpan Duration, string? Message);

/// <summary>Читає TRX (логер «trx» у dotnet test).</summary>
public static class TrxReader
{
    private static readonly XNamespace Ns = "http://microsoft.com/schemas/VisualStudio/TeamTest/2010";

    public static IReadOnlyList<TrxResult> Read(string path)
    {
        if (!File.Exists(path))
            return [];

        return XDocument.Load(path)
            .Descendants(Ns + "UnitTestResult")
            .Select(result => new TrxResult(
                result.Attribute("testName")?.Value ?? string.Empty,
                result.Attribute("outcome")?.Value ?? "Unknown",
                TimeSpan.TryParse(result.Attribute("duration")?.Value, CultureInfo.InvariantCulture, out var duration) ? duration : TimeSpan.Zero,
                result.Descendants(Ns + "ErrorInfo").Elements(Ns + "Message").FirstOrDefault()?.Value.Trim()))
            .ToList();
    }
}
