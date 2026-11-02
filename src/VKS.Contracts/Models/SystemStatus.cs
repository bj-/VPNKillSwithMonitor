using VKS.Contracts.Enums;

namespace VKS.Contracts.Models;

public sealed record SystemStatus
{
    public VpnState Vpn { get; init; } = VpnState.Unknown;
    public NetworkProtectionState Network { get; init; } = NetworkProtectionState.Unknown;
    public ProcessGuardState ProcessGuard { get; init; } = ProcessGuardState.Unknown;
    public LockReason? LockReason { get; init; }
    public IReadOnlyList<ProcessInfo> BlockingProcesses { get; init; } = [];
    public DateTimeOffset LastUpdated { get; init; } = DateTimeOffset.UtcNow;
}
