using System.Collections.Concurrent;
using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using PharmaAccess.Application.MachineLearning;

namespace PharmaAccess.Llm;

internal sealed record ExplorerAnalysisSession(ExplorerBusinessAnalysisGrounding Grounding,ExplorerBusinessAnalysisResponse Report,List<(string Role,string Text)> Messages);
public sealed class ExplorerAnalysisSessionStore
{
    private readonly ConcurrentDictionary<Guid,ExplorerAnalysisSession> sessions=new();
    public Guid Add(ExplorerBusinessAnalysisGrounding grounding,ExplorerBusinessAnalysisResponse report){var id=Guid.NewGuid();sessions[id]=new(grounding,report,[]);return id;}
    internal bool TryGet(Guid id,out ExplorerAnalysisSession session)=>sessions.TryGetValue(id,out session!);
}

public sealed class ExplorerBusinessAnalysisComposer(IResearchLaunchCatalogService catalog,IStateMarketContextService market,IStateReadinessContextService readiness,ILanguageModelClient model,ExplorerAnalysisSessionStore sessions)
{
    public const string SystemInstruction="""
You are the interpretation layer for a pharmaceutical Medicaid research dashboard.
Use ONLY the structured analytical grounding supplied by the application. All numeric results are calculated and verified by the application. Do not recalculate, replace, adjust or invent them.
Help a non-technical business user understand what the results mean. Always keep separate: (1) Entry Outlook, the selected-drug next-quarter model prediction; (2) Medicaid Market Context, overall observed Medicaid utilization size and growth; and (3) State Readiness Context, descriptive historical behavior across prior generic launches and selected-launch neighbor adoption.
Never interpret historical participation as future probability. Never interpret Market Size Score as product-specific demand. Never interpret Growth Momentum as absolute market size. Never imply Nearby Adoption causes entry. Never create an overall opportunity or readiness score. Never recommend commercial targeting. Never estimate revenue or market share.
For Historical Replay, clearly distinguish what was predicted at the historical point from what was later observed. Do not hide prediction misses. Use clear business language, explain why supported patterns are interesting, say when signals are mixed, and preserve unavailable information as unavailable. If a question cannot be answered from the grounding, say so clearly.
""";

    public async Task<HistoricalReplayExplanationResponse> HistoricalAsync(HistoricalReplayResponse result,CancellationToken token)
    {
        var grounding=await Grounding(result.Mode,result.GenericLaunchId,result.LaunchDisplayIdentifier,result.AsOfQuarter,result.TargetQuarter,result.DatasetVersion,result.ModelVersion,result.SelectedThreshold,result.RankedStates,
            new(result.ActualObservedEntryCount,result.ActualObservedStates,result.TopPredictedStateActuallyEntered,result.BestActualObservedRank,result.ThresholdDecisionCorrect,result.HistoricalOutcomeSummary),token);
        return await Create(grounding,token);
    }

    public async Task<HistoricalReplayExplanationResponse> DynamicAsync(DynamicAsOfResponse result,CancellationToken token)
    {
        var grounding=await Grounding(result.Mode,result.GenericLaunchId,result.LaunchDisplayIdentifier,result.AsOfQuarter,result.TargetQuarter,result.DatasetVersion,result.ModelVersion,result.SelectedThreshold,result.RankedStates,null,token);
        return await Create(grounding,token);
    }

