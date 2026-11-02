using VKS.Contracts.Events;
using VKS.Contracts.Models;

namespace VKS.Core.Interfaces;

public interface IVpnMonitor
{
    Task<VpnSnapshot> GetSnapshotAsync(CancellationToken cancellationToken);
    Task StartAsync(CancellationToken cancellationToken);
    Task StopAsync(CancellationToken cancellationToken);
    event EventHandler<VpnStateChangedEventArgs>? StateChanged;
}

public interface IProcessMonitor
{
    Task<IReadOnlyList<ProcessInfo>> GetRunningBlockedProcessesAsync(CancellationToken cancellationToken);
    Task StartAsync(CancellationToken cancellationToken);
    Task StopAsync(CancellationToken cancellationToken);
    event EventHandler<ProcessChangedEventArgs>? ProcessChanged;
}
