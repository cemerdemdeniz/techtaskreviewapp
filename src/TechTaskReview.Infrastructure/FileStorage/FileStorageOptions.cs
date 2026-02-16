namespace TechTaskReview.Infrastructure.FileStorage;

public class FileStorageOptions
{
    public string Provider { get; set; } = "MinIO";
    public string Endpoint { get; set; } = "localhost:9000";
    public string AccessKey { get; set; } = "minioadmin";
    public string SecretKey { get; set; } = "minioadmin";
    public string BucketName { get; set; } = "techtask-submissions";
    public bool UseSSL { get; set; } = false;
}
