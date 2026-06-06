using ClaimsModule.Domain.Claims;
using ClaimsModule.Domain.Entities;
using ClaimsModule.Persistence.Seeding;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClaimsModule.Persistence.Configurations;

public sealed class ClaimStatusTransitionConfiguration : IEntityTypeConfiguration<ClaimStatusTransition>
{
    public void Configure(EntityTypeBuilder<ClaimStatusTransition> builder)
    {
        builder.ToTable("ClaimStatusTransitions");
        builder.HasKey(t => t.Id);

        builder.Property(t => t.FromStatus).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(t => t.ToStatus).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(t => t.RequiredRole).HasConversion<string>().HasMaxLength(50);

        builder.HasIndex(t => new { t.FromStatus, t.ToStatus }).IsUnique();

        // Seeded from the single source of truth in the domain (FRS §3.2, §4.2).
        builder.HasData(ClaimLifecycle.Transitions.Select(t => new ClaimStatusTransition
        {
            Id = SeedConstants.DeterministicGuid($"transition:{t.From}:{t.To}"),
            FromStatus = t.From,
            ToStatus = t.To,
            RequiredRole = t.RequiredRole
        }));
    }
}
