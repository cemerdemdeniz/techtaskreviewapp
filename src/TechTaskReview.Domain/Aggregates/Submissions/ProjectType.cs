using TechTaskReview.Domain.Common;

namespace TechTaskReview.Domain.Aggregates.Submissions;

public class ProjectType : ValueObject
{
    public string PrimaryLanguage { get; }        // "TypeScript", "C#"
    public string? Framework { get; }              // "React", "ASP.NET Core"
    public string[] BuildFiles { get; }            // ["package.json"], [".csproj"]
    public bool IsMonorepo { get; }

    public ProjectType(string primaryLanguage, string? framework, string[] buildFiles, bool isMonorepo)
    {
        PrimaryLanguage = primaryLanguage;
        Framework = framework;
        BuildFiles = buildFiles;
        IsMonorepo = isMonorepo;
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return PrimaryLanguage;
        yield return Framework;
        yield return IsMonorepo;
    }
}
