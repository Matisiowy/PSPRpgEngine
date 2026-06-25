using System.Text.Json;

namespace PSPRpgEditor.Core;

public static class ProjectSerializer
{
    public const string FileExtension = ".psprpg";
    public const string FileFilter =
        "PSP RPG project (*.psprpg)|*.psprpg|Legacy JSON project (*.json)|*.json";

    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    public static GameProject Load(string path)
    {
        using var stream = File.OpenRead(path);
        var project = JsonSerializer.Deserialize<GameProject>(stream, Options)
            ?? throw new InvalidDataException("Project file is empty.");

        if (project.FormatVersion != 1)
        {
            throw new InvalidDataException(
                $"Unsupported project format version: {project.FormatVersion}.");
        }

        return project;
    }

    public static void Save(GameProject project, string path)
    {
        var directory = Path.GetDirectoryName(Path.GetFullPath(path));
        if (directory is not null)
        {
            Directory.CreateDirectory(directory);
        }

        using var stream = File.Create(path);
        JsonSerializer.Serialize(stream, project, Options);
    }
}
