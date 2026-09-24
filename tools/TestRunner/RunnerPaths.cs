using System.Xml.Linq;

namespace TestRunner;

/// <summary>Шляхи до тестового проєкту, його збірки, результатів Allure й власних даних запускача.</summary>
public sealed class RunnerPaths
{
    private const string Configuration = "Debug";
    private const string TargetFramework = "net10.0-windows";

    private RunnerPaths(string repositoryRoot)
    {
        RepositoryRoot = repositoryRoot;
        TestProjectDirectory = Path.Combine(repositoryRoot, "tests", "ADAICA_AUTOTEST");
        TestOutputDirectory = Path.Combine(TestProjectDirectory, "bin", Configuration, TargetFramework);
        DataDirectory = Path.Combine(repositoryRoot, ".testrunner");
        ReportsDirectory = Path.Combine(DataDirectory, "reports");
        RunsDirectory = Path.Combine(DataDirectory, "runs");
    }

    public string RepositoryRoot { get; }

    public string TestProjectDirectory { get; }

    public string TestProjectFile => Path.Combine(TestProjectDirectory, "ADAICA_AUTOTEST.csproj");

    public string RunSettingsFile => Path.Combine(TestProjectDirectory, "ADAICA_AUTOTEST.runsettings");

    public string TestOutputDirectory { get; }

    public string TestAssembly => Path.Combine(TestOutputDirectory, "ADAICA_AUTOTEST.dll");

    public string TestDocumentation => Path.Combine(TestOutputDirectory, "ADAICA_AUTOTEST.xml");

    /// <summary>Куди allure-csharp пише результати: поруч зі збіркою тестів (allureConfig.json).</summary>
    public string AllureResultsDirectory => Path.Combine(TestOutputDirectory, "allure-results");

    /// <summary>Дані запускача: звіти Allure й результати прогонів. Тека в .gitignore.</summary>
    public string DataDirectory { get; }

    public string ReportsDirectory { get; }

    public string RunsDirectory { get; }

    /// <summary>Шукає корінь репозиторію (теку з ADAICA_AUTOTEST.sln) вгору від теки запускача.</summary>
    public static RunnerPaths Locate()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, "ADAICA_AUTOTEST.sln")))
                return new RunnerPaths(directory.FullName);
        }

        throw new InvalidOperationException("Не знайдено ADAICA_AUTOTEST.sln вище за теку запускача.");
    }

    /// <summary>Параметри з ADAICA_AUTOTEST.runsettings (TestRunParameters), як їх бачать тести.</summary>
    public IReadOnlyDictionary<string, string> ReadRunSettingsParameters()
    {
        if (!File.Exists(RunSettingsFile))
            return new Dictionary<string, string>();

        return XDocument.Load(RunSettingsFile)
            .Descendants("Parameter")
            .Where(parameter => parameter.Attribute("name") is not null)
            .ToDictionary(parameter => parameter.Attribute("name")!.Value, parameter => parameter.Attribute("value")?.Value ?? string.Empty);
    }
}
