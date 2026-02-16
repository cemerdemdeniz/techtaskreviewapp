using TechTaskReview.Domain.Common;

namespace TechTaskReview.Domain.Aggregates.Reviews;

public class ReviewVersion : ValueObject
{
    public int Major { get; }
    public int Minor { get; }
    public string ScoringModelVersion { get; }

    public ReviewVersion(int major, int minor, string scoringModelVersion)
    {
        Major = major;
        Minor = minor;
        ScoringModelVersion = scoringModelVersion;
    }

    public static ReviewVersion Current => new(1, 0, "v1.0");

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Major;
        yield return Minor;
        yield return ScoringModelVersion;
    }

    public override string ToString() => $"{Major}.{Minor}-{ScoringModelVersion}";
}
