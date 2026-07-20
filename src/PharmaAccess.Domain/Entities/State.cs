using PharmaAccess.Domain.Common;
using PharmaAccess.Domain.ValueObjects;

namespace PharmaAccess.Domain.Entities;

public sealed class State
{
    private State() { }

    public State(StateCode stateCode, string stateName, bool isEligible, DateTime createdAtUtc, string? region = null, string? division = null, string? exclusionReason = null)
    {
        StateCode = stateCode;
        StateName = DomainGuard.RequiredText(stateName, 128, nameof(stateName));
        Region = DomainGuard.OptionalText(region, 128, nameof(region));
        Division = DomainGuard.OptionalText(division, 128, nameof(division));
        IsEligible = isEligible;
        ExclusionReason = DomainGuard.OptionalText(exclusionReason, 512, nameof(exclusionReason));
        CreatedAtUtc = DomainGuard.Utc(createdAtUtc, nameof(createdAtUtc));
        UpdatedAtUtc = CreatedAtUtc;
    }

    public int StateId { get; private set; }
    public StateCode StateCode { get; private set; }
    public string StateName { get; private set; } = null!;
    public string? Region { get; private set; }
    public string? Division { get; private set; }
    public bool IsEligible { get; private set; }
    public string? ExclusionReason { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }
}
