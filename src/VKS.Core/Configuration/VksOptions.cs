namespace VKS.Core.Configuration;

public sealed class VksOptions
{
    public VpnOptions Vpn { get; set; } = new();
    public ProcessGuardOptions ProcessGuard { get; set; } = new();
    public NetworkOptions Network { get; set; } = new();
    public ManualUnlockOptions ManualUnlock { get; set; } = new();
}

public sealed class VpnOptions
{
    public int HealthCheckIntervalMs { get; set; } = 2000;
    public int FailureThreshold { get; set; } = 2;
    public List<string> HealthCheckTargets { get; set; } = ["1.1.1.1", "9.9.9.9"];
}

public sealed class ProcessGuardOptions
{
    public bool Enabled { get; set; } = true;
    public int RescanIntervalMs { get; set; } = 2000;
    public List<BlockedProcessRule> BlockedProcesses { get; set; } = [];
}

public sealed class BlockedProcessRule
{
    public string Name { get; set; } = "";
    public string? Path { get; set; }
    public string? Sha256 { get; set; }
}

public sealed class NetworkOptions
{
    public bool DefaultBlock { get; set; } = true;
    public bool Ipv4 { get; set; } = true;
    public bool Ipv6 { get; set; } = true;
    public bool AllowLoopback { get; set; } = true;
}

public sealed class ManualUnlockOptions
{
    public bool Enabled { get; set; } = true;
    public bool RequireVpnHealthy { get; set; } = true;
    public bool RequireNoBlockedProcesses { get; set; } = true;
}
