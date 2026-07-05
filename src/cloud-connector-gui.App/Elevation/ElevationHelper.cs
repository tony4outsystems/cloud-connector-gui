using System.ComponentModel;
using System.Diagnostics;
using System.Security.Principal;

namespace CloudConnectorGui.App;

public static class ElevationHelper
{
    public static bool IsElevated
    {
        get
        {
            if (!OperatingSystem.IsWindows())
            {
                return false;
            }

            using var identity = WindowsIdentity.GetCurrent();
            return new WindowsPrincipal(identity).IsInRole(WindowsBuiltInRole.Administrator);
        }
    }

    public static bool TryRelaunchElevated(PendingServiceAction pendingAction)
    {
        if (!OperatingSystem.IsWindows())
        {
            return false;
        }

        var exePath = Environment.ProcessPath;
        if (string.IsNullOrEmpty(exePath))
        {
            return false;
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = exePath,
            UseShellExecute = true,
            Verb = "runas"
        };
        startInfo.ArgumentList.Add(PendingServiceActions.ArgumentName);
        startInfo.ArgumentList.Add(PendingServiceActions.ToArgument(pendingAction));

        try
        {
            Process.Start(startInfo);
            return true;
        }
        catch (Win32Exception)
        {
            return false;
        }
    }
}
