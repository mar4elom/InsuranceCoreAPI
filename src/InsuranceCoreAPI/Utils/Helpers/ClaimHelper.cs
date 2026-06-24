using InsuranceCoreApi.Domain;
using InsuranceCoreAPI.DTOs.Claims;

namespace InsuranceCoreAPI.Utils.Helpers;

public static class ClaimHelper
{
    public static ClaimResponse ToResponse(Claim c) => new(
        c.Id, c.PolicyId, c.IncidentDate, c.AmountRequested, c.Status, c.DecisionReason);
}