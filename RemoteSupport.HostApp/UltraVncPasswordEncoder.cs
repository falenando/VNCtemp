using System;
using System.Security.Cryptography;
using System.Text;

namespace RemoteSupport.HostApp;

internal static class UltraVncPasswordEncoder
{
    private static readonly byte[] FixedKey =
    [
        0x17, 0x52, 0x6B, 0x06, 0x23, 0x4E, 0x58, 0x07
    ];

    public static byte[] EncodeToBytes(string password)
    {
        var passwordBytes = new byte[8];
        if (!string.IsNullOrEmpty(password))
        {
            var raw = Encoding.ASCII.GetBytes(password.Length > 8 ? password[..8] : password);
            Array.Copy(raw, passwordBytes, raw.Length);
        }

        using var des = DES.Create();
        des.Mode = CipherMode.ECB;
        des.Padding = PaddingMode.None;
        des.Key = FixedKey;

        using var encryptor = des.CreateEncryptor();
        return encryptor.TransformFinalBlock(passwordBytes, 0, passwordBytes.Length);
    }

    public static string EncodeToHex(string password)
    {
        var encoded = EncodeToBytes(password);
        var sb = new StringBuilder(encoded.Length * 2);
        foreach (var value in encoded)
        {
            sb.Append(value.ToString("X2"));
        }
        return sb.ToString();
    }
}
