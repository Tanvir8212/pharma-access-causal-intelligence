using PharmaAccess.Application.MachineLearning;
using Xunit;

namespace PharmaAccess.Application.Tests;

public sealed class StateMarketContextTests
{
    [Fact]
    public void Duplicate_source_file_lineage_fails_closed_before_metrics_can_inflate()
    {
        var quarters = StateMarketContextCalculator.PreviousQuarters(20244, 8);
        var source = new StateMarketContextSource(1, "AL", 10, 1m, 1m,
            quarters.Select(q => new StateMarketQuarterVolume(q, 10, false, q == 20244 ? 2 : 1)).ToArray());

        Assert.Throws<InvalidDataException>(() => StateMarketContextCalculator.Calculate(20244, [source]));
    }

    [Fact]
    public void Fewer_than_two_valid_jurisdictions_returns_unavailable_percentiles()
    {
        var quarters = StateMarketContextCalculator.PreviousQuarters(20244, 8);
        var source = new StateMarketContextSource(1, "AL", 10, 1m, 1m,
            quarters.Select((q, i) => new StateMarketQuarterVolume(q, i < 4 ? 10 : 20, false)).ToArray());
        var result = StateMarketContextCalculator.Calculate(20244, [source]).Single();

        Assert.Null(result.MarketSizeScore);
        Assert.Null(result.GrowthMomentumScore);
    }

    [Fact]
    public void Recent_market_size_uses_exact_latest_four_quarters_and_ignores_future()
    {
        var q = StateMarketContextCalculator.PreviousQuarters(20244, 8);
        var result = Calculate(Sources(
            (1, "AL", q.Select((x, i) => V(x, i < 4 ? 10 : 20)).Append(V(20251, 9_999_999)).ToArray()),
            (2, "AK", q.Select((x, i) => V(x, i < 4 ? 10 : 10)).ToArray())));
        Assert.Equal(80, result.Single(x=>x.StateId==1).RecentMarketVolume);
        Assert.Equal(100, result.Single(x=>x.StateId==1).MarketSizeScore);
        Assert.Equal(0, result.Single(x=>x.StateId==2).MarketSizeScore);
    }

    [Fact]
    public void Missing_recent_quarter_or_value_makes_size_unavailable_but_does_not_create_zero()
    {
        var q = StateMarketContextCalculator.PreviousQuarters(20244, 8);
        var missingQuarter = One(q.Where(x => x != 20243).Select(x => V(x, 10)).ToArray());
        var missingValue = One(q.Select(x => V(x, x == 20243 ? null : 10, x == 20243)).ToArray());
        Assert.Null(missingQuarter.RecentMarketVolume); Assert.Null(missingQuarter.MarketSizeScore);
        Assert.Null(missingValue.RecentMarketVolume); Assert.Null(missingValue.MarketSizeScore);
        Assert.Equal(MarketDataSupport.Limited, missingValue.DataSupport);
    }

    [Fact]
    public void Suppressed_inputs_are_not_zero_and_reduce_data_support_without_discarding_observed_aggregate()
    {
        var q = StateMarketContextCalculator.PreviousQuarters(20244, 8);
        var result = One(q.Select(x => V(x, 10, x == 20243)).ToArray());
        Assert.Equal(40, result.RecentMarketVolume);
        Assert.Equal(MarketDataSupport.Moderate, result.DataSupport);
    }

    [Fact]
    public void Size_and_growth_percentiles_use_average_ties_and_full_boundaries()
    {
        var q = StateMarketContextCalculator.PreviousQuarters(20244, 8);
        var result = Calculate(Sources(
            (1,"AL",q.Select((x,i)=>V(x,i<4?10:20)).ToArray()),
            (2,"AK",q.Select((x,i)=>V(x,i<4?10:20)).ToArray()),
            (4,"AZ",q.Select((x,i)=>V(x,i<4?10:30)).ToArray()),
            (5,"AR",q.Select((x,i)=>V(x,i<4?10:10)).ToArray())));
        Assert.Equal(50m, result.Single(x=>x.StateId==1).MarketSizeScore);
        Assert.Equal(50m, result.Single(x=>x.StateId==2).MarketSizeScore);
        Assert.Equal(100m, result.Single(x=>x.StateId==4).GrowthMomentumScore);
        Assert.Equal(0m, result.Single(x=>x.StateId==5).GrowthMomentumScore);
    }

    [Fact]
    public void Growth_uses_exact_eight_quarters_for_positive_and_negative_values()
    {
        var q = StateMarketContextCalculator.PreviousQuarters(20244, 8);
        Assert.Equal(.5m, One(q.Select((x,i)=>V(x,i<4?100:150)).ToArray()).StateMarketGrowthRate);
        Assert.Equal(-.5m, One(q.Select((x,i)=>V(x,i<4?200:100)).ToArray()).StateMarketGrowthRate);
    }

