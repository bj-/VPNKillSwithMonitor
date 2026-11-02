using VKS.Service;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using VKS.Core.Configuration;
using VKS.Core.Interfaces;
using VKS.Infrastructure.Configuration;
using VKS.PolicyEngine;
using VKS.ProcessMonitor;
using VKS.VpnMonitor;
using VKS.Wfp;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddWindowsService(options => options.ServiceName = "VpnKillSwitch");

var configPath = Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
    "VpnKillSwitch", "config", "settings.json");

builder.Services.AddSingleton<IOptionsProvider>(_ => new JsonOptionsProvider(configPath));
builder.Services.AddSingleton<NetworkInterfaceScanner>();
builder.Services.AddSingleton<IVpnMonitor, VpnMonitor>();
builder.Services.AddSingleton<IProcessMonitor, ProcessMonitor>();
builder.Services.AddSingleton<IWfpController, WfpController>();
builder.Services.AddSingleton<UnlockValidator>();
builder.Services.AddSingleton<IPolicyEngine, PolicyEngine>();
builder.Services.AddHostedService<KillSwitchWorker>();

await builder.Build().RunAsync();
