using System.Linq.Expressions;
using System.Reflection;
using ClaimsModule.Application.Common.Events;
using ClaimsModule.Domain.Common;
using ClaimsModule.Domain.Entities;
using ClaimsModule.Persistence.Idempotency;
using ClaimsModule.Persistence.Seeding;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClaimsModule.Persistence;

/// <summary>
/// EF Core unit of work for the claims module. On save it drains domain events from tracked
/// aggregates and publishes them as MediatR notifications, so audit entries are written within the
/// same transaction (FRS: domain events handled to write audit log; Unit of Work coordinates persistence).
/// </summary>
public sealed class ClaimsDbContext(DbContextOptions<ClaimsDbContext> options, IPublisher publisher)
    : DbContext(options)
{
    public DbSet<Claim> Claims => Set<Claim>();
    public DbSet<LossEvent> LossEvents => Set<LossEvent>();
    public DbSet<ClaimParty> ClaimParties => Set<ClaimParty>();
    public DbSet<ClaimRiskObject> ClaimRiskObjects => Set<ClaimRiskObject>();
    public DbSet<ClaimReserveComponent> ClaimReserveComponents => Set<ClaimReserveComponent>();
    public DbSet<ReserveHistory> ReserveHistory => Set<ReserveHistory>();
    public DbSet<ClaimDocument> ClaimDocuments => Set<ClaimDocument>();
    public DbSet<ClaimAuditLog> ClaimAuditLogs => Set<ClaimAuditLog>();
    public DbSet<CauseOfLossCode> CauseOfLossCodes => Set<CauseOfLossCode>();
    public DbSet<Policy> Policies => Set<Policy>();
    public DbSet<ClaimStatusTransition> ClaimStatusTransitions => Set<ClaimStatusTransition>();
    public DbSet<ClaimNumberSequence> ClaimNumberSequences => Set<ClaimNumberSequence>();
    public DbSet<IdempotencyRecord> IdempotencyRecords => Set<IdempotencyRecord>();

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        // FRS §15.1: all monetary fields DECIMAL(19,4); all timestamps DATETIMEOFFSET(7).
        configurationBuilder.Properties<decimal>().HavePrecision(19, 4);
        configurationBuilder.Properties<DateTimeOffset>().HaveColumnType("datetimeoffset(7)");
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            var clrType = entityType.ClrType;

            // Global query filters (FRS §15.2 + §6.1 "soft delete, tenant isolation"): every read is
            // automatically scoped to non-deleted rows of the current tenant. The tenant is the single
            // seeded OrganisationId (§15.1); in a multi-tenant deployment this constant would be replaced
            // by the current user's tenant via a DbContext member. Both predicates are combined because
            // EF Core allows only one query filter per entity.
            var isSoftDelete = typeof(ISoftDelete).IsAssignableFrom(clrType);
            var isTenantScoped = typeof(ITenantScoped).IsAssignableFrom(clrType);
            if (isSoftDelete || isTenantScoped)
            {
                var parameter = Expression.Parameter(clrType, "e");
                Expression? predicate = null;

                if (isSoftDelete)
                    predicate = Expression.Not(Expression.Property(parameter, nameof(ISoftDelete.IsDeleted)));

                if (isTenantScoped)
                {
                    var tenantPredicate = Expression.Equal(
                        Expression.Property(parameter, nameof(ITenantScoped.OrganisationId)),
                        Expression.Constant(SeedConstants.OrganisationId));
                    predicate = predicate is null ? tenantPredicate : Expression.AndAlso(predicate, tenantPredicate);
                }

                modelBuilder.Entity(clrType).HasQueryFilter(Expression.Lambda(predicate!, parameter));

                // FRS §15.1: BIT flags are NOT NULL DEFAULT 0. IsDeleted defaults to false at the DB level.
                if (isSoftDelete)
                    modelBuilder.Entity(clrType).Property(nameof(ISoftDelete.IsDeleted)).HasDefaultValue(false);
            }

            // GUID keys are assigned client-side as sequential (COMB) GUIDs (see SequentialGuid), which
            // satisfies the FRS §15.1 "sequential GUIDs to avoid fragmentation" intent. They are marked
            // ValueGeneratedNever so EF always sends the supplied value and correctly treats a new entity
            // added to a loaded aggregate's navigation as an INSERT — a store-generated key (e.g. a
            // NEWSEQUENTIALID default) would make EF assume the row already exists and emit an UPDATE,
            // causing a spurious DbUpdateConcurrencyException.
            if (entityType.FindPrimaryKey()?.Properties is [{ ClrType: var pkType, Name: "Id" }]
                && pkType == typeof(Guid))
            {
                modelBuilder.Entity(clrType).Property("Id").ValueGeneratedNever();
            }
        }

        base.OnModelCreating(modelBuilder);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await DispatchDomainEventsAsync(cancellationToken);
        return await base.SaveChangesAsync(cancellationToken);
    }

    private async Task DispatchDomainEventsAsync(CancellationToken cancellationToken)
    {
        // Loop until no new events remain: an event handler may mutate another aggregate.
        while (true)
        {
            var entities = ChangeTracker.Entries<BaseEntity>()
                .Where(e => e.Entity.DomainEvents.Count != 0)
                .Select(e => e.Entity)
                .ToList();

            if (entities.Count == 0)
                return;

            var events = entities.SelectMany(e => e.DomainEvents).ToList();
            entities.ForEach(e => e.ClearDomainEvents());

            foreach (var domainEvent in events)
            {
                var notificationType = typeof(DomainEventNotification<>).MakeGenericType(domainEvent.GetType());
                var notification = (INotification)Activator.CreateInstance(notificationType, domainEvent)!;
                await publisher.Publish(notification, cancellationToken);
            }
        }
    }
}
