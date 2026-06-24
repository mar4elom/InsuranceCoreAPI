using System.ComponentModel.DataAnnotations;

namespace InsuranceCoreAPI.DTOs.Claims;

/// <summary>Request body for POST /claims.</summary>
public sealed record CreateClaimRequest(
    [Required] 
    Guid PolicyId,
    
    [Required] 
    DateOnly IncidentDate,
    
    [Range(0.01, double.MaxValue, ErrorMessage = "AmountRequested must be greater than zero.")]
    decimal AmountRequested
);
