using PharmaAccess.Domain.Research;
using Xunit;

namespace PharmaAccess.Domain.Tests;

public sealed class AndaLaunchGovernanceTests
{
    [Fact] public void Identity_UsesAndaDateYearAndSequence()
    {
        var a = new AndaLaunchKey("123456", new(2023, 4, 5), 2023, 7);
        var b = new AndaLaunchKey("123456", new(2023, 4, 5), 2023, 8);
        Assert.NotEqual(a.GenericLaunchId, b.GenericLaunchId);
    }

    [Theory]
    [InlineData(true,true,true,AndaLaunchEligibility.AuthoritativeNdcObserved)]
    [InlineData(true,true,false,AndaLaunchEligibility.AuthoritativeNdcZero)]
    [InlineData(true,false,false,AndaLaunchEligibility.ProductIdentityWithoutNdc)]
    [InlineData(false,false,false,AndaLaunchEligibility.NoProductIdentity)]
    public void Eligibility_DistinguishesZeroFromUnavailable(bool product, bool ndc, bool observed, AndaLaunchEligibility expected)
        => Assert.Equal(expected, AndaLaunchGovernance.Classify(product, ndc, observed));

    [Fact] public void TemporalSplit_IsGroupedByLaunchIdentityAndLocks2024()
    {
        Assert.Equal(AndaTemporalPartition.Training, AndaLaunchGovernance.Partition(new(2022,12,31)));
        Assert.Equal(AndaTemporalPartition.Validation, AndaLaunchGovernance.Partition(new(2023,1,1)));
        Assert.Equal(AndaTemporalPartition.LockedTest, AndaLaunchGovernance.Partition(new(2024,1,1)));
    }

    [Fact] public void Aggregation_DoesNotDoubleCountAndSuppressionRemainsNull()
    {
        Assert.Equal(7, AndaLaunchGovernance.AggregatePrescriptions(new[] { (1L,(long?)3,false), (2L,(long?)4,false) }));
        Assert.Throws<InvalidOperationException>(() => AndaLaunchGovernance.AggregatePrescriptions(new[] { (1L,(long?)3,false), (1L,(long?)3,false) }));
        Assert.Null(AndaLaunchGovernance.AggregatePrescriptions(new[] { (1L,(long?)3,false), (2L,(long?)null,true) }));
    }
}
