using ClaimsModule.Application.Common.Exceptions;
using ClaimsModule.Application.Common.Interfaces;
using FluentValidation;
using MediatR;

namespace ClaimsModule.Application.Claims;

/// <summary>Updates the claim's free-text notes (FRS §11.3 editable notes field).</summary>
public sealed record UpdateClaimNotesCommand : IRequest<Unit>
{
    public Guid ClaimId { get; init; }
    public string? Notes { get; init; }
}

public sealed class UpdateClaimNotesCommandValidator : AbstractValidator<UpdateClaimNotesCommand>
{
    public UpdateClaimNotesCommandValidator() =>
        RuleFor(x => x.Notes).MaximumLength(2000);
}

public sealed class UpdateClaimNotesCommandHandler(
    IClaimRepository claims, IUnitOfWork unitOfWork) : IRequestHandler<UpdateClaimNotesCommand, Unit>
{
    public async Task<Unit> Handle(UpdateClaimNotesCommand request, CancellationToken cancellationToken)
    {
        var claim = await claims.GetAsync(request.ClaimId, cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Entities.Claim), request.ClaimId);

        claim.UpdateNotes(request.Notes);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
