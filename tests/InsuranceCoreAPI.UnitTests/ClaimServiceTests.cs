using InsuranceCoreApi.Domain;
using InsuranceCoreAPI.Domain.Enums;
using InsuranceCoreAPI.DTOs.Claims;
using InsuranceCoreAPI.Errors;
using InsuranceCoreAPI.Repositories.Interfaces;
using InsuranceCoreAPI.Services;
using NSubstitute;

namespace InsuranceCoreAPI.UnitTests;

public class ClaimServiceTests
{
    private readonly IClaimRepository _claimRepo = Substitute.For<IClaimRepository>();
    private readonly IPolicyRepository _policyRepo = Substitute.For<IPolicyRepository>();
    private readonly ClaimService _sut;

    private static readonly DateOnly PolicyStart = new(2026, 1, 1);
    private static readonly DateOnly PolicyEnd = new(2026, 12, 31);
    private static readonly DateOnly MidYear = new(2026, 6, 15);

    public ClaimServiceTests()
    {
        _sut = new ClaimService(_claimRepo, _policyRepo);
    }

    // ── CreateAsync ────────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateAsync_PolicyNotFound_ThrowsNotFoundException()
    {
        _policyRepo.GetByIdAsync(Arg.Any<Guid>()).Returns((Policy?)null);

        var request = new CreateClaimRequest(Guid.NewGuid(), MidYear, 500m);

        await Assert.ThrowsAsync<NotFoundException>(() => _sut.CreateAsync(request));
    }

    // Test 4 (required): Create claim for Draft policy → 409
    [Fact]
    public async Task CreateAsync_PolicyIsDraft_ThrowsConflictException()
    {
        var policy = MakePolicy(PolicyStatus.Draft);
        _policyRepo.GetByIdAsync(policy.Id).Returns(policy);

        var request = new CreateClaimRequest(policy.Id, MidYear, 500m);

        await Assert.ThrowsAsync<ConflictException>(() => _sut.CreateAsync(request));
    }

    // Test 5 (required): Create claim with incident date outside policy period → 409
    [Fact]
    public async Task CreateAsync_IncidentDateBeforePolicyStart_ThrowsConflictException()
    {
        var policy = MakePolicy(PolicyStatus.Active);
        _policyRepo.GetByIdAsync(policy.Id).Returns(policy);

        var request = new CreateClaimRequest(policy.Id, PolicyStart.AddDays(-1), 500m);

        await Assert.ThrowsAsync<ConflictException>(() => _sut.CreateAsync(request));
    }

    [Fact]
    public async Task CreateAsync_IncidentDateAfterPolicyEnd_ThrowsConflictException()
    {
        var policy = MakePolicy(PolicyStatus.Active);
        _policyRepo.GetByIdAsync(policy.Id).Returns(policy);

        var request = new CreateClaimRequest(policy.Id, PolicyEnd.AddDays(1), 500m);

        await Assert.ThrowsAsync<ConflictException>(() => _sut.CreateAsync(request));
    }

    [Fact]
    public async Task CreateAsync_IncidentDateOnStartBoundary_Succeeds()
    {
        var policy = MakePolicy(PolicyStatus.Active);
        _policyRepo.GetByIdAsync(policy.Id).Returns(policy);
        _claimRepo.AddAsync(Arg.Any<Claim>()).Returns(x => x.Arg<Claim>());

        var request = new CreateClaimRequest(policy.Id, PolicyStart, 500m);
        var claim = await _sut.CreateAsync(request);

        Assert.Equal(ClaimStatus.New, claim.Status);
    }

    [Fact]
    public async Task CreateAsync_IncidentDateOnEndBoundary_Succeeds()
    {
        var policy = MakePolicy(PolicyStatus.Active);
        _policyRepo.GetByIdAsync(policy.Id).Returns(policy);
        _claimRepo.AddAsync(Arg.Any<Claim>()).Returns(x => x.Arg<Claim>());

        var request = new CreateClaimRequest(policy.Id, PolicyEnd, 500m);
        var claim = await _sut.CreateAsync(request);

        Assert.Equal(ClaimStatus.New, claim.Status);
    }

