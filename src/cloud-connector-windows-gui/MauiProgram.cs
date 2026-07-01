using CloudConnectorWindowsGui.Services;
using CloudConnectorWindowsGui.ViewModels;

using Microsoft.Extensions.Logging;

namespace CloudConnectorWindowsGui;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder.UseMauiApp<App>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        builder.Services.AddSingleton<ConnectorProcess>();
        builder.Services.AddSingleton<CloudConnectorBinaryManager>();
        builder.Services.AddSingleton<MainViewModel>();
        builder.Services.AddSingleton<MainPage>();

        return builder.Build();
    }
}
