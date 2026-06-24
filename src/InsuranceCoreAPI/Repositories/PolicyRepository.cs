using System.Collections.Concurrent;
using InsuranceCoreApi.Domain;
using InsuranceCoreAPI.Domain.Enums;
using InsuranceCoreAPI.Repositories.Interfaces;

namespace InsuranceCoreAPI.Repositories;

/// <summary>
/// Thread-safe in-memory store for <see cref="Policy"/> entities.
/// </summary>
public sealed class PolicyRepository : IPolicyRepository
{
    private readonly ConcurrentDictionary<Guid, Policy> _store = new();

    public Task<Policy?> GetByIdAsync(Guid id)
    {
        _store.TryGetValue(id, out var policy);
        return Task.FromResult(policy);
    }

    public Task<Policy> AddAsync(Policy policy)
    {
        _store[policy.Id] = policy;
        return Task.FromResult(policy);
    }

    public Task<Policy> UpdateAsync(Policy policy)
    {
        _store[policy.Id] = policy;
        return Task.FromResult(policy);
    }

    public Task<IReadOnlyList<Policy>> GetActivePoliciesAsync(Guid customerId, ProductType productType)
    {
        IReadOnlyList<Policy> results = _store.Values
            .Where(p => p.CustomerId == customerId
                        && p.ProductType == productType
                        && p.Status == PolicyStatus.Active)
            .ToList();

        return Task.FromResult(results);
    }
}
