using ClaimsModule.Application.Common.Interfaces;
using ClaimsModule.Application.Common.Models;
using ClaimsModule.Domain.Auditing;
using ClaimsModule.Domain.Entities;
using ClaimsModule.Domain.Enums;
using FluentValidation;
using MediatR;

namespace ClaimsModule.Application.Claims;

public sealed record CreateClaimPartyInput(
    PartyRole PartyRole,
    PartyType PartyType,
    string? FirstName,
    string? LastName,
    string? CompanyName,
    string? Email,
    string? Phone,
    string? Notes);

public sealed record CreateClaimRiskObjectInput(
    AssetType AssetType,
    string AssetDescription,
    string? DamageDescription,
    bool IsPrimary,
    string? AssetReference);

public sealed record InitialReserveInput(
    ReserveComponentType Component,
    decimal Amount,
    string ChangeReason);

/// <summary>FNOL intake — creates a claim with its loss event, parties, risk objects and optional
/// initial reserve in a single transaction (FRS §5, §10.1 POST /api/claims).</summary>
public sealed record CreateClaimCommand : IRequest<CreateClaimResult>
{
    public Guid? PolicyId { get; init; }
    public DateTimeOffset LossDate { get; init; }
    public string LossDescription { get; init; } = string.Empty;
    public string CauseOfLossCode { get; init; } = string.Empty;
    public string? LossLocation { get; init; }
    public decimal? EstimatedLossAmount { get; init; }
    public string? PoliceReportNumber { get; init; }
    public ClaimSeverity? Severity { get; init; }
    public Guid? AssignedHandlerId { get; init; }
    public string? Notes { get; init; }
    public IReadOnlyList<CreateClaimPartyInput> Parties { get; init; } = [];
    public IReadOnlyList<CreateClaimRiskObjectInput> RiskObjects { get; init; } = [];
    public InitialReserveInput? InitialReserve { get; init; }
}

public sealed class CreateClaimCommandValidator : AbstractValidator<CreateClaimCommand>
{
    public CreateClaimCommandValidator(IReferenceDataRepository referenceData, IDateTime clock)
    {
        RuleFor(x => x.LossDate)
            .NotEmpty().WithMessage("Loss date is required.")
            .Must(d => d <= clock.UtcNow).WithMessage("Loss date cannot be in the future."); // BR-C-01

        RuleFor(x => x.LossDescription)
            .NotEmpty().MinimumLength(20)
            .WithMessage("Loss description is required and must be at least 20 characters."); // BR-C-07

        RuleFor(x => x.CauseOfLossCode)
            .NotEmpty().WithMessage("Cause of loss code is not recognised or is inactive.")
            .MustAsync(referenceData.CauseOfLossCodeIsActiveAsync)
            .WithMessage("Cause of loss code is not recognised or is inactive."); // BR-C-05

        When(x => x.InitialReserve is not null, () =>
        {
            RuleFor(x => x.InitialReserve!.Amount)
                .Must((cmd, amount) =>
                    cmd.InitialReserve!.Component == ReserveComponentType.SubrogationRecoverable || amount > 0)
                .WithMessage("Reserve amount must be greater than zero."); // BR-R-01
            RuleFor(x => x.InitialReserve!.Amount).NotEqual(0m).WithMessage("Reserve amount must not be zero.");

            RuleFor(x => x.InitialReserve!.ChangeReason).NotEmpty();
        });

        RuleForEach(x => x.Parties).ChildRules(p =>
        {
            p.RuleFor(x => x).Must(HasName).WithMessage("A party must have a person name or a company name.");
            p.RuleFor(x => x.Email).EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.Email)); // align with AddParty
        });
    }

    private static bool HasName(CreateClaimPartyInput p) => p.PartyType == PartyType.Company
        ? !string.IsNullOrWhiteSpace(p.CompanyName)
        : !string.IsNullOrWhiteSpace(p.FirstName) || !string.IsNullOrWhiteSpace(p.LastName);
}

