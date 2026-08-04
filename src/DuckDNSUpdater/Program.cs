namespace DuckDNSUpdater;

/// <summary>
/// Application entry point.
/// </summary>
static class Program
{
    /// <summary>
    /// Starts the WinForms message loop with <see cref="MainForm"/>.
    /// </summary>
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.Run(new MainForm());
    }
}
