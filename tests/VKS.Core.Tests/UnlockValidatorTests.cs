using VKS.Contracts.Enums;
using VKS.Contracts.Models;
using VKS.PolicyEngine;

namespace VKS.Core.Tests;

public sealed class UnlockValidatorTests
{
    private readonly UnlockValidator _validator = new();

    [Fact]
    public void RejectsUnknownVpn()
    {
        var result = _validator.Validate(new VpnSnapshot(), []);
        Assert.Equal("VPN_NOT_HEALTHY", result);
    }

    [Fact]
    public void RejectsVpnWithoutVerifiedRoute()
    {
        var vpn = new VpnSnapshot {
            State = VpnState.Connected,
            RouteVerified = false,
            TunnelHealthVerified = true
        };
        Assert.Equal("VPN_NOT_HEALTHY", _validator.Validate(vpn, []));
    }

    [Fact]
    public void RejectsForbiddenProcesses()
    {
        var vpn = new VpnSnapshot {
            State = VpnState.Connected,
            RouteVerified = true,
            TunnelHealthVerified = true
        };
        var process = new ProcessInfo { ProcessId = 42, Name = "blocked.exe" };
        Assert.Equal("FORBIDDEN_PROCESS_RUNNING", _validator.Validate(vpn, [process]));
    }

    [Fact]
    public void AllowsOnlyHealthyVpnAndClearProcessList()
    {
        var vpn = new VpnSnapshot {
            State = VpnState.Connected,
            RouteVerified = true,
            TunnelHealthVerified = true
        };
        Assert.Null(_validator.Validate(vpn, []));
    }
}
