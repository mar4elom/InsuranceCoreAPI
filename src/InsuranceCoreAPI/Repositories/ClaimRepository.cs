using System.Collections.Concurrent;
using InsuranceCoreApi.Domain;
using InsuranceCoreAPI.Repositories.Interfaces;

namespace InsuranceCoreAPI.Repositories;

/// <summary>
/// Thread-safe in-memory store for <see cref="Claim"/> entities.
/// </summary>
public sealed class ClaimRepository : IClaimRepository
{
    private readonly ConcurrentDictionary<Guid, Claim> _store = new();

    public Task<Claim?> GetByIdAsync(Guid id)
    {
        _store.TryGetValue(id, out var claim);
        return Task.FromResult(claim);
    }

    public Task<Claim> AddAsync(Claim claim)
    {
        _store[claim.Id] = claim;
        return Task.FromResult(claim);
    }

    public Task<Claim> UpdateAsync(Claim claim)
    {
        _store[claim.Id] = claim;
        return Task.FromResult(claim);
    }
}
