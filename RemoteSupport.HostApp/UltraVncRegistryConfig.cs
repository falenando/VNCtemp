using System;
using Microsoft.Win32;

namespace RemoteSupport.HostApp;

internal sealed class UltraVncRegistryConfig
{
    private const string RegistryPath = @"SOFTWARE\UltraVNC";

    public void ApplySettings(string password, string viewOnlyPassword, int port, bool allowLoopback)
    {
        using var key = Registry.LocalMachine.CreateSubKey(RegistryPath, true);
        if (key is null)
        {
            throw new InvalidOperationException("Unable to open UltraVNC registry key.");
        }

        var passwordBytes = UltraVncPasswordEncoder.EncodeToBytes(password);
        var viewOnlyBytes = UltraVncPasswordEncoder.EncodeToBytes(viewOnlyPassword);

        key.SetValue("passwd", passwordBytes, RegistryValueKind.Binary);
        key.SetValue("passwd2", viewOnlyBytes, RegistryValueKind.Binary);
        key.SetValue("PortNumber", port, RegistryValueKind.DWord);
        key.SetValue("LoopbackOnly", allowLoopback ? 1 : 0, RegistryValueKind.DWord);
    }
}
