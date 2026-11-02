using VKS.Contracts.Enums;
using VKS.Contracts.Models;

namespace VKS.Core.Interfaces;

public interface IWfpController
{
    Task InitializeAsync(CancellationToken cancellationToken);
    Task ApplyLockAsync(LockReason reason, CancellationToken cancellationToken);
    Task ApplyUnlockAsync(CancellationToken cancellationToken);
    Task<WfpStatus> GetStatusAsync(CancellationToken cancellationToken);
    Task RestoreProtectionAsync(CancellationToken cancellationToken);
}
