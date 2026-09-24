using System.Diagnostics;
using System.Text.Json.Serialization;
using Microsoft.Extensions.FileProviders;
using TestRunner;

// Веб-запускач тестів ADAICA_AUTOTEST. Слухає лише 127.0.0.1: сторінка отримує пароль до бази й запускає тести,
// тож з інших машин вона недоступна.
var paths = RunnerPaths.Locate();

// Сторінку віддаємо з теки проєкту, а не з bin: так правки в wwwroot видно без перезбирання.
var projectDirectory = Path.Combine(paths.RepositoryRoot, "tools", "TestRunner");
var builder = WebApplication.CreateBuilder(new WebApplicationOptions
{
    Args = args,
    ContentRootPath = projectDirectory,
    WebRootPath = Path.Combine(projectDirectory, "wwwroot"),
});
builder.WebHost.UseUrls(builder.Configuration["urls"] ?? "http://127.0.0.1:5080");
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

Directory.CreateDirectory(paths.ReportsDirectory);
builder.Services.AddSingleton(paths);
builder.Services.AddSingleton<RunManager>();

var app = builder.Build();

// Захист від DNS rebinding: чужий сайт не зможе видати себе за localhost.
app.Use(async (context, next) =>
{
    if (context.Request.Host.Host is not ("127.0.0.1" or "localhost"))
    {
        context.Response.StatusCode = StatusCodes.Status400BadRequest;
        return;
    }

    await next();
});

app.UseDefaultFiles();
app.UseStaticFiles();
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(paths.ReportsDirectory),
    RequestPath = "/reports",
    // У звіті Allure є файли з нестандартними розширеннями.
    ServeUnknownFileTypes = true,
});

IReadOnlyList<TestGroup>? catalog = null;
string? catalogError = null;
void ReloadCatalog()
{
    try
    {
        catalog = TestCatalog.Load(paths);
        catalogError = null;
    }
    catch (Exception ex)
    {
        catalog = [];
        catalogError = ex.Message;
    }
}

ReloadCatalog();

app.MapGet("/api/tests", () => Results.Ok(new
{
    groups = catalog,
    error = catalogError,
    builtAt = File.Exists(paths.TestAssembly) ? File.GetLastWriteTime(paths.TestAssembly) : (DateTime?)null,
}));

// Перезбирає тести й перечитує список (наприклад, після додавання нового тесту).
app.MapPost("/api/tests/refresh", async (RunManager runs, CancellationToken token) =>
{
    if (runs.IsBusy)
        return Results.Conflict(new { error = "Триває прогін — список оновиться після нього." });

    var output = new List<string>();
    var exitCode = await ProcessRunner.RunAsync(
        "dotnet", ["build", paths.TestProjectFile, "-v", "q", "-nologo"], paths.RepositoryRoot, new Dictionary<string, string>(), output.Add, token);
    ReloadCatalog();
    return exitCode == 0
        ? Results.Ok(new { groups = catalog, error = catalogError })
        : Results.Ok(new { groups = catalog, error = "Тести не зібралися:\n" + string.Join('\n', output.TakeLast(30)) });
});

// Значення за замовчуванням — ті самі, що й у тестів: .runsettings, змінні середовища, пароль лише зі змінної.
app.MapGet("/api/defaults", () =>
{
    var parameters = paths.ReadRunSettingsParameters();
    string Value(string environmentVariable, string parameter) =>
        Environment.GetEnvironmentVariable(environmentVariable) is { Length: > 0 } fromEnvironment
            ? fromEnvironment
            : parameters.GetValueOrDefault(parameter, string.Empty);

    var password = Environment.GetEnvironmentVariable("ADAICA_PASSWORD")
        ?? Environment.GetEnvironmentVariable("ADAICA_PASSWORD", EnvironmentVariableTarget.User)
        ?? string.Empty;

    return Results.Ok(new
    {
        user = Value("ADAICA_USER", "UserName"),
        password,
        database = Value("ADAICA_DATABASE", "Database"),
        schema = Value("ADAICA_SCHEMA", "Schema"),
        appPath = Value("ADAICA_APP_PATH", "AppPath"),
    });
});

app.MapGet("/api/run", (RunManager runs) => runs.Current is { } run ? Results.Ok(run) : Results.NoContent());

app.MapPost("/api/run", (RunRequest request, RunManager runs) =>
{
    if (request.Tests.Count == 0)
        return Results.BadRequest(new { error = "Не вибрано жодного тесту." });
    if (new[] { request.User, request.Password, request.Database, request.Schema, request.AppPath }.Any(string.IsNullOrWhiteSpace))
        return Results.BadRequest(new { error = "Заповніть користувача, пароль, базу, схему й шлях до ADAICA.exe." });
    if (!File.Exists(request.AppPath))
        return Results.BadRequest(new { error = $"ADAICA.exe не знайдено: {request.AppPath}" });

    var known = (catalog ?? []).SelectMany(group => group.Tests).ToDictionary(test => test.Id, test => test.Name);
    var unknown = request.Tests.Where(id => !known.ContainsKey(id)).ToList();
    if (unknown.Count > 0)
        return Results.BadRequest(new { error = $"Невідомі тести: {string.Join(", ", unknown)}. Оновіть список." });

    return runs.TryStart(request, known)
        ? Results.Accepted("/api/run", runs.Current)
        : Results.Conflict(new { error = "Прогін уже триває." });
});

app.MapPost("/api/run/stop", (RunManager runs) =>
{
    runs.Stop();
    return Results.Accepted();
});

app.Lifetime.ApplicationStarted.Register(() =>
{
    var url = app.Urls.FirstOrDefault() ?? "http://127.0.0.1:5080";
    Console.WriteLine($"Запускач тестів: {url}  (Ctrl+C — зупинити)");
    if (builder.Configuration["no-browser"] is null)
        Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
});

app.Run();
