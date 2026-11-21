using AngleSharp.Dom;

using Bunit;

using MauiWeather.Web.Views.Components;

using Microsoft.AspNetCore.Components.Web;

namespace MauiWeather.Tests.Web;

public sealed class BottomSheetTests : WebTest
{
    /// <summary>A sheet 600 tall that shows 200 at rest, so it rests pushed 400 down.</summary>
    private IRenderedComponent<BottomSheet> Sheet()
    {
        return Render<BottomSheet>(parameters => parameters
            .Add(sheet => sheet.RestingHeight, 200)
            .Add(sheet => sheet.ExpandedHeight, 600)
            .AddChildContent("<p>The forecast</p>"));
    }

    private static IElement Handle(IRenderedComponent<BottomSheet> sheet)
    {
        return sheet.Find(".handle");
    }

    private static string? Style(IRenderedComponent<BottomSheet> sheet)
    {
        return sheet.Find(".sheet").GetAttribute("style");
    }

    private static void Drag(IRenderedComponent<BottomSheet> sheet, double from, double to)
    {
        Handle(sheet).PointerDown(new PointerEventArgs { PointerId = 1, ClientY = from });
        Handle(sheet).PointerMove(new PointerEventArgs { PointerId = 1, ClientY = to });
        Handle(sheet).PointerUp(new PointerEventArgs { PointerId = 1, ClientY = to });
    }

    /// <summary>A click made with a mouse or a finger says how many presses it was; one from the keyboard says none.</summary>
    private static MouseEventArgs Press => new() { Detail = 1 };

    private static MouseEventArgs KeyPress => new() { Detail = 0 };

    [Fact]
    public void At_rest_it_is_pushed_down_until_only_its_top_shows()
    {
        IRenderedComponent<BottomSheet> sheet = Sheet();

        Assert.Equal("height: 600px; transform: translateY(400px)", Style(sheet));
        Assert.Equal("Show more of the forecast", Handle(sheet).GetAttribute("aria-label"));
        Assert.Equal("false", Handle(sheet).GetAttribute("aria-expanded"));
        Assert.Contains("The forecast", sheet.Find(".body").TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public void Pressing_the_handle_pulls_it_up_and_pressing_again_lets_it_down()
    {
        IRenderedComponent<BottomSheet> sheet = Sheet();

        Handle(sheet).Click(Press);

        Assert.Equal("height: 600px; transform: translateY(0px)", Style(sheet));
        Assert.Equal("Show more of the map", Handle(sheet).GetAttribute("aria-label"));
        Assert.Equal("true", Handle(sheet).GetAttribute("aria-expanded"));

        Handle(sheet).Click(Press);

        Assert.Equal("height: 600px; transform: translateY(400px)", Style(sheet));
    }

    [Fact]
    public void A_press_that_barely_moves_is_still_a_press()
    {
        IRenderedComponent<BottomSheet> sheet = Sheet();

        Drag(sheet, from: 500, to: 498);
        Handle(sheet).Click(Press);

        Assert.Equal("height: 600px; transform: translateY(0px)", Style(sheet));
    }

    [Fact]
    public void Under_a_drag_it_follows_the_pointer_and_keeps_hold_of_it()
    {
        IRenderedComponent<BottomSheet> sheet = Sheet();

        Handle(sheet).PointerDown(new PointerEventArgs { PointerId = 7, ClientY = 500 });
        Handle(sheet).PointerMove(new PointerEventArgs { PointerId = 7, ClientY = 350 });

        Assert.Equal("height: 600px; transform: translateY(250px)", Style(sheet));
        Assert.Contains("dragging", sheet.Find(".sheet").ClassList);
        Assert.Equal(7L, BrowserModule.VerifyInvoke("capturePointer").Arguments[1]);
    }

    [Fact]
    public void A_drag_cannot_carry_it_past_either_stop()
    {
        IRenderedComponent<BottomSheet> sheet = Sheet();

        Handle(sheet).PointerDown(new PointerEventArgs { PointerId = 1, ClientY = 500 });
        Handle(sheet).PointerMove(new PointerEventArgs { PointerId = 1, ClientY = 900 });
        Assert.Equal("height: 600px; transform: translateY(400px)", Style(sheet));

        Handle(sheet).PointerMove(new PointerEventArgs { PointerId = 1, ClientY = -900 });
        Assert.Equal("height: 600px; transform: translateY(0px)", Style(sheet));
    }

    [Fact]
    public void A_drag_of_more_than_a_quarter_of_the_way_takes_it_to_the_other_stop()
    {
        IRenderedComponent<BottomSheet> sheet = Sheet();

        Drag(sheet, from: 500, to: 399);

        Assert.Equal("height: 600px; transform: translateY(0px)", Style(sheet));
        Assert.DoesNotContain("dragging", sheet.Find(".sheet").ClassList);

        Drag(sheet, from: 100, to: 201);

        Assert.Equal("height: 600px; transform: translateY(400px)", Style(sheet));
    }

    [Fact]
    public void A_shorter_drag_lets_it_fall_back()
    {
        IRenderedComponent<BottomSheet> sheet = Sheet();

        Drag(sheet, from: 500, to: 401);

        Assert.Equal("height: 600px; transform: translateY(400px)", Style(sheet));
    }

    [Fact]
    public void The_click_a_browser_sends_after_a_drag_does_not_move_it_back()
    {
        IRenderedComponent<BottomSheet> sheet = Sheet();

        Drag(sheet, from: 500, to: 300);
        Handle(sheet).Click(Press);

        Assert.Equal("height: 600px; transform: translateY(0px)", Style(sheet));

        // Only that one click: the next press counts.
        Handle(sheet).Click(Press);

        Assert.Equal("height: 600px; transform: translateY(400px)", Style(sheet));
    }

    [Fact]
    public void A_key_press_moves_it_even_straight_after_a_drag_with_no_click()
    {
        IRenderedComponent<BottomSheet> sheet = Sheet();

        Drag(sheet, from: 500, to: 300);
        Handle(sheet).Click(KeyPress);

        Assert.Equal("height: 600px; transform: translateY(400px)", Style(sheet));
    }

    [Fact]
    public void A_sheet_with_no_room_to_move_stays_where_it_is()
    {
        IRenderedComponent<BottomSheet> sheet = Render<BottomSheet>(parameters => parameters
            .Add(sheet => sheet.RestingHeight, 300)
            .Add(sheet => sheet.ExpandedHeight, 100));

        Assert.Equal("height: 100px; transform: translateY(0px)", Style(sheet));
    }
}