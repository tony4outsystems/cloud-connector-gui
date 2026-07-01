using CloudConnectorWindowsGui.ViewModels;

using Microsoft.UI.Xaml;

namespace CloudConnectorWindowsGui.WinUI;

public partial class App : MauiWinUIApplication
{
    public App()
    {
        InitializeComponent();
    }

    protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();

    protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
    {
        base.OnLaunched(args);

        if (Microsoft.UI.Xaml.Window.Current is { } nativeWindow)
        {
            HookClosing(nativeWindow);
        }
    }

    private static void HookClosing(Microsoft.UI.Xaml.Window nativeWindow)
    {
        var appWindow = nativeWindow.AppWindow;
        if (appWindow is null)
        {
            return;
        }

        var closingHandled = false;
        appWindow.Closing += async (sender, closingArgs) =>
        {
            if (closingHandled)
            {
                return;
            }

            var viewModel = IPlatformApplication.Current?.Services.GetService(typeof(MainViewModel)) as MainViewModel;
            if (viewModel is null)
            {
                return;
            }

            closingArgs.Cancel = true;
            await viewModel.StopIfRunningAsync().ConfigureAwait(true);
            closingHandled = true;
            appWindow.Destroy();
        };
    }
}
