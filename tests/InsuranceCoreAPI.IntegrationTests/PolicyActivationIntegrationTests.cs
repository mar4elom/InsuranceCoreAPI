using System.Net;
using System.Net.Http.Json;
using InsuranceCoreAPI.IntegrationTests;

namespace InsuranceCoreAPI.IntegrationTests;

/// <summary>
/// Integration tests for policy creation and the Draft → Active transition.
/// Each class gets its own <see cref="ApiFactory"/> and therefore its own isolated in-memory store.
/// </summary>
public class PolicyActivationIntegrationTests : IDisposable
{
    private readonly ApiFactory _factory = new();
    private readonly HttpClient _client;

    public PolicyActivationIntegrationTests() => _client = _factory.CreateClient();

    // Test 1 (required): Activating a Draft policy succeeds and returns 200.
    [Fact]
    public async Task Activate_DraftPolicy_Returns200WithActiveStatus()
    {
        var customerId = await ApiHelpers.CreateCustomerAsync(_client);
        var policyId   = await ApiHelpers.CreatePolicyAsync(_client, customerId);

        var response = await ApiHelpers.ActivatePolicyAsync(_client, policyId);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<PolicyDto>();
        Assert.Equal("Active", body!.Status);
    }

    // Test 2 (required): Activating the same policy again returns 409.
    [Fact]
    public async Task Activate_AlreadyActivePolicy_Returns409()
    {
        var customerId = await ApiHelpers.CreateCustomerAsync(_client);
        var policyId   = await ApiHelpers.CreatePolicyAsync(_client, customerId);
        (await ApiHelpers.ActivatePolicyAsync(_client, policyId)).EnsureSuccessStatusCode();

        var second = await ApiHelpers.ActivatePolicyAsync(_client, policyId);

        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    // Test 3 (required): Overlap with an existing active policy of the same product type → 409.
    [Fact]
    public async Task Activate_OverlappingPolicySameProductType_Returns409()
    {
        var customerId = await ApiHelpers.CreateCustomerAsync(_client);
        var start = new DateOnly(2026, 1, 1);
        var end   = new DateOnly(2026, 12, 31);

        await ApiHelpers.CreateActivePolicyAsync(_client, customerId, "Auto", start, end);

        var overlapId = await ApiHelpers.CreatePolicyAsync(
            _client, customerId, "Auto",
            startDate: new DateOnly(2025, 6, 1),
            endDate:   new DateOnly(2026, 5, 31));

        var response = await ApiHelpers.ActivatePolicyAsync(_client, overlapId);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Activate_OverlappingPoliciesDifferentProductType_Returns200()
    {
        var customerId = await ApiHelpers.CreateCustomerAsync(_client);
        var start = new DateOnly(2026, 1, 1);
        var end   = new DateOnly(2026, 12, 31);

        await ApiHelpers.CreateActivePolicyAsync(_client, customerId, "Auto", start, end);

        var propertyId = await ApiHelpers.CreatePolicyAsync(_client, customerId, "Property", start, end);
        var response   = await ApiHelpers.ActivatePolicyAsync(_client, propertyId);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Activate_UnknownPolicy_Returns404()
    {
        var response = await ApiHelpers.ActivatePolicyAsync(_client, Guid.NewGuid());
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CreatePolicy_EndDateNotAfterStartDate_Returns400()
    {
        var customerId = await ApiHelpers.CreateCustomerAsync(_client);

        var response = await _client.PostAsJsonAsync("/policies", new
        {
            customerId,
            productType = "Auto",
            startDate   = new DateOnly(2026, 12, 31),
            endDate     = new DateOnly(2026, 1, 1),
            premium     = 100m
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Activate_AdjacentNonOverlappingPolicies_Returns200()
    {
        // The inclusive overlap check: end == other.start means they share one day (overlap).
        // To be truly adjacent (no overlap), the second must start the day AFTER the first ends.
        var customerId = await ApiHelpers.CreateCustomerAsync(_client);

        await ApiHelpers.CreateActivePolicyAsync(
            _client, customerId, "Travel",
            startDate: new DateOnly(2026, 1, 1),
            endDate:   new DateOnly(2026, 6, 29));   // ends Jun 29

        var nextId = await ApiHelpers.CreatePolicyAsync(
            _client, customerId, "Travel",
            startDate: new DateOnly(2026, 6, 30),    // starts Jun 30 — no shared day
            endDate:   new DateOnly(2026, 12, 31));

        var response = await ApiHelpers.ActivatePolicyAsync(_client, nextId);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    public void Dispose() => _factory.Dispose();

    private record PolicyDto(Guid Id, string Status);
}
