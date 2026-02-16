using TechTaskReview.Domain.Aggregates.Submissions;

namespace TechTaskReview.Infrastructure.BackgroundJobs;

public class ProjectTypeDetector
{
    public ProjectType Detect(string extractedPath)
    {
        var rootFiles = Directory.GetFiles(extractedPath).Select(Path.GetFileName)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var allFiles = Directory.GetFiles(extractedPath, "*.*", SearchOption.AllDirectories)
            .Select(f => Path.GetRelativePath(extractedPath, f)).ToList();

        var buildFiles = new List<string>();
        string? language = null;
        string? framework = null;
        bool isMonorepo = rootFiles.Contains("lerna.json") || rootFiles.Contains("pnpm-workspace.yaml") || rootFiles.Contains("nx.json");

        if (rootFiles.Contains("package.json"))
        {
            buildFiles.Add("package.json");
            var content = File.ReadAllText(Path.Combine(extractedPath, "package.json"));
            language = allFiles.Any(f => f.EndsWith(".tsx") || f.EndsWith(".ts")) ? "TypeScript" : "JavaScript";
            framework = content.Contains("\"next\"") ? "Next.js"
                : content.Contains("\"react\"") ? "React"
                : content.Contains("\"vue\"") ? "Vue"
                : content.Contains("\"@angular/core\"") ? "Angular" : "Node.js";
        }

        var csprojs = allFiles.Where(f => f.EndsWith(".csproj")).ToList();
        if (csprojs.Count > 0)
        {
            buildFiles.AddRange(csprojs);
            language ??= "C#";
            var content = File.ReadAllText(Path.Combine(extractedPath, csprojs.First()));
            framework ??= content.Contains("Microsoft.AspNetCore") ? "ASP.NET Core" : ".NET";
        }

        if (language is null)
        {
            var ext = allFiles.GroupBy(f => Path.GetExtension(f).ToLower())
                .OrderByDescending(g => g.Count()).FirstOrDefault()?.Key;
            language = ext switch
            {
                ".py" => "Python", ".java" => "Java", ".go" => "Go",
                ".rs" => "Rust", ".rb" => "Ruby", ".php" => "PHP",
                _ => "Unknown"
            };
        }

        return new ProjectType(language ?? "Unknown", framework, buildFiles.ToArray(), isMonorepo);
    }
}
