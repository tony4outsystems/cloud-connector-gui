using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using CloudConnectorGui.ViewModels;

namespace CloudConnectorGui.Views;

public sealed partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        Opened += async (_, _) =>
        {
            ViewModel.Initialize();
            await ViewModel.OnShownAsync().ConfigureAwait(true);
        };
        Closing += async (_, args) =>
        {
            ViewModel.Save();
            if (ViewModel.IsConnectorRunning)
            {
                args.Cancel = true;
                await ViewModel.StopConnectorCommand.ExecuteAsync(null).ConfigureAwait(true);
                Close();
            }
        };
    }

    private MainWindowViewModel ViewModel => (MainWindowViewModel)DataContext!;

    private async void ServiceModeToggle_OnClick(object? sender, RoutedEventArgs args)
    {
        await ViewModel.ToggleServiceModeCommand.ExecuteAsync(null).ConfigureAwait(true);
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
