using InsuranceCoreAPI.Domain.Enums;

namespace InsuranceCoreApi.Domain;

/// <summary>
/// Represents an insurance policy belonging to a customer.
/// </summary>
/// <remarks>
/// Date choice: DateOnly is used because policy validity is defined in calendar days,
/// not at a specific time-of-day. This avoids timezone ambiguity when comparing
/// IncidentDate on claims.
/// </remarks>
public sealed class Policy
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public required Guid CustomerId { get; init; }

    public required ProductType ProductType { get; init; }

    /// <summary>
    /// First day the policy is in effect (inclusive).
    /// </summary>
    public required DateOnly StartDate { get; init; }

    /// <summary>
    /// Last day the policy is in effect (inclusive).
    /// Must be strictly greater than <see cref="StartDate"/>.
    /// </summary>
    public required DateOnly EndDate { get; init; }

    public required decimal Premium { get; init; }

    public PolicyStatus Status { get; set; } = PolicyStatus.Draft;

    /// <summary>
    /// Returns true when this policy's date range overlaps with another range.
    /// Both ranges are treated as inclusive on both ends.
    /// </summary>
    public bool OverlapsWith(DateOnly otherStart, DateOnly otherEnd)
        => StartDate <= otherEnd && otherStart <= EndDate;
}