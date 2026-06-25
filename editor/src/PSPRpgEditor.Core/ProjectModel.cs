using System.ComponentModel;

namespace PSPRpgEditor.Core;

public sealed class GameProject
{
    [Browsable(false)]
    public int FormatVersion { get; set; } = 1;

    [Category("Project"), DisplayName("Game name")]
    public string Name { get; set; } = "New PSP RPG";

    [Category("Project"), DisplayName("Startup scene")]
    public string StartupScene { get; set; } = "main";

    [Browsable(false)]
    public List<SceneDocument> Scenes { get; set; } = [];

    public static GameProject CreateDefault() => new()
    {
        Scenes =
        [
            new SceneDocument
            {
                Id = "main",
                Name = "Main Scene",
                Entities =
                [
                    new EntityDocument
                    {
                        Id = "player",
                        Name = "Player",
                        X = 224,
                        Y = 120,
                        Width = 32,
                        Height = 32,
                        Color = "#4DA6FFFF"
                    }
                ]
            }
        ]
    };
}

public sealed class SceneDocument
{
    [Category("Scene"), DisplayName("ID")]
    public string Id { get; set; } = "";

    [Category("Scene")]
    public string Name { get; set; } = "";

    [Category("World")]
    public int Width { get; set; } = 480;

    [Category("World")]
    public int Height { get; set; } = 272;

    [Browsable(false)]
    public List<EntityDocument> Entities { get; set; } = [];
}

public sealed class EntityDocument
{
    [Category("Entity"), DisplayName("ID")]
    public string Id { get; set; } = "";

    [Category("Entity")]
    public string Name { get; set; } = "";

    [Category("Transform")]
    public float X { get; set; }

    [Category("Transform")]
    public float Y { get; set; }

    [Category("Transform")]
    public float Width { get; set; } = 32;

    [Category("Transform")]
    public float Height { get; set; } = 32;

    [Category("Appearance"), DisplayName("Color (RRGGBBAA)")]
    public string Color { get; set; } = "#FFFFFFFF";
}
