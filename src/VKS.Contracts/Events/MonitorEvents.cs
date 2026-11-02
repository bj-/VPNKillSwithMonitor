using VKS.Contracts.Enums;
using VKS.Contracts.Models;

namespace VKS.Contracts.Events;

public sealed class VpnStateChangedEventArgs(VpnSnapshot snapshot) : EventArgs
{
    public VpnSnapshot Snapshot { get; } = snapshot;
}

public sealed class ProcessChangedEventArgs(ProcessInfo? process, bool started) : EventArgs
{
    public ProcessInfo? Process { get; } = process;
    public bool Started { get; } = started;
}
