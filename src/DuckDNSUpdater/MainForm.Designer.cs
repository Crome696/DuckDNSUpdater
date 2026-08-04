#nullable enable

namespace DuckDNSUpdater;

partial class MainForm
{
    private System.ComponentModel.IContainer? components;

    private TableLayoutPanel _rootLayout = null!;
    private GroupBox _grpDuckDns = null!;
    private GroupBox _grpUpdate = null!;
    private GroupBox _grpStatus = null!;
    private FlowLayoutPanel _buttonPanel = null!;

    private TextBox _txtDomain = null!;
    private TextBox _txtToken = null!;
    private NumericUpDown _numInterval = null!;
    private CheckBox _chkAutoStart = null!;
    private CheckBox _chkWriteLogsToFile = null!;

    private Button _btnSave = null!;
    private Button _btnStart = null!;
    private Button _btnStop = null!;

    private Label _lblStatusValue = null!;
    private Label _lblLastUpdateValue = null!;
    private Label _lblNextUpdateValue = null!;
    private TextBox _txtLog = null!;

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            components?.Dispose();
        }

        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        components = new System.ComponentModel.Container();

        _rootLayout = new TableLayoutPanel();
        _grpDuckDns = new GroupBox();
        _grpUpdate = new GroupBox();
        _grpStatus = new GroupBox();
        _buttonPanel = new FlowLayoutPanel();

        _txtDomain = new TextBox();
        _txtToken = new TextBox();
        _numInterval = new NumericUpDown();
        _chkAutoStart = new CheckBox();
        _chkWriteLogsToFile = new CheckBox();

        _btnSave = new Button();
        _btnStart = new Button();
        _btnStop = new Button();

        _lblStatusValue = new Label();
        _lblLastUpdateValue = new Label();
        _lblNextUpdateValue = new Label();
        _txtLog = new TextBox();

        SuspendLayout();
        _rootLayout.SuspendLayout();
        _grpDuckDns.SuspendLayout();
        _grpUpdate.SuspendLayout();
        _grpStatus.SuspendLayout();
        _buttonPanel.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)_numInterval).BeginInit();

        // MainForm
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(560, 500);
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        MinimizeBox = true;
        Name = "MainForm";
        StartPosition = FormStartPosition.CenterScreen;
        Text = "DuckDNS Updater";
        Padding = new Padding(16);
        Font = new Font("Segoe UI", 9F);
        ApplyApplicationIcon();

        // Root layout: single column, no absolute positioning
        _rootLayout.ColumnCount = 1;
        _rootLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        _rootLayout.Dock = DockStyle.Fill;
        _rootLayout.RowCount = 4;
        _rootLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _rootLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _rootLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _rootLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

        BuildDuckDnsGroup();
        BuildUpdateGroup();
        BuildButtonPanel();
        BuildStatusGroup();

        _rootLayout.Controls.Add(_grpDuckDns, 0, 0);
        _rootLayout.Controls.Add(_grpUpdate, 0, 1);
        _rootLayout.Controls.Add(_buttonPanel, 0, 2);
        _rootLayout.Controls.Add(_grpStatus, 0, 3);

        Controls.Add(_rootLayout);

        ((System.ComponentModel.ISupportInitialize)_numInterval).EndInit();
        _buttonPanel.ResumeLayout(false);
        _buttonPanel.PerformLayout();
        _grpStatus.ResumeLayout(false);
        _grpUpdate.ResumeLayout(false);
        _grpDuckDns.ResumeLayout(false);
        _rootLayout.ResumeLayout(false);
        _rootLayout.PerformLayout();
        ResumeLayout(false);
    }

    private void BuildDuckDnsGroup()
    {
        _grpDuckDns.Text = "DuckDNS";
        _grpDuckDns.Dock = DockStyle.Fill;
        _grpDuckDns.AutoSize = true;
        _grpDuckDns.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        _grpDuckDns.Padding = new Padding(10, 8, 10, 10);
        _grpDuckDns.Margin = new Padding(0, 0, 0, 8);

        var layout = CreateTwoColumnLayout();
        layout.RowCount = 2;
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 32F));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 32F));

        var lblDomain = CreateFieldLabel("Domain:");
        var lblToken = CreateFieldLabel("Token:");

        _txtDomain.Name = "Domain";
        _txtDomain.AccessibleName = "Domain";
        _txtDomain.Dock = DockStyle.Fill;
        _txtDomain.Margin = new Padding(0, 4, 0, 4);

        _txtToken.Name = "Token";
        _txtToken.AccessibleName = "Token";
        _txtToken.Dock = DockStyle.Fill;
        _txtToken.Margin = new Padding(0, 4, 0, 4);
        _txtToken.UseSystemPasswordChar = true;

        layout.Controls.Add(lblDomain, 0, 0);
        layout.Controls.Add(_txtDomain, 1, 0);
        layout.Controls.Add(lblToken, 0, 1);
        layout.Controls.Add(_txtToken, 1, 1);

        _grpDuckDns.Controls.Add(layout);
    }

    private void BuildUpdateGroup()
    {
        _grpUpdate.Text = "Update";
        _grpUpdate.Dock = DockStyle.Fill;
        _grpUpdate.AutoSize = true;
        _grpUpdate.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        _grpUpdate.Padding = new Padding(10, 8, 10, 10);
        _grpUpdate.Margin = new Padding(0, 0, 0, 8);

        var layout = CreateTwoColumnLayout();
        layout.RowCount = 2;
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 32F));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 32F));

        var lblInterval = CreateFieldLabel("Interval (s):");
        var lblAutoStart = CreateFieldLabel("Auto-start:");

        _numInterval.Name = "Interval";
        _numInterval.AccessibleName = "Interval";
        _numInterval.Dock = DockStyle.Left;
        _numInterval.Width = 100;
        _numInterval.Minimum = 30;
        _numInterval.Maximum = 86_400;
        _numInterval.Value = 300;
        _numInterval.Margin = new Padding(0, 4, 0, 4);

        _chkAutoStart.Name = "AutoStart";
        _chkAutoStart.AccessibleName = "Update automatically on startup";
        _chkAutoStart.Text = "Update automatically on startup";
        _chkAutoStart.AutoSize = true;
        _chkAutoStart.Dock = DockStyle.Fill;
        _chkAutoStart.Margin = new Padding(0, 6, 0, 4);
        _chkAutoStart.TextAlign = ContentAlignment.MiddleLeft;

        layout.Controls.Add(lblInterval, 0, 0);
        layout.Controls.Add(_numInterval, 1, 0);
        layout.Controls.Add(lblAutoStart, 0, 1);
        layout.Controls.Add(_chkAutoStart, 1, 1);

        _grpUpdate.Controls.Add(layout);
    }

    private void BuildButtonPanel()
    {
        _buttonPanel.Dock = DockStyle.Fill;
        _buttonPanel.AutoSize = true;
        _buttonPanel.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        _buttonPanel.FlowDirection = FlowDirection.LeftToRight;
        _buttonPanel.WrapContents = false;
        _buttonPanel.Padding = new Padding(0);
        _buttonPanel.Margin = new Padding(0, 0, 0, 8);
        _buttonPanel.Height = 40;

        ConfigureButton(_btnSave, "Save", "Save");
        ConfigureButton(_btnStart, "Start", "Start");
        ConfigureButton(_btnStop, "Stop", "Stop");
        _btnStop.Enabled = false;

        _buttonPanel.Controls.Add(_btnSave);
        _buttonPanel.Controls.Add(_btnStart);
        _buttonPanel.Controls.Add(_btnStop);
    }

    private void BuildStatusGroup()
    {
        _grpStatus.Text = "Status";
        _grpStatus.Dock = DockStyle.Fill;
        _grpStatus.Padding = new Padding(10, 8, 10, 10);
        _grpStatus.Margin = new Padding(0);

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 5,
            Padding = new Padding(0)
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110F));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 24F));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 24F));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 24F));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 28F));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

        var lblStatus = CreateFieldLabel("Status:");
        var lblLast = CreateFieldLabel("Last update:");
        var lblNext = CreateFieldLabel("Next update:");

        ConfigureValueLabel(_lblStatusValue, "Stopped", "StatusValue");
        ConfigureValueLabel(_lblLastUpdateValue, "—", "LastUpdateValue");
        ConfigureValueLabel(_lblNextUpdateValue, "—", "NextUpdateValue");

        _chkWriteLogsToFile.Name = "WriteLogsToFile";
        _chkWriteLogsToFile.AccessibleName = "Write logs to file";
        _chkWriteLogsToFile.Text = "Write logs to file";
        _chkWriteLogsToFile.AutoSize = true;
        _chkWriteLogsToFile.Dock = DockStyle.Fill;
        _chkWriteLogsToFile.Margin = new Padding(0, 2, 0, 2);
        _chkWriteLogsToFile.TextAlign = ContentAlignment.MiddleLeft;

        _txtLog.Name = "Log";
        _txtLog.AccessibleName = "Log";
        _txtLog.Dock = DockStyle.Fill;
        _txtLog.Multiline = true;
        _txtLog.ReadOnly = true;
        _txtLog.ScrollBars = ScrollBars.Vertical;
        _txtLog.Margin = new Padding(0, 4, 0, 0);
        _txtLog.BackColor = Color.White;

        layout.Controls.Add(lblStatus, 0, 0);
        layout.Controls.Add(_lblStatusValue, 1, 0);
        layout.Controls.Add(lblLast, 0, 1);
        layout.Controls.Add(_lblLastUpdateValue, 1, 1);
        layout.Controls.Add(lblNext, 0, 2);
        layout.Controls.Add(_lblNextUpdateValue, 1, 2);
        layout.Controls.Add(_chkWriteLogsToFile, 0, 3);
        layout.SetColumnSpan(_chkWriteLogsToFile, 2);
        layout.Controls.Add(_txtLog, 0, 4);
        layout.SetColumnSpan(_txtLog, 2);

        _grpStatus.Controls.Add(layout);
    }

    private void ApplyApplicationIcon()
    {
        try
        {
            var exePath = Application.ExecutablePath;
            if (string.IsNullOrWhiteSpace(exePath) || !File.Exists(exePath))
            {
                return;
            }

            var icon = Icon.ExtractAssociatedIcon(exePath);
            if (icon is not null)
            {
                Icon = icon;
            }
        }
        catch
        {
            // Keep the default window icon if extraction fails.
        }
    }

    private static TableLayoutPanel CreateTwoColumnLayout()
    {
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 2,
            Padding = new Padding(0)
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110F));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        return layout;
    }

    private static Label CreateFieldLabel(string text) => new()
    {
        Text = text,
        Dock = DockStyle.Fill,
        TextAlign = ContentAlignment.MiddleLeft,
        AutoSize = false,
        Margin = new Padding(0, 0, 8, 0)
    };

    private static void ConfigureValueLabel(Label label, string text, string automationId)
    {
        // Name maps to AutomationId; do not set AccessibleName so UIA Name stays as Text.
        label.Name = automationId;
        label.Text = text;
        label.Dock = DockStyle.Fill;
        label.TextAlign = ContentAlignment.MiddleLeft;
        label.AutoEllipsis = true;
        label.Margin = new Padding(0);
    }

    private static void ConfigureButton(Button button, string text, string automationId)
    {
        button.Name = automationId;
        button.AccessibleName = text;
        button.Text = text;
        button.Size = new Size(100, 32);
        button.Margin = new Padding(0, 0, 8, 0);
        button.UseVisualStyleBackColor = true;
    }
}
