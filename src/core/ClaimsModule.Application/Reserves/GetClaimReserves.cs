using AutoMapper;
using ClaimsModule.Application.Common.Exceptions;
using ClaimsModule.Application.Common.Interfaces;
using ClaimsModule.Domain.Enums;
using MediatR;

namespace ClaimsModule.Application.Reserves;

/// <summary>Reserve summary (balance per component) and full transaction history for a claim
/// (FRS §10.2 GET /api/claims/{id}/reserves).</summary>
public sealed record GetClaimReservesQuery(Guid ClaimId) : IRequest<ClaimReservesDto>;

public sealed class GetClaimReservesQueryHandler(
    IClaimReadRepository claims, IMapper mapper) : IRequestHandler<GetClaimReservesQuery, ClaimReservesDto>
{
    public async Task<ClaimReservesDto> Handle(GetClaimReservesQuery request, CancellationToken cancellationToken)
    {
        if (!await claims.ExistsAsync(request.ClaimId, cancellationToken))
            throw new NotFoundException(nameof(Domain.Entities.Claim), request.ClaimId);

        var components = await claims.GetReserveComponentsAsync(request.ClaimId, cancellationToken);

        var history = components
            .SelectMany(c => c.History.Select(h => new ReserveTransactionDto
            {
                Id = h.Id,
                ReserveComponentId = c.Id,
                Component = c.Component,
                TransactionType = h.TransactionType,
                Amount = h.Amount,
                PreviousBalance = h.PreviousBalance,
                NewBalance = h.NewBalance,
                ApprovalStatus = h.ApprovalStatus,
                PostingStatus = h.PostingStatus,
                ChangeReason = h.ChangeReason,
                ChangeSequence = h.ChangeSequence,
                SubmittedByUserId = h.SubmittedByUserId,
                ApprovedByUserId = h.ApprovedByUserId,
                RejectedByUserId = h.RejectedByUserId,
                RejectionReason = h.RejectionReason,
                IdempotencyKey = h.IdempotencyKey,
                CreatedAt = h.CreatedAt
            }))
            .OrderByDescending(h => h.CreatedAt)
            .ToList();

        return new ClaimReservesDto
        {
            Components = mapper.Map<List<ReserveComponentSummaryDto>>(components),
            History = history,
            TotalApproved = components.Sum(c => c.CurrentAmount),
            TotalPending = components
                .SelectMany(c => c.History)
                .Where(h => h.ApprovalStatus == ReserveApprovalStatus.PendingApproval)
                .Sum(h => h.Amount)
        };
    }
}
