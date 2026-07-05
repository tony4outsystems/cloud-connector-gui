namespace CloudConnectorGui.App;

public enum PendingServiceAction
{
    Install,
    Uninstall,
    Start,
    Stop,
    Restart
}

public static class PendingServiceActions
{
    public const string ArgumentName = "--pending-service-action";

    public static string ToArgument(PendingServiceAction action)
    {
        return action switch
        {
            PendingServiceAction.Install => "install",
            PendingServiceAction.Uninstall => "uninstall",
            PendingServiceAction.Start => "start",
            PendingServiceAction.Stop => "stop",
            PendingServiceAction.Restart => "restart",
            _ => throw new ArgumentOutOfRangeException(nameof(action))
        };
    }

    public static PendingServiceAction? TryParse(IReadOnlyList<string> args)
    {
        for (var i = 0; i < args.Count - 1; i++)
        {
            if (args[i] != ArgumentName)
            {
                continue;
            }

            return args[i + 1] switch
            {
                "install" => PendingServiceAction.Install,
                "uninstall" => PendingServiceAction.Uninstall,
                "start" => PendingServiceAction.Start,
                "stop" => PendingServiceAction.Stop,
                "restart" => PendingServiceAction.Restart,
                _ => null
            };
        }

        return null;
    }
}
