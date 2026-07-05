using CloudConnectorGui.App;
using CloudConnectorGui.Core;
using Xunit;

namespace CloudConnectorGui.App.Tests;

public sealed class NotSupportedConnectorServiceManagerTests
{
    private readonly NotSupportedConnectorServiceManager manager = new();

    [Fact]
    public void IsSupportedIsFalse()
    {
        Assert.False(manager.IsSupported);
    }

    [Fact]
    public void IsServiceInstalledIsFalse()
    {
        Assert.False(manager.IsServiceInstalled());
    }

    [Fact]
    public void GetStateIsNotInstalled()
    {
        Assert.Equal(ServiceRunState.NotInstalled, manager.GetState());
    }

    [Fact]
    public void InstallThrowsPlatformNotSupported()
    {
        var options = new LaunchOptions("gateway", "token", []);

        Assert.Throws<PlatformNotSupportedException>(() => manager.Install(options, "outsystemscc.exe"));
    }

    [Fact]
    public void UninstallThrowsPlatformNotSupported()
    {
        Assert.Throws<PlatformNotSupportedException>(manager.Uninstall);
    }

    [Fact]
    public void StartThrowsPlatformNotSupported()
    {
        Assert.Throws<PlatformNotSupportedException>(manager.Start);
    }

    [Fact]
    public void StopThrowsPlatformNotSupported()
    {
        Assert.Throws<PlatformNotSupportedException>(manager.Stop);
    }

    [Fact]
    public void RestartThrowsPlatformNotSupported()
    {
        Assert.Throws<PlatformNotSupportedException>(manager.Restart);
    }
}
