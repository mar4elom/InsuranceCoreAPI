using System.ComponentModel.DataAnnotations;
using InsuranceCoreAPI.Domain.Enums;

namespace InsuranceCoreAPI.DTOs.Claims;

/// <summary>
/// Request body for POST /claims/{id}/decide.
/// <para>
/// <see cref="Approved"/> requires <see cref="DecisionReason"/> to be null/empty.
/// <see cref="ClaimStatus.Rejected"/> requires a non-empty <see cref="DecisionReason"/>.
/// The service enforces this — the controller accepts both and delegates.
/// </para>
/// </summary>
public sealed record DecideClaimRequest(
    [Required] ClaimDecision Decision,
    string? DecisionReason
);
