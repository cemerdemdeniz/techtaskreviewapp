namespace TechTaskReview.Infrastructure.Git;

public class GitCloningOptions
{
    public string TempDirectory { get; set; } = "/tmp/git-clones";
    public long MaxRepoSizeBytes { get; set; } = 500 * 1024 * 1024;
    public int CloneTimeoutSeconds { get; set; } = 300;
}
