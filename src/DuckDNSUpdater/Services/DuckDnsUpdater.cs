using DuckDNSUpdater.Models;

namespace DuckDNSUpdater.Services;

/// <summary>
/// Runs DuckDNS updates on a timer and raises UI-friendly status events.
/// </summary>
public sealed class DuckDnsUpdater : IDisposable
{
    private readonly DuckDnsClient _client;
    private readonly object _sync = new();
    private CancellationTokenSource? _cts;
    private Task? _loopTask;
    private DateTime? _nextUpdateUtc;

    /// <summary>
    /// Creates an updater using an optional shared <see cref="DuckDnsClient"/>.
    /// </summary>
    public DuckDnsUpdater(DuckDnsClient? client = null)
    {
        _client = client ?? new DuckDnsClient();
    }

    /// <summary>
    /// Whether the update loop is currently active.
    /// </summary>
    public bool IsRunning { get; private set; }

    /// <summary>
    /// UTC time of the next scheduled update, or <c>null</c> while idle or mid-update.
    /// </summary>
    public DateTime? NextUpdateUtc
    {
        get
        {
            lock (_sync)
            {
                return _nextUpdateUtc;
            }
        }
    }

    /// <summary>
    /// Raised when <see cref="IsRunning"/> or <see cref="NextUpdateUtc"/> changes.
    /// </summary>
    public event EventHandler? StateChanged;

    /// <summary>
    /// Raised when a human-readable log line should be shown.
    /// </summary>
    public event EventHandler<UpdaterLogEventArgs>? Log;

    /// <summary>
    /// Raised after each successful or failed DuckDNS update attempt.
    /// </summary>
    public event EventHandler<DuckDnsUpdateResult>? Updated;

    /// <summary>
    /// Validates <paramref name="config"/> and starts the background update loop.
    /// </summary>
    public void Start(AppConfig config)
    {
        ConfigService.Validate(config);

        lock (_sync)
        {
            if (IsRunning)
            {
                return;
            }

            _cts = new CancellationTokenSource();
            IsRunning = true;
            _nextUpdateUtc = null;
            _loopTask = Task.Run(() => RunLoopAsync(config.Clone(), _cts.Token));
        }

        OnStateChanged();
        RaiseLog("Updater started.");
    }

    /// <summary>
    /// Cancels the update loop and waits for it to finish.
    /// </summary>
    public async Task StopAsync()
    {
        Task? loopTask;
        CancellationTokenSource? cts;

        lock (_sync)
        {
            if (!IsRunning)
            {
                return;
            }

            cts = _cts;
            loopTask = _loopTask;
            _cts = null;
            _loopTask = null;
            IsRunning = false;
            _nextUpdateUtc = null;
        }

        try
        {
            cts?.Cancel();
        }
        catch (ObjectDisposedException)
        {
            // already disposed
        }

        if (loopTask is not null)
        {
            try
            {
                await loopTask.ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                // expected on stop
            }
        }

        cts?.Dispose();
        OnStateChanged();
        RaiseLog("Updater stopped.");
    }

    private async Task RunLoopAsync(AppConfig config, CancellationToken cancellationToken)
    {
        try
        {
            await PerformUpdateAsync(config, cancellationToken).ConfigureAwait(false);

            using var timer = new PeriodicTimer(TimeSpan.FromSeconds(config.IntervalSeconds));

            while (!cancellationToken.IsCancellationRequested)
            {
                SetNextUpdate(DateTime.UtcNow.AddSeconds(config.IntervalSeconds));
                OnStateChanged();

                if (!await timer.WaitForNextTickAsync(cancellationToken).ConfigureAwait(false))
                {
                    break;
                }

                await PerformUpdateAsync(config, cancellationToken).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException)
        {
            // normal shutdown
        }
        catch (Exception ex)
        {
            RaiseLog($"Updater error: {ex.Message}");
        }
        finally
        {
            lock (_sync)
            {
                IsRunning = false;
                _nextUpdateUtc = null;
            }

            OnStateChanged();
        }
    }

    private async Task PerformUpdateAsync(AppConfig config, CancellationToken cancellationToken)
    {
        SetNextUpdate(null);
        OnStateChanged();
        RaiseLog("Running update…");

        try
        {
            var result = await _client.UpdateAsync(
                config.Domain,
                config.Token,
                cancellationToken: cancellationToken).ConfigureAwait(false);

            Updated?.Invoke(this, result);

            RaiseLog(result.Success
                ? $"[{result.Timestamp:HH:mm:ss}] OK – IP {result.IpAddress}"
                : $"[{result.Timestamp:HH:mm:ss}] Failed – DuckDNS responded: {result.Response} (IP {result.IpAddress})");
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            RaiseLog($"[{DateTime.Now:HH:mm:ss}] Network error: {ex.Message}");
        }
    }

    private void SetNextUpdate(DateTime? utc)
    {
        lock (_sync)
        {
            _nextUpdateUtc = utc;
        }
    }

    private void OnStateChanged() => StateChanged?.Invoke(this, EventArgs.Empty);

    private void RaiseLog(string message) =>
        Log?.Invoke(this, new UpdaterLogEventArgs(message));

    /// <inheritdoc />
    public void Dispose()
    {
        try
        {
            StopAsync().GetAwaiter().GetResult();
        }
        catch
        {
            // best-effort dispose
        }

        _client.Dispose();
    }
}

/// <summary>
/// Carries a single updater log message.
/// </summary>
public sealed class UpdaterLogEventArgs(string message) : EventArgs
{
    /// <summary>
    /// Log text to display.
    /// </summary>
    public string Message { get; } = message;
}
