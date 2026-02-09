using System;
using System.Diagnostics;
using System.IO;

namespace RemoteSupport.HostApp;

internal sealed class UltraVncInstaller
{
    public string InstallerPath { get; }
    public string InstallDirectory { get; }

    public UltraVncInstaller(string installerPath, string installDirectory)
    {
        InstallerPath = installerPath;
        InstallDirectory = installDirectory;
    }

    public bool IsInstalled()
    {
        var serverPath = Path.Combine(InstallDirectory, "winvnc.exe");
        return File.Exists(serverPath);
    }

    public bool Install()
    {
        if (!File.Exists(InstallerPath))
        {
            Logger.Error($"Installer not found at {InstallerPath}");
            return false;
        }

        Logger.Info("Starting UltraVNC installer.");
        var startInfo = new ProcessStartInfo
        {
            FileName = InstallerPath,
            Arguments = $"/silent /dir=\"{InstallDirectory}\"",
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = Process.Start(startInfo);
        if (process is null)
        {
            Logger.Error("Failed to launch installer process.");
            return false;
        }

        process.WaitForExit();
        Logger.Info($"Installer exited with code {process.ExitCode}.");
        return process.ExitCode == 0 && IsInstalled();
    }
}
