using ClaimsModule.Application.Reserves;
using ClaimsModule.Domain.Enums;
using FluentValidation.TestHelper;
using Xunit;

namespace ClaimsModule.Application.Tests.Reserves;

public class SubmitReserveCommandValidatorTests
{
    private static readonly SubmitReserveCommandValidator Validator = new();

    private static SubmitReserveCommand Command(ReserveComponentType component, decimal amount) => new()
    {
        Component = component,
        Amount = amount,
        ChangeReason = "initial reserve",
    };

    [Fact]
    public void Positive_Indemnity_Amount_Is_Valid()
    {
        var result = Validator.TestValidate(Command(ReserveComponentType.Indemnity, 5_000m));
        result.ShouldNotHaveValidationErrorFor(c => c.Amount);
    }

    [Fact]
    public void Zero_Amount_Is_Rejected()
    {
        var result = Validator.TestValidate(Command(ReserveComponentType.Indemnity, 0m));
        result.ShouldHaveValidationErrorFor(c => c.Amount);
    }

    [Fact]
    public void Negative_Indemnity_Amount_Is_Rejected() // BR-R-01
    {
        var result = Validator.TestValidate(Command(ReserveComponentType.Indemnity, -1_000m));
        result.ShouldHaveValidationErrorFor(c => c.Amount);
    }

    [Fact]
    public void Negative_Subrogation_Amount_Is_Allowed() // BR-R-01 exception
    {
        var result = Validator.TestValidate(Command(ReserveComponentType.SubrogationRecoverable, -8_000m));
        result.ShouldNotHaveValidationErrorFor(c => c.Amount);
    }

    [Fact]
    public void Empty_Change_Reason_Is_Rejected()
    {
        var result = Validator.TestValidate(new SubmitReserveCommand
        {
            Component = ReserveComponentType.Indemnity,
            Amount = 5_000m,
            ChangeReason = "",
        });
        result.ShouldHaveValidationErrorFor(c => c.ChangeReason);
    }
}