    private async Task<ExplorerBusinessAnalysisGrounding> Grounding(string mode,int launchId,string launchDisplayIdentifier,int asOf,int target,string dataset,string modelVersion,double threshold,IReadOnlyList<HistoricalReplayStateResult> ranked,ExplorerBusinessOutcome? outcome,CancellationToken token)
    {
        HistoricalReplayLaunch? product=null;try{product=(await catalog.GetLaunchesAsync(token)).SingleOrDefault(x=>x.GenericLaunchId==launchId);}catch(Exception)when(!token.IsCancellationRequested){}
        StateMarketContextResponse? m=null;StateReadinessContextResponse? r=null;
        try{m=await market.GetAsync(new(launchId,asOf),token);}catch(Exception)when(!token.IsCancellationRequested){}
        try{r=await readiness.GetAsync(new(launchId,asOf),token);}catch(Exception)when(!token.IsCancellationRequested){}
        var states=ranked.OrderBy(x=>x.Rank).Select(x=>{var mc=m?.States.SingleOrDefault(y=>y.StateId==x.StateId);var rc=r?.States.SingleOrDefault(y=>y.StateId==x.StateId);return new ExplorerBusinessStateResult(x.StateId,x.StateCode,x.StateName,x.Rank,x.Probability,x.AboveSelectedThreshold,mc?.RecentMarketVolume,mc?.PriorWindowVolume,mc?.RecentWindowVolume,mc?.MarketSizeScore,mc?.StateMarketGrowthRate,mc?.GrowthMomentumScore,mc?.MarketProfile,mc?.DataSupport.ToString(),rc?.HistoricalParticipationRate,rc?.HistoricalLaunchCount,rc?.HistoricalParticipatingLaunchCount,rc?.TypicalObservedEntryDelayQuarters,rc?.EntryDelayObservationCount,rc?.NearbyAdoptionShare,rc?.EligibleNeighborCount);}).ToArray();
        var top=states.First();
        return new(new(launchId,product?.PrimaryProductName??launchDisplayIdentifier,product is null?"":string.Join("; ",product.ActiveIngredients),product?.Anda,product?.ApprovalDate,product?.Forms??[]),new(mode,asOf,target,dataset,modelVersion,threshold),new(states.Length,top.StateName,top.EntryProbability,states.Count(x=>x.AboveSelectedThreshold)),outcome,states);
    }

    private async Task<HistoricalReplayExplanationResponse> Create(ExplorerBusinessAnalysisGrounding grounding,CancellationToken token)
    {
        var report=Fallback(grounding);string? generated=null;
        if(model.IsAvailable)try{generated=await model.GenerateAsync(BuildPrompt(grounding),token);var parsed=Parse(generated,grounding);if(parsed is not null)report=parsed;}catch(Exception)when(!token.IsCancellationRequested){}
        var id=sessions.Add(grounding,report);
        return new(report.WhatYouShouldKnow.FirstOrDefault()??"Verified analysis is available.",model.Provider,model.Model,DateTime.UtcNow,"CompleteStructuredExplorerAnalysis"){SessionId=id,Analysis=report};
    }

    internal static string BuildPrompt(ExplorerBusinessAnalysisGrounding grounding)=>SystemInstruction+"\nReturn JSON only matching this structure: {\"whatYouShouldKnow\":[3-5 strings],\"signalSnapshot\":{\"entryOutlook\":\"string\",\"medicaidMarket\":\"string\",\"historicalStateBehavior\":\"string\",\"nearbyAdoption\":\"string\"},\"whatStandsOut\":[{\"headline\":\"string\",\"explanation\":\"string\",\"whyThisMatters\":\"string or null\"}],\"statesWorthUnderstanding\":[{\"stateCode\":\"code from grounding\",\"stateName\":\"name from grounding\",\"descriptor\":\"string\",\"metrics\":[\"strings\"],\"narrative\":\"string\"}],\"modelOutcomeAssessment\":\"string\",\"howToUse\":[\"strings\"],\"limitations\":[\"strings\"],\"suggestedQuestions\":[5-7 strings]}. Keep qualitative prose concise; do not introduce numbers not present in grounding. Grounding JSON: "+JsonSerializer.Serialize(grounding);

    private static ExplorerBusinessAnalysisResponse? Parse(string? json,ExplorerBusinessAnalysisGrounding grounding)
    {
        if(string.IsNullOrWhiteSpace(json))return null;var text=json.Trim();if(text.StartsWith("```")){text=Regex.Replace(text,"^```(?:json)?\\s*|\\s*```$","",RegexOptions.IgnoreCase);}
        try{var value=JsonSerializer.Deserialize<ExplorerBusinessAnalysisResponse>(text,new JsonSerializerOptions{PropertyNameCaseInsensitive=true});if(value is null||value.WhatYouShouldKnow.Count is <3 or >5||value.WhatStandsOut.Count is <4 or >6||value.StatesWorthUnderstanding.Count is <3 or >5||value.SuggestedQuestions.Count is <5 or >7)return null;var states=grounding.StateResults.ToDictionary(x=>x.StateCode,StringComparer.OrdinalIgnoreCase);if(value.StatesWorthUnderstanding.Any(x=>!states.TryGetValue(x.StateCode,out var state)||!state.StateName.Equals(x.StateName,StringComparison.OrdinalIgnoreCase)))return null;return value;}catch(JsonException){return null;}
    }

