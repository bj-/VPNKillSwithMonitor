namespace VKS.Contracts.Enums;

public enum VpnState { Unknown, Disconnected, Connecting, Connected }
public enum NetworkProtectionState { Unknown, Locked, Unlocked, Applying }
public enum ProcessGuardState { Unknown, Clear, Blocked }
public enum LockReason
{
    Startup, VpnDisconnected, ForbiddenProcessRunning, ManualLock,
    MonitorFailure, ConfigurationError, EnforcementFailure
}
