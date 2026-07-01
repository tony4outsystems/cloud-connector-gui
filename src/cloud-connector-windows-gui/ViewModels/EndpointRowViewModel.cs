using CloudConnectorWindowsGui.Core;

namespace CloudConnectorWindowsGui.ViewModels;

public sealed class EndpointRowViewModel : ObservableObject
{
    private string localPort = string.Empty;
    private string remoteHost = string.Empty;
    private string remotePort = string.Empty;
    private bool isReadOnly;

    public string LocalPort
    {
        get => localPort;
        set => SetProperty(ref localPort, value);
    }

    public string RemoteHost
    {
        get => remoteHost;
        set => SetProperty(ref remoteHost, value);
    }

    public string RemotePort
    {
        get => remotePort;
        set => SetProperty(ref remotePort, value);
    }

    public bool IsReadOnly
    {
        get => isReadOnly;
        set => SetProperty(ref isReadOnly, value);
    }

    public Endpoint ToEndpoint() => new(LocalPort, RemoteHost, RemotePort);
}
