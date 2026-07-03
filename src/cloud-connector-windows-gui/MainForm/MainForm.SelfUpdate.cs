using CloudConnectorWindowsGui.App;

namespace CloudConnectorWindowsGui;

internal sealed partial class MainForm
{
    private async Task CheckSelfUpdateAsync()
    {
        CaptureStateFromControls();
        try
        {
            await controller.CheckSelfUpdateAsync(state).ConfigureAwait(true);
            RenderState();
            ApplyMinimumSize();
        }
        catch (Exception ex) when (ex is HttpRequestException or IOException or InvalidOperationException or UnauthorizedAccessException)
        {
            AppendLog($"GUI update check failed: {ex.Message}");
        }
    }

    private async Task ApplySelfUpdateAsync()
    {
        if (state.AvailableSelfUpdate is null)
        {
            return;
        }

        if (controller.IsConnectorRunning)
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

            CaptureStateFromControls();
            await controller.ApplySelfUpdateAsync(state, progress).ConfigureAwait(true);
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

    private void HideSelfUpdateBanner()
    {
        state.HideSelfUpdate();
        RenderState();
        ApplyMinimumSize();
    }
}
