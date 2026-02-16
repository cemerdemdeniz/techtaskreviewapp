using Microsoft.Extensions.Options;
using Minio;
using Minio.DataModel.Args;
using TechTaskReview.Application.Common.Interfaces;

namespace TechTaskReview.Infrastructure.FileStorage;

public class MinioFileStorageService : IFileStorageService
{
    private readonly IMinioClient _minioClient;
    private readonly FileStorageOptions _options;

    public MinioFileStorageService(IOptions<FileStorageOptions> options)
    {
        _options = options.Value;
        _minioClient = new MinioClient()
            .WithEndpoint(_options.Endpoint)
            .WithCredentials(_options.AccessKey, _options.SecretKey)
            .WithSSL(_options.UseSSL)
            .Build();
    }

    public async Task<string> StoreAsync(Stream fileStream, string fileName, CancellationToken ct)
    {
        await EnsureBucketExistsAsync(ct);

        var objectName = $"{DateTime.UtcNow:yyyy/MM/dd}/{Guid.NewGuid()}/{fileName}";

        await _minioClient.PutObjectAsync(new PutObjectArgs()
            .WithBucket(_options.BucketName)
            .WithObject(objectName)
            .WithStreamData(fileStream)
            .WithObjectSize(fileStream.Length)
            .WithContentType("application/octet-stream"), ct);

        return objectName;
    }

    public async Task<Stream> RetrieveAsync(string storagePath, CancellationToken ct)
    {
        var memoryStream = new MemoryStream();
        await _minioClient.GetObjectAsync(new GetObjectArgs()
            .WithBucket(_options.BucketName)
            .WithObject(storagePath)
            .WithCallbackStream(stream => stream.CopyTo(memoryStream)), ct);
        memoryStream.Position = 0;
        return memoryStream;
    }

    public async Task DeleteAsync(string storagePath, CancellationToken ct)
    {
        await _minioClient.RemoveObjectAsync(new RemoveObjectArgs()
            .WithBucket(_options.BucketName)
            .WithObject(storagePath), ct);
    }

    public async Task<bool> ExistsAsync(string storagePath, CancellationToken ct)
    {
        try
        {
            await _minioClient.StatObjectAsync(new StatObjectArgs()
                .WithBucket(_options.BucketName)
                .WithObject(storagePath), ct);
            return true;
        }
        catch { return false; }
    }

    private async Task EnsureBucketExistsAsync(CancellationToken ct)
    {
        var exists = await _minioClient.BucketExistsAsync(
            new BucketExistsArgs().WithBucket(_options.BucketName), ct);
        if (!exists)
        {
            await _minioClient.MakeBucketAsync(
                new MakeBucketArgs().WithBucket(_options.BucketName), ct);
        }
    }
}
