using CloudConnectorWindowsGui.App;
using CloudConnectorWindowsGui.Core;

namespace CloudConnectorWindowsGui;

internal sealed partial class MainForm
{
    private void StartConnector()
    {
        CaptureStateFromControls();
        var validationErrors = controller.ValidateLaunchOptions(state);
        if (validationErrors.Count > 0)
        {
            MessageBox.Show(string.Join(Environment.NewLine, validationErrors), "Cannot start connector", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        try
        {
            if (!File.Exists(controller.ConnectorExecutablePath))
            {
                MessageBox.Show("The connector binary is not installed yet. Use Download / Update Binary first.", "Cannot start connector", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            logTextBox.Clear();
            AppendLog(controller.GetDisplayCommand(state));
            controller.StartConnector(state);
            RenderState();
        }
        catch (Exception ex) when (ex is IOException or InvalidOperationException or ArgumentException)
        {
            MessageBox.Show(ex.Message, "Cannot start connector", MessageBoxButtons.OK, MessageBoxIcon.Error);
            state.SetRunning(false);
            RenderState();
        }
    }

    private async Task StopConnectorAsync()
    {
        stopButton.Enabled = false;
        AppendLog("Stopping outsystemscc...");
        await controller.StopConnectorAsync(state).ConfigureAwait(true);
        RenderState();
    }

    private async Task InstallOrUpdateBinaryAsync(bool force)
    {
        if (controller.IsConnectorRunning)
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
                ? await controller.InstallOrUpdateBinaryAsync(force: true, progress).ConfigureAwait(true)
                : await controller.InstallOrUpdateBinaryAsync(force: false, progress).ConfigureAwait(true);

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
            state.SetRunning(controller.IsConnectorRunning);
            RenderState();
        }
    }

    private async Task RefreshBinaryVersionAsync()
    {
        updateBinaryButton.Enabled = false;
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
            updateBinaryButton.Enabled = !controller.IsConnectorRunning;
        }
    }
}
