using System.ComponentModel.DataAnnotations;
using InsuranceCoreAPI.Domain.Enums;

namespace InsuranceCoreAPI.DTOs.Policies;

/// <summary>Request body for POST /policies.</summary>
public sealed record CreatePolicyRequest(
    [Required] 
    Guid CustomerId,
    
    [Required] 
    ProductType ProductType,
    
    [Required] 
    DateOnly StartDate,
    
    [Required] 
    DateOnly EndDate,
    
    [Range(0.01, double.MaxValue, ErrorMessage = "Premium must be greater than zero.")]
    decimal Premium
);
