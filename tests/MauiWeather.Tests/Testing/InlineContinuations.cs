namespace MauiWeather.Tests.Testing;

internal static class InlineContinuations
{
    /// <summary>
    /// Makes code after an <c>await</c> run as soon as the awaited task is
    /// completed, on the thread that completed it, for the rest of the test.
    /// </summary>
    /// <remarks>
    /// The test runner would otherwise queue it for later, and a test that
    /// completes a request by hand, or moves the clock, would have to wait to
    /// see what came of it. The runner sets its queue up just before the
    /// test's body, so this is called from inside the body: the view-model
    /// tests call it where they make their view model.
    /// </remarks>
    public static void Use()
    {
        SynchronizationContext.SetSynchronizationContext(null);
    }
}