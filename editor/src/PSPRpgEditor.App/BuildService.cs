using System.Diagnostics;
using PSPRpgEditor.Core;

namespace PSPRpgEditor.App;

internal sealed record BuildResult(
    string OutputDirectory, string GamePakPath, string? EbootPath,
    IReadOnlyList<string> Messages);

internal static class BuildService
{
    public static BuildResult Build(
        GameProject project, string manifestPath, ToolchainStatus toolchain)
    {
        var projectDirectory = Path.GetDirectoryName(Path.GetFullPath(manifestPath))!;
        var safeName = MakeSafeFileName(project.Name);
        var outputDirectory = Path.Combine(
            projectDirectory, "Build", "PSP", "GAME", safeName);
        Directory.CreateDirectory(outputDirectory);

        var messages = new List<string>();
        var pakPath = Path.Combine(outputDirectory, "game.pak");
        GamePakCompiler.CompileToFile(project, pakPath);
        messages.Add($"Compiled {project.Scenes.Count} scene(s) to game.pak.");

        var runtimeEboot = FindRuntimeEboot(projectDirectory);
        string? outputEboot = null;
        if (runtimeEboot is not null)
        {
            outputEboot = Path.Combine(outputDirectory, "EBOOT.PBP");
            File.Copy(runtimeEboot, outputEboot, overwrite: true);
            messages.Add("Copied PSP runtime EBOOT.PBP.");
        }
        else
        {
            messages.Add(toolchain.HasPspToolchain
                ? "PSP toolchain detected, but runtime/EBOOT.PBP has not been built yet."
                : "game.pak built. Configure PSPSDK to produce EBOOT.PBP.");
        }

        return new BuildResult(outputDirectory, pakPath, outputEboot, messages);
    }

    public static async Task<(int ExitCode, string Output)> BuildRuntimeAsync(
        string repositoryRoot, CancellationToken cancellationToken = default)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "powershell",
            Arguments = "-NoProfile -Command \"psp-cmake -S runtime -B build/psp; " +
                        "if ($LASTEXITCODE -eq 0) { cmake --build build/psp }\"",
            WorkingDirectory = repositoryRoot,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        using var process = Process.Start(startInfo)
            ?? throw new InvalidOperationException("Could not start PSP build.");
        var outputTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var errorTask = process.StandardError.ReadToEndAsync(cancellationToken);
        await process.WaitForExitAsync(cancellationToken);
        return (process.ExitCode,
            await outputTask + Environment.NewLine + await errorTask);
    }

    public static string? FindRepositoryRoot(string startDirectory)
    {
        var directory = new DirectoryInfo(startDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(
                    directory.FullName, "runtime", "CMakeLists.txt")))
                return directory.FullName;
            directory = directory.Parent;
        }
        return null;
    }

    private static string? FindRuntimeEboot(string projectDirectory)
    {
        var repositoryRoot = FindRepositoryRoot(projectDirectory);
        if (repositoryRoot is null)
            return null;
        var candidate = Path.Combine(repositoryRoot, "build", "psp", "EBOOT.PBP");
        return File.Exists(candidate) ? candidate : null;
    }

    private static string MakeSafeFileName(string name)
    {
        foreach (var invalid in Path.GetInvalidFileNameChars())
            name = name.Replace(invalid, '_');
        return string.IsNullOrWhiteSpace(name) ? "Game" : name;
    }
}
