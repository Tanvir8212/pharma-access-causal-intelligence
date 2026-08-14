using System.Text.Json.Serialization;

namespace PharmaAccess.Application.MachineLearning;

public static class HistoricalReplayMode { public const string Name="ResearchDevelopmentHistoricalReplay"; public const double Threshold=.08; public static bool MeetsThreshold(float probability)=>probability>=Threshold; }
public sealed record HistoricalReplayLaunch(int GenericLaunchId,string DisplayIdentifier);
public sealed record HistoricalReplayRequest(int GenericLaunchId,int AsOfQuarter);
public sealed record HistoricalReplayStateResult(int StateId,string StateCode,string StateName,float Probability,bool AboveSelectedThreshold,int Rank);
public sealed record HistoricalReplayResponse(string Mode,int GenericLaunchId,string LaunchDisplayIdentifier,int AsOfQuarter,int TargetQuarter,string ModelVersion,string ModelArtifactSha256,string DatasetVersion,int DatasetVersionId,string FeatureSetVersion,int FeatureSetVersionId,double SelectedThreshold,int TotalEligibleJurisdictions,IReadOnlyList<HistoricalReplayStateResult> RankedStates,IReadOnlyList<string> Warnings,[property:JsonConverter(typeof(JsonStringEnumConverter))] ModelApprovalStatus GovernanceStatus,bool ProductionApproved,bool Champion);
public interface IHistoricalPredictionReplayService
{
    Task<IReadOnlyList<HistoricalReplayLaunch>> GetLaunchesAsync(CancellationToken cancellationToken=default);
    Task<IReadOnlyList<int>> GetQuartersAsync(int genericLaunchId,CancellationToken cancellationToken=default);
    Task<HistoricalReplayResponse> ReplayAsync(HistoricalReplayRequest request,CancellationToken cancellationToken=default);
}
public sealed record HistoricalReplayExplanationRequest(int GenericLaunchId,int AsOfQuarter);
public sealed record HistoricalReplayExplanationResponse(string Explanation,string Provider,string ModelName,DateTime GeneratedAtUtc,string GroundingMode);
public interface IHistoricalReplayExplanationService { Task<HistoricalReplayExplanationResponse> ExplainAsync(HistoricalReplayExplanationRequest request,CancellationToken cancellationToken=default); }
