using InsuranceCoreApi.Domain;
using InsuranceCoreAPI.Domain.Enums;
using InsuranceCoreAPI.DTOs.Policies;
using InsuranceCoreAPI.Errors;
using InsuranceCoreAPI.Repositories.Interfaces;
using InsuranceCoreAPI.Services;
using NSubstitute;

namespace InsuranceCoreAPI.UnitTests;

public class PolicyServiceTests
{
    private readonly IPolicyRepository _policyRepo = Substitute.For<IPolicyRepository>();
    private readonly ICustomerRepository _customerRepo = Substitute.For<ICustomerRepository>();
    private readonly PolicyService _sut;

    private static readonly DateOnly Jan1 = new(2026, 1, 1);
    private static readonly DateOnly Dec31 = new(2026, 12, 31);

    public PolicyServiceTests()
    {
        _sut = new PolicyService(_policyRepo, _customerRepo);
    }

    // ── CreateAsync ────────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateAsync_EndDateEqualToStartDate_ThrowsValidationException()
    {
        var request = new CreatePolicyRequest(Guid.NewGuid(), ProductType.Auto, Jan1, Jan1, 100m);

        await Assert.ThrowsAsync<ValidationException>(() => _sut.CreateAsync(request));
    }

    [Fact]
    public async Task CreateAsync_EndDateBeforeStartDate_ThrowsValidationException()
    {
        var request = new CreatePolicyRequest(Guid.NewGuid(), ProductType.Auto, Dec31, Jan1, 100m);

        await Assert.ThrowsAsync<ValidationException>(() => _sut.CreateAsync(request));
    }

    [Fact]
    public async Task CreateAsync_CustomerNotFound_ThrowsNotFoundException()
    {
        var customerId = Guid.NewGuid();
        _customerRepo.GetByIdAsync(customerId).Returns((Domain.Customer?)null);

        var request = new CreatePolicyRequest(customerId, ProductType.Auto, Jan1, Dec31, 100m);

        await Assert.ThrowsAsync<NotFoundException>(() => _sut.CreateAsync(request));
    }

    [Fact]
    public async Task CreateAsync_ValidRequest_ReturnsDraftPolicy()
    {
        var customerId = Guid.NewGuid();
        _customerRepo.GetByIdAsync(customerId).Returns(new Domain.Customer { FullName = "Alice" });
        _policyRepo.AddAsync(Arg.Any<Policy>()).Returns(x => x.Arg<Policy>());

        var request = new CreatePolicyRequest(customerId, ProductType.Property, Jan1, Dec31, 250m);

        var policy = await _sut.CreateAsync(request);

        Assert.Equal(PolicyStatus.Draft, policy.Status);
        Assert.Equal(ProductType.Property, policy.ProductType);
        Assert.Equal(250m, policy.Premium);
    }

    // ── ActivateAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task ActivateAsync_PolicyNotFound_ThrowsNotFoundException()
    {
        _policyRepo.GetByIdAsync(Arg.Any<Guid>()).Returns((Policy?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => _sut.ActivateAsync(Guid.NewGuid()));
    }

    // Test 2 (required): Activating an already-Active policy → 409
    [Fact]
    public async Task ActivateAsync_PolicyAlreadyActive_ThrowsConflictException()
    {
        var policy = MakePolicy(PolicyStatus.Active);
        _policyRepo.GetByIdAsync(policy.Id).Returns(policy);

        await Assert.ThrowsAsync<ConflictException>(() => _sut.ActivateAsync(policy.Id));
    }

    // Test 3 (required): Overlapping active policies for same customer + product → 409
    [Fact]
    public async Task ActivateAsync_OverlappingActivePolicy_ThrowsConflictException()
    {
        var policy = MakePolicy(PolicyStatus.Draft, new DateOnly(2026, 6, 1), new DateOnly(2026, 12, 31));
        var existing = MakePolicy(PolicyStatus.Active, Jan1, Dec31, policy.CustomerId, policy.ProductType);

        _policyRepo.GetByIdAsync(policy.Id).Returns(policy);
        _policyRepo.GetActivePoliciesAsync(policy.CustomerId, policy.ProductType)
            .Returns(new List<Policy> { existing });

        await Assert.ThrowsAsync<ConflictException>(() => _sut.ActivateAsync(policy.Id));
    }

    // Test 1 (required): Activating a Draft policy succeeds
    [Fact]
    public async Task ActivateAsync_ValidDraftPolicy_ReturnsActivatedPolicy()
    {
        var policy = MakePolicy(PolicyStatus.Draft);
        _policyRepo.GetByIdAsync(policy.Id).Returns(policy);
        _policyRepo.GetActivePoliciesAsync(policy.CustomerId, policy.ProductType)
            .Returns(new List<Policy>());
        _policyRepo.UpdateAsync(policy).Returns(policy);

        var result = await _sut.ActivateAsync(policy.Id);

        Assert.Equal(PolicyStatus.Active, result.Status);
    }

    [Fact]
    public async Task ActivateAsync_NonOverlappingPoliciesSameProduct_Succeeds()
    {
        // Existing active: Jan–Jun; candidate: Jul–Dec — no overlap
        var candidate = MakePolicy(PolicyStatus.Draft,
            startDate: new DateOnly(2026, 7, 1),
            endDate: new DateOnly(2026, 12, 31));

        var existing = MakePolicy(PolicyStatus.Active,
            startDate: new DateOnly(2026, 1, 1),
            endDate: new DateOnly(2026, 6, 30),
            customerId: candidate.CustomerId,
            productType: candidate.ProductType);

        _policyRepo.GetByIdAsync(candidate.Id).Returns(candidate);
        _policyRepo.GetActivePoliciesAsync(candidate.CustomerId, candidate.ProductType)
            .Returns(new List<Policy> { existing });
        _policyRepo.UpdateAsync(candidate).Returns(candidate);

        var result = await _sut.ActivateAsync(candidate.Id);

        Assert.Equal(PolicyStatus.Active, result.Status);
    }

    // ── Helpers ────────────────────────────────────────────────────────────────

    private static Policy MakePolicy(
        PolicyStatus status,
        DateOnly? startDate = null,
        DateOnly? endDate = null,
        Guid? customerId = null,
        ProductType productType = ProductType.Auto) => new()
    {
        CustomerId = customerId ?? Guid.NewGuid(),
        ProductType = productType,
        StartDate = startDate ?? Jan1,
        EndDate = endDate ?? Dec31,
        Premium = 100m,
        Status = status
    };
}
