using CloudConnectorWindowsGui.App;
using CloudConnectorWindowsGui.Core;

namespace CloudConnectorWindowsGui;

internal sealed partial class MainForm
{
    private void CaptureStateFromControls()
    {
        state.Address = addressTextBox.Text;
        state.Token = tokenTextBox.Text;
        state.Proxy = proxyTextBox.Text;
        state.Verbose = verboseCheckBox.Checked;
        state.SelfUpdateCheckInterval = Convert.ToString(selfUpdateCheckIntervalComboBox.SelectedItem) ?? SelfUpdateIntervals.Daily;
        state.Endpoints.Clear();
        state.Endpoints.AddRange(ReadEndpoints());
    }

    private void LoadConfiguration()
    {
        try
        {
            state.ApplyConfiguration(controller.LoadConfiguration());
            ApplyConfigurationToControls();
            RenderState();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or FormatException)
        {
            AppendLog($"Configuration load failed: {ex.Message}");
        }
    }

    private void ApplyConfigurationToControls()
    {
        addressTextBox.Text = state.Address;
        tokenTextBox.Text = state.Token;
        proxyTextBox.Text = state.Proxy;
        verboseCheckBox.Checked = state.Verbose;
        selfUpdateCheckIntervalComboBox.SelectedItem = state.SelfUpdateCheckInterval;
        if (selfUpdateCheckIntervalComboBox.SelectedItem is null)
        {
            selfUpdateCheckIntervalComboBox.SelectedItem = SelfUpdateIntervals.Daily;
        }

        endpointsGrid.Rows.Clear();

        foreach (var endpoint in state.Endpoints)
        {
            endpointsGrid.Rows.Add(endpoint.LocalPort, endpoint.RemoteHost, endpoint.RemotePort);
        }
    }

    private void SaveConfiguration()
    {
        try
        {
            CaptureStateFromControls();
            controller.SaveConfiguration(state);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            AppendLog($"Configuration save failed: {ex.Message}");
        }
    }

    private IReadOnlyList<Endpoint> ReadEndpoints()
    {
        var endpoints = new List<Endpoint>();
        foreach (DataGridViewRow row in endpointsGrid.Rows)
        {
            if (row.IsNewRow)
            {
                continue;
            }

            var localPort = Convert.ToString(row.Cells["LocalPort"].Value) ?? string.Empty;
            var remoteHost = Convert.ToString(row.Cells["RemoteHost"].Value) ?? string.Empty;
            var remotePort = Convert.ToString(row.Cells["RemotePort"].Value) ?? string.Empty;

            if (string.IsNullOrWhiteSpace(localPort) && string.IsNullOrWhiteSpace(remoteHost) && string.IsNullOrWhiteSpace(remotePort))
            {
                continue;
            }

            endpoints.Add(new Endpoint(localPort, remoteHost, remotePort));
        }

        return endpoints;
    }
}
