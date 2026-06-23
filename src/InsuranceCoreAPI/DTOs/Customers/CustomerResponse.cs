namespace InsuranceCoreAPI.DTOs.Customers;

/// <summary>Response body for customer endpoints.</summary>
public sealed record CustomerResponse(
    Guid Id,
    string FullName
);
