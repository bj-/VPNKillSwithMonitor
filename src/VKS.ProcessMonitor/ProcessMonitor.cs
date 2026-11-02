using System.Diagnostics;
using VKS.Contracts.Events;
using VKS.Contracts.Models;
using VKS.Core.Configuration;
using VKS.Core.Interfaces;

namespace VKS.ProcessMonitor;

public sealed class ProcessMonitor(IOptionsProvider options) : IProcessMonitor
{
    private readonly ProcessMatcher _matcher = new(options.Current.ProcessGuard.BlockedProcesses);
    private CancellationTokenSource? _cts;
    private Task? _loop;

    public event EventHandler<ProcessChangedEventArgs>? ProcessChanged;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _loop = Task.Run(() => MonitorLoop(_cts.Token), CancellationToken.None);
        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_cts is null) return;
        await _cts.CancelAsync();
        if (_loop is not null) await _loop.WaitAsync(cancellationToken);
        _cts.Dispose();
        _cts = null;
    }

    public Task<IReadOnlyList<ProcessInfo>> GetRunningBlockedProcessesAsync(
        CancellationToken cancellationToken)
    {
        var found = new List<ProcessInfo>();
        foreach (var process in Process.GetProcesses())
        {
            cancellationToken.ThrowIfCancellationRequested();
            using (process)
            {
                try
                {
                    var name = process.ProcessName + ".exe";
                    string? path = null;
                    try { path = process.MainModule?.FileName; } catch { /* access denied */ }

                    if (!_matcher.IsBlocked(name, path)) continue;

                    DateTimeOffset start = DateTimeOffset.UtcNow;
                    try { start = new DateTimeOffset(process.StartTime); } catch { }

                    found.Add(new ProcessInfo {
                        ProcessId = process.Id, Name = name,
                        ExecutablePath = path, StartTime = start
                    });
                }
                catch (InvalidOperationException) { }
                catch (System.ComponentModel.Win32Exception) { }
            }
        }
        return Task.FromResult<IReadOnlyList<ProcessInfo>>(found);
    }

    private async Task MonitorLoop(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            var current = await GetRunningBlockedProcessesAsync(ct);
            foreach (var process in current)
                ProcessChanged?.Invoke(this, new ProcessChangedEventArgs(process, true));

            await Task.Delay(options.Current.ProcessGuard.RescanIntervalMs, ct);
        }
    }
}
