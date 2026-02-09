using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace RemoteSupport.HostApp;

internal static class NetworkHelper
{
    public static IReadOnlyList<string> GetLocalIPv4Addresses()
    {
        var results = new List<string>();
        foreach (var networkInterface in NetworkInterface.GetAllNetworkInterfaces())
        {
            if (networkInterface.OperationalStatus != OperationalStatus.Up)
            {
                continue;
            }

            foreach (var unicast in networkInterface.GetIPProperties().UnicastAddresses)
            {
                if (unicast.Address.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(unicast.Address))
                {
                    results.Add(unicast.Address.ToString());
                }
            }
        }

        return results.Distinct().OrderBy(value => value).ToArray();
    }
}
