using PSPRpgEditor.Core;

if (args.Length is < 1 or > 2)
{
    Console.Error.WriteLine(
        "Usage: PSPRpgAssetCompiler <project.psprpg> [output.game.pak]");
    return 2;
}

try
{
    var projectPath = Path.GetFullPath(args[0]);
    var outputPath = args.Length == 2
        ? Path.GetFullPath(args[1])
        : Path.Combine(Path.GetDirectoryName(projectPath)!, "Build", "game.pak");

    var project = ProjectSerializer.Load(projectPath);
    var issues = ProjectValidator.Validate(project);
    foreach (var issue in issues)
        Console.WriteLine($"{(issue.IsError ? "error" : "warning")} {issue.Code}: {issue.Message}");

    if (issues.Any(issue => issue.IsError))
        return 1;

    GamePakCompiler.CompileToFile(project, outputPath);
    Console.WriteLine($"Built {outputPath}");
    return 0;
}
catch (Exception exception)
{
    Console.Error.WriteLine($"error BUILD_FAILED: {exception.Message}");
    return 1;
}
