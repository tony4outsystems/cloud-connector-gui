using System.Collections.Specialized;

using CloudConnectorWindowsGui.ViewModels;

namespace CloudConnectorWindowsGui;

public partial class MainPage : ContentPage
{
    private readonly MainViewModel viewModel;

    public MainPage(MainViewModel viewModel)
    {
        InitializeComponent();
        this.viewModel = viewModel;
        BindingContext = viewModel;
        viewModel.LogLines.CollectionChanged += OnLogLinesChanged;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await viewModel.InitializeAsync().ConfigureAwait(true);
    }

    private void OnLogLinesChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (viewModel.LogLines.Count == 0)
        {
            return;
        }

        var lastIndex = viewModel.LogLines.Count - 1;
        LogCollectionView.ScrollTo(lastIndex, position: ScrollToPosition.End, animate: false);
    }
}
