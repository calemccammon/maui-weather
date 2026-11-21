using MauiWeather.Data.Repositories;

using Microsoft.JSInterop;

namespace MauiWeather.Web.Data;

/// <summary>The browser's key-value store for this site: <c>localStorage</c>.</summary>
internal sealed class BrowserStore(IJSInProcessRuntime browser) : IKeyValueStore
{
    public string? Read(string key)
    {
        try
        {
            return browser.Invoke<string?>("localStorage.getItem", key);
        }
        catch (JSException)
        {
            // A browser set to keep nothing refuses even to be read. Nothing
            // is stored, then.
            return null;
        }
    }

    public void Write(string key, string value)
    {
        browser.InvokeVoid("localStorage.setItem", key, value);
    }
}