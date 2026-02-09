using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;

namespace RemoteSupport.ViewerLauncher;

internal static class Program
{
    private const string DefaultViewerPath = @"C:\Program Files\uvnc bvba\UltraVNC\vncviewer.exe";

    private static int Main(string[] args)
    {
        if (args.Length < 3)
        {
            Console.Error.WriteLine("Usage: RemoteSupport.ViewerLauncher <host> <port> <password> [viewerPath]");
            return 1;
        }

        var host = args[0].Trim();
        if (string.IsNullOrWhiteSpace(host))
        {
            Console.Error.WriteLine("Host is required.");
            return 1;
        }

        if (!int.TryParse(args[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var port) || port <= 0 || port > 65535)
        {
            Console.Error.WriteLine("Port must be a number between 1 and 65535.");
            return 1;
        }

        var password = args[2];
        if (string.IsNullOrEmpty(password))
        {
            Console.Error.WriteLine("Password is required.");
            return 1;
        }

        var viewerPath = args.Length > 3 ? args[3].Trim() : DefaultViewerPath;
        if (!File.Exists(viewerPath))
        {
            viewerPath = "vncviewer.exe";
        }

        var tempFile = Path.Combine(Path.GetTempPath(), $"remotesupport_{Guid.NewGuid():N}.vnc");
        try
        {
            var configContents = $"[connection]{Environment.NewLine}" +
                                 $"host={host}{Environment.NewLine}" +
                                 $"port={port}{Environment.NewLine}" +
                                 $"Password={UltraVncPasswordEncoder.EncodeToHex(password)}{Environment.NewLine}";

            File.WriteAllText(tempFile, configContents);
            Console.WriteLine($"Launching viewer using {tempFile}.");

            var startInfo = new ProcessStartInfo
            {
                FileName = viewerPath,
                Arguments = $"-config \"{tempFile}\"",
                UseShellExecute = false
            };

            using var process = Process.Start(startInfo);
            if (process is null)
            {
                Console.Error.WriteLine("Failed to start vncviewer.exe.");
                return 1;
            }

            process.WaitForExit();
            return process.ExitCode;
        }
        finally
        {
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }
}

internal static class UltraVncPasswordEncoder
{
    private static readonly byte[] FixedKey =
    [
        0x17, 0x52, 0x6B, 0x06, 0x23, 0x4E, 0x58, 0x07
    ];

    public static string EncodeToHex(string password)
    {
        var passwordBytes = new byte[8];
        if (!string.IsNullOrEmpty(password))
        {
            var raw = System.Text.Encoding.ASCII.GetBytes(password.Length > 8 ? password[..8] : password);
            Array.Copy(raw, passwordBytes, raw.Length);
        }

        using var des = System.Security.Cryptography.DES.Create();
        des.Mode = System.Security.Cryptography.CipherMode.ECB;
        des.Padding = System.Security.Cryptography.PaddingMode.None;
        des.Key = FixedKey;

        using var encryptor = des.CreateEncryptor();
        var encoded = encryptor.TransformFinalBlock(passwordBytes, 0, passwordBytes.Length);
        return BitConverter.ToString(encoded).Replace("-", string.Empty, StringComparison.Ordinal);
    }
}
