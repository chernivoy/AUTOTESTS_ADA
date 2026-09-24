using System.Text.Json;

namespace TestRunner;

/// <summary>Параметри запуску зі сторінки.</summary>
/// <param name="Tests">Id тестів у тому порядку, в якому їх позначили.</param>
/// <param name="AppPath">Повний шлях до ADAICA.exe, яку запускатимуть тести.</param>
public sealed record RunRequest(
    IReadOnlyList<string> Tests,
    string User,
    string Password,
    string Database,
    string Schema,
    string AppPath);

public enum RunStatus
{
    Building,
    Running,
    Reporting,
    Finished,
    Stopped,
    Failed,
}

public enum TestStatus
{
    Pending,
    Running,
    Passed,
    Failed,
    Skipped,
    NotFound,
}

/// <summary>Тест у прогоні: статус, тривалість (сума тривалостей випадків за TRX) і перша помилка.</summary>
public sealed record RunItem(
    string Id,
    string Name,
    TestStatus Status,
    double? DurationSeconds,
    int Passed,
    int Failed,
    int Skipped,
    string? Message);

public sealed record RunSnapshot(
    string Id,
    RunStatus Status,
    DateTimeOffset StartedAt,
    DateTimeOffset? FinishedAt,
    string User,
    string Database,
    string Schema,
    string AppPath,
    IReadOnlyList<RunItem> Items,
    IReadOnlyList<string> Log,
    string? ReportUrl,
    string? ReportError,
    string? Error);

