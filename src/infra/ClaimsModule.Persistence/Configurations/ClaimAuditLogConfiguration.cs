using ClaimsModule.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClaimsModule.Persistence.Configurations;

public sealed class ClaimAuditLogConfiguration : IEntityTypeConfiguration<ClaimAuditLog>
{
    public void Configure(EntityTypeBuilder<ClaimAuditLog> builder)
    {
        builder.ToTable("ClaimAuditLog");
        builder.HasKey(a => a.Id);

        builder.Property(a => a.OrganisationId).IsRequired();
        builder.Property(a => a.EventType).HasMaxLength(100).IsRequired();
        builder.Property(a => a.Description).IsRequired();
        builder.Property(a => a.RelatedEntityType).HasMaxLength(100);

        // Reverse-chronological reads per claim; no FK navigation keeps the append-only log decoupled.
        builder.HasIndex(a => new { a.ClaimId, a.CreatedAt });
        builder.HasIndex(a => a.EventType);

        // PERSIST-01: GL posting is idempotent at the DB level — at most one GL_POSTING_SIMULATED entry
        // per reserve transaction, so concurrent at-least-once job executions cannot double-post.
        builder.HasIndex(a => a.RelatedEntityId)
            .IsUnique()
            .HasFilter("[EventType] = 'GL_POSTING_SIMULATED'");
    }
}