public sealed class CreateClaimCommandHandler(
    IClaimRepository claims,
    IPolicyRepository policies,
    IClaimNumberGenerator claimNumbers,
    IAuditLogService auditLog,
    IBackgroundJobScheduler jobs,
    ICurrentUserService currentUser,
    IDateTime clock,
    IUnitOfWork unitOfWork) : IRequestHandler<CreateClaimCommand, CreateClaimResult>
{
    public async Task<CreateClaimResult> Handle(CreateClaimCommand request, CancellationToken cancellationToken)
    {
        var org = currentUser.OrganisationId;
        var warnings = new List<ValidationIssue>();

        Policy? policy = request.PolicyId is { } pid
            ? await policies.GetByIdAsync(pid, cancellationToken)
            : null;

        if (request.PolicyId is null || policy is null)
            warnings.Add(ValidationIssue.Warning("POLICY_UNKNOWN",
                "No policy linked. Policy must be associated before reserves can be set.")); // BR-C-06

        if (policy is not null && !policy.CoversDate(DateOnly.FromDateTime(request.LossDate.UtcDateTime)))
            warnings.Add(ValidationIssue.Warning("POLICY_PERIOD",
                "Loss date is outside the policy effective period.")); // BR-C-02

        if (request.RiskObjects.Count == 0)
            warnings.Add(ValidationIssue.Warning("NO_RISK_OBJECTS", "No risk objects linked to the claim."));

        var reserveBlocked = policy is null && request.InitialReserve is not null;
        if (reserveBlocked)
            warnings.Add(ValidationIssue.Warning("RESERVE_BLOCKED_NO_POLICY",
                "Initial reserve was not created: a policy must be linked before reserves can be set.")); // BR-C-06

        Claim claim = null!;
        ReserveHistory? initialReserveTxn = null;
        ClaimReserveComponent? initialReserveComponent = null;

        await unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            var year = clock.UtcNow.Year;
            var claimNumber = await claimNumbers.NextAsync(org, year, ct); // BR-C-04 atomic

            var lossEvent = new LossEvent
            {
                LossDate = request.LossDate,
                LossDescription = request.LossDescription,
                LossLocation = request.LossLocation,
                CauseOfLossCode = request.CauseOfLossCode,
                EstimatedLossAmount = request.EstimatedLossAmount,
                ReportDate = clock.UtcNow,
                PoliceReportNumber = request.PoliceReportNumber
            };

            claim = Claim.Create(
                org, claimNumber, policy?.Id, policy?.PolicyNumber, policy?.ClientName,
                clock.UtcNow, request.AssignedHandlerId, request.Severity, request.Notes, lossEvent);

            foreach (var p in request.Parties)
                claim.AddParty(new ClaimParty
                {
                    PartyRole = p.PartyRole,
                    PartyType = p.PartyType,
                    FirstName = p.FirstName,
                    LastName = p.LastName,
                    CompanyName = p.CompanyName,
                    Email = p.Email,
                    Phone = p.Phone,
                    Notes = p.Notes
                });

            foreach (var r in request.RiskObjects)
                claim.AddRiskObject(new ClaimRiskObject
                {
                    AssetType = r.AssetType,
                    AssetDescription = r.AssetDescription,
                    DamageDescription = r.DamageDescription,
                    IsPrimary = r.IsPrimary,
                    AssetReference = r.AssetReference
                });

            if (request.InitialReserve is { } ir && !reserveBlocked)
            {
                initialReserveComponent = ClaimReserveComponent.Open(org, claim.Id, ir.Component);
                claim.AddReserveComponent(initialReserveComponent);
                initialReserveTxn = initialReserveComponent.SubmitTransaction(
                    ir.Amount, ReserveTransactionType.Add, ir.ChangeReason, currentUser.UserId);
            }

            claims.Add(claim);

            foreach (var w in warnings)
                await auditLog.WriteAsync(new AuditEntry(
                    claim.Id, AuditEventTypes.ValidationIssueAdded, w.Message, NewValue: w.Code), ct);

            await unitOfWork.SaveChangesAsync(ct);
        }, cancellationToken);

        // Enqueue GL posting only after the transaction has committed (FRS §6.5), then persist the
        // real Hangfire job id on the transaction (FRS §12.1) — mirrors SubmitReserve/ApproveReserve.
        if (initialReserveTxn is { ApprovalStatus: ReserveApprovalStatus.AutoApproved } txn
            && initialReserveComponent is { } reserveComponent)
            await Reserves.ReserveGlPosting.ScheduleAsync(jobs, unitOfWork, reserveComponent, txn, cancellationToken);

        return new CreateClaimResult
        {
            ClaimId = claim.Id,
            ClaimNumber = claim.ClaimNumber,
            Warnings = warnings
        };
    }
}
