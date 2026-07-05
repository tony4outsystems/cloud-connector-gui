using System.Collections.ObjectModel;
using Avalonia.Controls;
using CloudConnectorGui.App;
using CloudConnectorGui.Core;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CloudConnectorGui.Views;

namespace CloudConnectorGui.ViewModels;

public sealed partial class MainWindowViewModel : ObservableObject
{
    private readonly MainWindowController controller = new();
    private readonly MainWindowState state = new();
    private readonly string logFilePath = Path.Combine(GuiPaths.GetAppDataDirectory(), "cloud-connector-gui.log");
    private bool logFileErrorShown;

    public Window? OwnerWindow { get; set; }

    public bool IsMacOS { get; } = OperatingSystem.IsMacOS();

    public IReadOnlyList<string> SelfUpdateIntervalOptions { get; } = SelfUpdateIntervals.All;

    public ObservableCollection<EndpointRowViewModel> Endpoints { get; } = [];

    [ObservableProperty]
    private string address = string.Empty;

    [ObservableProperty]
    private string token = string.Empty;

    [ObservableProperty]
    private string proxy = string.Empty;

    [ObservableProperty]
    private bool verbose;

    [ObservableProperty]
    private string selfUpdateCheckInterval = SelfUpdateIntervals.Daily;

    [ObservableProperty]
    private string statusText = "Stopped";

    [ObservableProperty]
    private string binaryVersionText = "Connector binary: not checked";

    [ObservableProperty]
    private string selfUpdateBannerText = string.Empty;

    [ObservableProperty]
    private bool isSelfUpdateBannerVisible;

    [ObservableProperty]
    private string logText = string.Empty;

    [ObservableProperty]
    private bool canStart = true;

    [ObservableProperty]
    private bool canStop;

    [ObservableProperty]
    private bool canEditConfiguration = true;

    [ObservableProperty]
    private bool canUpdateBinary = true;

    [ObservableProperty]
    private bool canApplySelfUpdate;

    [ObservableProperty]
    private bool isServiceModeEnabled;

    [ObservableProperty]
    private bool canInstallService;

    [ObservableProperty]
    private bool canStartService;

    [ObservableProperty]
    private bool canStopService;

    [ObservableProperty]
    private bool canRestartService;

    [ObservableProperty]
    private string serviceStatusText = "Not installed";

    public bool IsServiceModeSupported { get; private set; }

    public MainWindowViewModel()
    {
        controller.LogRequested += line => AppendLog(line);
        controller.ConnectorExited += exitCode =>
        {
            AppendLog($"outsystemscc exited with code {exitCode}");
            state.SetRunning(false);
            RenderState();
        };
        IsServiceModeSupported = controller.IsServiceModeSupported;
    }

    public bool IsConnectorRunning => controller.IsConnectorRunning;

    public void Initialize()
    {
        LoadConfiguration();
    }

    public async Task OnShownAsync()
    {
        await EnsureBinaryInstalledAsync().ConfigureAwait(true);
        await RefreshBinaryVersionAsync().ConfigureAwait(true);
        await CheckSelfUpdateAsync().ConfigureAwait(true);
        RunPendingServiceAction();
    }

    private void RunPendingServiceAction()
    {
        var pendingAction = PendingServiceActions.TryParse(GuiApplication.StartupArgs);
        if (pendingAction is null)
        {
            return;
        }

        switch (pendingAction.Value)
        {
            case PendingServiceAction.Install:
                PerformInstallService();
                break;
            case PendingServiceAction.Uninstall:
                PerformUninstallService();
                break;
            case PendingServiceAction.Start:
                PerformStartService();
                break;
            case PendingServiceAction.Stop:
                PerformStopService();
                break;
            case PendingServiceAction.Restart:
                PerformRestartService();
                break;
        }
    }

    public void Save()
    {
        SaveConfiguration();
    }

