using System.Globalization;

using MauiWeather.Web.Views;

namespace MauiWeather.Tests.Web;

public sealed class CssTests
{
    [Fact]
    public void Lengths_are_in_pixels_to_two_decimal_places_at_most()
    {
        Assert.Equal("240px", Css.Pixels(240));
        Assert.Equal("12.5px", Css.Pixels(12.5));
        Assert.Equal("0.33px", Css.Pixels(1 / 3d));
    }

    [Fact]
    public void Figures_are_written_with_a_full_stop_whatever_the_language()
    {
        CultureInfo before = CultureInfo.CurrentCulture;
        // German writes a half as "0,5", which a style sheet cannot read.
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("de-DE");
        try
        {
            Assert.Equal("12.5px", Css.Pixels(12.5));
            Assert.Equal("0.75", Css.Number(0.75));
        }
        finally
        {
            CultureInfo.CurrentCulture = before;
        }
    }
}