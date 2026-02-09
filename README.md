# VNCtemp

VNCtemp is a lightweight, temporary VNC deployment helper intended for quick, LAN-scoped access during short troubleshooting sessions. It emphasizes minimal setup, automatic password rotation, and a clean uninstall.

## Installation

1. Download the latest release artifact to the target machine.
2. Extract the archive to a local folder (e.g. `C:\VNCtemp`).
3. Run the installer script from an elevated PowerShell prompt:

```powershell
cd C:\VNCtemp
.\install.ps1
```

4. Confirm that the service reports as running.

```powershell
Get-Service VNCtemp
```

## LAN testing

Use a second machine on the same LAN to verify connectivity:

1. Determine the target machine’s LAN IP address.
2. Connect from a VNC client using the target IP and the current temporary password.
3. Validate input/graphics for a few minutes, then disconnect.

If the connection fails, verify Windows Firewall rules and that the VNCtemp service is running.

## Password expiration

VNCtemp generates a short-lived password at install time and rotates it on a schedule:

- The password is valid for a limited duration (default: 24 hours).
- When the password expires, the service generates a new password and logs it locally.
- Expired passwords immediately stop working; users must retrieve the current password before reconnecting.

## Disable or uninstall

To disable the service without removing files:

```powershell
Stop-Service VNCtemp
Set-Service VNCtemp -StartupType Disabled
```

To uninstall completely:

```powershell
cd C:\VNCtemp
.\uninstall.ps1
```

This removes the service, deletes scheduled tasks, and cleans up any local configuration files.

## Limitations (UAC secure desktop)

VNCtemp cannot interact with the Windows secure desktop:

- UAC elevation prompts (secure desktop) will appear black or frozen to the remote user.
- Credential prompts that switch to the secure desktop require local user interaction.

## Antivirus considerations

Some endpoint protection tools flag remote access software by default. To reduce false positives:

- Use signed release artifacts and verify checksums.
- Add a temporary allowlist entry for the installation directory, if required by policy.
- Remove the software promptly after the support session to minimize exposure.
