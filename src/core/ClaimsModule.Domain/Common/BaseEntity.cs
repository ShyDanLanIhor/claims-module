namespace ClaimsModule.Domain.Common;

/// <summary>
/// Base class for all entities. Carries the GUID identity and an in-memory list of domain
/// events that the persistence layer drains and dispatches when the unit of work is saved.
/// </summary>
public abstract class BaseEntity
{
    private readonly List<IDomainEvent> _domainEvents = [];

    // Sequential (COMB) GUID so client-assigned identities don't fragment the clustered index (FRS §15.1).
    public Guid Id { get; set; } = SequentialGuid.Create();

    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    protected void RaiseDomainEvent(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);

    public void ClearDomainEvents() => _domainEvents.Clear();
}

/// <summary>
/// Base for business entities: tenant-scoped, soft-deletable, and fully audited.
/// </summary>
public abstract class AuditableEntity : BaseEntity, IAuditable, ISoftDelete, ITenantScoped
{
    public Guid OrganisationId { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public Guid? UserCreated { get; set; }
    public Guid? UserModified { get; set; }

    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
}
