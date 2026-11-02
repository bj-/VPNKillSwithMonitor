using VKS.Contracts.Enums;
using VKS.Contracts.Events;
using VKS.Contracts.Models;
using VKS.Core.Interfaces;

namespace VKS.VpnMonitor;

// Safe initial implementation: it deliberately does NOT infer tunnel health
// from the presence of a virtual adapter.
public sealed class VpnMonitor(NetworkInterfaceScanner scanner) : IVpnMonitor
{
    public event EventHandler<VpnStateChangedEventArgs>? StateChanged;

    public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public Task<VpnSnapshot> GetSnapshotAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _ = scanner.GetUpInterfaces(); // discovery groundwork only
        return Task.FromResult(new VpnSnapshot {
            State = VpnState.Unknown,
            RouteVerified = false,
            TunnelHealthVerified = false,
            CheckedAt = DateTimeOffset.UtcNow
        });
    }
}
