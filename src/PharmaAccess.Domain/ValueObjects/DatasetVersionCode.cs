using System.Text.RegularExpressions;
using PharmaAccess.Domain.Common;

namespace PharmaAccess.Domain.ValueObjects;

public readonly partial record struct DatasetVersionCode
{
    public DatasetVersionCode(string value)
    {
        var normalized = DomainGuard.RequiredText(value, 64, nameof(value));
        if (!AllowedCharacters().IsMatch(normalized))
        {
            throw new ArgumentException("Version code may contain only letters, digits, '.', '_', and '-'.", nameof(value));
        }

        Value = normalized;
    }

    public string Value { get; }
    public override string ToString() => Value;
    public static implicit operator string(DatasetVersionCode value) => value.Value;

    [GeneratedRegex("^[A-Za-z0-9][A-Za-z0-9._-]*$", RegexOptions.CultureInvariant)]
    private static partial Regex AllowedCharacters();
}
