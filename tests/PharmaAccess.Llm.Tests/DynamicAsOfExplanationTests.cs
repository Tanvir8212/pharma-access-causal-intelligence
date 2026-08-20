using PharmaAccess.Application.MachineLearning;
using Xunit;
namespace PharmaAccess.Llm.Tests;
public sealed class DynamicAsOfExplanationTests
{
 [Fact]public async Task Dynamic_grounding_excludes_future_outcome(){var scoring=new Scoring();var model=new ModelStub{Available=false};var response=await new DynamicAsOfExplanationService(scoring,Doubles.Composer(model)).ExplainAsync(new(7,20243));Assert.Equal(new(7,20243),scoring.Request);Assert.NotNull(response.Analysis);Assert.Contains("Outcome is not shown",response.Analysis!.ModelOutcomeAssessment);Assert.DoesNotContain("actual entry",string.Join('|',response.Analysis.WhatYouShouldKnow),StringComparison.OrdinalIgnoreCase);}
 sealed class Scoring:IDynamicAsOfPredictionService{public DynamicAsOfRequest? Request;public Task<IReadOnlyList<HistoricalReplayLaunch>> GetLaunchesAsync(CancellationToken t=default)=>throw new NotSupportedException();public Task<IReadOnlyList<int>> GetQuartersAsync(int id,CancellationToken t=default)=>throw new NotSupportedException();public Task<DynamicAsOfResponse> ScoreAsync(DynamicAsOfRequest r,CancellationToken t=default){Request=r;return Task.FromResult(new DynamicAsOfResponse(DynamicAsOfMode.Name,7,"Launch 7",20243,20244,1,[new(48,"TX","Texas",.2f,true,1)],"v","h","d","f",.08,ModelApprovalStatus.ValidationSelected,false,false,[]));}}
}
