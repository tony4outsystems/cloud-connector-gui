using CloudConnectorGui.App;
using Xunit;

namespace CloudConnectorGui.App.Tests;

public sealed class PendingServiceActionsTests
{
    [Theory]
    [InlineData("install", PendingServiceAction.Install)]
    [InlineData("uninstall", PendingServiceAction.Uninstall)]
    [InlineData("start", PendingServiceAction.Start)]
    [InlineData("stop", PendingServiceAction.Stop)]
    [InlineData("restart", PendingServiceAction.Restart)]
    public void TryParseReturnsMatchingAction(string argumentValue, PendingServiceAction expected)
    {
        var args = new[] { "--pending-service-action", argumentValue };

        var result = PendingServiceActions.TryParse(args);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void TryParseReturnsNullWhenArgumentMissing()
    {
        var result = PendingServiceActions.TryParse(["--some-other-flag", "value"]);

        Assert.Null(result);
    }

    [Fact]
    public void TryParseReturnsNullWhenValueUnrecognized()
    {
        var result = PendingServiceActions.TryParse(["--pending-service-action", "bogus"]);

        Assert.Null(result);
    }

    [Fact]
    public void TryParseReturnsNullForEmptyArgs()
    {
        var result = PendingServiceActions.TryParse([]);

        Assert.Null(result);
    }

    [Theory]
    [InlineData(PendingServiceAction.Install, "install")]
    [InlineData(PendingServiceAction.Uninstall, "uninstall")]
    [InlineData(PendingServiceAction.Start, "start")]
    [InlineData(PendingServiceAction.Stop, "stop")]
    [InlineData(PendingServiceAction.Restart, "restart")]
    public void ToArgumentRoundTripsWithTryParse(PendingServiceAction action, string expected)
    {
        var argument = PendingServiceActions.ToArgument(action);

        Assert.Equal(expected, argument);
        Assert.Equal(action, PendingServiceActions.TryParse(["--pending-service-action", argument]));
    }
}
