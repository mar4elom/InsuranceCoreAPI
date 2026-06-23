using System.ComponentModel.DataAnnotations;

namespace InsuranceCoreAPI.DTOs.Customers;

/// <summary>Request body for POST /customers.</summary>
public sealed record CreateCustomerRequest(
    [Required, MinLength(1)] string FullName
);
