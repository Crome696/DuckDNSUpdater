using System.Diagnostics;
using System.Text.Json;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using FlaUI.Core.Tools;
using FlaUI.UIA3;
using Application = FlaUI.Core.Application;

[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace DuckDNSUpdater.Tests.E2E;

public sealed class AppLaunchFixture : IDisposable
{
    public string TempDirectory { get; }
    public string ConfigPath => Path.Combine(TempDirectory, "config.json");
    public Application App { get; }
    public UIA3Automation Automation { get; }
    public Window MainWindow { get; }

    public AppLaunchFixture(string? configJson = null)
    {
        KillStrayAppProcesses();

        var exeSource = ResolveBuiltAppExe();
        var sourceDir = Path.GetDirectoryName(exeSource)
            ?? throw new InvalidOperationException("Could not resolve app directory.");

        TempDirectory = Path.Combine(Path.GetTempPath(), "DuckDNSUpdaterUi_" + Guid.NewGuid().ToString("N"));
        CopyDirectory(sourceDir, TempDirectory);

        File.WriteAllText(ConfigPath, configJson ?? """
            {
              "domain": "ui-host",
              "token": "ui-token-xxxxxxxxxxxxxxxxxxxx",
              "intervalSeconds": 300,
              "autoStart": false,
              "writeLogsToFile": false
            }
            """);

        var startInfo = new ProcessStartInfo
        {
            FileName = Path.Combine(TempDirectory, "DuckDNSUpdater.exe"),
            WorkingDirectory = TempDirectory,
            UseShellExecute = false
        };

        App = Application.Launch(startInfo);
        Automation = new UIA3Automation();
        MainWindow = Retry.WhileNull(
            () =>
            {
                App.WaitWhileMainHandleIsMissing(TimeSpan.FromSeconds(10));
                var window = App.GetMainWindow(Automation, TimeSpan.FromSeconds(10));
                return window is not null && !window.Properties.ProcessId.IsSupported
                    ? window
                    : window is not null && window.Properties.ProcessId.ValueOrDefault == App.ProcessId
                        ? window
                        : null;
            },
            timeout: TimeSpan.FromSeconds(15),
            ignoreException: true).Result
            ?? throw new InvalidOperationException("Main window did not appear.");

        MainWindow.SetForeground();
        Thread.Sleep(400);
    }

    public AutomationElement ById(string automationId) =>
        MainWindow.FindFirstDescendant(cf => cf.ByAutomationId(automationId))
        ?? throw new InvalidOperationException($"Control '{automationId}' not found.");

    public void SetTextBox(string automationId, string value)
    {
        var box = ById(automationId).AsTextBox();
        MainWindow.SetForeground();
        box.Focus();
        box.Text = value;
        Thread.Sleep(100);
    }

    /// <summary>
    /// Clicks a button (non-blocking for modal MessageBox) and dismisses the expected dialog.
    /// </summary>
    public void ClickAndDismissDialog(string buttonAutomationId, string dialogTitle, TimeSpan timeout)
    {
        MainWindow.SetForeground();
        var button = ById(buttonAutomationId);
        button.Focus();
        button.Click();

        var dialog = Retry.WhileNull(
            () => FindDialog(dialogTitle),
            timeout: timeout,
            interval: TimeSpan.FromMilliseconds(200),
            ignoreException: true).Result
            ?? throw new TimeoutException($"Dialog '{dialogTitle}' did not appear.");

        var ok = dialog.FindFirstDescendant(cf => cf.ByControlType(ControlType.Button).And(cf.ByName("OK")))
            ?? dialog.FindFirstDescendant(cf => cf.ByControlType(ControlType.Button));
        ok?.Click();
        Thread.Sleep(300);
    }

    private Window? FindDialog(string dialogTitle)
    {
        foreach (var modal in MainWindow.ModalWindows)
        {
            if (string.Equals(modal.Title, dialogTitle, StringComparison.Ordinal))
            {
                return modal;
            }
        }

        // MessageBox windows are top-level; match by title and same process id.
        var desktop = Automation.GetDesktop();
        var windows = desktop.FindAllChildren(cf => cf.ByControlType(ControlType.Window));
        foreach (var element in windows)
        {
            var window = element.AsWindow();
            if (!string.Equals(window.Title, dialogTitle, StringComparison.Ordinal))
            {
                continue;
            }

            if (window.Properties.ProcessId.IsSupported
                && window.Properties.ProcessId.ValueOrDefault == App.ProcessId)
            {
                return window;
            }
        }

        return null;
    }

    public void Dispose()
    {
        try
        {
            if (!App.HasExited)
            {
                App.Close();
                Thread.Sleep(400);
            }
        }
        catch
        {
            // ignore
        }

        try
        {
            if (!App.HasExited)
            {
                App.Kill();
            }
        }
        catch
        {
            // ignore
        }

        try
        {
            using var process = Process.GetProcessById(App.ProcessId);
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
                process.WaitForExit(3000);
            }
        }
        catch
        {
            // already exited or invalid
        }

        try
        {
            Automation.Dispose();
        }
        catch
        {
            // ignore
        }

        try
        {
            App.Dispose();
        }
        catch
        {
            // ignore
        }

        try
        {
            if (Directory.Exists(TempDirectory))
            {
                Directory.Delete(TempDirectory, recursive: true);
            }
        }
        catch
        {
            // best-effort cleanup
        }
    }

    private static void KillStrayAppProcesses()
    {
        foreach (var process in Process.GetProcessesByName("DuckDNSUpdater"))
        {
            try
            {
                process.Kill(entireProcessTree: true);
                process.WaitForExit(3000);
            }
            catch
            {
                // ignore
            }
            finally
            {
                process.Dispose();
            }
        }
    }

    private static string ResolveBuiltAppExe()
    {
        var baseDir = AppContext.BaseDirectory;
        var configuration = new DirectoryInfo(baseDir).Parent?.Parent?.Name ?? "Debug";
        var repoRoot = Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", "..", "..", ".."));
        var exe = Path.Combine(
            repoRoot,
            "src",
            "DuckDNSUpdater",
            "bin",
            configuration,
            "net8.0-windows",
            "win-x64",
            "DuckDNSUpdater.exe");

        if (!File.Exists(exe))
        {
            throw new FileNotFoundException(
                $"DuckDNSUpdater.exe not found at '{exe}'. Build the app project first.",
                exe);
        }

        return exe;
    }

    private static void CopyDirectory(string sourceDir, string destDir)
    {
        Directory.CreateDirectory(destDir);
        foreach (var file in Directory.GetFiles(sourceDir))
        {
            File.Copy(file, Path.Combine(destDir, Path.GetFileName(file)), overwrite: true);
        }

        foreach (var dir in Directory.GetDirectories(sourceDir))
        {
            CopyDirectory(dir, Path.Combine(destDir, Path.GetFileName(dir)));
        }
    }
}

