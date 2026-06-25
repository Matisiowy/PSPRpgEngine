using PSPRpgEditor.Core;

var tests = new (string Name, Action Run)[]
{
    ("v2 project round-trip", ProjectRoundTrip),
    ("v1 migration", LegacyMigration),
    ("deterministic game.pak", DeterministicPak),
    ("game.pak inspection", PakInspection),
    ("compiled scene binary layout", CompiledSceneLayout),
    ("project path traversal rejected", PathTraversalRejected)
};

var failures = 0;
foreach (var test in tests)
{
    try
    {
        test.Run();
        Console.WriteLine($"PASS {test.Name}");
    }
    catch (Exception exception)
    {
        failures++;
        Console.Error.WriteLine($"FAIL {test.Name}: {exception.Message}");
    }
}
return failures == 0 ? 0 : 1;

static void ProjectRoundTrip()
{
    using var directory = new TempDirectory();
    var manifest = Path.Combine(directory.Path, "game.psprpg");
    var source = GameProject.CreateDefault();
    ProjectSerializer.Save(source, manifest);
    var loaded = ProjectSerializer.Load(manifest);

    Assert(loaded.FormatVersion == 2, "format version");
    Assert(loaded.ProjectId == source.ProjectId, "project ID");
    Assert(loaded.Scenes.Count == 1, "scene count");
    Assert(loaded.Scenes[0].Entities.Count == 1, "entity count");
    Assert(File.Exists(Path.Combine(directory.Path,
        loaded.SceneFiles[0].Path.Replace('/', Path.DirectorySeparatorChar))),
        "external scene file");
}

static void LegacyMigration()
{
    using var directory = new TempDirectory();
    var manifest = Path.Combine(directory.Path, "legacy.psprpg");
    File.WriteAllText(manifest, """
    {
      "formatVersion": 1,
      "name": "Legacy",
      "startupScene": "main",
      "scenes": [{
        "id": "main", "name": "Main", "width": 480, "height": 272,
        "entities": [{
          "id": "player", "name": "Player", "x": 16, "y": 32,
          "width": 32, "height": 32, "color": "#112233FF"
        }]
      }]
    }
    """);

    var project = ProjectSerializer.Load(manifest);
    Assert(project.WasMigrated, "migration flag");
    Assert(project.FormatVersion == 2, "migrated version");
    Assert(project.Scenes[0].Entities[0].Transform.X == 16, "migrated transform");

    ProjectSerializer.Save(project, manifest);
    Assert(File.Exists(Path.Combine(directory.Path, "Scenes",
        $"{project.StartupScene:N}.scene.json")), "migrated scene file");
}

static void DeterministicPak()
{
    var project = GameProject.CreateDefault();
    using var first = new MemoryStream();
    using var second = new MemoryStream();
    GamePakCompiler.Compile(project, first);
    GamePakCompiler.Compile(project, second);
    Assert(first.ToArray().SequenceEqual(second.ToArray()), "pak differs");
}

static void PakInspection()
{
    var project = GameProject.CreateDefault();
    using var stream = new MemoryStream();
    GamePakCompiler.Compile(project, stream);
    stream.Position = 0;
    var info = GamePakCompiler.Inspect(stream);
    Assert(info.Version == 1, "pak version");
    Assert(info.Entries.Count == 1, "pak entry count");
    Assert(info.StartupScene == project.StartupScene, "startup scene");
    Assert(info.Entries[0].Id == project.StartupScene, "scene ID");
    Assert(info.Entries[0].Type == GamePakCompiler.SceneResourceType, "scene type");
}

static void CompiledSceneLayout()
{
    var project = GameProject.CreateDefault();
    var entity = project.Scenes[0].Entities[0];
    using var stream = new MemoryStream();
    GamePakCompiler.Compile(project, stream);
    stream.Position = 0;
    var info = GamePakCompiler.Inspect(stream);
    var entry = info.Entries[0];
    stream.Position = entry.Offset;
    using var reader = new BinaryReader(stream);

    Assert(reader.ReadUInt32() == 1, "scene version");
    Assert(reader.ReadUInt32() == 480, "scene width");
    Assert(reader.ReadUInt32() == 272, "scene height");
    Assert(reader.ReadUInt32() == 1, "compiled entity count");
    Assert(new Guid(reader.ReadBytes(16)) == entity.Id, "compiled entity ID");
    Assert(reader.ReadSingle() == 224, "compiled x");
    Assert(reader.ReadSingle() == 120, "compiled y");
    Assert(reader.ReadSingle() == 32, "compiled width");
    Assert(reader.ReadSingle() == 32, "compiled height");
    Assert(reader.ReadUInt32() == 0xFFFFA64D, "compiled ABGR color");
    Assert(reader.ReadUInt32() == 1, "compiled visibility flags");
}

static void PathTraversalRejected()
{
    using var directory = new TempDirectory();
    try
    {
        ProjectSerializer.ResolveProjectPath(directory.Path, "../outside.json");
        throw new Exception("path traversal was accepted");
    }
    catch (InvalidDataException)
    {
    }
}

static void Assert(bool condition, string message)
{
    if (!condition)
        throw new Exception(message);
}

sealed class TempDirectory : IDisposable
{
    public string Path { get; } = System.IO.Path.Combine(
        System.IO.Path.GetTempPath(), "psprpg-tests", Guid.NewGuid().ToString("N"));

    public TempDirectory() => Directory.CreateDirectory(Path);
    public void Dispose() => Directory.Delete(Path, recursive: true);
}
