using PharmaAccess.Application.MachineLearning;
namespace PharmaAccess.Llm;
public sealed class HistoricalReplayExplanationService(IHistoricalPredictionReplayService replay,ExplorerBusinessAnalysisComposer composer):IHistoricalReplayExplanationService
{
 public async Task<HistoricalReplayExplanationResponse> ExplainAsync(HistoricalReplayExplanationRequest request,CancellationToken token=default)=>await composer.HistoricalAsync(await replay.ReplayAsync(new(request.GenericLaunchId,request.AsOfQuarter),token),token);
}
