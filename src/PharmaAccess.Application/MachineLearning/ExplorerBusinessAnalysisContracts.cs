namespace PharmaAccess.Application.MachineLearning;

public sealed record ExplorerBusinessProduct(int GenericLaunchId,string DrugName,string ActiveIngredient,string? Anda,DateOnly? ApprovalDate,IReadOnlyList<string> DosageForms);
public sealed record ExplorerBusinessAnalysisIdentity(string Mode,int AsOfQuarter,int TargetQuarter,string DatasetVersion,string ModelVersion,double SelectedThreshold);
public sealed record ExplorerBusinessPredictionSummary(int CandidateStateCount,string HighestRankedState,float HighestProbability,int StatesMeetingThreshold);
public sealed record ExplorerBusinessOutcome(int ActualObservedEntryCount,IReadOnlyList<HistoricalObservedState> ActualObservedStates,bool TopPredictedStateActuallyEntered,int? BestActualObservedRank,bool ThresholdDecisionCorrect,string Summary);
public sealed record ExplorerBusinessStateResult(int StateId,string StateCode,string StateName,int EntryRank,float EntryProbability,bool AboveSelectedThreshold,long? RecentMarketVolume,long? PriorWindowVolume,long? RecentWindowVolume,decimal? MarketSizeScore,decimal? StateMarketGrowthRate,decimal? GrowthMomentumScore,string? MarketProfile,string? MarketDataSupport,decimal? HistoricalParticipationRate,int? HistoricalLaunchCount,int? HistoricalParticipatingLaunchCount,decimal? TypicalObservedEntryDelayQuarters,int? EntryDelayObservationCount,decimal? NearbyAdoptionShare,int? EligibleNeighborCount);
public sealed record ExplorerBusinessAnalysisGrounding(ExplorerBusinessProduct Product,ExplorerBusinessAnalysisIdentity Analysis,ExplorerBusinessPredictionSummary PredictionSummary,ExplorerBusinessOutcome? HistoricalOutcome,IReadOnlyList<ExplorerBusinessStateResult> StateResults);

public sealed record ExplorerSignalSnapshot(string EntryOutlook,string MedicaidMarket,string HistoricalStateBehavior,string NearbyAdoption);
public sealed record ExplorerBusinessFinding(string Headline,string Explanation,string? WhyThisMatters=null);
public sealed record ExplorerStateSpotlight(string StateCode,string StateName,string Descriptor,IReadOnlyList<string> Metrics,string Narrative);
public sealed record ExplorerBusinessAnalysisResponse(IReadOnlyList<string> WhatYouShouldKnow,ExplorerSignalSnapshot SignalSnapshot,IReadOnlyList<ExplorerBusinessFinding> WhatStandsOut,IReadOnlyList<ExplorerStateSpotlight> StatesWorthUnderstanding,string ModelOutcomeAssessment,IReadOnlyList<string> HowToUse,IReadOnlyList<string> Limitations,IReadOnlyList<string> SuggestedQuestions);

public sealed record ExplorerAnalysisChatRequest(Guid SessionId,string Question);
public sealed record ExplorerAnalysisChatResponse(Guid SessionId,string Answer,DateTime GeneratedAtUtc);
public interface IExplorerAnalysisChatService { Task<ExplorerAnalysisChatResponse> AskAsync(ExplorerAnalysisChatRequest request,CancellationToken cancellationToken=default); }