/// <summary>
/// Один прогін за раз. Тести йдуть у заданому порядку, кожен окремим dotnet test: NUnit сам упорядковує тести
/// всередині одного запуску, а так порядок тримаємо ми. Результати Allure всіх тестів збираються в одній теці
/// (тестам передається ADAICA_KEEP_ALLURE_RESULTS), і наприкінці з них генерується один звіт.
/// </summary>
public sealed class RunManager(RunnerPaths paths, ILogger<RunManager> logger)
{
    private const int MaxLogLines = 400;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web) { WriteIndented = true };

    private readonly Lock _lock = new();
    private readonly List<string> _log = [];
    private RunSnapshot? _current;
    private CancellationTokenSource? _cancellation;

    public RunSnapshot? Current
    {
        get
        {
            lock (_lock)
                return _current is null ? null : _current with { Log = _log.ToList() };
        }
    }

    public bool IsBusy
    {
        get
        {
            lock (_lock)
                return _current?.Status is RunStatus.Building or RunStatus.Running or RunStatus.Reporting;
        }
    }

    /// <returns>false, якщо прогін уже триває.</returns>
    public bool TryStart(RunRequest request, IReadOnlyDictionary<string, string> testNames)
    {
        lock (_lock)
        {
            if (_current?.Status is RunStatus.Building or RunStatus.Running or RunStatus.Reporting)
                return false;

            _log.Clear();
            _cancellation = new CancellationTokenSource();
            _current = new RunSnapshot(
                DateTime.Now.ToString("yyyyMMdd-HHmmss"),
                RunStatus.Building,
                DateTimeOffset.Now,
                null,
                request.User,
                request.Database,
                request.Schema,
                request.AppPath,
                request.Tests.Select(id => new RunItem(id, testNames.GetValueOrDefault(id, id), TestStatus.Pending, null, 0, 0, 0, null)).ToList(),
                [],
                null,
                null,
                null);
        }

        var token = _cancellation.Token;
        _ = Task.Run(() => ExecuteAsync(request, token), CancellationToken.None);
        return true;
    }

    public void Stop()
    {
        lock (_lock)
            _cancellation?.Cancel();
    }

    private async Task ExecuteAsync(RunRequest request, CancellationToken token)
    {
        var run = Current!;
        var runDirectory = Path.Combine(paths.RunsDirectory, run.Id);
        Directory.CreateDirectory(runDirectory);

        try
        {
            AddLog("== Збирання тестів");
            var build = await ProcessRunner.RunAsync(
                "dotnet", ["build", paths.TestProjectFile, "-v", "q", "-nologo"], paths.RepositoryRoot, new Dictionary<string, string>(), AddLog, token);
            if (build != 0)
            {
                Update(state => state with { Status = RunStatus.Failed, Error = "Тести не зібралися — див. журнал.", FinishedAt = DateTimeOffset.Now });
                return;
            }

            ClearAllureResults();
            Update(state => state with { Status = RunStatus.Running });

            var environment = new Dictionary<string, string>
            {
                ["ADAICA_USER"] = request.User,
                ["ADAICA_PASSWORD"] = request.Password,
                ["ADAICA_DATABASE"] = request.Database,
                ["ADAICA_SCHEMA"] = request.Schema,
                ["ADAICA_APP_PATH"] = request.AppPath,
                ["ADAICA_KEEP_ALLURE_RESULTS"] = "true",
            };

            for (var index = 0; index < request.Tests.Count; index++)
            {
                token.ThrowIfCancellationRequested();
                var id = request.Tests[index];
                UpdateItem(id, item => item with { Status = TestStatus.Running });
                AddLog($"== [{index + 1}/{request.Tests.Count}] {id}");

                var trx = Path.Combine(runDirectory, $"{index + 1:00}.trx");
                await ProcessRunner.RunAsync(
                    "dotnet",
                    [
                        "test", paths.TestProjectFile, "--no-build", "--nologo",
                        "--settings", paths.RunSettingsFile,
                        "--logger", $"trx;LogFileName={trx}",
                        "--results-directory", runDirectory,
                        "--",
                        Filter(id),
                    ],
                    paths.RepositoryRoot,
                    environment,
                    line =>
                    {
                        // Застосунок пише в консоль кожен HTTP-запит — у журналі сторінки вони лише заважають.
                        if (!line.Contains("[HTTP ", StringComparison.Ordinal))
                            AddLog(line);
                    },
                    token);

                var results = TrxReader.Read(trx);
                UpdateItem(id, item => ToItem(item, results));
            }

            Update(state => state with { Status = RunStatus.Reporting });
            await GenerateReportAsync(run.Id, token);
            Update(state => state with { Status = RunStatus.Finished, FinishedAt = DateTimeOffset.Now });
        }
        catch (OperationCanceledException)
        {
            AddLog("== Прогін зупинено");
            Update(state => state with
            {
                Status = RunStatus.Stopped,
                FinishedAt = DateTimeOffset.Now,
                Items = state.Items.Select(item => item.Status is TestStatus.Pending or TestStatus.Running ? item with { Status = TestStatus.Skipped } : item).ToList(),
            });
            // Звіт про те, що встигло пройти, теж корисний.
            await GenerateReportAsync(run.Id, CancellationToken.None);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Прогін {RunId} завершився з помилкою", run.Id);
            AddLog($"== Помилка запускача: {ex.Message}");
            Update(state => state with { Status = RunStatus.Failed, Error = ex.Message, FinishedAt = DateTimeOffset.Now });
        }
        finally
        {
            await File.WriteAllTextAsync(Path.Combine(runDirectory, "summary.json"), JsonSerializer.Serialize(Current, JsonOptions), CancellationToken.None);
        }
    }

    /// <summary>
    /// Фільтр самого NUnit (параметр runsettings NUnit.Where): точно цей метод цього класу разом з усіма його
    /// випадками. «--filter FullyQualifiedName~Method» зачепив би й тести, назва яких лише починається так само
    /// (KontakteNavigator_NewCompany і KontakteNavigator_NewCompanyWithChildRecords), а екрановану дужку для
    /// випадків («Method\(») адаптер NUnit не розбирає.
    /// </summary>
    private static string Filter(string id)
    {
        var separator = id.LastIndexOf('.');
        return $"NUnit.Where=class == {id[..separator]} && method == {id[(separator + 1)..]}";
    }

    private static RunItem ToItem(RunItem item, IReadOnlyList<TrxResult> results)
    {
        if (results.Count == 0)
            return item with { Status = TestStatus.NotFound, Message = "dotnet test не знайшов цей тест або не запустив його — див. журнал." };

        var passed = results.Count(result => result.Outcome == "Passed");
        var failed = results.Count(result => result.Outcome is "Failed" or "Error" or "Timeout" or "Aborted");
        var skipped = results.Count - passed - failed;
        var status = failed > 0 ? TestStatus.Failed : passed > 0 ? TestStatus.Passed : TestStatus.Skipped;
        var message = results.FirstOrDefault(result => result.Message is not null && result.Outcome != "Passed")?.Message;
        return item with
        {
            Status = status,
            DurationSeconds = results.Sum(result => result.Duration.TotalSeconds),
            Passed = passed,
            Failed = failed,
            Skipped = skipped,
            Message = message,
        };
    }

    private void ClearAllureResults()
    {
        if (!Directory.Exists(paths.AllureResultsDirectory))
            return;

        foreach (var file in Directory.EnumerateFiles(paths.AllureResultsDirectory))
            File.Delete(file);
    }

    private async Task GenerateReportAsync(string runId, CancellationToken token)
    {
        if (!Directory.Exists(paths.AllureResultsDirectory) || !Directory.EnumerateFiles(paths.AllureResultsDirectory).Any())
        {
            Update(state => state with { ReportError = "Немає результатів Allure: жоден тест не запустився." });
            return;
        }

        var output = Path.Combine(paths.ReportsDirectory, runId);
        AddLog("== Генерація звіту Allure");
        try
        {
            // allure на Windows — це allure.bat, тому запускаємо через cmd, щоб спрацював PATHEXT.
            var exitCode = await ProcessRunner.RunAsync(
                "cmd.exe",
                ["/c", "allure", "generate", paths.AllureResultsDirectory, "-o", output, "--clean"],
                paths.RepositoryRoot,
                new Dictionary<string, string>(),
                AddLog,
                token);

            if (exitCode == 0 && File.Exists(Path.Combine(output, "index.html")))
                Update(state => state with { ReportUrl = $"/reports/{runId}/index.html" });
            else
                Update(state => state with { ReportError = "Allure не згенерував звіт — чи встановлено Allure CLI (команда allure)? Див. журнал." });
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            Update(state => state with { ReportError = $"Не вдалося запустити Allure: {ex.Message}" });
        }
    }

    private void AddLog(string line)
    {
        lock (_lock)
        {
            _log.Add(line);
            if (_log.Count > MaxLogLines)
                _log.RemoveRange(0, _log.Count - MaxLogLines);
        }
    }

    private void Update(Func<RunSnapshot, RunSnapshot> change)
    {
        lock (_lock)
            _current = change(_current!);
    }

    private void UpdateItem(string id, Func<RunItem, RunItem> change) =>
        Update(state => state with { Items = state.Items.Select(item => item.Id == id ? change(item) : item).ToList() });
}
