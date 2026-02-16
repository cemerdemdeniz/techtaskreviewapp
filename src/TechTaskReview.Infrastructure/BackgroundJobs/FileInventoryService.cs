using TechTaskReview.Domain.Aggregates.Submissions;

namespace TechTaskReview.Infrastructure.BackgroundJobs;

public class FileInventoryService
{
    private static readonly Dictionary<string, string> ExtensionToLanguage = new(StringComparer.OrdinalIgnoreCase)
    {
        [".ts"] = "TypeScript", [".tsx"] = "TypeScript",
        [".js"] = "JavaScript", [".jsx"] = "JavaScript",
        [".cs"] = "C#", [".py"] = "Python", [".java"] = "Java",
        [".go"] = "Go", [".rs"] = "Rust", [".rb"] = "Ruby",
        [".css"] = "CSS", [".scss"] = "SCSS", [".html"] = "HTML",
        [".json"] = "JSON", [".yaml"] = "YAML", [".yml"] = "YAML",
        [".md"] = "Markdown", [".sql"] = "SQL",
    };

    private static readonly string[] TestIndicators = ["test", "spec", "__tests__", "__mocks__", ".test.", ".spec."];
    private static readonly string[] ConfigIndicators = [".config.", "tsconfig", "eslint", "prettier", "webpack", "vite.config", "jest.config", "babel", ".env", "docker", "Dockerfile"];

    public List<SubmissionFile> BuildInventory(Guid submissionId, string extractedPath)
    {
        var results = new List<SubmissionFile>();

        foreach (var filePath in Directory.EnumerateFiles(extractedPath, "*.*", SearchOption.AllDirectories))
        {
            var ext = Path.GetExtension(filePath).ToLowerInvariant();
            if (!ExtensionToLanguage.TryGetValue(ext, out var lang)) continue;

            var relativePath = Path.GetRelativePath(extractedPath, filePath);
            if (relativePath.Contains("node_modules") || relativePath.Contains(".git/") || relativePath.Contains("/bin/") || relativePath.Contains("/obj/")) continue;

            var lineCount = File.ReadLines(filePath).Count();
            var fileInfo = new FileInfo(filePath);
            var lower = relativePath.ToLowerInvariant();

            results.Add(SubmissionFile.Create(submissionId, relativePath, lang, lineCount, fileInfo.Length,
                TestIndicators.Any(t => lower.Contains(t)),
                ConfigIndicators.Any(c => lower.Contains(c))));
        }

        return results;
    }
}
