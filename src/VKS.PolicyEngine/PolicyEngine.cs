using VKS.Contracts.Enums;
using VKS.Contracts.Models;
using VKS.Core.Interfaces;

namespace VKS.PolicyEngine;

public sealed class PolicyEngine(
    IVpnMonitor vpnMonitor,
    IProcessMonitor processMonitor,
    IWfpController wfpController,
    UnlockValidator unlockValidator) : IPolicyEngine
{
    private readonly SemaphoreSlim _gate = new(1, 1);
    private SystemStatus _status = new();
    private bool _manualLockRequested;

    public event EventHandler<SystemStatus>? StatusChanged;

    public Task<SystemStatus> GetStatusAsync(CancellationToken cancellationToken)
        => Task.FromResult(_status);

    public async Task EvaluateAsync(CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            var vpn = await vpnMonitor.GetSnapshotAsync(cancellationToken);
            var processes = await processMonitor.GetRunningBlockedProcessesAsync(cancellationToken);
            var wfp = await wfpController.GetStatusAsync(cancellationToken);

            if (!wfp.EnforcementVerified)
            {
                await wfpController.ApplyLockAsync(LockReason.EnforcementFailure, cancellationToken);
                Publish(new SystemStatus {
                    Vpn = vpn.State, Network = NetworkProtectionState.Locked,
                    ProcessGuard = processes.Count > 0 ? ProcessGuardState.Blocked : ProcessGuardState.Clear,
                    LockReason = LockReason.EnforcementFailure, BlockingProcesses = processes
                });
                return;
            }

            if (_manualLockRequested)
            {
                await LockCore(LockReason.ManualLock, vpn, processes, cancellationToken);
                return;
            }

            var rejection = unlockValidator.Validate(vpn, processes);
            if (rejection is not null)
            {
                var reason = processes.Count > 0
                    ? LockReason.ForbiddenProcessRunning
                    : LockReason.VpnDisconnected;
                await LockCore(reason, vpn, processes, cancellationToken);
                return;
            }

            await wfpController.ApplyUnlockAsync(cancellationToken);
            var after = await wfpController.GetStatusAsync(cancellationToken);
            if (!after.EnforcementVerified || !after.AllowPolicyInstalled)
            {
                await wfpController.ApplyLockAsync(LockReason.EnforcementFailure, cancellationToken);
                Publish(new SystemStatus { Vpn = vpn.State, Network = NetworkProtectionState.Locked,
                    ProcessGuard = ProcessGuardState.Clear, LockReason = LockReason.EnforcementFailure });
                return;
            }

            Publish(new SystemStatus { Vpn = vpn.State, Network = NetworkProtectionState.Unlocked,
                ProcessGuard = ProcessGuardState.Clear });
        }
        finally { _gate.Release(); }
    }

    public async Task RequestLockAsync(CancellationToken cancellationToken)
    {
        _manualLockRequested = true;
        await EvaluateAsync(cancellationToken);
    }

    public async Task<UnlockResult> RequestUnlockAsync(CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            var vpn = await vpnMonitor.GetSnapshotAsync(cancellationToken);
            var processes = await processMonitor.GetRunningBlockedProcessesAsync(cancellationToken);
            var rejection = unlockValidator.Validate(vpn, processes);
            if (rejection is not null)
                return new UnlockResult { Success = false, RejectionReason = rejection, Status = _status };

            _manualLockRequested = false;
        }
        finally { _gate.Release(); }

        await EvaluateAsync(cancellationToken);
        return new UnlockResult {
            Success = _status.Network == NetworkProtectionState.Unlocked,
            RejectionReason = _status.Network == NetworkProtectionState.Unlocked ? null : "ENFORCEMENT_NOT_CONFIRMED",
            Status = _status
        };
    }

    private async Task LockCore(LockReason reason, VpnSnapshot vpn,
        IReadOnlyList<ProcessInfo> processes, CancellationToken ct)
    {
        await wfpController.ApplyLockAsync(reason, ct);
        Publish(new SystemStatus {
            Vpn = vpn.State, Network = NetworkProtectionState.Locked,
            ProcessGuard = processes.Count > 0 ? ProcessGuardState.Blocked : ProcessGuardState.Clear,
            LockReason = reason, BlockingProcesses = processes
        });
    }

    private void Publish(SystemStatus status)
    {
        _status = status with { LastUpdated = DateTimeOffset.UtcNow };
        StatusChanged?.Invoke(this, _status);
    }
}
