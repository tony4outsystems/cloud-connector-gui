namespace CloudConnectorWindowsGui;

public partial class App : Application
{
    private readonly MainPage mainPage;

    public App(MainPage mainPage)
    {
        InitializeComponent();
        this.mainPage = mainPage;
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        return new Window(mainPage)
        {
            Title = "OutSystems Cloud Connector",
            MinimumWidth = 960,
            MinimumHeight = 780,
            Width = 1080,
            Height = 880
        };
    }
}
