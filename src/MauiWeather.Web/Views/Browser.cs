using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace MauiWeather.Web.Views;

/// <summary>
/// The few things the views need of the browser that Blazor does not offer,
/// from <c>wwwroot/js/browser.js</c>.
/// </summary>
public sealed class Browser(IJSRuntime javaScript)
{
    /// <summary>The script, fetched when it is first needed and kept for as long as the app runs.</summary>
    private Task<IJSObjectReference>? _module;

    private Task<IJSObjectReference> Module =>
        _module ??= javaScript.InvokeAsync<IJSObjectReference>("import", "./js/browser.js").AsTask();

    /// <summary>
    /// Has <paramref name="listener"/> told the window's size and theme now,
    /// and again whenever either changes. Dispose of what is returned, after
    /// calling its <c>stop</c>, to end it.
    /// </summary>
    public async Task<IJSObjectReference> WatchWindowAsync<T>(DotNetObjectReference<T> listener)
        where T : class
    {
        IJSObjectReference module = await Module;
        return await module.InvokeAsync<IJSObjectReference>("watchWindow", listener);
    }

    /// <summary>Takes the keyboard away from <paramref name="element"/>.</summary>
    public async Task BlurAsync(ElementReference element)
    {
        IJSObjectReference module = await Module;
        await module.InvokeVoidAsync("blur", element);
    }

    /// <summary>
    /// Keeps a drag that began on <paramref name="element"/> reporting to it,
    /// wherever the pointer goes next.
    /// </summary>
    public async Task CapturePointerAsync(ElementReference element, long pointerId)
    {
        IJSObjectReference module = await Module;
        await module.InvokeVoidAsync("capturePointer", element, pointerId);
    }
}