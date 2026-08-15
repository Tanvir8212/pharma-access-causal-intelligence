using System.Text.Json.Serialization;

namespace PharmaAccess.Application.MachineLearning;

public static class HistoricalReplayMode { public const string Name="ResearchDevelopmentHistoricalReplay"; public const double Threshold=.08; public static bool MeetsThreshold(float probability)=>probability>=Threshold; }
public sealed record HistoricalReplayLaunch(int GenericLaunchId,string DisplayIdentifier,string? GenericLaunchCode=null,string? Anda=null,DateOnly? ApprovalDate=null,int? ApprovalYear=null,int? SequenceNumber=null)
{
 public IReadOnlyList<string> DrugNames { get; init; }=[];
 public IReadOnlyList<string> ActiveIngredients { get; init; }=[];
 public IReadOnlyList<string> Forms { get; init; }=[];
 public IReadOnlyList<string> Strengths { get; init; }=[];
 public int ProductCount { get; init; }
 public string PrimaryProductName=>DrugNames.FirstOrDefault()??(!string.IsNullOrWhiteSpace(Anda)?$"ANDA {Anda}":DisplayIdentifier);
}
public interface IResearchLaunchCatalogService { Task<IReadOnlyList<HistoricalReplayLaunch>> GetLaunchesAsync(CancellationToken cancellationToken=default); }
public static class FdaApplicationNumber
{
 public static bool TryNormalize(string? value,out long normalized){normalized=0;var text=value?.Trim();return !string.IsNullOrWhiteSpace(text)&&text.All(char.IsAsciiDigit)&&long.TryParse(text,System.Globalization.NumberStyles.None,System.Globalization.CultureInfo.InvariantCulture,out normalized);}
}
public static class ResearchLaunchSearch
{
 public static IReadOnlyList<HistoricalReplayLaunch> Find(IEnumerable<HistoricalReplayLaunch> source,string? query,int limit=30,int? approvalYear=null,string? form=null){var q=query?.Trim();if(string.IsNullOrWhiteSpace(q))return[];return Filter(source,approvalYear,form).Where(x=>x.GenericLaunchId.ToString().Equals(q,StringComparison.OrdinalIgnoreCase)||(x.Anda?.Contains(q,StringComparison.OrdinalIgnoreCase)??false)||(x.GenericLaunchCode?.Contains(q,StringComparison.OrdinalIgnoreCase)??false)||(x.ApprovalDate?.ToString("yyyy-MM-dd").Contains(q,StringComparison.OrdinalIgnoreCase)??false)||(x.ApprovalYear?.ToString().Equals(q,StringComparison.OrdinalIgnoreCase)??false)||x.DrugNames.Any(v=>v.Contains(q,StringComparison.OrdinalIgnoreCase))||x.ActiveIngredients.Any(v=>v.Contains(q,StringComparison.OrdinalIgnoreCase))).OrderByDescending(x=>x.GenericLaunchId.ToString().Equals(q,StringComparison.OrdinalIgnoreCase)).ThenByDescending(x=>x.Anda?.Equals(q,StringComparison.OrdinalIgnoreCase)??false).ThenByDescending(x=>x.ApprovalDate).ThenBy(x=>x.GenericLaunchId).Take(limit).ToArray();}
 public static IReadOnlyList<HistoricalReplayLaunch> Filter(IEnumerable<HistoricalReplayLaunch> source,int? approvalYear=null,string? form=null)=>source.Where(x=>(approvalYear is null||x.ApprovalYear==approvalYear)&&(string.IsNullOrWhiteSpace(form)||x.Forms.Any(v=>v.Equals(form,StringComparison.OrdinalIgnoreCase)))).OrderByDescending(x=>x.ApprovalDate).ThenBy(x=>x.GenericLaunchId).ToArray();
 public static IReadOnlyList<HistoricalReplayLaunch> Recent(IEnumerable<HistoricalReplayLaunch> source,int count=8)=>source.OrderByDescending(x=>x.ApprovalDate).ThenBy(x=>x.GenericLaunchId).Take(count).ToArray();
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
public static class ExplorerPresentation
{
 public static string Quarter(int value)=>$"{value/10} Q{value%10}";
 public static string Percent(double value)=>(value*100).ToString("0.00",System.Globalization.CultureInfo.InvariantCulture)+"%";
 public static IReadOnlyList<HistoricalReplayStateResult> CanonicalTop(IEnumerable<HistoricalReplayStateResult> states,int count=5)=>states.OrderBy(x=>x.Rank).Take(count).ToArray();
 public static string Summary(IReadOnlyList<HistoricalReplayStateResult> states,double threshold){if(states.Count==0)return "No eligible candidate states were available for this scenario.";var top=states.OrderBy(x=>x.Rank).First();var meeting=states.Count(x=>x.AboveSelectedThreshold);return $"{top.StateName} has the highest model-estimated next-entry probability for this scenario at {Percent(top.Probability)}. "+(meeting==0?$"None of the {states.Count} eligible states meet the selected {Percent(threshold)} research threshold.":$"{meeting} of {states.Count} eligible states meet the selected {Percent(threshold)} research threshold.");}
}
public sealed record DynamicAsOfRequest(int GenericLaunchId,int AsOfQuarter);
public sealed record DynamicAsOfResponse(string Mode,int GenericLaunchId,string LaunchDisplayIdentifier,int AsOfQuarter,int TargetQuarter,int CandidateJurisdictionCount,IReadOnlyList<HistoricalReplayStateResult> RankedStates,string ModelVersion,string ModelArtifactSha256,string DatasetVersion,string FeatureSetVersion,double SelectedThreshold,[property:JsonConverter(typeof(JsonStringEnumConverter))] ModelApprovalStatus GovernanceStatus,bool ProductionApproved,bool Champion,IReadOnlyList<string> Warnings);
public interface IDynamicAsOfPredictionService
{
 Task<IReadOnlyList<HistoricalReplayLaunch>> GetLaunchesAsync(CancellationToken cancellationToken=default);
 Task<IReadOnlyList<int>> GetQuartersAsync(int genericLaunchId,CancellationToken cancellationToken=default);
 Task<DynamicAsOfResponse> ScoreAsync(DynamicAsOfRequest request,CancellationToken cancellationToken=default);
}
public interface IDynamicAsOfExplanationService { Task<HistoricalReplayExplanationResponse> ExplainAsync(DynamicAsOfRequest request,CancellationToken cancellationToken=default); }
