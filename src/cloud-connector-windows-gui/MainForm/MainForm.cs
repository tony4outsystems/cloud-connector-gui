using CloudConnectorWindowsGui.App;

namespace CloudConnectorWindowsGui;

internal sealed partial class MainForm : Form
{
    private const int MinimumEndpointsGridHeight = 220;
    private const int MinimumLogHeight = 240;

    private readonly TextBox addressTextBox = new();
    private readonly TextBox tokenTextBox = new();
    private readonly TextBox proxyTextBox = new();
    private readonly CheckBox verboseCheckBox = new();
    private readonly ComboBox selfUpdateCheckIntervalComboBox = new();
    private readonly Panel selfUpdateBanner = new();
    private readonly Label selfUpdateBannerLabel = new();
    private readonly Button selfUpdateButton = new();
    private readonly Button dismissSelfUpdateButton = new();
    private readonly DataGridView endpointsGrid = new();
    private readonly Button startButton = new();
    private readonly Button stopButton = new();
    private readonly Button updateBinaryButton = new();
    private readonly Label binaryVersionLabel = new();
    private readonly Label statusLabel = new();
    private readonly TextBox logTextBox = new();
    private readonly MainWindowController controller = new();
    private readonly MainWindowState state = new();
    private readonly TableLayoutPanel root = new();
    private readonly string logFilePath = Path.Combine(
        Path.GetDirectoryName(Application.ExecutablePath) ?? AppContext.BaseDirectory,
        "cloud-connector-windows-gui.log");
    private bool logFileErrorShown;

    public MainForm()
    {
        Text = "OutSystems Cloud Connector";
        Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath) ?? Icon;
        Size = new Size(1000, 840);
        MinimumSize = new Size(920, 800);
        StartPosition = FormStartPosition.CenterScreen;
        AutoScaleMode = AutoScaleMode.Dpi;

        BuildLayout();
        WireEvents();
        RenderState();

        Load += (_, _) =>
        {
            LoadConfiguration();
            ApplyMinimumSize();
        };
    }

    private void WireEvents()
    {
        startButton.Click += (_, _) => StartConnector();
        stopButton.Click += async (_, _) => await StopConnectorAsync().ConfigureAwait(true);
        updateBinaryButton.Click += async (_, _) => await InstallOrUpdateBinaryAsync(force: true).ConfigureAwait(true);
        selfUpdateButton.Click += async (_, _) => await ApplySelfUpdateAsync().ConfigureAwait(true);
        dismissSelfUpdateButton.Click += (_, _) => HideSelfUpdateBanner();
        controller.LogRequested += line => BeginInvoke(() => AppendLog(line));
        controller.ConnectorExited += exitCode => BeginInvoke(() =>
        {
            AppendLog($"outsystemscc exited with code {exitCode}");
            state.SetRunning(false);
            RenderState();
        });
        Shown += async (_, _) =>
        {
            await RefreshBinaryVersionAsync().ConfigureAwait(true);
            await CheckSelfUpdateAsync().ConfigureAwait(true);
        };
        FormClosing += async (_, args) =>
        {
            SaveConfiguration();
            if (controller.IsConnectorRunning)
            {
                args.Cancel = true;
                await StopConnectorAsync().ConfigureAwait(true);
                Close();
            }
        };
    }
}
