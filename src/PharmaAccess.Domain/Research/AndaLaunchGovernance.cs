using System.Security.Cryptography;
using System.Text;

namespace PharmaAccess.Domain.Research;

public enum AndaLaunchEligibility { AuthoritativeNdcObserved, AuthoritativeNdcZero, ProductIdentityWithoutNdc, NoProductIdentity }
public enum AndaTemporalPartition { Training, Validation, LockedTest }

public sealed record AndaLaunchKey(string Anda, DateOnly ApprovalDate, int ApprovalPageYear, int SequenceNumber)
{
    public string GenericLaunchId { get; } = Create(Anda, ApprovalDate, ApprovalPageYear, SequenceNumber);

    private static string Create(string anda, DateOnly date, int year, int sequence)
    {
        if (anda.Length != 6 || !anda.All(char.IsDigit)) throw new ArgumentException("ANDA must be exactly six digits.", nameof(anda));
        if (year is < 2021 or > 2025 || date.Year != year) throw new ArgumentException("Approval date and approval-page year must agree.", nameof(date));
        if (sequence <= 0) throw new ArgumentOutOfRangeException(nameof(sequence));
        var canonical = $"ANDA|{anda}|{date:yyyy-MM-dd}|{year}|{sequence}";
        return "ANDA-" + Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical)))[..24];
    }
}

public static class AndaLaunchGovernance
{
    public static AndaLaunchEligibility Classify(bool hasProductIdentity, bool hasAuthoritativeNdcUniverse, bool hasObservedUtilization)
    {
        if (hasObservedUtilization && !hasAuthoritativeNdcUniverse)
            throw new ArgumentException("Observed utilization cannot be assigned without an authoritative NDC universe.");
        if (hasAuthoritativeNdcUniverse && !hasProductIdentity)
            throw new ArgumentException("An authoritative NDC universe requires authoritative product identity.");
        return hasAuthoritativeNdcUniverse
            ? hasObservedUtilization ? AndaLaunchEligibility.AuthoritativeNdcObserved : AndaLaunchEligibility.AuthoritativeNdcZero
            : hasProductIdentity ? AndaLaunchEligibility.ProductIdentityWithoutNdc : AndaLaunchEligibility.NoProductIdentity;
    }

    public static AndaTemporalPartition Partition(DateOnly approvalDate) => approvalDate.Year switch
    {
        2021 or 2022 => AndaTemporalPartition.Training,
        2023 => AndaTemporalPartition.Validation,
        2024 => AndaTemporalPartition.LockedTest,
        _ => throw new ArgumentOutOfRangeException(nameof(approvalDate), "Approval date is outside the frozen predictive cohorts.")
    };

    public static long? AggregatePrescriptions(IEnumerable<(long SourceRowId, long? Value, bool Suppressed)> rows)
    {
        var unique = rows.GroupBy(x => x.SourceRowId).Select(g => g.Single());
        return unique.Any(x => x.Suppressed || x.Value is null) ? null : unique.Sum(x => x.Value!.Value);
    }
}
