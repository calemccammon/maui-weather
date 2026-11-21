using MauiWeather.Presentation.Resources;

namespace MauiWeather.Views.Components;

[ContentProperty(nameof(SheetContent))]
public partial class BottomSheet : ContentView
{
    public static readonly BindableProperty SheetContentProperty = BindableProperty.Create(
        nameof(SheetContent),
        typeof(View),
        typeof(BottomSheet),
        propertyChanged: (sheet, _, content) => ((BottomSheet)sheet).Body.Content = (View?)content);

    public static readonly BindableProperty RestingHeightProperty = BindableProperty.Create(
        nameof(RestingHeight),
        typeof(double),
        typeof(BottomSheet),
        0d,
        propertyChanged: (sheet, _, _) => ((BottomSheet)sheet).Settle(animated: false));

    public static readonly BindableProperty ExpandedHeightProperty = BindableProperty.Create(
        nameof(ExpandedHeight),
        typeof(double),
        typeof(BottomSheet),
        0d,
        propertyChanged: (sheet, _, _) => ((BottomSheet)sheet).Settle(animated: false));

    private const uint SettleMilliseconds = 200;

    /// <summary>A drag has to carry the sheet this far towards the other stop to take it there.</summary>
    private const double SnapFraction = 0.25;

    private bool _isExpanded;

    /// <summary>How far down the sheet was pushed when the current drag began.</summary>
    private double _dragStart;

    public BottomSheet()
    {
        InitializeComponent();
        DescribeHandle();
    }

    /// <summary>What the sheet holds, under its handle.</summary>
    public View? SheetContent
    {
        get => (View?)GetValue(SheetContentProperty);
        set => SetValue(SheetContentProperty, value);
    }

    /// <summary>How much of the sheet shows while it is at rest.</summary>
    public double RestingHeight
    {
        get => (double)GetValue(RestingHeightProperty);
        set => SetValue(RestingHeightProperty, value);
    }

    /// <summary>How tall the sheet is when it is pulled right up.</summary>
    public double ExpandedHeight
    {
        get => (double)GetValue(ExpandedHeightProperty);
        set => SetValue(ExpandedHeightProperty, value);
    }

    /// <summary>
    /// The sheet is always as tall as it gets; at rest it is pushed down so
    /// that only its top shows. This is how far.
    /// </summary>
    private double RestingOffset => Math.Max(0, ExpandedHeight - RestingHeight);

    private void OnHandleTapped(object? sender, TappedEventArgs e)
    {
        _isExpanded = !_isExpanded;
        Settle(animated: true);
    }

    private void OnHandlePanned(object? sender, PanUpdatedEventArgs e)
    {
        switch (e.StatusType)
        {
            case GestureStatus.Started:
                this.CancelAnimations();
                _dragStart = TranslationY;
                break;
            case GestureStatus.Running:
                // The sheet moves under the finger, so each report is measured
                // from where the sheet has already got to.
                TranslationY = Math.Clamp(TranslationY + e.TotalY, 0, RestingOffset);
                break;
            case GestureStatus.Completed:
            case GestureStatus.Canceled:
                double moved = TranslationY - _dragStart;
                double enough = RestingOffset * SnapFraction;
                if (_isExpanded && moved > enough)
                {
                    _isExpanded = false;
                }
                else if (!_isExpanded && moved < -enough)
                {
                    _isExpanded = true;
                }

                Settle(animated: true);
                break;
        }
    }

    /// <summary>Puts the sheet at whichever stop it belongs at.</summary>
    private void Settle(bool animated)
    {
        HeightRequest = ExpandedHeight;
        double target = _isExpanded ? 0 : RestingOffset;
        DescribeHandle();

        this.CancelAnimations();
        if (animated)
        {
            _ = this.TranslateToAsync(0, target, SettleMilliseconds, Easing.CubicOut);
        }
        else
        {
            TranslationY = target;
        }
    }

    private void DescribeHandle()
    {
        SemanticProperties.SetDescription(Handle, _isExpanded ? Strings.ForecastCollapse : Strings.ForecastExpand);
    }
}