using System.Diagnostics;
using System.Text;

namespace TestRunner;

/// <summary>Запуск консольної команди з потоковим читанням виводу й зупинкою всього дерева процесів.</summary>
public static class ProcessRunner
{
    /// <returns>Код завершення процесу.</returns>
    /// <remarks>
    /// Зупинка вбиває все дерево: dotnet test → testhost → ADAICA, запущена тестом. Інші екземпляри ADAICA
    /// (наприклад, користувача) не є нащадками цієї команди й лишаються.
    /// </remarks>
    public static async Task<int> RunAsync(
        string fileName,
        IEnumerable<string> arguments,
        string workingDirectory,
        IReadOnlyDictionary<string, string> environment,
        Action<string> onOutput,
        CancellationToken cancellationToken)
    {
        var startInfo = new ProcessStartInfo(fileName)
        {
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
        };
        foreach (var argument in arguments)
            startInfo.ArgumentList.Add(argument);
        foreach (var (name, value) in environment)
            startInfo.Environment[name] = value;

        using var process = new Process { StartInfo = startInfo };
        process.OutputDataReceived += (_, e) =>
        {
            if (e.Data is not null)
                onOutput(e.Data);
        };
        process.ErrorDataReceived += (_, e) =>
        {
            if (e.Data is not null)
                onOutput(e.Data);
        };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        try
        {
            await process.WaitForExitAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            if (!process.HasExited)
                process.Kill(entireProcessTree: true);
            await process.WaitForExitAsync(CancellationToken.None);
            throw;
        }

        return process.ExitCode;
    }
}
