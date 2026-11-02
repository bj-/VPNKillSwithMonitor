namespace VKS.Contracts.Models;

public sealed record WfpStatus
{
    public bool Initialized { get; init; }
    public bool LockFiltersInstalled { get; init; }
    public bool AllowPolicyInstalled { get; init; }
    public bool EnforcementVerified { get; init; }
    public string? Error { get; init; }
}