    private void LoadConfiguration()
    {
        try
        {
            state.ApplyConfiguration(controller.LoadConfiguration());
            controller.RefreshServiceState(state);
            ApplyStateToProperties();
            RenderState();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or FormatException)
        {
            AppendLog($"Configuration load failed: {ex.Message}");
        }
    }

    private void SaveConfiguration()
    {
        try
        {
            CaptureStateFromProperties();
            controller.SaveConfiguration(state);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            AppendLog($"Configuration save failed: {ex.Message}");
        }
    }

    private void CaptureStateFromProperties()
    {
        state.Address = Address;
        state.Token = Token;
        state.Proxy = Proxy;
        state.Verbose = Verbose;
        state.SelfUpdateCheckInterval = SelfUpdateCheckInterval;
        state.Endpoints.Clear();
        state.Endpoints.AddRange(ReadEndpoints());
    }

    private void ApplyStateToProperties()
    {
        Address = state.Address;
        Token = state.Token;
        Proxy = state.Proxy;
        Verbose = state.Verbose;
        SelfUpdateCheckInterval = state.SelfUpdateCheckInterval;

        Endpoints.Clear();
        foreach (var endpoint in state.Endpoints)
        {
            Endpoints.Add(new EndpointRowViewModel(endpoint));
        }
    }

    private IReadOnlyList<Endpoint> ReadEndpoints()
    {
        return Endpoints
            .Where(row => !row.IsEmpty)
            .Select(row => row.ToEndpoint())
            .ToList();
    }

    [RelayCommand]
    private async Task StartConnectorAsync()
    {
        CaptureStateFromProperties();
        var validationErrors = controller.ValidateLaunchOptions(state);
        if (validationErrors.Count > 0)
        {
            await ShowMessageAsync(string.Join(Environment.NewLine, validationErrors), "Cannot start connector").ConfigureAwait(true);
            return;
        }

        try
        {
            if (!File.Exists(controller.ConnectorExecutablePath))
            {
                await ShowMessageAsync("The connector binary is not installed yet. Use Download / Update Binary first.", "Cannot start connector").ConfigureAwait(true);
                return;
            }

            LogText = string.Empty;
            AppendLog(controller.GetDisplayCommand(state));
            controller.StartConnector(state);
            RenderState();
        }
        catch (Exception ex) when (ex is IOException or InvalidOperationException or ArgumentException)
        {
            await ShowMessageAsync(ex.Message, "Cannot start connector").ConfigureAwait(true);
            state.SetRunning(false);
            RenderState();
        }
    }

    [RelayCommand]
    private async Task StopConnectorAsync()
    {
        CanStop = false;
        AppendLog("Stopping outsystemscc...");
        await controller.StopConnectorAsync(state).ConfigureAwait(true);
        RenderState();
    }

    [RelayCommand]
    private async Task InstallOrUpdateBinaryAsync()
    {
        if (controller.IsConnectorRunning)
        {
            await ShowMessageAsync("Stop the connector before updating the binary.", "Connector is running").ConfigureAwait(true);
            return;
        }

        CanUpdateBinary = false;
        CanStart = false;
        var previousStatus = StatusText;
        try
        {
            var progress = new Progress<string>(message =>
            {
                StatusText = message;
                AppendLog(message);
            });

            var result = await controller.InstallOrUpdateBinaryAsync(force: true, progress).ConfigureAwait(true);

            if (result.Installed)
            {
                AppendLog($"Installed outsystemscc {result.Version}.");
            }

            await RefreshBinaryVersionAsync().ConfigureAwait(true);
        }
        catch (Exception ex) when (ex is HttpRequestException or IOException or InvalidOperationException or UnauthorizedAccessException)
        {
            AppendLog($"Binary install failed: {ex.Message}");
            await ShowMessageAsync(ex.Message, "Cannot install connector binary").ConfigureAwait(true);
        }
        finally
        {
            StatusText = previousStatus;
            state.SetRunning(controller.IsConnectorRunning);
            RenderState();
        }
    }

