using PharmaAccess.Application.MachineLearning;
namespace PharmaAccess.Llm;
public sealed class DynamicAsOfExplanationService(IDynamicAsOfPredictionService scoring,ExplorerBusinessAnalysisComposer composer):IDynamicAsOfExplanationService
{
 public async Task<HistoricalReplayExplanationResponse> ExplainAsync(DynamicAsOfRequest request,CancellationToken token=default)=>await composer.DynamicAsync(await scoring.ScoreAsync(request,token),token);
}
