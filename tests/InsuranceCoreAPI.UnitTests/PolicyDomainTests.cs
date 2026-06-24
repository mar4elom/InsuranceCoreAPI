using InsuranceCoreApi.Domain;
using InsuranceCoreAPI.Domain.Enums;

namespace InsuranceCoreAPI.UnitTests;

/// <summary>
/// Pure unit tests for <see cref="Policy.OverlapsWith"/>.
/// The method uses inclusive bounds on both ends: StartDate &lt;= otherEnd &amp;&amp; otherStart &lt;= EndDate.
/// </summary>
public class PolicyDomainTests
{
    private static Policy MakePolicy(DateOnly start, DateOnly end) => new()
    {
        CustomerId = Guid.NewGuid(),
        ProductType = ProductType.Auto,
        StartDate = start,
        EndDate = end,
        Premium = 100m
    };

    [Fact]
    public void OverlapsWith_IdenticalRanges_ReturnsTrue()
    {
        var policy = MakePolicy(new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31));
        Assert.True(policy.OverlapsWith(new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31)));
    }

    [Fact]
    public void OverlapsWith_PartialOverlapFromLeft_ReturnsTrue()
    {
        var policy = MakePolicy(new DateOnly(2026, 6, 1), new DateOnly(2026, 12, 31));
        Assert.True(policy.OverlapsWith(new DateOnly(2026, 1, 1), new DateOnly(2026, 6, 30)));
    }

    [Fact]
    public void OverlapsWith_PartialOverlapFromRight_ReturnsTrue()
    {
        var policy = MakePolicy(new DateOnly(2026, 1, 1), new DateOnly(2026, 6, 30));
        Assert.True(policy.OverlapsWith(new DateOnly(2026, 6, 1), new DateOnly(2026, 12, 31)));
    }

    [Fact]
    public void OverlapsWith_FullyContainedInside_ReturnsTrue()
    {
        var policy = MakePolicy(new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31));
        Assert.True(policy.OverlapsWith(new DateOnly(2026, 3, 1), new DateOnly(2026, 9, 30)));
    }

    [Fact]
    public void OverlapsWith_EntirelyBefore_ReturnsFalse()
    {
        var policy = MakePolicy(new DateOnly(2026, 7, 1), new DateOnly(2026, 12, 31));
        Assert.False(policy.OverlapsWith(new DateOnly(2026, 1, 1), new DateOnly(2026, 6, 30)));
    }

    [Fact]
    public void OverlapsWith_EntirelyAfter_ReturnsFalse()
    {
        var policy = MakePolicy(new DateOnly(2026, 1, 1), new DateOnly(2026, 6, 30));
        Assert.False(policy.OverlapsWith(new DateOnly(2026, 7, 1), new DateOnly(2026, 12, 31)));
    }

    [Fact]
    public void OverlapsWith_EndDateTouchesOtherStartDate_ReturnsTrue()
    {
        // Inclusive bounds: policy ends 2026-06-30, other starts 2026-06-30 → they share one day.
        var policy = MakePolicy(new DateOnly(2026, 1, 1), new DateOnly(2026, 6, 30));
        Assert.True(policy.OverlapsWith(new DateOnly(2026, 6, 30), new DateOnly(2026, 12, 31)));
    }

    [Fact]
    public void OverlapsWith_StartDateTouchesOtherEndDate_ReturnsTrue()
    {
        // policy starts 2026-07-01, other ends 2026-07-01 → share one day.
        var policy = MakePolicy(new DateOnly(2026, 7, 1), new DateOnly(2026, 12, 31));
        Assert.True(policy.OverlapsWith(new DateOnly(2026, 1, 1), new DateOnly(2026, 7, 1)));
    }
}
