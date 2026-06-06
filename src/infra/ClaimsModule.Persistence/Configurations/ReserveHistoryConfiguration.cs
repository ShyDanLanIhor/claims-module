using ClaimsModule.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClaimsModule.Persistence.Configurations;

public sealed class ReserveHistoryConfiguration : IEntityTypeConfiguration<ReserveHistory>
{
    public void Configure(EntityTypeBuilder<ReserveHistory> builder)
    {
        builder.ToTable("ReserveHistory");
        builder.HasKey(h => h.Id);

        builder.Property(h => h.OrganisationId).IsRequired();
        builder.Property(h => h.TransactionType).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(h => h.ApprovalStatus).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(h => h.PostingStatus).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(h => h.ChangeReason).HasMaxLength(500).IsRequired();
        builder.Property(h => h.RejectionReason).HasMaxLength(500);
        builder.Property(h => h.PostingJobId).HasMaxLength(100);
        builder.Property(h => h.IdempotencyKey).HasMaxLength(200).IsRequired();

        builder.Property(h => h.Amount).HasPrecision(19, 4);
        builder.Property(h => h.PreviousBalance).HasPrecision(19, 4);
        builder.Property(h => h.NewBalance).HasPrecision(19, 4);

        builder.Ignore(h => h.IsEffective);

        // FRS §6.5: idempotency key is the de-duplication guard for GL posting.
        builder.HasIndex(h => h.IdempotencyKey).IsUnique();
        builder.HasIndex(h => h.ClaimId);
        builder.HasIndex(h => new { h.ReserveComponentId, h.ChangeSequence }).IsUnique();
    }
}
