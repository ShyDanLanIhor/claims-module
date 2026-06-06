using ClaimsModule.Application.Claims;
using ClaimsModule.Application.Common.Interfaces;
using ClaimsModule.Domain.Entities;
using ClaimsModule.Domain.Enums;
using FluentValidation.TestHelper;
using Xunit;

namespace ClaimsModule.Application.Tests.Claims;

public class CreateClaimCommandValidatorTests
{
    private static CreateClaimCommandValidator BuildValidator(bool causeActive = true) =>
        new(new FakeReferenceData(causeActive), new FixedClock(new DateTimeOffset(2026, 6, 1, 0, 0, 0, TimeSpan.Zero)));

    private static CreateClaimCommand ValidCommand() => new()
    {
        LossDate = new DateTimeOffset(2026, 5, 1, 0, 0, 0, TimeSpan.Zero),
        LossDescription = "Vehicle collision at the north gate causing front-end damage.",
        CauseOfLossCode = "COL-VEH-COL"
    };

    [Fact]
    public async Task Valid_Command_Has_No_Errors()
    {
        var result = await BuildValidator().TestValidateAsync(ValidCommand());
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task LossDate_In_The_Future_Is_Rejected() // BR-C-01
    {
        var command = ValidCommand() with { LossDate = new DateTimeOffset(2026, 12, 1, 0, 0, 0, TimeSpan.Zero) };
        var result = await BuildValidator().TestValidateAsync(command);
        result.ShouldHaveValidationErrorFor(c => c.LossDate);
    }

    [Fact]
    public async Task LossDescription_Under_20_Characters_Is_Rejected() // BR-C-07
    {
        var command = ValidCommand() with { LossDescription = "too short" };
        var result = await BuildValidator().TestValidateAsync(command);
        result.ShouldHaveValidationErrorFor(c => c.LossDescription);
    }

    [Fact]
    public async Task Inactive_Cause_Of_Loss_Code_Is_Rejected() // BR-C-05
    {
        var result = await BuildValidator(causeActive: false).TestValidateAsync(ValidCommand());
        result.ShouldHaveValidationErrorFor(c => c.CauseOfLossCode);
    }

    [Fact]
    public async Task Initial_Subrogation_Reserve_Of_Zero_Is_Rejected() // BR-R-01 (FNOL initial reserve)
    {
        var command = ValidCommand() with
        {
            InitialReserve = new InitialReserveInput(ReserveComponentType.SubrogationRecoverable, 0m, "salvage"),
        };
        var result = await BuildValidator().TestValidateAsync(command);
        result.ShouldHaveValidationErrorFor("InitialReserve.Amount");
    }

    private sealed class FixedClock(DateTimeOffset now) : IDateTime
    {
        public DateTimeOffset UtcNow { get; } = now;
    }

    private sealed class FakeReferenceData(bool causeActive) : IReferenceDataRepository
    {
        public Task<IReadOnlyList<CauseOfLossCode>> GetCauseOfLossCodesAsync(PerilCategory? perilCategory, bool activeOnly, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<CauseOfLossCode>>([]);

        public Task<bool> CauseOfLossCodeIsActiveAsync(string code, CancellationToken cancellationToken = default) =>
            Task.FromResult(causeActive);

        public Task<IReadOnlyList<ClaimStatusTransition>> GetStatusTransitionsAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<ClaimStatusTransition>>([]);
    }
}
