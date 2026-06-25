using System.Globalization;

namespace PSPRpgEditor.Core;

public sealed record ValidationIssue(string Code, string Message, bool IsError = true);

public static class ProjectValidator
{
    public static IReadOnlyList<ValidationIssue> Validate(GameProject project)
    {
        var issues = new List<ValidationIssue>();

        if (string.IsNullOrWhiteSpace(project.Name))
            issues.Add(new("PROJECT_NAME", "Project name cannot be empty."));
        if (project.ProjectId == Guid.Empty)
            issues.Add(new("PROJECT_ID", "Project ID cannot be empty."));
        if (project.Scenes.Count == 0)
            issues.Add(new("NO_SCENES", "Project must contain at least one scene."));
        if (project.Scenes.All(scene => scene.Id != project.StartupScene))
            issues.Add(new("STARTUP_SCENE", "Startup scene does not exist."));

        AddDuplicateIssues(project.Scenes.Select(scene => scene.Id), "scene", issues);
        foreach (var scene in project.Scenes)
        {
            if (scene.Width < 480 || scene.Height < 272)
                issues.Add(new("SCENE_SIZE",
                    $"Scene '{scene.Name}' must be at least 480x272."));
            AddDuplicateIssues(scene.Entities.Select(entity => entity.Id),
                $"entity in '{scene.Name}'", issues);

            foreach (var entity in scene.Entities)
            {
                if (entity.Id == Guid.Empty)
                    issues.Add(new("ENTITY_ID",
                        $"Entity '{entity.Name}' has an empty ID."));
                if (entity.Transform.Width <= 0 || entity.Transform.Height <= 0)
                    issues.Add(new("ENTITY_SIZE",
                        $"Entity '{entity.Name}' must have a positive size."));
                if (!TryParseColor(entity.SpriteRenderer.Color, out _))
                    issues.Add(new("COLOR",
                        $"Entity '{entity.Name}' has invalid RRGGBBAA color."));
            }
        }
        return issues;
    }

    public static bool TryParseColor(string value, out uint abgr)
    {
        abgr = 0;
        if (value.Length != 9 ||
            !uint.TryParse(value.AsSpan(1), NumberStyles.HexNumber,
                CultureInfo.InvariantCulture, out var rgba))
            return false;

        var red = (rgba >> 24) & 0xFF;
        var green = (rgba >> 16) & 0xFF;
        var blue = (rgba >> 8) & 0xFF;
        var alpha = rgba & 0xFF;
        abgr = (alpha << 24) | (blue << 16) | (green << 8) | red;
        return true;
    }

    private static void AddDuplicateIssues(
        IEnumerable<Guid> ids, string subject, List<ValidationIssue> issues)
    {
        foreach (var duplicate in ids.GroupBy(id => id).Where(group => group.Count() > 1))
            issues.Add(new("DUPLICATE_ID",
                $"Duplicate {subject} ID: {duplicate.Key}."));
    }
}
