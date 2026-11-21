using MauiWeather.Presentation.Resources;
using MauiWeather.Views;

namespace MauiWeather;

public partial class App : Application
{
    private readonly IServiceProvider _services;

    public App(IServiceProvider services)
    {
        InitializeComponent();
        _services = services;
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        // The page is made here and not handed to the constructor: its XAML
        // uses the styles that InitializeComponent has just loaded.
        return new Window(_services.GetRequiredService<HomePage>()) { Title = Strings.AppTitle };
    }
}