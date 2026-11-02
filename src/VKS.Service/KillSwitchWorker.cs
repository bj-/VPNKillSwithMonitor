using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using VKS.Core.Interfaces;

namespace VKS.Service;

public sealed class KillSwitchWorker(
    IVpnMonitor vpnMonitor,
    IProcessMonitor processMonitor,
    IWfpController wfp,
    IPolicyEngine policy,
    ILogger<KillSwitchWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // In production WFP boot-time protection must already be present before
        // normal networking is permitted. This placeholder cannot guarantee that.
        await wfp.InitializeAsync(stoppingToken);
        await wfp.RestoreProtectionAsync(stoppingToken);
        await vpnMonitor.StartAsync(stoppingToken);
        await processMonitor.StartAsync(stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try { await policy.EvaluateAsync(stoppingToken); }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception ex) { logger.LogError(ex, "Policy evaluation failed"); }

            await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await processMonitor.StopAsync(cancellationToken);
        await vpnMonitor.StopAsync(cancellationToken);
        await base.StopAsync(cancellationToken);
    }
}