    private async Task EnsureBinaryInstalledAsync()
    {
        if (File.Exists(controller.ConnectorExecutablePath))
        {
            return;
        }

        CanUpdateBinary = false;
        CanStart = false;
        var previousStatus = StatusText;
        try
        {
            var progress = new Progress<string>(message =>
            {
                StatusText = message;
                AppendLog(message);
            });

            var result = await controller.InstallOrUpdateBinaryAsync(force: false, progress).ConfigureAwait(true);

            if (result.Installed)
            {
                AppendLog($"Installed outsystemscc {result.Version}.");
            }
        }
        catch (Exception ex) when (ex is HttpRequestException or IOException or InvalidOperationException or UnauthorizedAccessException)
        {
            AppendLog($"Binary install failed: {ex.Message}");
        }
        finally
        {
            StatusText = previousStatus;
            state.SetRunning(controller.IsConnectorRunning);
            RenderState();
        }
    }

    private async Task RefreshBinaryVersionAsync()
    {
        CanUpdateBinary = false;
        try
        {
            await controller.RefreshBinaryVersionAsync(state).ConfigureAwait(true);
            RenderState();
        }
        catch (Exception ex) when (ex is HttpRequestException or IOException or InvalidOperationException)
        {
            controller.SetBinaryVersionUnavailable(state);
            RenderState();
            AppendLog($"Version check failed: {ex.Message}");
        }
        finally
        {
            CanUpdateBinary = !controller.IsConnectorRunning;
        }
    }