    [Fact]
    public void Zero_denominator_and_incomplete_growth_are_unavailable_while_valid_recent_size_remains()
    {
        var q = StateMarketContextCalculator.PreviousQuarters(20244, 8);
        var zero = One(q.Select((x,i)=>V(x,i<4?0:10)).ToArray());
        var incomplete = One(q.Skip(1).Select(x=>V(x,10)).ToArray());
        Assert.Null(zero.StateMarketGrowthRate); Assert.Equal(40, zero.RecentMarketVolume);
        Assert.Null(incomplete.StateMarketGrowthRate); Assert.Equal(40, incomplete.RecentMarketVolume);
        Assert.Equal("Market context unavailable", incomplete.MarketProfile);
    }

    [Fact]
    public void Scores_are_invariant_to_drug_candidate_subset()
    {
        var q = StateMarketContextCalculator.PreviousQuarters(20244, 8);
        var full = Calculate(Sources(
            (1,"AL",q.Select((x,i)=>V(x,i<4?10:20)).ToArray()),
            (2,"AK",q.Select((x,i)=>V(x,i<4?20:30)).ToArray()),
            (48,"TX",q.Select((x,i)=>V(x,i<4?30:60)).ToArray())));
        var drugA = full.Where(x => new[] { 1, 48 }.Contains(x.StateId)).Single(x => x.StateId == 48);
        var drugB = full.Where(x => new[] { 2, 48 }.Contains(x.StateId)).Single(x => x.StateId == 48);
        Assert.Equal(drugA.MarketSizeScore, drugB.MarketSizeScore);
        Assert.Equal(drugA.GrowthMomentumScore, drugB.GrowthMomentumScore);
    }

    [Fact]
    public void Changing_future_data_cannot_change_any_as_of_value()
    {
        var q = StateMarketContextCalculator.PreviousQuarters(20244, 8);
        var baseline = One(q.Select((x,i)=>V(x,i<4?10:20)).ToArray());
        var withFuture = One(q.Select((x,i)=>V(x,i<4?10:20)).Append(V(20251,999_999)).ToArray());
        Assert.Equal(baseline, withFuture);
    }

    [Theory]
    [InlineData(.95, true, true, false, MarketDataSupport.High)]
    [InlineData(.80, true, true, false, MarketDataSupport.Moderate)]
    [InlineData(.95, true, true, true, MarketDataSupport.Moderate)]
    [InlineData(.95, true, false, false, MarketDataSupport.Limited)]
    [InlineData(.95, false, false, false, MarketDataSupport.Limited)]
    public void Data_support_considers_size_growth_completeness_and_missingness(double completeness, bool size, bool growth, bool missing, MarketDataSupport expected) =>
        Assert.Equal(expected, StateMarketContextCalculator.DataSupport((decimal)completeness,size,growth,missing));

    [Theory]
    [InlineData(50,50,"Large & Growing")][InlineData(50,49,"Large & Established")]
    [InlineData(49,50,"Emerging")][InlineData(49,49,"Smaller / Stable")]
    public void Profiles_cover_quadrants(double size,double growth,string expected) =>
        Assert.Equal(expected,StateMarketContextCalculator.Profile((decimal)size,(decimal)growth));

    [Fact]
    public void Profiles_distinguish_missing_size_and_growth()
    {
        Assert.Equal("Market context unavailable",StateMarketContextCalculator.Profile(null,80));
        Assert.Equal("Growth history unavailable",StateMarketContextCalculator.Profile(80,null));
    }

    [Fact]
    public void Export_appends_recent_and_baseline_semantics_without_removing_layer_one_columns()
    {
        var context=new StateMarketContext(48,"TX","Texas",20244,1200,90,1000,.04m,.2m,80,1200,1000,8,1,MarketDataSupport.High,"Large & Growing");
        var files=PredictionResultExport.Create(new SafePredictionExport("mode",1,"launch",20244,20251,
            [new HistoricalReplayStateResult(48,"TX","Texas",.1f,true,1)],.08,"model","dataset","features",ModelApprovalStatus.ValidationSelected,false){MarketContexts=[context]});
        Assert.Contains("Probability,AboveSelectedThreshold",files.Csv);
        Assert.Contains("RecentMarketVolume,MarketSizeScore,HistoricalBaselineVolume,HistoricalBaselineMarketWeight",files.Csv);
        Assert.Contains("1200,90,1000,0.04",files.Csv);
    }

    private static StateMarketQuarterVolume V(int quarter,long? volume,bool missing=false)=>new(quarter,volume,missing);
    private static IReadOnlyList<StateMarketContextSource> Sources(params (int Id,string Code,StateMarketQuarterVolume[] Volumes)[] states)=>
        states.Select(x=>new StateMarketContextSource(x.Id,x.Code,1_000,.01m,.95m,x.Volumes)).ToArray();
    private static StateMarketContext One(StateMarketQuarterVolume[] volumes)=>Calculate(Sources((1,"AL",volumes))).Single();
    private static IReadOnlyList<StateMarketContext> Calculate(IReadOnlyList<StateMarketContextSource> sources)=>StateMarketContextCalculator.Calculate(20244,sources);
}
