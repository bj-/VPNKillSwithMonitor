using System.Net.NetworkInformation;

namespace VKS.VpnMonitor;

public sealed class NetworkInterfaceScanner
{
    public IReadOnlyList<NetworkInterfaceInfo> GetUpInterfaces()
        => NetworkInterface.GetAllNetworkInterfaces()
            .Where(x => x.OperationalStatus == OperationalStatus.Up)
            .Select(x => new NetworkInterfaceInfo(x.Id, x.Name, x.NetworkInterfaceType.ToString()))
            .ToArray();
}

public sealed record NetworkInterfaceInfo(string Id, string Name, string Type);
