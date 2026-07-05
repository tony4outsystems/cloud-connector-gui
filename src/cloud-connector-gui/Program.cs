using Avalonia;
using Velopack;

namespace CloudConnectorGui;

internal static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        VelopackApp.Build().Run();

        GuiApplication.StartupArgs = args;
        GuiApplication.AlreadyRunning = !SingleInstanceGuard.TryAcquire();

        try
        {
            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        }
        finally
        {
            SingleInstanceGuard.Release();
        }
    }

    public static AppBuilder BuildAvaloniaApp()
    {
        return AppBuilder.Configure<GuiApplication>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
    }
}
