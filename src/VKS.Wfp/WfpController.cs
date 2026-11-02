using VKS.Contracts.Enums;
using VKS.Contracts.Models;
using VKS.Core.Interfaces;

namespace VKS.Wfp;

// SECURITY PLACEHOLDER. Never reports enforcement as verified.
// No real WFP filters are installed by this project yet.
public sealed class WfpController : IWfpController
{
    public Task InitializeAsync(CancellationToken cancellationToken)
        => Task.CompletedTask;

    public Task ApplyLockAsync(LockReason reason, CancellationToken cancellationToken)
        => Task.CompletedTask;

    public Task ApplyUnlockAsync(CancellationToken cancellationToken)
        => throw new NotSupportedException(
            "WFP enforcement is not implemented. Unlock is intentionally disabled.");

    public Task<WfpStatus> GetStatusAsync(CancellationToken cancellationToken)
        => Task.FromResult(new WfpStatus {
            Initialized = false,
            LockFiltersInstalled = false,
            AllowPolicyInstalled = false,
            EnforcementVerified = false,
            Error = "WFP_NOT_IMPLEMENTED"
        });

    public Task RestoreProtectionAsync(CancellationToken cancellationToken)
        => Task.CompletedTask;
}
