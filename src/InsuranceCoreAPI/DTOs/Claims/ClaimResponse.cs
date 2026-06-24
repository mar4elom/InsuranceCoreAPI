using InsuranceCoreAPI.Domain.Enums;

namespace InsuranceCoreAPI.DTOs.Claims;

/// <summary>Response body for claim endpoints.</summary>
public sealed record ClaimResponse(
    Guid Id,
    Guid PolicyId,
    DateOnly IncidentDate,
    decimal AmountRequested,
    ClaimStatus Status,
    string? DecisionReason
);
