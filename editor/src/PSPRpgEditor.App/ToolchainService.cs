using System.Diagnostics;
using System.Text.Json;

namespace PSPRpgEditor.App;

internal sealed class EditorSettings
{
    public string PspDevPath { get; set; } = "";
    public string PpssppPath { get; set; } = "";
}

internal static class ToolchainService
{
    private static readonly string SettingsPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "PSPRpgStudio", "settings.json");

    public static EditorSettings LoadSettings()
    {
        try
        {
            if (File.Exists(SettingsPath))
                return JsonSerializer.Deserialize<EditorSettings>(
                    File.ReadAllText(SettingsPath)) ?? new EditorSettings();
        }
        catch
        {
        }
        return new EditorSettings();
    }

    public static void SaveSettings(EditorSettings settings)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath)!);
        File.WriteAllText(SettingsPath, JsonSerializer.Serialize(
            settings, new JsonSerializerOptions { WriteIndented = true }));
    }

    public static ToolchainStatus Detect(EditorSettings settings)
    {
        var pspDev = FirstExistingDirectory(
            settings.PspDevPath,
            Environment.GetEnvironmentVariable("PSPDEV") ?? "");
        var ppsspp = FirstExistingFile(
            settings.PpssppPath,
            Path.Combine(Environment.GetFolderPath(
                Environment.SpecialFolder.ProgramFiles), "PPSSPP", "PPSSPPWindows64.exe"),
            Path.Combine(Environment.GetFolderPath(
                Environment.SpecialFolder.ProgramFilesX86), "PPSSPP", "PPSSPPWindows.exe"));
        return new ToolchainStatus(pspDev, ppsspp, FindOnPath("psp-cmake"));
    }

    public static void LaunchPpsspp(string executable, string ebootPath)
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = executable,
            Arguments = $"\"{ebootPath}\"",
            UseShellExecute = true,
            WorkingDirectory = Path.GetDirectoryName(ebootPath)!
        });
    }

    private static string FirstExistingDirectory(params string[] candidates) =>
        candidates.FirstOrDefault(Directory.Exists) ?? "";

    private static string FirstExistingFile(params string[] candidates) =>
        candidates.FirstOrDefault(File.Exists) ?? "";

    private static string FindOnPath(string command)
    {
        foreach (var directory in (Environment.GetEnvironmentVariable("PATH") ?? "")
                     .Split(Path.PathSeparator))
        {
            var candidate = Path.Combine(directory, command);
            if (File.Exists(candidate) || File.Exists(candidate + ".exe"))
                return candidate;
        }
        return "";
    }
}

internal sealed record ToolchainStatus(
    string PspDevPath, string PpssppPath, string PspCmakePath)
{
    public bool HasPspToolchain =>
        !string.IsNullOrWhiteSpace(PspDevPath) ||
        !string.IsNullOrWhiteSpace(PspCmakePath);
    public bool HasPpsspp => !string.IsNullOrWhiteSpace(PpssppPath);
}
