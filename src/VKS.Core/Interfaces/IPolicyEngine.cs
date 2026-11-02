using VKS.Contracts.Models;

namespace VKS.Core.Interfaces;

public interface IPolicyEngine
{
    Task<SystemStatus> GetStatusAsync(CancellationToken cancellationToken);
    Task EvaluateAsync(CancellationToken cancellationToken);
    Task RequestLockAsync(CancellationToken cancellationToken);
    Task<UnlockResult> RequestUnlockAsync(CancellationToken cancellationToken);
    event EventHandler<SystemStatus>? StatusChanged;
}
