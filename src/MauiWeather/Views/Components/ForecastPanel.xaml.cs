namespace MauiWeather.Views.Components;

public partial class ForecastPanel : ContentView
{
    public static readonly BindableProperty ContentPaddingProperty = BindableProperty.Create(
        nameof(ContentPadding),
        typeof(Thickness),
        typeof(ForecastPanel),
        new Thickness(24),
        propertyChanged: (panel, _, padding) => ((ForecastPanel)panel).Stack.Padding = (Thickness)padding);

    public ForecastPanel()
    {
        InitializeComponent();
        Stack.Padding = ContentPadding;
    }

    /// <summary>The space around what scrolls: the sheet's handle already gives some above it.</summary>
    public Thickness ContentPadding
    {
        get => (Thickness)GetValue(ContentPaddingProperty);
        set => SetValue(ContentPaddingProperty, value);
    }
}