using CloudConnectorWindowsGui.Core;

namespace CloudConnectorWindowsGui;

internal sealed partial class MainForm
{
    private LaunchOptions ReadOptions()
    {
        return new LaunchOptions(
            addressTextBox.Text,
            tokenTextBox.Text,
            ReadEndpoints(),
            proxyTextBox.Text,
            verboseCheckBox.Checked);
    }

    private void LoadConfiguration()
    {
        try
        {
            ApplyConfiguration(configurationStore.Load());
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or FormatException)
        {
            AppendLog($"Configuration load failed: {ex.Message}");
        }
    }

    private void ApplyConfiguration(GuiConfiguration configuration)
    {
        addressTextBox.Text = configuration.Address;
        tokenTextBox.Text = configuration.Token;
        proxyTextBox.Text = configuration.Proxy;
        verboseCheckBox.Checked = configuration.Verbose;
        selfUpdateCheckIntervalComboBox.SelectedItem = configuration.SelfUpdateCheckInterval;
        if (selfUpdateCheckIntervalComboBox.SelectedItem is null)
        {
            selfUpdateCheckIntervalComboBox.SelectedItem = "daily";
        }

        lastSelfUpdateCheck = configuration.LastSelfUpdateCheck;
        endpointsGrid.Rows.Clear();

        foreach (var endpoint in configuration.Endpoints)
        {
            endpointsGrid.Rows.Add(endpoint.LocalPort, endpoint.RemoteHost, endpoint.RemotePort);
        }
    }

    private void SaveConfiguration()
    {
        SaveConfiguration(ReadOptions());
    }

    private void SaveConfiguration(LaunchOptions options)
    {
        try
        {
            configurationStore.Save(ReadConfiguration(options));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            AppendLog($"Configuration save failed: {ex.Message}");
        }
    }

    private GuiConfiguration ReadConfiguration(LaunchOptions options)
    {
        return GuiConfiguration.FromLaunchOptions(options, new GuiConfiguration
        {
            SelfUpdateCheckInterval = Convert.ToString(selfUpdateCheckIntervalComboBox.SelectedItem) ?? "daily",
            LastSelfUpdateCheck = lastSelfUpdateCheck
        });
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
