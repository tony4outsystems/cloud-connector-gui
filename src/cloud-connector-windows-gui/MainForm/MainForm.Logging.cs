namespace CloudConnectorWindowsGui;

internal sealed partial class MainForm
{
    private void AppendLog(string line)
    {
        var timestamp = DateTime.Now;
        var logLine = $"[{timestamp:HH:mm:ss}] {line}";
        logTextBox.AppendText($"{logLine}{Environment.NewLine}");

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
            logTextBox.AppendText($"[{DateTime.Now:HH:mm:ss}] Could not write log file {logFilePath}: {ex.Message}{Environment.NewLine}");
        }
    }
}
