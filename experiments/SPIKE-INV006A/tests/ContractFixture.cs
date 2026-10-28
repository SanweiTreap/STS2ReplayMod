namespace SpikeInv006A.Tests;

internal enum AllowedDomain { None, AccountSelector, ActiveProfile, MachineSettings }

internal static class ContractFixture
{
    internal static AllowedDomain Authorize(string? candidate, object? secondArgument = null)
    {
        if (string.IsNullOrEmpty(candidate) || candidate.Length > 512) return AllowedDomain.None;
        if (candidate.Any(char.IsControl) || candidate.Contains(':') || candidate.Contains('%')) return AllowedDomain.None;
        if (candidate.StartsWith('/') || candidate.StartsWith('\\')) return AllowedDomain.None;
        if (candidate.StartsWith("\\\\", StringComparison.Ordinal) || candidate.StartsWith("//", StringComparison.Ordinal)) return AllowedDomain.None;
        var normalized = candidate.Replace('\\', '/');
        if (normalized.StartsWith('/') || normalized.EndsWith('/') || normalized.Contains("//", StringComparison.Ordinal)) return AllowedDomain.None;
        var segments = normalized.Split('/');
        if (segments.Any(segment => segment is "" or "." or "..")) return AllowedDomain.None;
        if (string.Equals(normalized, "modded/profile.save", StringComparison.Ordinal)) return AllowedDomain.AccountSelector;
        if (string.Equals(normalized, "settings.save", StringComparison.Ordinal)) return AllowedDomain.MachineSettings;
        if (string.Equals(normalized, "modded/profile1", StringComparison.Ordinal)
            || normalized.StartsWith("modded/profile1/", StringComparison.Ordinal)) return AllowedDomain.ActiveProfile;
        return AllowedDomain.None;
    }
}

internal sealed class PoisonPayload
{
    internal int Touches;
    public int Length { get { Touches++; throw new InvalidOperationException(); } }
    public override string ToString() { Touches++; throw new InvalidOperationException(); }
    public override int GetHashCode() { Touches++; throw new InvalidOperationException(); }
}

internal sealed class ArgumentSpy
{
    internal int Reads;
    internal object Read() { Reads++; return new object(); }
}
