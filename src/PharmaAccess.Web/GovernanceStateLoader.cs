using PharmaAccess.Application.MachineLearning;

namespace PharmaAccess.Web;

public sealed record GovernanceLoadResult(bool IsAvailable, ModelGovernanceState State, string DriftSeverity, string? Message)
{
    public const string UnavailableMessage = "Governance status temporarily unavailable";
}

public sealed class GovernanceStateLoader(
    IDriftReportStore reports,
    IHumanGovernedModelManager manager,
    IConfiguration configuration,
    ILogger<GovernanceStateLoader> logger)
{
    private static readonly EventId LoadFailedEvent = new(2101, "GovernanceLoadFailed");
    private static readonly ModelGovernanceState UnavailableState =
        new("Unavailable", null, "Unavailable", false, [], [], []);

    public async Task<GovernanceLoadResult> LoadAsync(CancellationToken cancellationToken = default)
    {
        var configuredMilliseconds = configuration.GetValue<int?>("ModelGovernance:LoadTimeoutMilliseconds") ?? 3000;
        var timeoutMilliseconds = Math.Clamp(configuredMilliseconds, 1, 3000);
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromMilliseconds(timeoutMilliseconds));

        try
        {
            var reportsTask = reports.ListAsync(timeout.Token);
            var stateTask = manager.GetStateAsync(timeout.Token);
            await Task.WhenAll(reportsTask, stateTask).WaitAsync(timeout.Token);
            var driftSeverity = reportsTask.Result.FirstOrDefault()?.Severity.ToString() ?? "Not evaluated";
            return new GovernanceLoadResult(true, stateTask.Result, driftSeverity, null);
        }
        catch (OperationCanceledException) when (timeout.IsCancellationRequested)
        {
            logger.LogWarning(LoadFailedEvent,
                "Governance state loading did not complete. FailureReason: {FailureReason}", "Timeout");
        }
        catch (Exception)
        {
            logger.LogWarning(LoadFailedEvent,
                "Governance state loading did not complete. FailureReason: {FailureReason}", "Unavailable");
        }

        return new GovernanceLoadResult(false, UnavailableState, "Unavailable", GovernanceLoadResult.UnavailableMessage);
    }
}
