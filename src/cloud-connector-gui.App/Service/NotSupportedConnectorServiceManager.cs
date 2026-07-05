using CloudConnectorGui.Core;

namespace CloudConnectorGui.App;

public sealed class NotSupportedConnectorServiceManager : IConnectorServiceManager
{
    public bool IsSupported => false;

    public string ServiceName => string.Empty;

    public string DisplayName => string.Empty;

    public bool IsServiceInstalled()
    {
        return false;
    }

    public ServiceRunState GetState()
    {
        return ServiceRunState.NotInstalled;
    }

    public void Install(LaunchOptions options, string connectorExecutableSourcePath)
    {
        throw new PlatformNotSupportedException("Service mode is only supported on Windows.");
    }

    public void Uninstall()
    {
        throw new PlatformNotSupportedException("Service mode is only supported on Windows.");
    }

    public void Start()
    {
        throw new PlatformNotSupportedException("Service mode is only supported on Windows.");
    }

    public void Stop()
    {
        throw new PlatformNotSupportedException("Service mode is only supported on Windows.");
    }

    public void Restart()
    {
        throw new PlatformNotSupportedException("Service mode is only supported on Windows.");
    }
}
