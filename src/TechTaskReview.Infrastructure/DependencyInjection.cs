using Hangfire;
using Hangfire.Redis.StackExchange;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TechTaskReview.Application.Common.Interfaces;
using TechTaskReview.Application.Submissions.EventHandlers;
using TechTaskReview.Domain.Common;
using TechTaskReview.Infrastructure.AI;
using TechTaskReview.Infrastructure.AI.Chunking;
using TechTaskReview.Infrastructure.AI.Prompts;
using TechTaskReview.Infrastructure.AI.Providers;
using TechTaskReview.Infrastructure.BackgroundJobs;
using TechTaskReview.Infrastructure.Caching;
using TechTaskReview.Infrastructure.Export;
using TechTaskReview.Infrastructure.FileStorage;
using TechTaskReview.Infrastructure.Git;
using TechTaskReview.Infrastructure.Persistence;
using TechTaskReview.Infrastructure.Persistence.Interceptors;
using TechTaskReview.Infrastructure.Persistence.Repositories;
using TechTaskReview.Infrastructure.Security;
using TechTaskReview.Infrastructure.StaticAnalysis;
using TechTaskReview.Infrastructure.StaticAnalysis.Analyzers;

namespace TechTaskReview.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Persistence
        services.AddScoped<AuditableEntityInterceptor>();
        services.AddDbContext<ApplicationDbContext>((sp, options) =>
        {
            var interceptor = sp.GetRequiredService<AuditableEntityInterceptor>();
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection"))
                   .AddInterceptors(interceptor);
        });
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // Repositories
        services.AddScoped<ISubmissionRepository, SubmissionRepository>();
        services.AddScoped<ICodeReviewRepository, CodeReviewRepository>();
        services.AddScoped<ICandidateRepository, CandidateRepository>();
        services.AddScoped<IScoringConfigRepository, ScoringConfigRepository>();
        services.AddScoped<IUserRepository, UserRepository>();

        // File storage
        services.Configure<FileStorageOptions>(configuration.GetSection("FileStorage"));
        services.AddScoped<IFileStorageService, MinioFileStorageService>();

        // Git
        services.Configure<GitCloningOptions>(configuration.GetSection("Git"));
        services.AddScoped<IGitCloningService, GitCloningService>();

        // Malware scanning
        services.AddHttpClient<IMalwareScannerService, ClamAvScannerService>(client =>
        {
            client.BaseAddress = new Uri(configuration["Malware:ClamAvUrl"] ?? "http://clamav:3310");
            client.Timeout = TimeSpan.FromMinutes(5);
        });

        // Static analysis
        services.AddScoped<IStaticAnalyzer, ESLintAnalyzer>();
        services.AddScoped<IStaticAnalyzer, DotNetAnalyzer>();
        services.AddScoped<IStaticAnalysisService, StaticAnalysisOrchestrator>();

        // AI
        services.Configure<AIProviderOptions>(configuration.GetSection("AI"));
        services.AddHttpClient<OllamaProvider>();
        services.AddHttpClient<OpenAIProvider>();
        services.AddScoped<IAIProvider>(sp => sp.GetRequiredService<OllamaProvider>());
        services.AddScoped<IAIProvider>(sp => sp.GetRequiredService<OpenAIProvider>());
        services.AddScoped<IAIProviderFactory, AIProviderFactory>();
        services.AddScoped<PromptTemplateService>();
        services.AddScoped<IFileChunker>(sp =>
        {
            var aiOptions = configuration.GetSection("AI").Get<AIProviderOptions>() ?? new();
            return new TokenAwareChunker(new ChunkingOptions
            {
                MaxTokensPerChunk = 3000,
                MaxChunksPerSubmission = aiOptions.MaxChunksPerSubmission
            });
        });
        services.AddScoped<IAIReviewService, AIReviewService>();

        // Caching
        services.AddStackExchangeRedisCache(options =>
            options.Configuration = configuration.GetConnectionString("Redis") ?? "localhost:6379");
        services.AddScoped<ICacheService, RedisCacheService>();

        // Background jobs
        services.AddHangfire(config => config
            .UseRedisStorage(configuration.GetConnectionString("Redis") ?? "localhost:6379"));
        services.AddHangfireServer(options =>
        {
            options.Queues = ["critical", "default", "ai-review", "export"];
            options.WorkerCount = Environment.ProcessorCount * 2;
        });
        services.AddScoped<IFileProcessingJob, FileProcessingJob>();

        // PDF Export
        services.AddScoped<IPdfExportService, QuestPdfExportService>();

        return services;
    }
}