    private async Task CheckSelfUpdateAsync()
    {
        CaptureStateFromProperties();
        try
        {
            await controller.CheckSelfUpdateAsync(state).ConfigureAwait(true);
            RenderState();
        }
        catch (Exception ex) when (ex is HttpRequestException or IOException or InvalidOperationException or UnauthorizedAccessException)
        {
            AppendLog($"GUI update check failed: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task ApplySelfUpdateAsync()
    {
        if (state.AvailableSelfUpdate is null)
        {
            return;
        }

        if (controller.IsConnectorRunning)
        {
            await ShowMessageAsync("Stop the connector before updating the GUI.", "Connector is running").ConfigureAwait(true);
            return;
        }

        var previousStatus = StatusText;
        try
        {
            var progress = new Progress<string>(message =>
            {
                StatusText = message;
                AppendLog(message);
            });

            CaptureStateFromProperties();
            await controller.ApplySelfUpdateAsync(state, progress).ConfigureAwait(true);
        }
        catch (Exception ex) when (ex is HttpRequestException or IOException or InvalidOperationException or UnauthorizedAccessException)
        {
            AppendLog($"GUI update failed: {ex.Message}");
            await ShowMessageAsync(ex.Message, "Cannot update GUI").ConfigureAwait(true);
            StatusText = previousStatus;
            RenderState();
        }
    }

    [RelayCommand]
    private async Task ToggleServiceModeAsync()
    {
        if (IsServiceModeEnabled)
        {
            var installed = await InstallServiceFlowAsync().ConfigureAwait(true);
            if (!installed)
            {
                IsServiceModeEnabled = false;
            }
        }
        else
        {
            var uninstalled = await UninstallServiceFlowAsync().ConfigureAwait(true);
            if (!uninstalled)
            {
                IsServiceModeEnabled = true;
            }
        }
    }

    private async Task<bool> InstallServiceFlowAsync()
    {
        CaptureStateFromProperties();
        var validationErrors = controller.ValidateLaunchOptions(state);
        if (validationErrors.Count > 0)
        {
            await ShowMessageAsync(string.Join(Environment.NewLine, validationErrors), "Cannot install service").ConfigureAwait(true);
            return false;
        }

        var confirmed = await ConfirmDialogWindow.ShowAsync(
            OwnerWindow!,
            "Install as Windows Service",
            "The service will be installed with the current configuration. You cannot change the configuration when the service is installed. Make sure to validate it first before installing the service. Clicking the switch again will uninstall the service.",
            confirmText: "Install").ConfigureAwait(true);
        if (!confirmed)
        {
            return false;
        }

        controller.SaveConfiguration(state);

        if (!ElevationHelper.IsElevated)
        {
            return await RelaunchElevatedAsync(PendingServiceAction.Install).ConfigureAwait(true);
        }

        return PerformInstallService();
    }

    private async Task<bool> UninstallServiceFlowAsync()
    {
        var confirmed = await ConfirmDialogWindow.ShowAsync(
            OwnerWindow!,
            "Uninstall Windows Service",
            "This will stop and remove the Windows Service and delete the copied connector binary. Continue?",
            confirmText: "Uninstall").ConfigureAwait(true);
        if (!confirmed)
        {
            return false;
        }

        if (!ElevationHelper.IsElevated)
        {
            return await RelaunchElevatedAsync(PendingServiceAction.Uninstall).ConfigureAwait(true);
        }

        return PerformUninstallService();
    }

    [RelayCommand]
    private async Task StartServiceAsync()
    {
        if (!ElevationHelper.IsElevated)
        {
            await RelaunchElevatedAsync(PendingServiceAction.Start).ConfigureAwait(true);
            return;
        }

        PerformStartService();
    }

    [RelayCommand]
    private async Task StopServiceAsync()
    {
        if (!ElevationHelper.IsElevated)
        {
            await RelaunchElevatedAsync(PendingServiceAction.Stop).ConfigureAwait(true);
            return;
        }

        PerformStopService();
    }

    [RelayCommand]
    private async Task RestartServiceAsync()
    {
        if (!ElevationHelper.IsElevated)
        {
            await RelaunchElevatedAsync(PendingServiceAction.Restart).ConfigureAwait(true);
            return;
        }

        PerformRestartService();
    }

    private bool PerformInstallService()
    {
        try
        {
            AppendLog("Installing Windows Service...");
            controller.InstallService(state);
            AppendLog("Windows Service installed and started.");
            RenderState();
            return true;
        }
        catch (Exception ex) when (ex is InvalidOperationException or IOException or UnauthorizedAccessException or FileNotFoundException)
        {
            AppendLog($"Service install failed: {ex.Message}");
            TryRollbackService();
            RenderState();
            return false;
        }
    }

    private bool PerformUninstallService()
    {
        try
        {
            AppendLog("Uninstalling Windows Service...");
            controller.UninstallService(state);
            AppendLog("Windows Service uninstalled.");
            RenderState();
            return true;
        }
        catch (Exception ex) when (ex is InvalidOperationException or IOException or UnauthorizedAccessException)
        {
            AppendLog($"Service uninstall failed: {ex.Message}");
            RenderState();
            return false;
        }
    }

    private void PerformStartService()
    {
        try
        {
            AppendLog("Starting Windows Service...");
            controller.StartService(state);
            AppendLog("Windows Service started.");
        }
        catch (Exception ex) when (ex is InvalidOperationException or System.ServiceProcess.TimeoutException)
        {
            AppendLog($"Service start failed: {ex.Message}");
        }
        finally
        {
            RenderState();
        }
    }

    private void PerformStopService()
    {
        try
        {
            AppendLog("Stopping Windows Service...");
            controller.StopService(state);
            AppendLog("Windows Service stopped.");
        }
        catch (Exception ex) when (ex is InvalidOperationException or System.ServiceProcess.TimeoutException)
        {
            AppendLog($"Service stop failed: {ex.Message}");
        }
        finally
        {
            RenderState();
        }
    }

    private void PerformRestartService()
    {
        try
        {
            AppendLog("Restarting Windows Service...");
            controller.RestartService(state);
            AppendLog("Windows Service restarted.");
        }
        catch (Exception ex) when (ex is InvalidOperationException or System.ServiceProcess.TimeoutException)
        {
            AppendLog($"Service restart failed: {ex.Message}");
        }
        finally
        {
            RenderState();
        }
    }

    private void TryRollbackService()
    {
        try
        {
            controller.UninstallService(state);
        }
        catch (Exception ex) when (ex is InvalidOperationException or IOException or UnauthorizedAccessException)
        {
            AppendLog($"Service rollback failed: {ex.Message}");
        }
    }

    private async Task<bool> RelaunchElevatedAsync(PendingServiceAction action)
    {
        if (!OperatingSystem.IsWindows())
        {
            return false;
        }

        await ShowMessageAsync(
            "Cloud Connector GUI needs to restart with administrator rights to continue.",
            "Administrator rights required").ConfigureAwait(true);

        SingleInstanceGuard.Release();
        var launched = ElevationHelper.TryRelaunchElevated(action);
        if (launched)
        {
            Environment.Exit(0);
            return true;
        }

        SingleInstanceGuard.TryAcquire();
        await ShowMessageAsync(
            "Administrator rights were not granted. No changes were made.",
            "Cannot continue").ConfigureAwait(true);
        return false;
    }

    [RelayCommand]
    private async Task OpenConfigurationAsync()
    {
        if (OwnerWindow is not null)
        {
            await ConfigurationWindow.ShowAsync(OwnerWindow, this).ConfigureAwait(true);
        }
    }

    [RelayCommand]
    private async Task OpenAboutAsync()
    {
        if (OwnerWindow is not null)
        {
            await AboutWindow.ShowAsync(OwnerWindow).ConfigureAwait(true);
        }
    }

    [RelayCommand]
    private void HideSelfUpdateBanner()
    {
        state.HideSelfUpdate();
        RenderState();
    }

    [RelayCommand]
    private void AddEndpoint()
    {
        Endpoints.Add(new EndpointRowViewModel());
    }

    [RelayCommand]
    private void RemoveEndpoint(EndpointRowViewModel? row)
    {
        if (row is not null)
        {
            Endpoints.Remove(row);
        }
    }

    private void RenderState()
    {
        CanStart = state.CanStart;
        CanStop = state.CanStop;
        StatusText = state.StatusText;
        BinaryVersionText = state.BinaryVersionText;
        CanEditConfiguration = state.CanEditConfiguration;
        CanUpdateBinary = state.CanUpdateBinary;
        CanApplySelfUpdate = state.CanApplySelfUpdate;
        SelfUpdateBannerText = state.SelfUpdateBannerText;
        IsSelfUpdateBannerVisible = state.IsSelfUpdateBannerVisible;
        IsServiceModeEnabled = state.IsServiceModeEnabled;
        CanInstallService = state.CanInstallService;
        CanStartService = state.CanStartService;
        CanStopService = state.CanStopService;
        CanRestartService = state.CanRestartService;
        ServiceStatusText = state.ServiceState switch
        {
            ServiceRunState.NotInstalled => "Not installed",
            ServiceRunState.Stopped => "Stopped",
            ServiceRunState.StartPending => "Starting...",
            ServiceRunState.Running => "Running",
            ServiceRunState.StopPending => "Stopping...",
            _ => "Unknown"
        };
    }

    private void AppendLog(string line)
    {
        var timestamp = DateTime.Now;
        LogText += $"[{timestamp:HH:mm:ss}] {line}{Environment.NewLine}";

        try
        {
            File.AppendAllText(logFilePath, $"[{timestamp:yyyy-MM-dd HH:mm:ss}] {line}{Environment.NewLine}");
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            if (logFileErrorShown)
            {
                return;
            }

            logFileErrorShown = true;
            LogText += $"[{DateTime.Now:HH:mm:ss}] Could not write log file {logFilePath}: {ex.Message}{Environment.NewLine}";
        }
    }

    private async Task ShowMessageAsync(string message, string title)
    {
        await MessageDialogWindow.ShowAsync(OwnerWindow, title, message).ConfigureAwait(true);
    }
}
