using InsuranceCoreAPI.Domain.Enums;

namespace InsuranceCoreApi.Domain;

/// <summary>
/// Represents a claim filed against an active policy.
/// </summary>
/// <remarks>
/// Date choice: DateOnly is used for IncidentDate because an incident happens on a
/// specific calendar day. Using DateOnly keeps comparison with Policy.StartDate /
/// Policy.EndDate straightforward and timezone-independent.
/// </remarks>
public sealed class Claim
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public required Guid PolicyId { get; init; }

    /// <summary>
    /// The calendar date on which the insured incident occurred.
    /// Must fall within the policy's [StartDate, EndDate] range (inclusive).
    /// </summary>
    public required DateOnly IncidentDate { get; init; }

    public required decimal AmountRequested { get; init; }

    public ClaimStatus Status { get; set; } = ClaimStatus.New;

    /// <summary>
    /// Required when <see cref="Status"/> is <see cref="ClaimStatus.Rejected"/>.
    /// Must be null or empty for Approved claims.
    /// </summary>
    public string? DecisionReason { get; set; }
}