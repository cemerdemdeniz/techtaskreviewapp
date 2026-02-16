using MediatR;
using TechTaskReview.Domain.Common;

namespace TechTaskReview.Infrastructure.Persistence;

public class UnitOfWork : IUnitOfWork
{
    private readonly ApplicationDbContext _context;
    private readonly IMediator _mediator;

    public UnitOfWork(ApplicationDbContext context, IMediator mediator)
    {
        _context = context;
        _mediator = mediator;
    }

    public async Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        var domainEntities = _context.ChangeTracker.Entries<AggregateRoot>()
            .Where(e => e.Entity.DomainEvents.Any())
            .ToList();

        var domainEvents = domainEntities
            .SelectMany(e => e.Entity.DomainEvents)
            .ToList();

        domainEntities.ForEach(e => e.Entity.ClearDomainEvents());

        var result = await _context.SaveChangesAsync(ct);

        foreach (var domainEvent in domainEvents)
            await _mediator.Publish(domainEvent, ct);

        return result;
    }
}
