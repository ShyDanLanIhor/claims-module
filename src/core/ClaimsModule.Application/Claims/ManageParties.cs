using ClaimsModule.Application.Common.Exceptions;
using ClaimsModule.Application.Common.Interfaces;
using ClaimsModule.Domain.Entities;
using ClaimsModule.Domain.Enums;
using FluentValidation;
using MediatR;

namespace ClaimsModule.Application.Claims;

// ---- Add party (FRS §10.1 POST /api/claims/{id}/parties) ------------------------------------

public sealed record AddClaimPartyCommand : IRequest<Guid>
{
    public Guid ClaimId { get; init; }
    public PartyRole PartyRole { get; init; }
    public PartyType PartyType { get; init; }
    public string? FirstName { get; init; }
    public string? LastName { get; init; }
    public string? CompanyName { get; init; }
    public string? Email { get; init; }
    public string? Phone { get; init; }
    public string? Notes { get; init; }
}

public sealed class AddClaimPartyCommandValidator : AbstractValidator<AddClaimPartyCommand>
{
    public AddClaimPartyCommandValidator()
    {
        RuleFor(x => x).Must(p => p.PartyType == PartyType.Company
                ? !string.IsNullOrWhiteSpace(p.CompanyName)
                : !string.IsNullOrWhiteSpace(p.FirstName) || !string.IsNullOrWhiteSpace(p.LastName))
            .WithMessage("A party must have a person name or a company name.");
        RuleFor(x => x.Email).EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.Email));
    }
}

public sealed class AddClaimPartyCommandHandler(
    IClaimRepository claims, IUnitOfWork unitOfWork) : IRequestHandler<AddClaimPartyCommand, Guid>
{
    public async Task<Guid> Handle(AddClaimPartyCommand request, CancellationToken cancellationToken)
    {
        var claim = await claims.GetWithPartiesAsync(request.ClaimId, cancellationToken)
            ?? throw new NotFoundException(nameof(Claim), request.ClaimId);

        var party = claim.AddParty(new ClaimParty
        {
            PartyRole = request.PartyRole,
            PartyType = request.PartyType,
            FirstName = request.FirstName,
            LastName = request.LastName,
            CompanyName = request.CompanyName,
            Email = request.Email,
            Phone = request.Phone,
            Notes = request.Notes
        });

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return party.Id;
    }
}

// ---- Remove party (FRS §10.1 DELETE /api/claims/{id}/parties/{partyId}) ----------------------

public sealed record RemoveClaimPartyCommand(Guid ClaimId, Guid PartyId) : IRequest<Unit>;

public sealed class RemoveClaimPartyCommandHandler(
    IClaimRepository claims, IUnitOfWork unitOfWork) : IRequestHandler<RemoveClaimPartyCommand, Unit>
{
    public async Task<Unit> Handle(RemoveClaimPartyCommand request, CancellationToken cancellationToken)
    {
        var claim = await claims.GetWithPartiesAsync(request.ClaimId, cancellationToken)
            ?? throw new NotFoundException(nameof(Claim), request.ClaimId);

        var party = claim.Parties.FirstOrDefault(p => p.Id == request.PartyId && p.IsActive)
            ?? throw new NotFoundException(nameof(ClaimParty), request.PartyId);

        var isLastClaimant = party.PartyRole == PartyRole.Claimant
            && claim.Parties.Count(p => p.IsActive && p.PartyRole == PartyRole.Claimant) == 1;
        if (isLastClaimant)
            throw new BusinessRuleException("party", "Cannot remove the last Claimant from a claim."); // FRS §10.1

        claim.RemoveParty(party);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
