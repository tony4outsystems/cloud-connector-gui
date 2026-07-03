using System.Runtime.InteropServices;
using Avalonia;

namespace CloudConnectorGui;

internal static class Program
{
    private const string SingleInstanceMutexName = @"Local\CloudConnectorGui";

    [STAThread]
    public static void Main(string[] args)
    {
        using var singleInstanceMutex = new Mutex(true, SingleInstanceMutexName, out var createdNew);
        if (!createdNew)
        {
            ShowAlreadyRunningNotice();
            return;
        }

        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    public static AppBuilder BuildAvaloniaApp()
    {
        return AppBuilder.Configure<GuiApplication>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
    }

    private static void ShowAlreadyRunningNotice()
    {
        const string message = "Cloud Connector GUI is already running.";
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            MessageBoxW(IntPtr.Zero, message, "Already running", MB_OK | MB_ICONINFORMATION);
            return;
        }

        Console.Error.WriteLine(message);
    }

    private const uint MB_OK = 0x0;
    private const uint MB_ICONINFORMATION = 0x40;

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int MessageBoxW(IntPtr hWnd, string text, string caption, uint type);
}
