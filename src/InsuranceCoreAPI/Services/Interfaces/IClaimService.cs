using InsuranceCoreApi.Domain;
using InsuranceCoreAPI.DTOs.Claims;

namespace InsuranceCoreAPI.Services.Interfaces;

public interface IClaimService
{
    Task<Claim> CreateAsync(CreateClaimRequest request);

    Task<Claim> DecideAsync(Guid id, DecideClaimRequest request);

    Task<Claim> GetByIdAsync(Guid id);
}
