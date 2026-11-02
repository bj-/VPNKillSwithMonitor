using VKS.Contracts.Enums;

namespace VKS.Contracts.Models;

public sealed record VpnSnapshot
{
    public VpnState State { get; init; } = VpnState.Unknown;
    public int? InterfaceIndex { get; init; }
    public string? InterfaceName { get; init; }
    public string? RemoteEndpoint { get; init; }
    public bool RouteVerified { get; init; }
    public bool TunnelHealthVerified { get; init; }
    public DateTimeOffset CheckedAt { get; init; } = DateTimeOffset.UtcNow;
}
