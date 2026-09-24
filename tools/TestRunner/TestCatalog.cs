using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace TestRunner;

/// <summary>Тест-метод NUnit, як його показує сторінка.</summary>
/// <param name="Id">Повне ім'я методу: за ним будується фільтр dotnet test.</param>
/// <param name="Cases">Скільки випадків дає метод: TestCase-атрибути або 1, якщо випадки задає TestCaseSource.</param>
/// <param name="HasCaseSource">Випадки задає TestCaseSource, тож їхню кількість видно лише під час прогону.</param>
public sealed record TestInfo(
    string Id,
    string Name,
    string? Summary,
    IReadOnlyList<string> Categories,
    bool IsExplicit,
    int Cases,
    bool HasCaseSource);

/// <summary>Клас тестів.</summary>
public sealed record TestGroup(
    string Id,
    string Name,
    string? Suite,
    string? Summary,
    IReadOnlyList<TestInfo> Tests);

/// <summary>
/// Список тестів зі збірки ADAICA_AUTOTEST. Збірку читає MetadataLoadContext — без виконання коду й без блокування
/// файлу, тож тести можна перезбирати, поки запускач працює. Описи беруться з XML-документації збірки.
/// </summary>
public static partial class TestCatalog
{
    private const string NUnitNamespace = "NUnit.Framework.";

    public static IReadOnlyList<TestGroup> Load(RunnerPaths paths)
    {
        if (!File.Exists(paths.TestAssembly))
            throw new FileNotFoundException($"Збірки тестів немає: {paths.TestAssembly}. Натисніть «Оновити список», щоб зібрати тести.");

        var documentation = LoadDocumentation(paths.TestDocumentation);

        // Резолвер бачить збірки .NET, WindowsDesktop (net10.0-windows) і все, що лежить поруч зі збіркою тестів.
        var runtimeDirectory = RuntimeEnvironment.GetRuntimeDirectory();
        var sharedDirectory = Directory.GetParent(runtimeDirectory.TrimEnd(Path.DirectorySeparatorChar))!.Parent!.FullName;
        var desktopDirectory = Directory.GetDirectories(Path.Combine(sharedDirectory, "Microsoft.WindowsDesktop.App"))
            .OrderByDescending(directory => directory, StringComparer.Ordinal)
            .FirstOrDefault();
        var assemblies = Directory.GetFiles(runtimeDirectory, "*.dll")
            .Concat(desktopDirectory is null ? [] : Directory.GetFiles(desktopDirectory, "*.dll"))
            .Concat(Directory.GetFiles(paths.TestOutputDirectory, "*.dll"))
            .GroupBy(Path.GetFileName, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.Last());

        // Збірку читаємо з пам'яті: так файл не блокується і його можна перезбирати.
        using var context = new MetadataLoadContext(new PathAssemblyResolver(assemblies));
        var assembly = context.LoadFromByteArray(File.ReadAllBytes(paths.TestAssembly));

        var groups = new List<TestGroup>();
        foreach (var type in assembly.GetTypes().Where(type => type is { IsClass: true, IsAbstract: false, IsPublic: true }))
        {
            var tests = type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .Select(method => ToTest(type, method, documentation))
                .OfType<TestInfo>()
                .ToList();
            if (tests.Count == 0)
                continue;

            var suite = AttributeArgument(type.GetCustomAttributesData(), "Allure.Net.Commons.Attributes.AllureSuiteAttribute");
            groups.Add(new TestGroup(type.FullName!, type.Name, suite, documentation.GetValueOrDefault($"T:{type.FullName}"), tests));
        }

        return groups.OrderBy(group => group.Suite ?? "￿", StringComparer.CurrentCulture)
            .ThenBy(group => group.Name, StringComparer.Ordinal)
            .ToList();
    }

    private static TestInfo? ToTest(Type type, MethodInfo method, IReadOnlyDictionary<string, string> documentation)
    {
        var attributes = method.GetCustomAttributesData();
        var names = attributes.Select(attribute => attribute.AttributeType.FullName).ToList();
        var testCases = names.Count(name => name == NUnitNamespace + "TestCaseAttribute");
        var hasCaseSource = names.Contains(NUnitNamespace + "TestCaseSourceAttribute");
        if (!names.Contains(NUnitNamespace + "TestAttribute") && testCases == 0 && !hasCaseSource)
            return null;

        var categories = attributes
            .Where(attribute => attribute.AttributeType.FullName == NUnitNamespace + "CategoryAttribute")
            .Select(attribute => attribute.ConstructorArguments.FirstOrDefault().Value as string)
            .OfType<string>()
            .ToList();

        var id = $"{type.FullName}.{method.Name}";
        return new TestInfo(
            id,
            method.Name,
            documentation.GetValueOrDefault($"M:{id}"),
            categories,
            names.Contains(NUnitNamespace + "ExplicitAttribute"),
            Math.Max(testCases, 1),
            hasCaseSource);
    }

    private static string? AttributeArgument(IEnumerable<CustomAttributeData> attributes, string attributeName) =>
        attributes.FirstOrDefault(attribute => attribute.AttributeType.FullName == attributeName)
            ?.ConstructorArguments.FirstOrDefault().Value as string;

    /// <summary>Summary методів і класів: ключ — ім'я члена без параметрів («M:Ns.Class.Method»).</summary>
    private static Dictionary<string, string> LoadDocumentation(string path)
    {
        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        if (!File.Exists(path))
            return result;

        foreach (var member in XDocument.Load(path).Descendants("member"))
        {
            var name = member.Attribute("name")?.Value;
            var summary = member.Element("summary");
            if (name is null || summary is null)
                continue;

            var key = name.Split('(')[0];
            result.TryAdd(key, SummaryText(summary));
        }

        return result;
    }

    // <see cref="T:Ns.Class"/> → «Class», <paramref name="x"/> → «x», решта тегів — їхній текст.
    private static string SummaryText(XElement summary)
    {
        var text = string.Concat(summary.Nodes().Select(node => node switch
        {
            XText plain => plain.Value,
            XElement { Name.LocalName: "see" or "seealso" } see => ShortName(see.Attribute("cref")?.Value ?? see.Attribute("langword")?.Value ?? see.Value),
            XElement { Name.LocalName: "paramref" or "typeparamref" } reference => reference.Attribute("name")?.Value ?? string.Empty,
            XElement element => element.Value,
            _ => string.Empty,
        }));
        return Whitespace().Replace(text, " ").Trim();
    }

    private static string ShortName(string cref)
    {
        var name = cref.Contains(':') ? cref[(cref.IndexOf(':') + 1)..] : cref;
        name = name.Split('(')[0];
        var parts = name.Split('.');
        // Для члена лишаємо «Клас.Член», для типу — «Клас».
        return cref.StartsWith("T:", StringComparison.Ordinal) || parts.Length < 2 ? parts[^1] : $"{parts[^2]}.{parts[^1]}";
    }

    [GeneratedRegex(@"\s+")]
    private static partial Regex Whitespace();
}
