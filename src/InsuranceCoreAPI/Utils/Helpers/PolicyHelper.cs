using InsuranceCoreApi.Domain;
using InsuranceCoreAPI.DTOs.Policies;

namespace InsuranceCoreAPI.Utils.Helpers;

public static class PolicyHelper
{
    public static PolicyResponse ToResponse(Policy p) => new(
        p.Id, p.CustomerId, p.ProductType, p.StartDate, p.EndDate, p.Premium, p.Status);
}
