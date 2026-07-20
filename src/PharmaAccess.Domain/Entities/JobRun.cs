using PharmaAccess.Domain.Common;

namespace PharmaAccess.Domain.Entities;

public sealed class JobRun
{
    private JobRun() { }

    public JobRun(string jobType, string correlationId, DateTime startedAtUtc, int? datasetVersionId = null, string? metadataJson = null)
    {
        if (datasetVersionId <= 0) throw new ArgumentOutOfRangeException(nameof(datasetVersionId));
        JobType = DomainGuard.RequiredText(jobType, 128, nameof(jobType));
        CorrelationId = DomainGuard.RequiredText(correlationId, 128, nameof(correlationId));
        DatasetVersionId = datasetVersionId;
        StartedAtUtc = DomainGuard.Utc(startedAtUtc, nameof(startedAtUtc));
        MetadataJson = DomainGuard.OptionalText(metadataJson, 8000, nameof(metadataJson));
        Status = JobRunStatus.Pending;
    }

    public int JobRunId { get; private set; }
    public string JobType { get; private set; } = null!;
    public JobRunStatus Status { get; private set; }
    public string CorrelationId { get; private set; } = null!;
    public int? DatasetVersionId { get; private set; }
    public DateTime StartedAtUtc { get; private set; }
    public DateTime? CompletedAtUtc { get; private set; }
    public string? ErrorMessage { get; private set; }
    public string? MetadataJson { get; private set; }

    public void MarkRunning()
    {
        RequireStatus(JobRunStatus.Pending);
        Status = JobRunStatus.Running;
    }

    public void MarkSucceeded(DateTime completedAtUtc) => Complete(JobRunStatus.Succeeded, completedAtUtc, null);
    public void MarkFailed(DateTime completedAtUtc, string errorMessage) => Complete(JobRunStatus.Failed, completedAtUtc, DomainGuard.RequiredText(errorMessage, 4000, nameof(errorMessage)));
    public void Cancel(DateTime completedAtUtc) => Complete(JobRunStatus.Cancelled, completedAtUtc, null);

    private void Complete(JobRunStatus finalStatus, DateTime completedAtUtc, string? errorMessage)
    {
        if (Status is not (JobRunStatus.Pending or JobRunStatus.Running)) throw new InvalidOperationException("Job has already completed.");
        var value = DomainGuard.Utc(completedAtUtc, nameof(completedAtUtc));
        if (value < StartedAtUtc) throw new ArgumentOutOfRangeException(nameof(completedAtUtc));
        Status = finalStatus;
        CompletedAtUtc = value;
        ErrorMessage = errorMessage;
    }

    private void RequireStatus(JobRunStatus expected)
    {
        if (Status != expected) throw new InvalidOperationException($"Transition requires {expected} status.");
    }
}
