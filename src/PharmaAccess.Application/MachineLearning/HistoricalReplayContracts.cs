using System.Text.Json.Serialization;

namespace PharmaAccess.Application.MachineLearning;

public static class HistoricalReplayMode { public const string Name="ResearchDevelopmentHistoricalReplay"; public const double Threshold=.08; public static bool MeetsThreshold(float probability)=>probability>=Threshold; }
public sealed record HistoricalReplayLaunch(int GenericLaunchId,string DisplayIdentifier,string? GenericLaunchCode=null,string? Anda=null,DateOnly? ApprovalDate=null,int? ApprovalYear=null,int? SequenceNumber=null);
public interface IResearchLaunchCatalogService { Task<IReadOnlyList<HistoricalReplayLaunch>> GetLaunchesAsync(CancellationToken cancellationToken=default); }
public static class ResearchLaunchSearch
{
 public static IReadOnlyList<HistoricalReplayLaunch> Find(IEnumerable<HistoricalReplayLaunch> source,string? query,int limit=30){var q=query?.Trim();if(string.IsNullOrWhiteSpace(q))return[];return source.Where(x=>x.GenericLaunchId.ToString().Equals(q,StringComparison.OrdinalIgnoreCase)||(x.Anda?.Contains(q,StringComparison.OrdinalIgnoreCase)??false)||(x.GenericLaunchCode?.Contains(q,StringComparison.OrdinalIgnoreCase)??false)||(x.ApprovalDate?.ToString("yyyy-MM-dd").Contains(q,StringComparison.OrdinalIgnoreCase)??false)||(x.ApprovalYear?.ToString().Equals(q,StringComparison.OrdinalIgnoreCase)??false)).OrderBy(x=>x.ApprovalDate).ThenBy(x=>x.GenericLaunchId).Take(limit).ToArray();}
}
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
public static class DynamicAsOfMode { public const string Name="ResearchDevelopmentDynamicAsOf"; }
public sealed record DynamicAsOfRequest(int GenericLaunchId,int AsOfQuarter);
public sealed record DynamicAsOfResponse(string Mode,int GenericLaunchId,string LaunchDisplayIdentifier,int AsOfQuarter,int TargetQuarter,int CandidateJurisdictionCount,IReadOnlyList<HistoricalReplayStateResult> RankedStates,string ModelVersion,string ModelArtifactSha256,string DatasetVersion,string FeatureSetVersion,double SelectedThreshold,[property:JsonConverter(typeof(JsonStringEnumConverter))] ModelApprovalStatus GovernanceStatus,bool ProductionApproved,bool Champion,IReadOnlyList<string> Warnings);
public interface IDynamicAsOfPredictionService
{
 Task<IReadOnlyList<HistoricalReplayLaunch>> GetLaunchesAsync(CancellationToken cancellationToken=default);
 Task<IReadOnlyList<int>> GetQuartersAsync(int genericLaunchId,CancellationToken cancellationToken=default);
 Task<DynamicAsOfResponse> ScoreAsync(DynamicAsOfRequest request,CancellationToken cancellationToken=default);
}
public interface IDynamicAsOfExplanationService { Task<HistoricalReplayExplanationResponse> ExplainAsync(DynamicAsOfRequest request,CancellationToken cancellationToken=default); }