    [Fact]
    public async Task CreateAsync_ValidRequest_ReturnsNewClaim()
    {
        var policy = MakePolicy(PolicyStatus.Active);
        _policyRepo.GetByIdAsync(policy.Id).Returns(policy);
        _claimRepo.AddAsync(Arg.Any<Claim>()).Returns(x => x.Arg<Claim>());

        var request = new CreateClaimRequest(policy.Id, MidYear, 750m);
        var claim = await _sut.CreateAsync(request);

        Assert.Equal(ClaimStatus.New, claim.Status);
        Assert.Equal(750m, claim.AmountRequested);
        Assert.Null(claim.DecisionReason);
    }

    // ── DecideAsync ────────────────────────────────────────────────────────────

    [Fact]
    public async Task DecideAsync_ClaimNotFound_ThrowsNotFoundException()
    {
        _claimRepo.GetByIdAsync(Arg.Any<Guid>()).Returns((Claim?)null);

        await Assert.ThrowsAsync<NotFoundException>(
            () => _sut.DecideAsync(Guid.NewGuid(), new DecideClaimRequest(ClaimDecision.Approve, null)));
    }

    [Fact]
    public async Task DecideAsync_ClaimAlreadyApproved_ThrowsConflictException()
    {
        var claim = MakeClaim(ClaimStatus.Approved);
        _claimRepo.GetByIdAsync(claim.Id).Returns(claim);

        await Assert.ThrowsAsync<ConflictException>(
            () => _sut.DecideAsync(claim.Id, new DecideClaimRequest(ClaimDecision.Approve, null)));
    }

    [Fact]
    public async Task DecideAsync_ClaimAlreadyRejected_ThrowsConflictException()
    {
        var claim = MakeClaim(ClaimStatus.Rejected);
        _claimRepo.GetByIdAsync(claim.Id).Returns(claim);

        await Assert.ThrowsAsync<ConflictException>(
            () => _sut.DecideAsync(claim.Id, new DecideClaimRequest(ClaimDecision.Approve, null)));
    }

    // Test 6 (required): Reject claim without DecisionReason → 400
    [Fact]
    public async Task DecideAsync_RejectWithoutDecisionReason_ThrowsValidationException()
    {
        var claim = MakeClaim(ClaimStatus.New);
        _claimRepo.GetByIdAsync(claim.Id).Returns(claim);

        await Assert.ThrowsAsync<ValidationException>(
            () => _sut.DecideAsync(claim.Id, new DecideClaimRequest(ClaimDecision.Reject, null)));
    }

    [Fact]
    public async Task DecideAsync_RejectWithWhitespaceReason_ThrowsValidationException()
    {
        var claim = MakeClaim(ClaimStatus.New);
        _claimRepo.GetByIdAsync(claim.Id).Returns(claim);

        await Assert.ThrowsAsync<ValidationException>(
            () => _sut.DecideAsync(claim.Id, new DecideClaimRequest(ClaimDecision.Reject, "   ")));
    }

    [Fact]
    public async Task DecideAsync_Approve_SetsApprovedStatusAndNullReason()
    {
        var claim = MakeClaim(ClaimStatus.New);
        _claimRepo.GetByIdAsync(claim.Id).Returns(claim);
        _claimRepo.UpdateAsync(claim).Returns(claim);

        var result = await _sut.DecideAsync(claim.Id, new DecideClaimRequest(ClaimDecision.Approve, null));

        Assert.Equal(ClaimStatus.Approved, result.Status);
        Assert.Null(result.DecisionReason);
    }

    [Fact]
    public async Task DecideAsync_RejectWithReason_SetsRejectedStatusAndReason()
    {
        var claim = MakeClaim(ClaimStatus.New);
        _claimRepo.GetByIdAsync(claim.Id).Returns(claim);
        _claimRepo.UpdateAsync(claim).Returns(claim);

        var result = await _sut.DecideAsync(
            claim.Id,
            new DecideClaimRequest(ClaimDecision.Reject, "Insufficient documentation"));

        Assert.Equal(ClaimStatus.Rejected, result.Status);
        Assert.Equal("Insufficient documentation", result.DecisionReason);
    }

    // ── Helpers ────────────────────────────────────────────────────────────────

    private static Policy MakePolicy(PolicyStatus status) => new()
    {
        CustomerId = Guid.NewGuid(),
        ProductType = ProductType.Auto,
        StartDate = PolicyStart,
        EndDate = PolicyEnd,
        Premium = 100m,
        Status = status
    };

    private static Claim MakeClaim(ClaimStatus status) => new()
    {
        PolicyId = Guid.NewGuid(),
        IncidentDate = MidYear,
        AmountRequested = 500m,
        Status = status
    };
}
