using System.Text.Json;
using System.Text.Json.Serialization;

namespace PSPRpgEditor.Core;

public static class ProjectSerializer
{
    public const string FileExtension = ".psprpg";
    public const string FileFilter =
        "PSP RPG project (*.psprpg)|*.psprpg|Legacy JSON project (*.json)|*.json";

    internal static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public static GameProject Load(string path)
    {
        using var document = JsonDocument.Parse(File.ReadAllText(path));
        var version = document.RootElement.GetProperty("formatVersion").GetInt32();

        return version switch
        {
            1 => LoadLegacy(document.RootElement),
            GameProject.CurrentFormatVersion => LoadV2(path, document.RootElement),
            _ => throw new InvalidDataException(
                $"Unsupported project format version: {version}.")
        };
    }

    public static void Save(GameProject project, string manifestPath)
    {
        var projectDirectory = Path.GetDirectoryName(Path.GetFullPath(manifestPath))
            ?? throw new InvalidOperationException("Project path has no directory.");

        Directory.CreateDirectory(projectDirectory);
        CreateProjectDirectories(projectDirectory);

        project.FormatVersion = GameProject.CurrentFormatVersion;
        project.SceneFiles = project.Scenes
            .OrderBy(scene => scene.Id)
            .Select(scene => new SceneReference(scene.Id, ProjectPaths.ScenePath(scene.Id)))
            .ToList();

        foreach (var scene in project.Scenes)
        {
            var reference = project.SceneFiles.Single(item => item.Id == scene.Id);
            var scenePath = ResolveProjectPath(projectDirectory, reference.Path);
            Directory.CreateDirectory(Path.GetDirectoryName(scenePath)!);
            File.WriteAllText(scenePath, JsonSerializer.Serialize(scene, Options));
        }

        File.WriteAllText(manifestPath, JsonSerializer.Serialize(project, Options));
        project.WasMigrated = false;
    }

    public static void CreateProjectDirectories(string projectDirectory)
    {
        foreach (var relativePath in new[]
        {
            "Assets/Textures", "Assets/Audio", "Assets/Fonts", "Assets/Tilesets",
            "Scenes", "Animations", "Events", "RPG/Dialogs", "RPG/Items", "RPG/Quests"
        })
        {
            Directory.CreateDirectory(Path.Combine(
                projectDirectory, relativePath.Replace('/', Path.DirectorySeparatorChar)));
        }
    }

    public static string ResolveProjectPath(string projectDirectory, string relativePath)
    {
        if (Path.IsPathRooted(relativePath))
        {
            throw new InvalidDataException(
                $"Project path must be relative: {relativePath}");
        }

        var root = Path.GetFullPath(projectDirectory)
            .TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        var resolved = Path.GetFullPath(Path.Combine(
            root, relativePath.Replace('/', Path.DirectorySeparatorChar)));

        if (!resolved.StartsWith(root, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException(
                $"Project path escapes the project directory: {relativePath}");
        }
        return resolved;
    }

    private static GameProject LoadV2(string path, JsonElement root)
    {
        var project = root.Deserialize<GameProject>(Options)
            ?? throw new InvalidDataException("Project manifest is empty.");
        var projectDirectory = Path.GetDirectoryName(Path.GetFullPath(path))!;

        project.Scenes = [];
        foreach (var reference in project.SceneFiles)
        {
            var scenePath = ResolveProjectPath(projectDirectory, reference.Path);
            if (!File.Exists(scenePath))
            {
                throw new FileNotFoundException(
                    $"Scene file is missing: {reference.Path}", scenePath);
            }

            var scene = JsonSerializer.Deserialize<SceneDocument>(
                File.ReadAllText(scenePath), Options)
                ?? throw new InvalidDataException(
                    $"Scene file is empty: {reference.Path}");
            if (scene.Id != reference.Id)
            {
                throw new InvalidDataException(
                    $"Scene ID does not match manifest: {reference.Path}");
            }
            project.Scenes.Add(scene);
        }
        return project;
    }

    private static GameProject LoadLegacy(JsonElement root)
    {
        var legacy = root.Deserialize<LegacyProject>(Options)
            ?? throw new InvalidDataException("Legacy project file is empty.");
        var scenes = legacy.Scenes.Select(ConvertLegacyScene).ToList();
        var startup = scenes.FirstOrDefault(scene =>
            string.Equals(scene.LegacyId, legacy.StartupScene,
                StringComparison.OrdinalIgnoreCase)) ?? scenes.FirstOrDefault();

        if (startup is null)
        {
            throw new InvalidDataException("Legacy project contains no scenes.");
        }

        foreach (var scene in scenes)
        {
            scene.Document.Name = scene.Document.Name.Trim();
        }

        return new GameProject
        {
            ProjectId = StableGuid($"project:{legacy.Name}"),
            Name = legacy.Name,
            StartupScene = startup.Document.Id,
            Scenes = scenes.Select(scene => scene.Document).ToList(),
            SceneFiles = scenes.Select(scene =>
                new SceneReference(scene.Document.Id, ProjectPaths.ScenePath(scene.Document.Id)))
                .ToList(),
            WasMigrated = true
        };
    }

    private static ConvertedLegacyScene ConvertLegacyScene(LegacyScene legacy)
    {
        var sceneId = StableGuid($"scene:{legacy.Id}");
        var scene = new SceneDocument
        {
            Id = sceneId,
            Name = legacy.Name,
            Width = legacy.Width,
            Height = legacy.Height,
            Entities = legacy.Entities.Select(entity => new EntityDocument
            {
                Id = StableGuid($"scene:{legacy.Id}:entity:{entity.Id}"),
                Name = entity.Name,
                Transform = new Transform2D
                {
                    X = entity.X,
                    Y = entity.Y,
                    Width = entity.Width,
                    Height = entity.Height
                },
                SpriteRenderer = new SpriteRenderer { Color = entity.Color }
            }).ToList()
        };
        return new ConvertedLegacyScene(legacy.Id, scene);
    }

    private static Guid StableGuid(string value)
    {
        var hash = System.Security.Cryptography.MD5.HashData(
            System.Text.Encoding.UTF8.GetBytes(value));
        return new Guid(hash);
    }

    private sealed record ConvertedLegacyScene(string LegacyId, SceneDocument Document);

    private sealed class LegacyProject
    {
        public string Name { get; set; } = "";
        public string StartupScene { get; set; } = "";
        public List<LegacyScene> Scenes { get; set; } = [];
    }

    private sealed class LegacyScene
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public int Width { get; set; }
        public int Height { get; set; }
        public List<LegacyEntity> Entities { get; set; } = [];
    }

    private sealed class LegacyEntity
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public float X { get; set; }
        public float Y { get; set; }
        public float Width { get; set; }
        public float Height { get; set; }
        public string Color { get; set; } = "#FFFFFFFF";
    }
}
