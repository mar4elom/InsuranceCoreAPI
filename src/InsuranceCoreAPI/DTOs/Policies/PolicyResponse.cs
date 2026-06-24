using InsuranceCoreAPI.Domain.Enums;

namespace InsuranceCoreAPI.DTOs.Policies;

/// <summary>Response body for policy endpoints.</summary>
public sealed record PolicyResponse(
    Guid Id,
    Guid CustomerId,
    ProductType ProductType,
    DateOnly StartDate,
    DateOnly EndDate,
    decimal Premium,
    PolicyStatus Status
);
