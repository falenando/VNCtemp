using System;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace RemoteSupport.HostApp;

public sealed class MainForm : Form
{
    private const string DefaultInstallDirectory = @"C:\Program Files\uvnc bvba\UltraVNC";
    private const string DefaultInstallerPath = @"C:\ProgramData\RemoteSupport\UltraVNC\UltraVNC_Setup.exe";
    private const string ServiceName = "uvnc_service";

    private readonly TextBox _installerPathTextBox = new();
    private readonly TextBox _installDirTextBox = new();
    private readonly TextBox _portTextBox = new();
    private readonly TextBox _passwordTextBox = new();
    private readonly TextBox _viewOnlyPasswordTextBox = new();
    private readonly CheckBox _allowLoopbackCheckBox = new();
    private readonly TextBox _ipListTextBox = new();
    private readonly TextBox _logTextBox = new();
    private readonly Label _countdownLabel = new();
    private readonly CountdownManager _countdownManager = new();

    public MainForm()
    {
        Text = "Remote Support Host";
        Width = 900;
        Height = 700;
        StartPosition = FormStartPosition.CenterScreen;

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 9,
            AutoSize = true
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));

        _installerPathTextBox.Text = DefaultInstallerPath;
        _installDirTextBox.Text = DefaultInstallDirectory;
        _portTextBox.Text = "5900";
        _passwordTextBox.UseSystemPasswordChar = true;
        _viewOnlyPasswordTextBox.UseSystemPasswordChar = true;
        _allowLoopbackCheckBox.Text = "Allow loopback";
        _allowLoopbackCheckBox.Checked = true;

        _ipListTextBox.ReadOnly = true;
        _ipListTextBox.Multiline = true;
        _ipListTextBox.Height = 80;

        _logTextBox.ReadOnly = true;
        _logTextBox.Multiline = true;
        _logTextBox.ScrollBars = ScrollBars.Vertical;
        _logTextBox.Height = 200;

        _countdownLabel.Text = "Session: idle";
        _countdownLabel.AutoSize = true;

        layout.Controls.Add(BuildGroup("Installer Path", _installerPathTextBox, BuildButton("Install UltraVNC", OnInstallClicked)), 0, 0);
        layout.SetColumnSpan(layout.Controls[^1], 2);

        layout.Controls.Add(BuildGroup("Install Directory", _installDirTextBox, BuildButton("Open Directory", OnOpenInstallDirectoryClicked)), 0, 1);
        layout.SetColumnSpan(layout.Controls[^1], 2);

        layout.Controls.Add(BuildSettingsPanel(), 0, 2);
        layout.SetColumnSpan(layout.Controls[^1], 2);

        layout.Controls.Add(BuildGroup("Detected IPs", _ipListTextBox, BuildButton("Refresh IPs", OnRefreshIpsClicked)), 0, 3);
        layout.SetColumnSpan(layout.Controls[^1], 2);

        layout.Controls.Add(BuildGroup("Session Countdown", _countdownLabel, BuildButton("Start 30 min", OnStartCountdownClicked)), 0, 4);
        layout.SetColumnSpan(layout.Controls[^1], 2);

        layout.Controls.Add(BuildGroup("Logs", _logTextBox, BuildButton("Clear Logs", (_, _) => _logTextBox.Clear())), 0, 5);
        layout.SetColumnSpan(layout.Controls[^1], 2);

        Controls.Add(layout);

        Logger.LogEmitted += OnLogEmitted;
        _countdownManager.RemainingSecondsChanged += UpdateCountdownLabel;
        _countdownManager.Completed += () => Logger.Info("Countdown completed.");

        Load += (_, _) => RefreshIps();
        FormClosing += (_, _) => Logger.LogEmitted -= OnLogEmitted;
    }

    private Control BuildSettingsPanel()
    {
        var settingsLayout = new TableLayoutPanel
        {
            ColumnCount = 2,
            Dock = DockStyle.Fill,
            AutoSize = true
        };

        settingsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        settingsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));

        var leftPanel = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, AutoSize = true };
        leftPanel.Controls.Add(BuildLabeledField("Port", _portTextBox));
        leftPanel.Controls.Add(BuildLabeledField("Control Password", _passwordTextBox));
        leftPanel.Controls.Add(BuildLabeledField("View-Only Password", _viewOnlyPasswordTextBox));
        leftPanel.Controls.Add(_allowLoopbackCheckBox);

        var rightPanel = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, AutoSize = true };
        rightPanel.Controls.Add(BuildButton("Apply Registry Config", OnApplyConfigClicked));
        rightPanel.Controls.Add(BuildButton("Open Firewall", OnOpenFirewallClicked));
        rightPanel.Controls.Add(BuildButton("Start Service", OnStartServiceClicked));
        rightPanel.Controls.Add(BuildButton("Stop Service", OnStopServiceClicked));

        settingsLayout.Controls.Add(leftPanel, 0, 0);
        settingsLayout.Controls.Add(rightPanel, 1, 0);

        return settingsLayout;
    }

    private static Control BuildGroup(string labelText, Control content, Control actionButton)
    {
        var group = new GroupBox { Text = labelText, Dock = DockStyle.Fill, AutoSize = true };
        var panel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            AutoSize = true
        };
        panel.Controls.Add(content);
        panel.Controls.Add(actionButton);
        group.Controls.Add(panel);
        return group;
    }

    private static Control BuildLabeledField(string labelText, Control input)
    {
        var panel = new FlowLayoutPanel { FlowDirection = FlowDirection.LeftToRight, AutoSize = true };
        panel.Controls.Add(new Label { Text = labelText, AutoSize = true, Width = 140 });
        input.Width = 240;
        panel.Controls.Add(input);
        return panel;
    }

    private static Button BuildButton(string text, EventHandler onClick)
    {
        var button = new Button { Text = text, AutoSize = true };
        button.Click += onClick;
        return button;
    }

    private void OnInstallClicked(object? sender, EventArgs e)
    {
        var installer = new UltraVncInstaller(_installerPathTextBox.Text.Trim(), _installDirTextBox.Text.Trim());
        if (installer.IsInstalled())
        {
            Logger.Info("UltraVNC is already installed.");
            return;
        }

        var success = installer.Install();
        Logger.Info(success ? "UltraVNC installation complete." : "UltraVNC installation failed.");
    }

    private void OnOpenInstallDirectoryClicked(object? sender, EventArgs e)
    {
        var path = _installDirTextBox.Text.Trim();
        if (!Directory.Exists(path))
        {
            Logger.Warning("Install directory does not exist.");
            return;
        }

        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
        {
            FileName = path,
            UseShellExecute = true
        });
    }

    private void OnApplyConfigClicked(object? sender, EventArgs e)
    {
        if (!int.TryParse(_portTextBox.Text, out var port))
        {
            Logger.Error("Invalid port value.");
            return;
        }

        var config = new UltraVncRegistryConfig();
        try
        {
            config.ApplySettings(_passwordTextBox.Text, _viewOnlyPasswordTextBox.Text, port, _allowLoopbackCheckBox.Checked);
            Logger.Info("UltraVNC registry configuration applied.");
        }
        catch (Exception ex)
        {
            Logger.Error($"Failed to apply registry config: {ex.Message}");
        }
    }

    private void OnOpenFirewallClicked(object? sender, EventArgs e)
    {
        if (!int.TryParse(_portTextBox.Text, out var port))
        {
            Logger.Error("Invalid port value.");
            return;
        }

        var manager = new FirewallManager("RemoteSupport UltraVNC");
        var success = manager.EnsurePortOpen(port);
        Logger.Info(success ? "Firewall rule applied." : "Firewall rule failed.");
    }

    private void OnStartServiceClicked(object? sender, EventArgs e)
    {
        var manager = new ServiceManager();
        try
        {
            manager.SetAutomaticStartup(ServiceName);
            var success = manager.StartService(ServiceName, TimeSpan.FromSeconds(30));
            Logger.Info(success ? "Service started." : "Service failed to start.");
        }
        catch (Exception ex)
        {
            Logger.Error($"Service error: {ex.Message}");
        }
    }

    private void OnStopServiceClicked(object? sender, EventArgs e)
    {
        var manager = new ServiceManager();
        try
        {
            var success = manager.StopService(ServiceName, TimeSpan.FromSeconds(30));
            Logger.Info(success ? "Service stopped." : "Service failed to stop.");
        }
        catch (Exception ex)
        {
            Logger.Error($"Service error: {ex.Message}");
        }
    }

    private void OnRefreshIpsClicked(object? sender, EventArgs e)
    {
        RefreshIps();
    }

    private void RefreshIps()
    {
        var ips = NetworkHelper.GetLocalIPv4Addresses();
        _ipListTextBox.Text = ips.Any() ? string.Join(Environment.NewLine, ips) : "No active IPv4 addresses detected.";
    }

    private void OnStartCountdownClicked(object? sender, EventArgs e)
    {
        _countdownManager.Start(30 * 60);
        Logger.Info("Countdown started for 30 minutes.");
    }

    private void UpdateCountdownLabel(int remainingSeconds)
    {
        if (InvokeRequired)
        {
            BeginInvoke(new Action<int>(UpdateCountdownLabel), remainingSeconds);
            return;
        }

        if (remainingSeconds <= 0)
        {
            _countdownLabel.Text = "Session: idle";
            return;
        }

        var time = TimeSpan.FromSeconds(remainingSeconds);
        _countdownLabel.Text = $"Session remaining: {time:hh\\:mm\\:ss}";
    }

    private void OnLogEmitted(string message)
    {
        if (InvokeRequired)
        {
            BeginInvoke(new Action<string>(OnLogEmitted), message);
            return;
        }

        _logTextBox.AppendText(message + Environment.NewLine);
    }
}
