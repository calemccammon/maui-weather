namespace MauiWeather.Views.Components;

/// <summary>
/// A track for the week's whole temperature range with one day's part of it
/// filled in, from <see cref="Start"/> to <see cref="End"/> (each 0 to 1).
/// </summary>
public sealed partial class RangeBar : GraphicsView, IDrawable
{
    public static readonly BindableProperty StartProperty = Redrawn(nameof(Start), typeof(double), 0d);
    public static readonly BindableProperty EndProperty = Redrawn(nameof(End), typeof(double), 1d);
    public static readonly BindableProperty TrackColorProperty = Redrawn(nameof(TrackColor), typeof(Color), Colors.Transparent);
    public static readonly BindableProperty FillColorProperty = Redrawn(nameof(FillColor), typeof(Color), Colors.Black);

    private const float Thickness = 6;

    public RangeBar()
    {
        Drawable = this;
        HeightRequest = Thickness;
        VerticalOptions = LayoutOptions.Center;
        // The numbers either side say the same thing to a screen reader.
        AutomationProperties.SetIsInAccessibleTree(this, false);
    }

    public double Start
    {
        get => (double)GetValue(StartProperty);
        set => SetValue(StartProperty, value);
    }

    public double End
    {
        get => (double)GetValue(EndProperty);
        set => SetValue(EndProperty, value);
    }

    public Color TrackColor
    {
        get => (Color)GetValue(TrackColorProperty);
        set => SetValue(TrackColorProperty, value);
    }

    public Color FillColor
    {
        get => (Color)GetValue(FillColorProperty);
        set => SetValue(FillColorProperty, value);
    }

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        float width = dirtyRect.Width;
        float height = dirtyRect.Height;
        float radius = height / 2;
        // Never thinner than the bar is tall, or a day whose low and high
        // match would vanish; and never pushed off the end of the track.
        float fill = Math.Clamp(width * (float)(End - Start), Math.Min(height, width), width);
        float left = Math.Min(width * (float)Start, width - fill);

        canvas.FillColor = TrackColor;
        canvas.FillRoundedRectangle(0, 0, width, height, radius);
        canvas.FillColor = FillColor;
        canvas.FillRoundedRectangle(left, 0, fill, height, radius);
    }

    private static BindableProperty Redrawn(string name, Type type, object defaultValue)
    {
        return BindableProperty.Create(
            name,
            type,
            typeof(RangeBar),
            defaultValue,
            propertyChanged: (bar, _, _) => ((RangeBar)bar).Invalidate());
    }
}