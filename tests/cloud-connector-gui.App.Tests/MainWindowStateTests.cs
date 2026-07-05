using CloudConnectorGui.App;
using CloudConnectorGui.Core;
using Xunit;

namespace CloudConnectorGui.App.Tests;

public sealed class MainWindowStateTests
{
    [Fact]
    public void ApplyConfigurationCopiesLaunchAndUpdateState()
    {
        var state = new MainWindowState();
        var lastCheck = new DateOnly(2026, 7, 3);

        state.ApplyConfiguration(new GuiConfiguration
        {
            Address = "gateway",
            Token = "token",
            Proxy = "http://proxy:8080",
            Verbose = true,
            SelfUpdateCheckInterval = SelfUpdateIntervals.Weekly,
            LastSelfUpdateCheck = lastCheck,
            Endpoints = [new Endpoint("8081", "api.internal", "443")]
        });

        Assert.Equal("gateway", state.Address);
        Assert.Equal("token", state.Token);
        Assert.Equal("http://proxy:8080", state.Proxy);
        Assert.True(state.Verbose);
        Assert.Equal(SelfUpdateIntervals.Weekly, state.SelfUpdateCheckInterval);
        Assert.Equal(lastCheck, state.LastSelfUpdateCheck);
        Assert.Equal([new Endpoint("8081", "api.internal", "443")], state.Endpoints);
    }

    [Fact]
    public void ToConfigurationPreservesSelfUpdateFields()
    {
        var state = new MainWindowState
        {
            Address = "gateway",
            Token = "token",
            Proxy = "http://proxy:8080",
            Verbose = true,
            SelfUpdateCheckInterval = SelfUpdateIntervals.Monthly,
            LastSelfUpdateCheck = new DateOnly(2026, 7, 3)
        };
        state.Endpoints.Add(new Endpoint("8081", "api.internal", "443"));

        var configuration = state.ToConfiguration();

        Assert.Equal("gateway", configuration.Address);
        Assert.Equal("token", configuration.Token);
        Assert.Equal("http://proxy:8080", configuration.Proxy);
        Assert.True(configuration.Verbose);
        Assert.Equal(SelfUpdateIntervals.Monthly, configuration.SelfUpdateCheckInterval);
        Assert.Equal(new DateOnly(2026, 7, 3), configuration.LastSelfUpdateCheck);
        Assert.Equal([new Endpoint("8081", "api.internal", "443")], configuration.Endpoints);
    }

    [Theory]
    [InlineData(false, false, true)]
    [InlineData(true, false, false)]
    [InlineData(false, true, false)]
    [InlineData(true, true, false)]
    public void CanEditConfigurationReflectsRunningAndServiceMode(bool isRunning, bool isServiceModeEnabled, bool expected)
    {
        var state = new MainWindowState
        {
            IsRunning = isRunning,
            IsServiceModeEnabled = isServiceModeEnabled
        };

        Assert.Equal(expected, state.CanEditConfiguration);
    }

    [Theory]
    [InlineData(false, false, true)]
    [InlineData(true, false, false)]
    [InlineData(false, true, false)]
    public void CanInstallServiceReflectsRunningAndServiceMode(bool isRunning, bool isServiceModeEnabled, bool expected)
    {
        var state = new MainWindowState
        {
            IsRunning = isRunning,
            IsServiceModeEnabled = isServiceModeEnabled
        };

        Assert.Equal(expected, state.CanInstallService);
    }

    [Theory]
    [InlineData(false, ServiceRunState.NotInstalled, false, false, false)]
    [InlineData(true, ServiceRunState.Stopped, true, false, false)]
    [InlineData(true, ServiceRunState.Running, false, true, true)]
    [InlineData(true, ServiceRunState.StartPending, false, false, false)]
    public void ServiceButtonsReflectServiceState(
        bool isServiceModeEnabled,
        ServiceRunState serviceState,
        bool canStart,
        bool canStop,
        bool canRestart)
    {
        var state = new MainWindowState
        {
            IsServiceModeEnabled = isServiceModeEnabled,
            ServiceState = serviceState
        };

        Assert.Equal(canStart, state.CanStartService);
        Assert.Equal(canStop, state.CanStopService);
        Assert.Equal(canRestart, state.CanRestartService);
    }
}
