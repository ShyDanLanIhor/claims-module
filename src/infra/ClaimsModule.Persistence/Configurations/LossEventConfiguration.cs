using ClaimsModule.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClaimsModule.Persistence.Configurations;

public sealed class LossEventConfiguration : IEntityTypeConfiguration<LossEvent>
{
    public void Configure(EntityTypeBuilder<LossEvent> builder)
    {
        builder.ToTable("LossEvents");
        builder.HasKey(l => l.Id);

        builder.Property(l => l.OrganisationId).IsRequired();
        builder.Property(l => l.LossDescription).IsRequired();
        builder.Property(l => l.LossLocation).HasMaxLength(500);
        builder.Property(l => l.CauseOfLossCode).HasMaxLength(50).IsRequired();
        builder.Property(l => l.PoliceReportNumber).HasMaxLength(100);

        builder.HasIndex(l => l.ClaimId).IsUnique();
        builder.HasIndex(l => l.CauseOfLossCode);
    }
}
