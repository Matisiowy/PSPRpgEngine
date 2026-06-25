using System.ComponentModel;
using System.Text.Json.Serialization;

namespace PSPRpgEditor.Core;

public sealed class GameProject
{
    public const int CurrentFormatVersion = 2;

    [Browsable(false)]
    public int FormatVersion { get; set; } = CurrentFormatVersion;

    [Browsable(false)]
    public Guid ProjectId { get; set; } = Guid.NewGuid();

    [Category("Project"), DisplayName("Game name")]
    public string Name { get; set; } = "New PSP RPG";

    [Category("Project"), DisplayName("Startup scene")]
    public Guid StartupScene { get; set; }

    [Browsable(false)]
    public List<SceneReference> SceneFiles { get; set; } = [];

    [JsonIgnore, Browsable(false)]
    public List<SceneDocument> Scenes { get; set; } = [];

    [JsonIgnore, Browsable(false)]
    public bool WasMigrated { get; set; }

    public static GameProject CreateDefault()
    {
        var scene = SceneDocument.CreateDefault();
        return new GameProject
        {
            StartupScene = scene.Id,
            SceneFiles = [new SceneReference(scene.Id, ProjectPaths.ScenePath(scene.Id))],
            Scenes = [scene]
        };
    }
}

public sealed record SceneReference(Guid Id, string Path);

public sealed class SceneDocument
{
    public int FormatVersion { get; set; } = 1;

    [Category("Scene"), DisplayName("ID"), ReadOnly(true)]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Category("Scene")]
    public string Name { get; set; } = "Scene";

    [Category("World")]
    public int Width { get; set; } = 480;

    [Category("World")]
    public int Height { get; set; } = 272;

    [Browsable(false)]
    public List<EntityDocument> Entities { get; set; } = [];

    public static SceneDocument CreateDefault() => new()
    {
        Name = "Main Scene",
        Entities =
        [
            new EntityDocument
            {
                Name = "Player",
                Transform = new Transform2D
                {
                    X = 224,
                    Y = 120,
                    Width = 32,
                    Height = 32
                },
                SpriteRenderer = new SpriteRenderer
                {
                    Color = "#4DA6FFFF"
                }
            }
        ]
    };
}

public sealed class EntityDocument
{
    [Category("Entity"), DisplayName("ID"), ReadOnly(true)]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Category("Entity")]
    public string Name { get; set; } = "Entity";

    [Category("Components"), TypeConverter(typeof(ExpandableObjectConverter))]
    public Transform2D Transform { get; set; } = new();

    [Category("Components"), DisplayName("Sprite Renderer"),
     TypeConverter(typeof(ExpandableObjectConverter))]
    public SpriteRenderer SpriteRenderer { get; set; } = new();
}

public sealed class Transform2D
{
    public float X { get; set; }
    public float Y { get; set; }
    public float Width { get; set; } = 32;
    public float Height { get; set; } = 32;
    public override string ToString() => $"({X}, {Y}) {Width}x{Height}";
}

public sealed class SpriteRenderer
{
    [DisplayName("Color (RRGGBBAA)")]
    public string Color { get; set; } = "#FFFFFFFF";
    public bool Visible { get; set; } = true;
    public override string ToString() => Visible ? Color : "Hidden";
}

public static class ProjectPaths
{
    public static string ScenePath(Guid sceneId) =>
        $"Scenes/{sceneId:N}.scene.json";
}
