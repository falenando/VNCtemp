using System;
using System.Diagnostics;

namespace RemoteSupport.HostApp;

internal sealed class FirewallManager
{
    private readonly string _ruleName;

    public FirewallManager(string ruleName)
    {
        _ruleName = ruleName;
    }

    public bool EnsurePortOpen(int port)
    {
        Logger.Info($"Ensuring firewall rule '{_ruleName}' for port {port}.");
        var deleteRule = RunNetsh($"advfirewall firewall delete rule name=\"{_ruleName}\"");
        if (!deleteRule)
        {
            Logger.Warning("Unable to remove existing firewall rule. Continuing.");
        }

        return RunNetsh($"advfirewall firewall add rule name=\"{_ruleName}\" dir=in action=allow protocol=TCP localport={port}");
    }

    private static bool RunNetsh(string arguments)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "netsh",
            Arguments = arguments,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = Process.Start(startInfo);
        if (process is null)
        {
            Logger.Error("Failed to launch netsh.");
            return false;
        }

        process.WaitForExit();
        Logger.Info($"netsh {arguments} exited with code {process.ExitCode}.");
        return process.ExitCode == 0;
    }
}
