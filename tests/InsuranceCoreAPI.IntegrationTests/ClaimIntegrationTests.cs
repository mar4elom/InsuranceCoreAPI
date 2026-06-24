using System.Net;
using System.Net.Http.Json;

namespace InsuranceCoreAPI.IntegrationTests;

/// <summary>
/// Integration tests for claim creation and the Approve / Reject decide flow.
/// </summary>
public class ClaimIntegrationTests : IDisposable
{
    private readonly ApiFactory _factory = new();
    private readonly HttpClient _client;

    public ClaimIntegrationTests() => _client = _factory.CreateClient();

    // Test 4 (required): Creating a claim against a Draft policy returns 409.
    [Fact]
    public async Task CreateClaim_DraftPolicy_Returns409()
    {
        var customerId = await ApiHelpers.CreateCustomerAsync(_client);
        var policyId   = await ApiHelpers.CreatePolicyAsync(_client, customerId);   // stays Draft

        var response = await ApiHelpers.CreateClaimAsync(_client, policyId);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    // Test 5 (required): Incident date outside policy period returns 409.
    [Fact]
    public async Task CreateClaim_IncidentDateBeforePolicyStart_Returns409()
    {
        var customerId = await ApiHelpers.CreateCustomerAsync(_client);
        var start = new DateOnly(2026, 3, 1);
        var end   = new DateOnly(2026, 11, 30);
        var policyId = await ApiHelpers.CreateActivePolicyAsync(_client, customerId, "Auto", start, end);

        var response = await ApiHelpers.CreateClaimAsync(_client, policyId,
            incidentDate: start.AddDays(-1));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task CreateClaim_IncidentDateAfterPolicyEnd_Returns409()
    {
        var customerId = await ApiHelpers.CreateCustomerAsync(_client);
        var start = new DateOnly(2026, 1, 1);
        var end   = new DateOnly(2026, 6, 30);
        var policyId = await ApiHelpers.CreateActivePolicyAsync(_client, customerId, "Property", start, end);

        var response = await ApiHelpers.CreateClaimAsync(_client, policyId,
            incidentDate: end.AddDays(1));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    // Test 6 (required): Rejecting a claim without a reason returns 400.
    [Fact]
    public async Task DecideClaim_RejectWithoutReason_Returns400()
    {
        var customerId = await ApiHelpers.CreateCustomerAsync(_client);
        var policyId   = await ApiHelpers.CreateActivePolicyAsync(_client, customerId);
        var createResp = await ApiHelpers.CreateClaimAsync(_client, policyId);
        var claim      = await createResp.Content.ReadFromJsonAsync<ClaimDto>();

        var response = await _client.PostAsJsonAsync(
            $"/claims/{claim!.Id}/decide",
            new { decision = "Reject", decisionReason = (string?)null });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateClaim_ActivePolicyWithinPeriod_Returns201()
    {
        var customerId = await ApiHelpers.CreateCustomerAsync(_client);
        var policyId   = await ApiHelpers.CreateActivePolicyAsync(_client, customerId);

        var response = await ApiHelpers.CreateClaimAsync(_client, policyId);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task CreateClaim_IncidentDateOnStartBoundary_Returns201()
    {
        var customerId = await ApiHelpers.CreateCustomerAsync(_client);
        var start = new DateOnly(2026, 1, 1);
        var end   = new DateOnly(2026, 12, 31);
        var policyId = await ApiHelpers.CreateActivePolicyAsync(_client, customerId, "Travel", start, end);

        var response = await ApiHelpers.CreateClaimAsync(_client, policyId, incidentDate: start);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task CreateClaim_IncidentDateOnEndBoundary_Returns201()
    {
        var customerId = await ApiHelpers.CreateCustomerAsync(_client);
        var start = new DateOnly(2026, 1, 1);
        var end   = new DateOnly(2026, 12, 31);
        var policyId = await ApiHelpers.CreateActivePolicyAsync(_client, customerId, "Auto", start, end);

        var response = await ApiHelpers.CreateClaimAsync(_client, policyId, incidentDate: end);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task DecideClaim_Approve_Returns200WithApprovedStatus()
    {
        var customerId = await ApiHelpers.CreateCustomerAsync(_client);
        var policyId   = await ApiHelpers.CreateActivePolicyAsync(_client, customerId);
        var createResp = await ApiHelpers.CreateClaimAsync(_client, policyId);
        var claim      = await createResp.Content.ReadFromJsonAsync<ClaimDto>();

        var response = await _client.PostAsJsonAsync(
            $"/claims/{claim!.Id}/decide",
            new { decision = "Approve", decisionReason = (string?)null });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var decided = await response.Content.ReadFromJsonAsync<ClaimDto>();
        Assert.Equal("Approved", decided!.Status);
    }

    [Fact]
    public async Task DecideClaim_RejectWithReason_Returns200WithRejectedStatus()
    {
        var customerId = await ApiHelpers.CreateCustomerAsync(_client);
        var policyId   = await ApiHelpers.CreateActivePolicyAsync(_client, customerId);
        var createResp = await ApiHelpers.CreateClaimAsync(_client, policyId);
        var claim      = await createResp.Content.ReadFromJsonAsync<ClaimDto>();

        var response = await _client.PostAsJsonAsync(
            $"/claims/{claim!.Id}/decide",
            new { decision = "Reject", decisionReason = "Insufficient documentation" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var decided = await response.Content.ReadFromJsonAsync<ClaimDto>();
        Assert.Equal("Rejected", decided!.Status);
        Assert.Equal("Insufficient documentation", decided.DecisionReason);
    }

    [Fact]
    public async Task DecideClaim_AlreadyDecided_Returns409()
    {
        var customerId = await ApiHelpers.CreateCustomerAsync(_client);
        var policyId   = await ApiHelpers.CreateActivePolicyAsync(_client, customerId);
        var createResp = await ApiHelpers.CreateClaimAsync(_client, policyId);
        var claim      = await createResp.Content.ReadFromJsonAsync<ClaimDto>();

        await _client.PostAsJsonAsync($"/claims/{claim!.Id}/decide",
            new { decision = "Approve", decisionReason = (string?)null });

        var second = await _client.PostAsJsonAsync($"/claims/{claim.Id}/decide",
            new { decision = "Reject", decisionReason = "Changed mind" });

        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task CreateClaim_UnknownPolicy_Returns404()
    {
        var response = await ApiHelpers.CreateClaimAsync(_client, Guid.NewGuid());
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    public void Dispose() => _factory.Dispose();

    private record ClaimDto(Guid Id, string Status, string? DecisionReason);
}
