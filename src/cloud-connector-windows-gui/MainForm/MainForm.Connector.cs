using CloudConnectorWindowsGui.Core;

namespace CloudConnectorWindowsGui;

internal sealed partial class MainForm
{
    private void StartConnector()
    {
        var options = ReadOptions();
        var validationErrors = ConnectorValidator.Validate(options);
        if (validationErrors.Count > 0)
        {
            MessageBox.Show(string.Join(Environment.NewLine, validationErrors), "Cannot start connector", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        try
        {
            SaveConfiguration(options);

            if (!File.Exists(binaryManager.ExecutablePath))
            {
                MessageBox.Show("The connector binary is not installed yet. Use Download / Update Binary first.", "Cannot start connector", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            logTextBox.Clear();
            AppendLog(ConnectorArguments.ToDisplayCommand("outsystemscc.exe", options));
            connector.Start(binaryManager.ExecutablePath, options);
            SetRunningState(true);
        }
        catch (Exception ex) when (ex is IOException or InvalidOperationException or ArgumentException)
        {
            MessageBox.Show(ex.Message, "Cannot start connector", MessageBoxButtons.OK, MessageBoxIcon.Error);
            SetRunningState(false);
        }
    }

    private async Task StopConnectorAsync()
    {
        stopButton.Enabled = false;
        AppendLog("Stopping outsystemscc...");
        await connector.StopAsync().ConfigureAwait(true);
        SetRunningState(false);
    }

    private async Task InstallOrUpdateBinaryAsync(bool force)
    {
        if (connector.IsRunning)
        {
            MessageBox.Show("Stop the connector before updating the binary.", "Connector is running", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        updateBinaryButton.Enabled = false;
        startButton.Enabled = false;
        var previousStatus = statusLabel.Text;
        try
        {
            var progress = new Progress<string>(message =>
            {
                statusLabel.Text = message;
                AppendLog(message);
            });

            var result = force
                ? await binaryManager.InstallLatestAsync(progress).ConfigureAwait(true)
                : await binaryManager.EnsureInstalledAsync(progress).ConfigureAwait(true);

            if (result.Installed)
            {
                AppendLog($"Installed outsystemscc {result.Version}.");
            }

            await RefreshBinaryVersionAsync().ConfigureAwait(true);
        }
        catch (Exception ex) when (ex is HttpRequestException or IOException or InvalidOperationException or UnauthorizedAccessException)
        {
            AppendLog($"Binary install failed: {ex.Message}");
            if (force)
            {
                MessageBox.Show(ex.Message, "Cannot install connector binary", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        finally
        {
            statusLabel.Text = previousStatus;
            SetRunningState(connector.IsRunning);
        }
    }

    private async Task RefreshBinaryVersionAsync()
    {
        updateBinaryButton.Enabled = false;
        try
        {
            var status = await binaryManager.GetVersionStatusAsync().ConfigureAwait(true);
            var current = status.CurrentVersion ?? "not installed";
            var latest = status.LatestVersion;
            var suffix = status.IsLatest ? "up to date" : "update available";
            binaryVersionLabel.Text = $"current {current} / latest {latest} ({suffix})";
        }
        catch (Exception ex) when (ex is HttpRequestException or IOException or InvalidOperationException)
        {
            var current = binaryManager.InstalledVersion ?? "not installed";
            binaryVersionLabel.Text = $"current {current} / latest unavailable";
            AppendLog($"Version check failed: {ex.Message}");
        }
        finally
        {
            updateBinaryButton.Enabled = !connector.IsRunning;
        }
    }
}
