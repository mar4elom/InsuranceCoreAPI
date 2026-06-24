using InsuranceCoreApi.Domain;
using InsuranceCoreAPI.Domain.Enums;
using InsuranceCoreAPI.DTOs.Claims;
using InsuranceCoreAPI.Errors;
using InsuranceCoreAPI.Repositories.Interfaces;
using InsuranceCoreAPI.Services.Interfaces;

namespace InsuranceCoreAPI.Services;

public sealed class ClaimService(
    IClaimRepository claimRepository,
    IPolicyRepository policyRepository) : IClaimService
{
    public async Task<Claim> CreateAsync(CreateClaimRequest request)
    {
        var policy = await policyRepository.GetByIdAsync(request.PolicyId);
        if (policy is null)
            throw new NotFoundException($"Policy '{request.PolicyId}' was not found.");

        // Rule: claims can only be filed against an Active policy
        if (policy.Status != PolicyStatus.Active)
            throw new ConflictException(
                $"Claims can only be created for Active policies. " +
                $"Policy '{request.PolicyId}' is currently '{policy.Status}'.");

        // Rule: incident date must fall within the policy period (inclusive)
        if (request.IncidentDate < policy.StartDate || request.IncidentDate > policy.EndDate)
            throw new ConflictException(
                $"IncidentDate ({request.IncidentDate}) is outside the policy period " +
                $"({policy.StartDate}–{policy.EndDate}).");

        var claim = new Claim
        {
            PolicyId = request.PolicyId,
            IncidentDate = request.IncidentDate,
            AmountRequested = request.AmountRequested,
            Status = ClaimStatus.New
        };

        return await claimRepository.AddAsync(claim);
    }

    public async Task<Claim> DecideAsync(Guid id, DecideClaimRequest request)
    {
        var claim = await claimRepository.GetByIdAsync(id);
        if (claim is null)
            throw new NotFoundException($"Claim '{id}' was not found.");

        // Rule: a decision can only be made on a New claim
        if (claim.Status != ClaimStatus.New)
            throw new ConflictException(
                $"Claim '{id}' cannot be decided because its status is '{claim.Status}'. " +
                "Only New claims can be decided.");

        if (request.Decision == ClaimDecision.Reject)
        {
            // Rule: rejection requires a reason
            if (string.IsNullOrWhiteSpace(request.DecisionReason))
                throw new ValidationException(
                    "DecisionReason is required when rejecting a claim.");

            claim.Status = ClaimStatus.Rejected;
            claim.DecisionReason = request.DecisionReason;
        }
        else
        {
            claim.Status = ClaimStatus.Approved;
            claim.DecisionReason = null;
        }

        return await claimRepository.UpdateAsync(claim);
    }

    public async Task<Claim> GetByIdAsync(Guid id)
    {
        var claim = await claimRepository.GetByIdAsync(id);
        if (claim is null)
            throw new NotFoundException($"Claim '{id}' was not found.");

        return claim;
    }
}