public class UiSmokeTests
{
    private const string EmptyDomainConfig = """
        {
          "domain": "",
          "token": "ui-token-xxxxxxxxxxxxxxxxxxxx",
          "intervalSeconds": 300,
          "autoStart": false,
          "writeLogsToFile": false
        }
        """;

    [Fact]
    public void MainWindow_ExposesKeyControls()
    {
        using var fixture = new AppLaunchFixture();

        Assert.Equal("DuckDNS Updater", fixture.MainWindow.Title);
        Assert.NotNull(fixture.ById("Domain"));
        Assert.NotNull(fixture.ById("Token"));
        Assert.NotNull(fixture.ById("Interval"));
        Assert.NotNull(fixture.ById("Save"));
        Assert.NotNull(fixture.ById("Start"));
        Assert.NotNull(fixture.ById("Stop"));
        Assert.NotNull(fixture.ById("StatusValue"));
    }

    [Fact]
    public void WhenStopped_StartEnabled_StopDisabled()
    {
        using var fixture = new AppLaunchFixture();

        var start = fixture.ById("Start").AsButton();
        var stop = fixture.ById("Stop").AsButton();

        Assert.True(start.IsEnabled);
        Assert.False(stop.IsEnabled);
        Assert.Equal("Stopped", fixture.ById("StatusValue").Name);
    }

    [Fact]
    public void EmptyDomain_SaveAndStart_ShowValidationDialogs()
    {
        using var fixture = new AppLaunchFixture(EmptyDomainConfig);
        fixture.ClickAndDismissDialog("Save", "Save failed", TimeSpan.FromSeconds(15));
        fixture.ClickAndDismissDialog("Start", "Start failed", TimeSpan.FromSeconds(15));
        Assert.False(fixture.ById("Stop").AsButton().IsEnabled);
    }

    [Fact]
    public void Save_WithValidValues_WritesConfigJson()
    {
        using var fixture = new AppLaunchFixture();

        fixture.SetTextBox("Domain", "saved-host");
        fixture.SetTextBox("Token", "saved-token-xxxxxxxxxxxxxxxxxxxx");

        fixture.MainWindow.SetForeground();
        fixture.ById("Save").Click();

        var saved = Retry.WhileFalse(
            () =>
            {
                if (!File.Exists(fixture.ConfigPath))
                {
                    return false;
                }

                var json = File.ReadAllText(fixture.ConfigPath);
                using var doc = JsonDocument.Parse(json);
                return doc.RootElement.GetProperty("domain").GetString() == "saved-host"
                    && doc.RootElement.GetProperty("token").GetString() == "saved-token-xxxxxxxxxxxxxxxxxxxx";
            },
            timeout: TimeSpan.FromSeconds(5),
            ignoreException: true,
            throwOnTimeout: false);

        Assert.True(saved.Result, "config.json was not updated with the saved domain/token.");
    }
}
