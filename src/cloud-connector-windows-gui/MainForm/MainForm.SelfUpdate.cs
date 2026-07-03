namespace CloudConnectorWindowsGui;

internal sealed partial class MainForm
{
    private async Task CheckSelfUpdateAsync()
    {
        var configuration = ReadConfiguration(ReadOptions());
        if (!IsSelfUpdateCheckDue(configuration))
        {
            return;
        }

        try
        {
            var status = await selfUpdateManager.GetUpdateStatusAsync().ConfigureAwait(true);
            lastSelfUpdateCheck = DateOnly.FromDateTime(DateTime.UtcNow);
            SaveConfiguration();

            if (status.IsUpdateAvailable)
            {
                ShowSelfUpdateBanner(status);
            }
        }
        catch (Exception ex) when (ex is HttpRequestException or IOException or InvalidOperationException or UnauthorizedAccessException)
        {
            AppendLog($"GUI update check failed: {ex.Message}");
        }
    }

    private async Task ApplySelfUpdateAsync()
    {
        if (availableSelfUpdate is null)
        {
            return;
        }

        if (connector.IsRunning)
        {
            MessageBox.Show("Stop the connector before updating the GUI.", "Connector is running", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        selfUpdateButton.Enabled = false;
        dismissSelfUpdateButton.Enabled = false;
        var previousStatus = statusLabel.Text;
        try
        {
            var progress = new Progress<string>(message =>
            {
                statusLabel.Text = message;
                AppendLog(message);
            });

            SaveConfiguration();
            await selfUpdateManager.ApplyUpdateAndRestartAsync(availableSelfUpdate, progress).ConfigureAwait(true);
        }
        catch (Exception ex) when (ex is HttpRequestException or IOException or InvalidOperationException or UnauthorizedAccessException)
        {
            AppendLog($"GUI update failed: {ex.Message}");
            MessageBox.Show(ex.Message, "Cannot update GUI", MessageBoxButtons.OK, MessageBoxIcon.Error);
            selfUpdateButton.Enabled = true;
            dismissSelfUpdateButton.Enabled = true;
            statusLabel.Text = previousStatus;
        }
    }

    private void ShowSelfUpdateBanner(SelfUpdateStatus status)
    {
        availableSelfUpdate = status;
        selfUpdateBannerLabel.Text = $"GUI update available: {status.CurrentVersion} -> {status.LatestVersion}";
        selfUpdateButton.Enabled = !connector.IsRunning;
        dismissSelfUpdateButton.Enabled = true;
        selfUpdateBanner.Visible = true;
        ApplyMinimumSize();
    }

    private void HideSelfUpdateBanner()
    {
        selfUpdateBanner.Visible = false;
        ApplyMinimumSize();
    }

    private static bool IsSelfUpdateCheckDue(GuiConfiguration configuration)
    {
        if (configuration.SelfUpdateCheckInterval == SelfUpdateIntervals.Off)
        {
            return false;
        }

        if (configuration.LastSelfUpdateCheck is null)
        {
            return true;
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var nextCheck = configuration.SelfUpdateCheckInterval switch
        {
            SelfUpdateIntervals.Weekly => configuration.LastSelfUpdateCheck.Value.AddDays(7),
            SelfUpdateIntervals.Monthly => configuration.LastSelfUpdateCheck.Value.AddMonths(1),
            _ => configuration.LastSelfUpdateCheck.Value.AddDays(1)
        };

        return today >= nextCheck;
    }
}
