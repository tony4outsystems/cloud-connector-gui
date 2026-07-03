namespace CloudConnectorWindowsGui;

internal static class Program
{
    private const string SingleInstanceMutexName = @"Local\OutSystems.CloudConnector.WindowsGui";

    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();

        using var singleInstanceMutex = new Mutex(true, SingleInstanceMutexName, out var createdNew);
        if (!createdNew)
        {
            MessageBox.Show(
                "OutSystems Cloud Connector is already running.",
                "Already running",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            return;
        }

        Application.Run(new MainForm());
    }
}
