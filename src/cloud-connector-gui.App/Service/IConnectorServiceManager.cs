using CloudConnectorGui.Core;

namespace CloudConnectorGui.App;

public interface IConnectorServiceManager
{
    bool IsSupported { get; }

    string ServiceName { get; }

    string DisplayName { get; }

    bool IsServiceInstalled();

    ServiceRunState GetState();

    void Install(LaunchOptions options, string connectorExecutableSourcePath);

    void Uninstall();

    void Start();

    void Stop();

    void Restart();
}
