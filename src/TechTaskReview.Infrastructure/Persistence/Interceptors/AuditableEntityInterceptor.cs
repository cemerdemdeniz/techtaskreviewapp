using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using TechTaskReview.Application.Common.Interfaces;
using TechTaskReview.Domain.Common;

namespace TechTaskReview.Infrastructure.Persistence.Interceptors;

public class AuditableEntityInterceptor : SaveChangesInterceptor
{
    private readonly ICurrentUserService _currentUser;
    private readonly IHttpContextAccessor _httpContext;

    public AuditableEntityInterceptor(ICurrentUserService currentUser, IHttpContextAccessor httpContext)
    {
        _currentUser = currentUser;
        _httpContext = httpContext;
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result, CancellationToken ct = default)
    {
        var context = eventData.Context;
        if (context is null) return base.SavingChangesAsync(eventData, result, ct);

        var entries = context.ChangeTracker.Entries<Entity>()
            .Where(e => e.State is EntityState.Added or EntityState.Modified)
            .ToList();

        foreach (var entry in entries)
        {
            if (entry.State == EntityState.Modified)
                entry.Property(nameof(Entity.UpdatedAt)).CurrentValue = DateTime.UtcNow;
        }

        // Add audit log entries
        var auditEntries = context.ChangeTracker.Entries()
            .Where(e => e.Entity is not AuditLogEntry && e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            .Select(e => new AuditLogEntry
            {
                UserId = _currentUser.UserId ?? "system",
                Action = e.State.ToString(),
                EntityType = e.Entity.GetType().Name,
                EntityId = e.Property("Id").CurrentValue?.ToString(),
                NewValues = e.State != EntityState.Deleted ? SerializeValues(e) : null,
                IpAddress = _httpContext.HttpContext?.Connection.RemoteIpAddress?.ToString()
            })
            .ToList();

        foreach (var audit in auditEntries)
            context.Set<AuditLogEntry>().Add(audit);

        return base.SavingChangesAsync(eventData, result, ct);
    }

    private static string SerializeValues(Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry entry)
    {
        var dict = entry.Properties
            .Where(p => !p.Metadata.IsShadowProperty())
            .ToDictionary(p => p.Metadata.Name, p => p.CurrentValue?.ToString());
        return JsonSerializer.Serialize(dict);
    }
}
