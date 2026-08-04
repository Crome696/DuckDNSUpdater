using DuckDNSUpdater.Models;
using DuckDNSUpdater.Services;

namespace DuckDNSUpdater;

/// <summary>
/// Main window for configuring and controlling the DuckDNS updater.
/// </summary>
public partial class MainForm : Form
{
    private const string LogFileName = "duckdns-updater.log";

    private readonly ConfigService _configService = new();
    private readonly DuckDnsUpdater _updater = new();
    private readonly System.Windows.Forms.Timer _countdownTimer = new() { Interval = 1000 };

    private DateTime? _lastUpdateLocal;
    private bool _lastUpdateSuccess;
    private bool _logFileWriteFailed;

    /// <summary>
    /// Initializes the form, loads configuration, and optionally auto-starts the updater.
    /// </summary>
    public MainForm()
    {
        InitializeComponent();
        WireEvents();
        LoadConfigIntoUi();
        UpdateUiState();

        if (_chkAutoStart.Checked)
        {
            BeginInvoke(StartUpdater);
        }
    }

    private void WireEvents()
    {
        _btnSave.Click += (_, _) => SaveConfig();
        _btnStart.Click += (_, _) => StartUpdater();
        _btnStop.Click += async (_, _) => await StopUpdaterAsync();
        FormClosing += (_, e) =>
        {
            if (!_updater.IsRunning)
            {
                return;
            }

            try
            {
                _updater.StopAsync().GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                AppendLog($"Stop on close failed: {ex.Message}");
            }
        };

        _updater.StateChanged += (_, _) =>
        {
            if (IsDisposed)
            {
                return;
            }

            BeginInvoke(UpdateUiState);
        };

        _updater.Log += (_, args) =>
        {
            if (IsDisposed)
            {
                return;
            }

            BeginInvoke(() => AppendLog(args.Message));
        };

        _updater.Updated += (_, result) =>
        {
            if (IsDisposed)
            {
                return;
            }

            BeginInvoke(() =>
            {
                _lastUpdateLocal = result.Timestamp;
                _lastUpdateSuccess = result.Success;
                UpdateStatusLabels();
            });
        };

        _countdownTimer.Tick += (_, _) => UpdateStatusLabels();
        _countdownTimer.Start();
    }

    private void LoadConfigIntoUi()
    {
        try
        {
            var config = _configService.Load();
            ApplyConfigToControls(config);
            AppendLog($"Configuration loaded: {_configService.ConfigPath}");
        }
        catch (Exception ex)
        {
            AppendLog($"Configuration could not be loaded: {ex.Message}");
            ApplyConfigToControls(AppConfig.CreateDefault());
        }
    }

    private void ApplyConfigToControls(AppConfig config)
    {
        _txtDomain.Text = config.Domain;
        _txtToken.Text = config.Token;
        _numInterval.Value = Math.Clamp(config.IntervalSeconds, _numInterval.Minimum, _numInterval.Maximum);
        _chkAutoStart.Checked = config.AutoStart;
        _chkWriteLogsToFile.Checked = config.WriteLogsToFile;
        _logFileWriteFailed = false;
    }

    private AppConfig ReadConfigFromControls() => new()
    {
        Domain = _txtDomain.Text,
        Token = _txtToken.Text,
        IntervalSeconds = (int)_numInterval.Value,
        AutoStart = _chkAutoStart.Checked,
        WriteLogsToFile = _chkWriteLogsToFile.Checked
    };

    private void SaveConfig()
    {
        try
        {
            var config = ReadConfigFromControls();
            _configService.Save(config);
            ApplyConfigToControls(config);
            AppendLog("Configuration saved.");
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Save failed",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            AppendLog($"Save failed: {ex.Message}");
        }
    }

    private void StartUpdater()
    {
        try
        {
            var config = ReadConfigFromControls();
            ConfigService.Validate(config);
            _configService.Save(config);
            ApplyConfigToControls(config);
            _updater.Start(config);
            UpdateUiState();
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Start failed",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            AppendLog($"Start failed: {ex.Message}");
        }
    }

    private async Task StopUpdaterAsync()
    {
        try
        {
            await _updater.StopAsync();
        }
        catch (Exception ex)
        {
            AppendLog($"Stop failed: {ex.Message}");
        }
        finally
        {
            UpdateUiState();
        }
    }

    private void UpdateUiState()
    {
        var running = _updater.IsRunning;

        _txtDomain.ReadOnly = running;
        _txtToken.ReadOnly = running;
        _numInterval.Enabled = !running;
        _chkAutoStart.Enabled = !running;
        _btnSave.Enabled = !running;
        _btnStart.Enabled = !running;
        _btnStop.Enabled = running;
        // Write-logs checkbox stays enabled while running so logging can be toggled live.

        UpdateStatusLabels();
    }

    private void UpdateStatusLabels()
    {
        var running = _updater.IsRunning;
        _lblStatusValue.Text = running ? "Running" : "Stopped";
        _lblStatusValue.ForeColor = running ? Color.ForestGreen : Color.DimGray;

        if (_lastUpdateLocal is null)
        {
            _lblLastUpdateValue.Text = "—";
            _lblLastUpdateValue.ForeColor = Color.DimGray;
        }
        else
        {
            var outcome = _lastUpdateSuccess ? "OK" : "Failed";
            _lblLastUpdateValue.Text = $"{outcome} at {_lastUpdateLocal:HH:mm:ss}";
            _lblLastUpdateValue.ForeColor = _lastUpdateSuccess ? Color.ForestGreen : Color.Firebrick;
        }

        var next = _updater.NextUpdateUtc;
        if (!running || next is null)
        {
            _lblNextUpdateValue.Text = "—";
        }
        else
        {
            var remaining = next.Value - DateTime.UtcNow;
            if (remaining < TimeSpan.Zero)
            {
                remaining = TimeSpan.Zero;
            }

            _lblNextUpdateValue.Text =
                $"{next.Value.ToLocalTime():HH:mm:ss} ({(int)remaining.TotalSeconds}s)";
        }
    }

    private void AppendLog(string message)
    {
        var line = $"[{DateTime.Now:HH:mm:ss}] {message}";
        if (_txtLog.TextLength == 0)
        {
            _txtLog.Text = line;
        }
        else
        {
            _txtLog.AppendText(Environment.NewLine + line);
        }

        if (_chkWriteLogsToFile.Checked)
        {
            TryAppendLogToFile(line);
        }
    }

    private void TryAppendLogToFile(string line)
    {
        try
        {
            var path = Path.Combine(AppContext.BaseDirectory, LogFileName);
            File.AppendAllText(path, line + Environment.NewLine);
            _logFileWriteFailed = false;
        }
        catch (Exception ex)
        {
            if (_logFileWriteFailed)
            {
                return;
            }

            _logFileWriteFailed = true;
            var note = $"[{DateTime.Now:HH:mm:ss}] Could not write log file: {ex.Message}";
            if (_txtLog.TextLength == 0)
            {
                _txtLog.Text = note;
            }
            else
            {
                _txtLog.AppendText(Environment.NewLine + note);
            }
        }
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        _countdownTimer.Stop();
        _countdownTimer.Dispose();
        _updater.Dispose();
        base.OnFormClosed(e);
    }
}