    internal static ExplorerBusinessAnalysisResponse Fallback(ExplorerBusinessAnalysisGrounding g)
    {
        var states=g.StateResults.OrderBy(x=>x.EntryRank).ToArray();var top=states[0];var largest=states.Where(x=>x.MarketSizeScore.HasValue).OrderByDescending(x=>x.MarketSizeScore).FirstOrDefault();var growth=states.Where(x=>x.GrowthMomentumScore.HasValue).OrderByDescending(x=>x.GrowthMomentumScore).FirstOrDefault();var participation=top.HistoricalParticipationRate.HasValue?$"{top.HistoricalParticipationRate:0%} ({top.HistoricalParticipatingLaunchCount} of {top.HistoricalLaunchCount} prior observable launches)":"Unavailable";var delay=top.TypicalObservedEntryDelayQuarters.HasValue?$"{top.TypicalObservedEntryDelayQuarters:0.#} quarters across {top.EntryDelayObservationCount} entries":"Unavailable";var nearby=top.NearbyAdoptionShare.HasValue?$"{top.NearbyAdoptionShare:0%} across {top.EligibleNeighborCount} eligible neighbors":"Unavailable";
        var know=new List<string>{$"{top.StateName} is the highest-ranked jurisdiction for next-quarter entry at {top.EntryProbability:P2}; the selected research threshold is {g.Analysis.SelectedThreshold:P0}.",largest is null?"Overall Medicaid market context is unavailable.":$"{largest.StateName} has the largest recent Medicaid market among current candidates.",$"Historically, {top.StateName} participated in {participation}, with a typical observed entry delay of {delay}."};
        string outcome;if(g.HistoricalOutcome is null)outcome="Outcome is not shown because Dynamic As-Of reconstructs only what was known at the selected historical point.";else{var o=g.HistoricalOutcome;outcome=$"At the historical point, {top.StateName} ranked #1. In the following quarter, {o.ActualObservedEntryCount} states entered; rank #1 {(o.TopPredictedStateActuallyEntered?"did":"did not")} enter. The best-ranked actual entry was {(o.BestActualObservedRank.HasValue?$"rank #{o.BestActualObservedRank}":"unavailable")}.";know.Add(outcome);}
        know.Add($"Bottom line: contextual strength and predictive confidence answer different questions; here the top prediction is {(top.AboveSelectedThreshold?"above":"below")} threshold.");
        var findings=new List<ExplorerBusinessFinding>{new("Highest rank does not necessarily mean strong confidence",$"{top.StateName} leads the relative ranking, but its {top.EntryProbability:P2} estimate must be read against the {g.Analysis.SelectedThreshold:P0} threshold."),new("Market size is a separate lens",largest is null?"Market context is unavailable.":$"{largest.StateName} has the strongest relative size context; this does not measure selected-drug demand."),new("Growth and size can diverge",growth is null?"Growth history is unavailable.":$"{growth.StateName} has the strongest relative growth momentum, which does not mean it is the largest market."),new("Historical behavior is descriptive",$"{top.StateName}'s historical participation is {participation}; it is not a future-entry probability."),new("Nearby adoption is contextual",$"For {top.StateName}, nearby adoption is {nearby}; it does not establish cause.")};
        var availableCodes=states.Select(x=>x.StateCode).ToHashSet(StringComparer.OrdinalIgnoreCase);var spotlightCodes=new[]{top.StateCode,largest?.StateCode,growth?.StateCode,g.HistoricalOutcome?.ActualObservedStates.OrderBy(x=>x.Rank).FirstOrDefault()?.StateCode}.Where(x=>x is not null&&availableCodes.Contains(x)).Distinct(StringComparer.OrdinalIgnoreCase).Take(4).ToArray();var spotlights=spotlightCodes.Select(code=>{var x=states.Single(s=>s.StateCode.Equals(code,StringComparison.OrdinalIgnoreCase));return new ExplorerStateSpotlight(x.StateCode,x.StateName,x.StateCode==top.StateCode?"Highest-ranked, mixed signal":x.StateCode==largest?.StateCode?"Size story":x.StateCode==growth?.StateCode?"Growth story":"Outcome comparison",[$"Entry rank #{x.EntryRank}",$"Entry probability {x.EntryProbability:P2}",x.MarketSizeScore.HasValue?$"Market Size {x.MarketSizeScore:0.##} / 100":"Market size unavailable"],"This state illustrates how the three analytical lenses can point in different directions.");}).ToArray();
        return new(know,new($"{top.StateName}: rank #{top.EntryRank}, {top.EntryProbability:P2}; {(top.AboveSelectedThreshold?"meets":"below")} threshold",largest is null?"Unavailable":$"{largest.StateName}: size {largest.MarketSizeScore:0.##} / 100",$"{top.StateName}: participation {participation}; delay {delay}",$"{top.StateName}: {nearby}"),findings,spotlights,outcome,["ENTRY OUTLOOK asks where the model estimates this selected generic may first appear next quarter.","MEDICAID MARKET CONTEXT describes overall observed Medicaid utilization size and change.","STATE READINESS CONTEXT describes historical participation across prior generic launches and selected-launch nearby adoption.","A state can be strong through one lens and weak through another because the lenses answer different questions."],["Medicaid utilization only; not the total commercial market, product-specific demand, revenue opportunity, or market share.","The probability is a research-model estimate and does not establish cause.","State Readiness is descriptive historical context."],[ $"Why is {top.StateName} ranked #1 if its probability is {top.EntryProbability:P2}?",$"What does {top.StateName}'s historical participation mean?",largest is null?"Explain Market Size versus Growth Momentum.":$"Compare {top.StateName} and {largest.StateName}.","Which states combine larger Medicaid markets with shorter observed entry delays?",g.HistoricalOutcome is null?"Why is future outcome not shown in Dynamic As-Of?":"Which actual entry states did the model rank highest?","Explain Market Size versus Growth Momentum."]);
    }
}

public sealed class ExplorerAnalysisChatService(ExplorerAnalysisSessionStore sessions,ILanguageModelClient model,TimeProvider clock):IExplorerAnalysisChatService
{
    public async Task<ExplorerAnalysisChatResponse> AskAsync(ExplorerAnalysisChatRequest request,CancellationToken token=default)
    {
        if(request.SessionId==Guid.Empty||string.IsNullOrWhiteSpace(request.Question))throw new ArgumentException("A valid analysis session and question are required.");if(!sessions.TryGet(request.SessionId,out var session))throw new KeyNotFoundException("The analysis session is unavailable or expired.");var q=request.Question.Trim();var answer=Safety(q,session.Grounding);
        if(answer is null&&model.IsAvailable)try{var prompt=ExplorerBusinessAnalysisComposer.SystemInstruction+"\nInitial report JSON: "+JsonSerializer.Serialize(session.Report)+"\nImmutable grounding JSON: "+JsonSerializer.Serialize(session.Grounding)+"\nConversation JSON: "+JsonSerializer.Serialize(session.Messages)+"\nUser question: "+q+"\nAnswer concisely using only this analysis.";answer=await model.GenerateAsync(prompt,token);}catch(Exception)when(!token.IsCancellationRequested){}
        answer=string.IsNullOrWhiteSpace(answer)?"AI analysis is temporarily unavailable. Verified dashboard results remain available.":answer.Trim();lock(session.Messages){session.Messages.Add(("user",q));session.Messages.Add(("assistant",answer));}return new(request.SessionId,answer,clock.GetUtcNow().UtcDateTime);
    }
    private static string? Safety(string q,ExplorerBusinessAnalysisGrounding g){if(q.Contains("revenue",StringComparison.OrdinalIgnoreCase))return "This analysis cannot estimate revenue. It contains Medicaid utilization context, not pricing, commercial demand, market share, or revenue assumptions.";if(q.Contains("target",StringComparison.OrdinalIgnoreCase)||q.Contains("recommend",StringComparison.OrdinalIgnoreCase))return "This research analysis does not support a commercial targeting recommendation. It should be interpreted through its separate prediction, Medicaid context, and descriptive historical lenses.";if(q.Contains("chance",StringComparison.OrdinalIgnoreCase)||q.Contains("probability",StringComparison.OrdinalIgnoreCase)&&q.Contains("participation",StringComparison.OrdinalIgnoreCase))return "Historical participation is not a future-entry probability. It is the share of prior observable generic launches with Medicaid utilization; the selected drug's entry probability is a separate model estimate.";if(q.Contains("demand",StringComparison.OrdinalIgnoreCase))return "Market Size Score measures relative overall observed Medicaid utilization, not selected-drug demand.";if(q.Contains("biggest",StringComparison.OrdinalIgnoreCase)&&q.Contains("growth",StringComparison.OrdinalIgnoreCase))return "Growth Momentum describes relative recent change, not absolute market size. A state can have the strongest growth momentum without being the largest market.";return null;}
}
