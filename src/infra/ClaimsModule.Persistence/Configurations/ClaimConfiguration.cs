using ClaimsModule.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClaimsModule.Persistence.Configurations;

public sealed class ClaimConfiguration : IEntityTypeConfiguration<Claim>
{
    public void Configure(EntityTypeBuilder<Claim> builder)
    {
        builder.ToTable("Claims");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.OrganisationId).IsRequired();
        builder.Property(c => c.ClaimNumber).HasMaxLength(50).IsRequired();
        builder.Property(c => c.PolicyNumber).HasMaxLength(50);
        builder.Property(c => c.ClientName).HasMaxLength(255);
        builder.Property(c => c.ClosureReason).HasMaxLength(500);
        builder.Property(c => c.Status).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(c => c.Severity).HasConversion<string>().HasMaxLength(50);
        builder.Property(c => c.ManagerOverrideApplied).HasDefaultValue(false); // §15.1 BIT NOT NULL DEFAULT 0
        builder.Property(c => c.RowVersion).IsRowVersion();

        builder.HasIndex(c => new { c.OrganisationId, c.ClaimNumber }).IsUnique();
        builder.HasIndex(c => c.Status);
        builder.HasIndex(c => c.PolicyId);
        builder.HasIndex(c => c.AssignedHandlerId);

        builder.HasOne(c => c.LossEvent)
            .WithOne(l => l.Claim)
            .HasForeignKey<LossEvent>(l => l.ClaimId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(c => c.Parties).WithOne(p => p.Claim)
            .HasForeignKey(p => p.ClaimId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(c => c.RiskObjects).WithOne(r => r.Claim)
            .HasForeignKey(r => r.ClaimId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(c => c.ReserveComponents).WithOne(rc => rc.Claim)
            .HasForeignKey(rc => rc.ClaimId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(c => c.Documents).WithOne(d => d.Claim)
            .HasForeignKey(d => d.ClaimId).OnDelete(DeleteBehavior.Cascade);

        // Map read-only navigations through their backing fields.
        builder.Navigation(c => c.Parties).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(c => c.RiskObjects).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(c => c.ReserveComponents).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(c => c.Documents).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
