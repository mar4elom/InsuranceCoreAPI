using InsuranceCoreApi.Domain;
using InsuranceCoreAPI.Domain.Enums;

namespace InsuranceCoreAPI.Repositories.Interfaces;

public interface IPolicyRepository
{
    Task<Policy?> GetByIdAsync(Guid id);
    Task<Policy> AddAsync(Policy policy);
    Task<Policy> UpdateAsync(Policy policy);

    /// <summary>
    /// Returns all Active policies for a given customer and product type.
    /// Used for overlap detection during activation.
    /// </summary>
    Task<IReadOnlyList<Policy>> GetActivePoliciesAsync(Guid customerId, ProductType productType);
}
