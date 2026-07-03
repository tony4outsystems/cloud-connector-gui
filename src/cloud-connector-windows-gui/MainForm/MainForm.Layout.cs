namespace CloudConnectorWindowsGui;

internal sealed partial class MainForm
{
    private void ApplyMinimumSize()
    {
        var requiredContentHeight = root.GetPreferredSize(new Size(ClientSize.Width, 0)).Height;
        var chromeHeight = Height - ClientSize.Height;
        var requiredHeight = requiredContentHeight + chromeHeight;

        MinimumSize = new Size(MinimumSize.Width, Math.Max(MinimumSize.Height, requiredHeight));
    }

    private void BuildLayout()
    {
        root.Dock = DockStyle.Fill;
        root.Padding = new Padding(16);
        root.ColumnCount = 1;
        root.RowCount = 7;
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 44));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 56));
        Controls.Add(root);

        var header = new Label
        {
            Text = "OutSystems Cloud Connector",
            Font = new Font(Font, FontStyle.Bold),
            AutoSize = true,
            Margin = new Padding(0, 0, 0, 12)
        };
        root.Controls.Add(header);

        ConfigureSelfUpdateBanner();
        root.Controls.Add(selfUpdateBanner);

        var inputs = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            ColumnCount = 2,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink
        };
        inputs.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110));
        inputs.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        root.Controls.Add(inputs);

        AddLabeledControl(inputs, "Address", addressTextBox);
        AddLabeledControl(inputs, "Token", tokenTextBox);
        AddLabeledControl(inputs, "Proxy", proxyTextBox);
        AddLabeledControl(inputs, "GUI update check", selfUpdateCheckIntervalComboBox);

        tokenTextBox.UseSystemPasswordChar = true;
        proxyTextBox.PlaceholderText = "Optional HTTP CONNECT or SOCKS5 proxy";
        addressTextBox.PlaceholderText = "https://organization.outsystems.app/sg_...";
        selfUpdateCheckIntervalComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
        selfUpdateCheckIntervalComboBox.Items.AddRange(SelfUpdateIntervals.All);
        selfUpdateCheckIntervalComboBox.SelectedItem = SelfUpdateIntervals.Daily;

        verboseCheckBox.Text = "Verbose logs";
        verboseCheckBox.AutoSize = true;
        verboseCheckBox.MinimumSize = new Size(0, 32);
        verboseCheckBox.Margin = new Padding(110, 8, 0, 10);
        inputs.SetColumnSpan(verboseCheckBox, 2);
        inputs.Controls.Add(verboseCheckBox);

        var binaryPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            ColumnCount = 2,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Padding = new Padding(0, 4, 0, 12)
        };
        binaryPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        binaryPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        root.Controls.Add(binaryPanel);

        updateBinaryButton.Text = "Download / Update Binary";
        ConfigureActionButton(updateBinaryButton, 190);
        binaryVersionLabel.AutoSize = true;
        binaryVersionLabel.Anchor = AnchorStyles.Left;
        binaryVersionLabel.Margin = new Padding(12, 0, 0, 0);
        binaryVersionLabel.Text = "Connector binary: not checked";

        binaryPanel.Controls.Add(updateBinaryButton, 0, 0);
        binaryPanel.Controls.Add(binaryVersionLabel, 1, 0);

        ConfigureEndpointGrid();
        root.Controls.Add(endpointsGrid);

        var actions = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            ColumnCount = 3,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Padding = new Padding(0, 12, 0, 12)
        };
        actions.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        actions.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        actions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        root.Controls.Add(actions);

        startButton.Text = "Start";
        ConfigureActionButton(startButton, 100);
        stopButton.Text = "Stop";
        ConfigureActionButton(stopButton, 100);
        statusLabel.AutoSize = true;
        statusLabel.Anchor = AnchorStyles.Left;
        statusLabel.Margin = new Padding(16, 0, 0, 0);

        actions.Controls.Add(startButton, 0, 0);
        actions.Controls.Add(stopButton, 1, 0);
        actions.Controls.Add(statusLabel, 2, 0);

        logTextBox.Dock = DockStyle.Fill;
        logTextBox.Multiline = true;
        logTextBox.ReadOnly = true;
        logTextBox.ScrollBars = ScrollBars.Vertical;
        logTextBox.Font = new Font(FontFamily.GenericMonospace, 9);
        logTextBox.MinimumSize = new Size(0, MinimumLogHeight);
        root.Controls.Add(logTextBox);
    }

    private static void ConfigureActionButton(Button button, int minimumWidth)
    {
        button.AutoSize = true;
        button.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        button.MinimumSize = new Size(minimumWidth, 40);
        button.Padding = new Padding(10, 5, 10, 5);
        button.Margin = new Padding(3, 3, 6, 3);
    }

    private static void AddLabeledControl(TableLayoutPanel panel, string label, Control control)
    {
        panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        panel.Controls.Add(new Label
        {
            Text = label,
            AutoSize = true,
            Anchor = AnchorStyles.Left,
            Margin = new Padding(0, 7, 8, 7)
        });

        control.Dock = DockStyle.Top;
        control.MinimumSize = new Size(0, 32);
        control.Margin = new Padding(0, 4, 0, 4);
        panel.Controls.Add(control);
    }

    private void ConfigureSelfUpdateBanner()
    {
        selfUpdateBanner.Dock = DockStyle.Top;
        selfUpdateBanner.Visible = false;
        selfUpdateBanner.AutoSize = true;
        selfUpdateBanner.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        selfUpdateBanner.Padding = new Padding(12);
        selfUpdateBanner.Margin = new Padding(0, 0, 0, 12);
        selfUpdateBanner.BackColor = Color.FromArgb(255, 248, 210);

        var bannerLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 3,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink
        };
        bannerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        bannerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        bannerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        selfUpdateBanner.Controls.Add(bannerLayout);

        selfUpdateBannerLabel.AutoSize = true;
        selfUpdateBannerLabel.Anchor = AnchorStyles.Left;
        selfUpdateBannerLabel.Margin = new Padding(0, 6, 12, 6);

        selfUpdateButton.Text = "Update and Restart";
        ConfigureActionButton(selfUpdateButton, 160);
        selfUpdateButton.Margin = new Padding(0, 0, 8, 0);

        dismissSelfUpdateButton.Text = "Dismiss";
        ConfigureActionButton(dismissSelfUpdateButton, 90);
        dismissSelfUpdateButton.Margin = new Padding(0);

        bannerLayout.Controls.Add(selfUpdateBannerLabel, 0, 0);
        bannerLayout.Controls.Add(selfUpdateButton, 1, 0);
        bannerLayout.Controls.Add(dismissSelfUpdateButton, 2, 0);
    }

    private void ConfigureEndpointGrid()
    {
        endpointsGrid.Dock = DockStyle.Fill;
        endpointsGrid.MinimumSize = new Size(0, MinimumEndpointsGridHeight);
        endpointsGrid.AllowUserToAddRows = true;
        endpointsGrid.AllowUserToDeleteRows = true;
        endpointsGrid.AutoGenerateColumns = false;
        endpointsGrid.RowHeadersWidth = 28;
        endpointsGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        endpointsGrid.MultiSelect = false;
        endpointsGrid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
        endpointsGrid.RowTemplate.Height = 30;
        endpointsGrid.BackgroundColor = SystemColors.Window;
        endpointsGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Local port",
            Name = "LocalPort",
            Width = 190
        });
        endpointsGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Remote host",
            Name = "RemoteHost",
            AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
        });
        endpointsGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Remote port",
            Name = "RemotePort",
            Width = 130
        });
        endpointsGrid.Columns.Add(new DataGridViewButtonColumn
        {
            HeaderText = string.Empty,
            Name = "Remove",
            Text = "Remove",
            UseColumnTextForButtonValue = true,
            Width = 90
        });
        endpointsGrid.CellContentClick += EndpointsGrid_CellContentClick;
    }

    private void EndpointsGrid_CellContentClick(object? sender, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex < 0 || endpointsGrid.Columns[e.ColumnIndex].Name != "Remove")
        {
            return;
        }

        var row = endpointsGrid.Rows[e.RowIndex];
        if (row.IsNewRow)
        {
            return;
        }

        endpointsGrid.Rows.Remove(row);
    }

    private void SetRunningState(bool running)
    {
        startButton.Enabled = !running;
        stopButton.Enabled = running;
        statusLabel.Text = running ? "Running" : "Stopped";
        endpointsGrid.ReadOnly = running;
        addressTextBox.ReadOnly = running;
        tokenTextBox.ReadOnly = running;
        proxyTextBox.ReadOnly = running;
        selfUpdateCheckIntervalComboBox.Enabled = !running;
        verboseCheckBox.Enabled = !running;
        updateBinaryButton.Enabled = !running;
        selfUpdateButton.Enabled = !running && availableSelfUpdate is not null;
    }
}
