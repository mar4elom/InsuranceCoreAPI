using InsuranceCoreApi.Domain;
using InsuranceCoreAPI.DTOs.Policies;

namespace InsuranceCoreAPI.Services.Interfaces;
public interface IPolicyService
{
    Task<Policy> CreateAsync(CreatePolicyRequest request);

    Task<Policy> ActivateAsync(Guid id);

    Task<Policy> GetByIdAsync(Guid id);
}
