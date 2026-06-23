using System.Collections.Concurrent;
using InsuranceCoreAPI.Domain;
using InsuranceCoreAPI.Repositories.Interfaces;

namespace InsuranceCoreAPI.Repositories;

/// <summary>
/// Thread-safe in-memory store for <see cref="Customer"/> entities.
/// </summary>
public sealed class CustomerRepository : ICustomerRepository
{
    private readonly ConcurrentDictionary<Guid, Customer> _store = new();

    public Task<Customer?> GetByIdAsync(Guid id)
    {
        _store.TryGetValue(id, out var customer);
        return Task.FromResult(customer);
    }

    public Task<Customer> AddAsync(Customer customer)
    {
        _store[customer.Id] = customer;
        return Task.FromResult(customer);
    }
}
