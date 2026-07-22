using PharmaAccess.Application.Research;using Xunit;
namespace PharmaAccess.Application.Tests;
public sealed class ResearchAnswerServiceTests
{
    private readonly DeterministicResearchAnswerService service=new(new Data(),new Artifacts());
    [Theory][InlineData("How many launches were analyzed?","261")][InlineData("What was the ROC AUC?","0.8221")][InlineData("What were the train, validation and test sizes?","147")][InlineData("What are ND, WD and Access Gap?","Numeric Distribution")]public async Task Routes_supported_questions(string q,string expected)=>Assert.Contains(expected,(await service.AnswerAsync(q)).Answer);
    [Fact]public async Task Causal_answer_is_conservative(){var a=await service.AnswerAsync("Did neighboring-state adoption cause adoption?");Assert.Contains("includes zero",a.Answer);Assert.Contains("does not establish",a.Answer);}
    [Fact]public async Task Discloses_python_dotnet_difference(){var a=await service.AnswerAsync("What is the difference between the .NET and Python estimates?");Assert.Contains("0.00157",a.Answer);Assert.Contains("-0.01382",a.Answer);Assert.Contains("nuisance-model",a.Answer);}
    [Fact]public async Task Unsupported_uses_exact_fallback()=>Assert.Equal("This first local version supports questions about the finalized cohort, predictive results, causal results, methodology, ND, WD, Access Gap and study limitations.",(await service.AnswerAsync("Who won the game? ")).Answer);
    [Fact]public async Task Empty_question_is_rejected()=>await Assert.ThrowsAsync<ArgumentException>(()=>service.AnswerAsync(" "));
    private sealed class Data:IFinalizedResearchReadService{public Task<FinalizedDatasetSnapshot> GetAsync(CancellationToken ct=default)=>Task.FromResult(new FinalizedDatasetSnapshot("real-2021-2025-v1","Finalized","Passed",174471,DateTime.UnixEpoch,261,147,66,48));}
    private sealed class Artifacts:IFinalResearchArtifactService{public FinalResearchResults Read()=>new(new(.8221,.1112,.0848,.0195,.1388,.3398,.1971,.9558),new(.00157,-.00377,.00928,.00202,.000003,.00266,-.01382),true);}
}
