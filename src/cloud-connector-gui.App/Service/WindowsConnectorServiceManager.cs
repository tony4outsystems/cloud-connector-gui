using System.Diagnostics;
using System.Runtime.Versioning;
using System.ServiceProcess;

using CloudConnectorGui.Core;

namespace CloudConnectorGui.App;

[SupportedOSPlatform("windows")]
public sealed class WindowsConnectorServiceManager : IConnectorServiceManager
{
    private const string ServiceNameConst = "OutSystemsCloudConnector";
    private const string DisplayNameConst = "OutSystems Cloud Connector";
    private static readonly TimeSpan StatusTimeout = TimeSpan.FromSeconds(15);

    public bool IsSupported => true;

    public string ServiceName => ServiceNameConst;

    public string DisplayName => DisplayNameConst;

    private static string ProgramDataRoot => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
        "OutSystems",
        "CloudConnector");

    private static string SharedExecutablePath => Path.Combine(ProgramDataRoot, "outsystemscc.exe");

    public bool IsServiceInstalled()
    {
        return GetState() != ServiceRunState.NotInstalled;
    }

    public ServiceRunState GetState()
    {
        try
        {
            using var controller = new ServiceController(ServiceNameConst);
            controller.Refresh();
            return controller.Status switch
            {
                ServiceControllerStatus.Running => ServiceRunState.Running,
                ServiceControllerStatus.Stopped => ServiceRunState.Stopped,
                ServiceControllerStatus.StartPending => ServiceRunState.StartPending,
                ServiceControllerStatus.StopPending => ServiceRunState.StopPending,
                _ => ServiceRunState.Unknown
            };
        }
        catch (InvalidOperationException)
        {
            return ServiceRunState.NotInstalled;
        }
    }

    public void Install(LaunchOptions options, string connectorExecutableSourcePath)
    {
        if (!File.Exists(connectorExecutableSourcePath))
        {
            throw new FileNotFoundException(
                "The connector binary is not installed. Download it first.",
                connectorExecutableSourcePath);
        }

        Directory.CreateDirectory(ProgramDataRoot);
        File.Copy(connectorExecutableSourcePath, SharedExecutablePath, overwrite: true);

        var commandLine = ConnectorArguments.ToDisplayCommand(SharedExecutablePath, options);

        RunScOrThrow(
            "create", ServiceNameConst,
            "binpath=", commandLine,
            "start=", "delayed-auto",
            "obj=", "LocalSystem",
            "displayname=", DisplayNameConst);

        RunScOrThrow("start", ServiceNameConst);
    }

    public void Uninstall()
    {
        try
        {
            using var controller = new ServiceController(ServiceNameConst);
            controller.Refresh();
            if (controller.Status != ServiceControllerStatus.Stopped)
            {
                controller.Stop();
                controller.WaitForStatus(ServiceControllerStatus.Stopped, StatusTimeout);
            }
        }
        catch (InvalidOperationException)
        {
        }

        RunScOrThrow("delete", ServiceNameConst);

        TryDeleteFile(SharedExecutablePath);
    }

    public void Start()
    {
        using var controller = new ServiceController(ServiceNameConst);
        controller.Start();
        controller.WaitForStatus(ServiceControllerStatus.Running, StatusTimeout);
    }

    public void Stop()
    {
        using var controller = new ServiceController(ServiceNameConst);
        controller.Stop();
        controller.WaitForStatus(ServiceControllerStatus.Stopped, StatusTimeout);
    }

    public void Restart()
    {
        Stop();
        Start();
    }

    private static void RunScOrThrow(params string[] arguments)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "sc.exe",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        using var process = Process.Start(startInfo)
            ?? throw new InvalidOperationException("Failed to start sc.exe.");

        var output = process.StandardOutput.ReadToEnd();
        var error = process.StandardError.ReadToEnd();
        process.WaitForExit();

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException(
                $"sc.exe {string.Join(' ', arguments)} failed with exit code {process.ExitCode}: {output}{error}");
        }
    }

    private static void TryDeleteFile(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }
}
