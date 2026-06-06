using ClaimsModule.Domain.Entities;
using ClaimsModule.Persistence.Seeding;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClaimsModule.Persistence.Configurations;

public sealed class ClaimNumberSequenceConfiguration : IEntityTypeConfiguration<ClaimNumberSequence>
{
    public void Configure(EntityTypeBuilder<ClaimNumberSequence> builder)
    {
        builder.ToTable("ClaimNumberSequences");
        builder.HasKey(s => s.Id);

        builder.Property(s => s.OrganisationId).IsRequired();
        builder.HasIndex(s => new { s.OrganisationId, s.Year }).IsUnique();

        // Pre-seed one counter row per year so claim-number generation only ever does an atomic
        // UPDATE … OUTPUT (no first-time insert race). FRS §5.3 / §15.4.
        var seed = new List<ClaimNumberSequence>();
        for (var year = SeedConstants.FirstSequenceYear; year <= SeedConstants.LastSequenceYear; year++)
        {
            seed.Add(new ClaimNumberSequence
            {
                Id = SeedConstants.DeterministicGuid($"sequence:{SeedConstants.OrganisationId}:{year}"),
                OrganisationId = SeedConstants.OrganisationId,
                Year = year,
                CurrentValue = 0
            });
        }

        builder.HasData(seed);
    }
}
