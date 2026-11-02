using VKS.Contracts.Enums;
using VKS.Contracts.Models;

namespace VKS.PolicyEngine;

public sealed class UnlockValidator
{
    public string? Validate(VpnSnapshot vpn, IReadOnlyList<ProcessInfo> blockedProcesses)
    {
        if (vpn.State != VpnState.Connected ||
            !vpn.RouteVerified ||
            !vpn.TunnelHealthVerified)
            return "VPN_NOT_HEALTHY";

        if (blockedProcesses.Count > 0)
            return "FORBIDDEN_PROCESS_RUNNING";

        return null;
    }
}
