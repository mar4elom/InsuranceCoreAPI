using Microsoft.AspNetCore.Mvc.Testing;
using System.Net.Http.Json;

namespace InsuranceCoreAPI.IntegrationTests;

/// <summary>
/// Wraps the real application in-process using WebApplicationFactory.
/// Each test class should create and dispose its own factory instance to get an
/// isolated in-memory store (repositories are singletons scoped to the host lifetime).
/// </summary>
public class ApiFactory : WebApplicationFactory<Program>;

/// <summary>
/// HTTP helpers that build up state by calling the real API endpoints.
/// </summary>
public static class ApiHelpers
{
    public static async Task<Guid> CreateCustomerAsync(HttpClient client, string fullName = "Test Customer")
    {
        var response = await client.PostAsJsonAsync("/customers", new { fullName });
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<IdDto>();
        return body!.Id;
    }

    public static async Task<Guid> CreatePolicyAsync(
        HttpClient client,
        Guid customerId,
        string productType = "Auto",
        DateOnly? startDate = null,
        DateOnly? endDate = null,
        decimal premium = 100m)
    {
        var start = startDate ?? new DateOnly(2026, 1, 1);
        var end   = endDate   ?? new DateOnly(2026, 12, 31);

        var response = await client.PostAsJsonAsync("/policies", new
        {
            customerId,
            productType,
            startDate = start,
            endDate = end,
            premium
        });
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<IdDto>();
        return body!.Id;
    }

    public static async Task<HttpResponseMessage> ActivatePolicyAsync(HttpClient client, Guid policyId) =>
        await client.PostAsync($"/policies/{policyId}/activate", null);

    public static async Task<Guid> CreateActivePolicyAsync(
        HttpClient client,
        Guid customerId,
        string productType = "Auto",
        DateOnly? startDate = null,
        DateOnly? endDate = null)
    {
        var id = await CreatePolicyAsync(client, customerId, productType, startDate, endDate);
        (await ActivatePolicyAsync(client, id)).EnsureSuccessStatusCode();
        return id;
    }

    public static async Task<HttpResponseMessage> CreateClaimAsync(
        HttpClient client,
        Guid policyId,
        DateOnly? incidentDate = null,
        decimal amount = 500m)
    {
        var date = incidentDate ?? new DateOnly(2026, 6, 15);
        return await client.PostAsJsonAsync("/claims", new { policyId, incidentDate = date, amountRequested = amount });
    }

    private record IdDto(Guid Id);
}
