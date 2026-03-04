using Microsoft.Extensions.Configuration;

namespace RacingAgentClaude.Tools;

public class FileSystemTool
{
    private readonly string _outputPath;

    private static readonly HashSet<string> AllowedExtensions =
        [".ts", ".html", ".scss", ".css", ".json", ".md"];

    private static readonly HashSet<string> IgnoredFolders =
        ["node_modules", ".git", "dist", ".angular", "coverage"];

    public FileSystemTool(IConfiguration config)
    {
        _outputPath = config["Agent:FrontendOutputPath"] ?? "./frontend-output";
        Directory.CreateDirectory(_outputPath);
    }

    public string OutputPath => Path.GetFullPath(_outputPath);

    public IEnumerable<string> ListFiles() =>
        Directory.Exists(_outputPath)
            ? Directory
                .GetFiles(_outputPath, "*.*", SearchOption.AllDirectories)
                .Where(f => !IsIgnored(f))
                .Where(f => AllowedExtensions.Contains(Path.GetExtension(f).ToLower()))
                .Select(f => Path.GetRelativePath(_outputPath, f).Replace('\\', '/'))
                .OrderBy(f => f)
            : [];

    public void WriteFile(string relativePath, string content)
    {
        relativePath = relativePath.Replace('/', Path.DirectorySeparatorChar);
        var fullPath = Path.GetFullPath(Path.Combine(_outputPath, relativePath));

        if (!fullPath.StartsWith(Path.GetFullPath(_outputPath)))
            throw new UnauthorizedAccessException($"Path traversal blocked: {relativePath}");

        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        File.WriteAllText(fullPath, content);
    }

    private bool IsIgnored(string path) =>
        IgnoredFolders.Any(f =>
            path.Contains($"{Path.DirectorySeparatorChar}{f}{Path.DirectorySeparatorChar}") ||
            path.EndsWith($"{Path.DirectorySeparatorChar}{f}"));
}