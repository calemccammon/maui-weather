using System.Diagnostics.CodeAnalysis;

using MauiWeather.Web.Data;

using Microsoft.JSInterop;

namespace MauiWeather.Tests.Web;

public sealed class BrowserStoreTests
{
    [Fact]
    public void It_keeps_text_in_the_browsers_local_storage()
    {
        FakeBrowser browser = new();
        BrowserStore store = new(browser);

        store.Write("units", "metric");

        Assert.Equal(["localStorage.setItem(units, metric)"], browser.Calls);
        Assert.Equal("metric", store.Read("units"));
        Assert.Null(store.Read("something else"));
    }

    [Fact]
    public void A_browser_that_refuses_to_be_read_has_nothing_stored()
    {
        BrowserStore store = new(new FakeBrowser { Refuses = true });

        Assert.Null(store.Read("units"));
    }

    [Fact]
    public void A_browser_that_refuses_to_store_says_so()
    {
        BrowserStore store = new(new FakeBrowser { Refuses = true });

        Assert.Throws<JSException>(() => store.Write("units", "metric"));
    }

    /// <summary>Just enough of a browser to have a <c>localStorage</c>.</summary>
    private sealed class FakeBrowser : IJSInProcessRuntime
    {
        private readonly Dictionary<string, string> _stored = [];

        /// <summary>When true, the store throws as one in a browser set to keep nothing does.</summary>
        public bool Refuses { get; init; }

        public List<string> Calls { get; } = [];

        public TResult Invoke<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.PublicProperties)] TResult>(
            string identifier,
            params object?[]? args)
        {
            if (Refuses)
            {
                throw new JSException("SecurityError: The operation is insecure.");
            }

            string key = (string)args![0]!;
            switch (identifier)
            {
                case "localStorage.getItem":
                    return (TResult)(object?)_stored.GetValueOrDefault(key)!;
                case "localStorage.setItem":
                    Calls.Add($"{identifier}({key}, {args[1]})");
                    _stored[key] = (string)args[1]!;
                    return default!;
                default:
                    throw new JSException($"No such function: {identifier}");
            }
        }

        public ValueTask<TValue> InvokeAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.PublicProperties)] TValue>(
            string identifier,
            object?[]? args)
        {
            throw new NotSupportedException();
        }

        public ValueTask<TValue> InvokeAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.PublicProperties)] TValue>(
            string identifier,
            CancellationToken cancellationToken,
            object?[]? args)
        {
            throw new NotSupportedException();
        }
    }
}