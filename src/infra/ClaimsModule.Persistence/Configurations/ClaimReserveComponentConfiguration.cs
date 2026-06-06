using ClaimsModule.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClaimsModule.Persistence.Configurations;

public sealed class ClaimReserveComponentConfiguration : IEntityTypeConfiguration<ClaimReserveComponent>
{
    public void Configure(EntityTypeBuilder<ClaimReserveComponent> builder)
    {
        builder.ToTable("ClaimReserveComponents");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.OrganisationId).IsRequired();
        builder.Property(c => c.Component).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(c => c.CurrentAmount).HasPrecision(19, 4);
        builder.Property(c => c.Status).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(c => c.RowVersion).IsRowVersion();
        builder.Ignore(c => c.PendingAmount);

        builder.HasIndex(c => c.ClaimId);
        builder.HasIndex(c => new { c.ClaimId, c.Component });

        builder.HasMany(c => c.History).WithOne(h => h.ReserveComponent)
            .HasForeignKey(h => h.ReserveComponentId).OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(c => c.History).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
