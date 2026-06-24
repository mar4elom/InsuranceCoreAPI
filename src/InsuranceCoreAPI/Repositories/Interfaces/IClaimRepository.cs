using InsuranceCoreApi.Domain;

namespace InsuranceCoreAPI.Repositories.Interfaces;

public interface IClaimRepository
{
    Task<Claim?> GetByIdAsync(Guid id);

    Task<Claim> AddAsync(Claim claim);

    Task<Claim> UpdateAsync(Claim claim);
}
