using System.Buffers.Binary;
using System.Text;

namespace PSPRpgEditor.Core;

public static class GamePakCompiler
{
    public const uint PakVersion = 1;
    public const uint SceneResourceType = 1;
    public const int HeaderSize = 48;
    public const int EntrySize = 32;
    private static readonly byte[] Magic = "PRPGPAK\0"u8.ToArray();

    public static void Compile(GameProject project, Stream output)
    {
        var errors = ProjectValidator.Validate(project).Where(issue => issue.IsError).ToList();
        if (errors.Count != 0)
            throw new InvalidDataException(string.Join(Environment.NewLine,
                errors.Select(issue => $"{issue.Code}: {issue.Message}")));

        var resources = project.Scenes.OrderBy(scene => scene.Id)
            .Select(scene => new Resource(
                SceneResourceType, scene.Id, CompileScene(scene)))
            .ToList();
        var dataOffset = HeaderSize + resources.Count * EntrySize;
        var currentOffset = dataOffset;

        using var writer = new BinaryWriter(output, Encoding.UTF8, leaveOpen: true);
        writer.Write(Magic);
        writer.Write(PakVersion);
        writer.Write((uint)resources.Count);
        writer.Write((uint)HeaderSize);
        writer.Write((uint)dataOffset);
        writer.Write(project.StartupScene.ToByteArray());
        writer.Write((uint)0);
        writer.Write((uint)0);

        foreach (var resource in resources)
        {
            writer.Write(resource.Type);
            writer.Write(Fnv1A(resource.Id.ToString("N")));
            writer.Write((uint)currentOffset);
            writer.Write((uint)resource.Data.Length);
            writer.Write(resource.Id.ToByteArray());
            currentOffset += resource.Data.Length;
        }
        foreach (var resource in resources)
            writer.Write(resource.Data);
    }

    public static void CompileToFile(GameProject project, string outputPath)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(outputPath))!);
        using var stream = File.Create(outputPath);
        Compile(project, stream);
    }

    public static GamePakInfo Inspect(Stream input)
    {
        Span<byte> header = stackalloc byte[HeaderSize];
        input.ReadExactly(header);
        if (!header[..8].SequenceEqual(Magic))
            throw new InvalidDataException("Invalid game.pak magic.");

        var version = BinaryPrimitives.ReadUInt32LittleEndian(header[8..]);
        var count = BinaryPrimitives.ReadUInt32LittleEndian(header[12..]);
        var startupScene = new Guid(header[24..40]);
        var entries = new List<GamePakEntry>((int)count);
        Span<byte> entry = stackalloc byte[EntrySize];
        for (var index = 0; index < count; index++)
        {
            input.ReadExactly(entry);
            entries.Add(new GamePakEntry(
                BinaryPrimitives.ReadUInt32LittleEndian(entry),
                new Guid(entry[16..32]),
                BinaryPrimitives.ReadUInt32LittleEndian(entry[8..]),
                BinaryPrimitives.ReadUInt32LittleEndian(entry[12..])));
        }
        return new GamePakInfo(version, startupScene, entries);
    }

    private static byte[] CompileScene(SceneDocument scene)
    {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true);
        var visibleEntities = scene.Entities
            .Where(entity => entity.SpriteRenderer.Visible).ToList();

        writer.Write((uint)1);
        writer.Write((uint)scene.Width);
        writer.Write((uint)scene.Height);
        writer.Write((uint)visibleEntities.Count);
        foreach (var entity in visibleEntities)
        {
            ProjectValidator.TryParseColor(entity.SpriteRenderer.Color, out var color);
            writer.Write(entity.Id.ToByteArray());
            writer.Write(entity.Transform.X);
            writer.Write(entity.Transform.Y);
            writer.Write(entity.Transform.Width);
            writer.Write(entity.Transform.Height);
            writer.Write(color);
            writer.Write((uint)1);
        }
        return stream.ToArray();
    }

    private static uint Fnv1A(string value)
    {
        var hash = 2166136261u;
        foreach (var character in Encoding.UTF8.GetBytes(value))
        {
            hash ^= character;
            hash *= 16777619;
        }
        return hash;
    }

    private sealed record Resource(uint Type, Guid Id, byte[] Data);
}

public sealed record GamePakEntry(uint Type, Guid Id, uint Offset, uint Size);
public sealed record GamePakInfo(
    uint Version, Guid StartupScene, IReadOnlyList<GamePakEntry> Entries);
