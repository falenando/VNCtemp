using System;
using System.Diagnostics;
using System.ServiceProcess;

namespace RemoteSupport.HostApp;

internal sealed class ServiceManager
{
    public bool IsRunning(string serviceName)
    {
        using var controller = new ServiceController(serviceName);
        return controller.Status == ServiceControllerStatus.Running;
    }

    public bool StartService(string serviceName, TimeSpan timeout)
    {
        using var controller = new ServiceController(serviceName);
        if (controller.Status == ServiceControllerStatus.Running)
        {
            Logger.Info($"Service {serviceName} already running.");
            return true;
        }

        Logger.Info($"Starting service {serviceName}.");
        controller.Start();
        controller.WaitForStatus(ServiceControllerStatus.Running, timeout);
        return controller.Status == ServiceControllerStatus.Running;
    }

    public bool StopService(string serviceName, TimeSpan timeout)
    {
        using var controller = new ServiceController(serviceName);
        if (controller.Status == ServiceControllerStatus.Stopped)
        {
            Logger.Info($"Service {serviceName} already stopped.");
            return true;
        }

        Logger.Info($"Stopping service {serviceName}.");
        controller.Stop();
        controller.WaitForStatus(ServiceControllerStatus.Stopped, timeout);
        return controller.Status == ServiceControllerStatus.Stopped;
    }

    public bool SetAutomaticStartup(string serviceName)
    {
        Logger.Info($"Setting service {serviceName} to automatic start.");
        return RunSc($"config \"{serviceName}\" start= auto");
    }

    private static bool RunSc(string arguments)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "sc.exe",
            Arguments = arguments,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = Process.Start(startInfo);
        if (process is null)
        {
            Logger.Error("Failed to launch sc.exe.");
            return false;
        }

        process.WaitForExit();
        Logger.Info($"sc.exe {arguments} exited with code {process.ExitCode}.");
        return process.ExitCode == 0;
    }
}
