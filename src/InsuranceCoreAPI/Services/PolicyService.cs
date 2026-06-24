using InsuranceCoreApi.Domain;
using InsuranceCoreAPI.Domain.Enums;
using InsuranceCoreAPI.DTOs.Policies;
using InsuranceCoreAPI.Errors;
using InsuranceCoreAPI.Repositories.Interfaces;
using InsuranceCoreAPI.Services.Interfaces;

namespace InsuranceCoreAPI.Services;

public sealed class PolicyService(
    IPolicyRepository policyRepository,
    ICustomerRepository customerRepository) : IPolicyService
{
    public async Task<Policy> CreateAsync(CreatePolicyRequest request)
    {
        // Rule: EndDate must be strictly after StartDate
        if (request.EndDate <= request.StartDate)
            throw new ValidationException(
                $"EndDate ({request.EndDate}) must be after StartDate ({request.StartDate}).");

        // Rule: the referenced customer must exist
        var customer = await customerRepository.GetByIdAsync(request.CustomerId);
        if (customer is null)
            throw new NotFoundException($"Customer '{request.CustomerId}' was not found.");

        var policy = new Policy
        {
            CustomerId = request.CustomerId,
            ProductType = request.ProductType,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            Premium = request.Premium,
            Status = PolicyStatus.Draft
        };

        return await policyRepository.AddAsync(policy);
    }

    public async Task<Policy> ActivateAsync(Guid id)
    {
        var policy = await policyRepository.GetByIdAsync(id);
        if (policy is null)
            throw new NotFoundException($"Policy '{id}' was not found.");

        // Rule: only a Draft policy may be activated
        if (policy.Status != PolicyStatus.Draft)
            throw new ConflictException(
                $"Policy '{id}' cannot be activated because its status is '{policy.Status}'. " +
                "Only Draft policies can be activated.");

        // Rule: no two Active policies for the same customer + product type with overlapping dates
        var activePolicies = await policyRepository.GetActivePoliciesAsync(
            policy.CustomerId, policy.ProductType);

        var overlap = activePolicies.FirstOrDefault(p => p.OverlapsWith(policy.StartDate, policy.EndDate));
        if (overlap is not null)
            throw new ConflictException(
                $"Policy '{id}' overlaps with existing active policy '{overlap.Id}' " +
                $"(type: {policy.ProductType}, period: {overlap.StartDate}–{overlap.EndDate}).");

        policy.Status = PolicyStatus.Active;
        return await policyRepository.UpdateAsync(policy);
    }

    public async Task<Policy> GetByIdAsync(Guid id)
    {
        var policy = await policyRepository.GetByIdAsync(id);
        if (policy is null)
            throw new NotFoundException($"Policy '{id}' was not found.");

        return policy;
    }
}
