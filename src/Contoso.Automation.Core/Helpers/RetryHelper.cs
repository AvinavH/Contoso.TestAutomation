using Microsoft.Playwright;
using Polly;
using Polly.Retry;
using Serilog;

namespace Contoso.Automation.Core.Helpers;

/// <summary>
/// Polly-based retry helper for D365 UI interactions. D365's React-like architecture
/// updates the DOM asynchronously, which can cause intermittent locator failures
/// that are not genuine test failures - just timing issues.
///
/// Strategy: exponential backoff with jitter to avoid thundering herd in parallel runs.
/// </summary>
public static class RetryHelper
{
    private static readonly ILogger Log = Serilog.Log.ForContext(typeof(RetryHelper));

    /// <summary>
    /// Retries an async action up to <paramref name="maxAttempts"/> times with exponential backoff.
    /// Only retries on PlaywrightException (locator not found, element not interactable, etc.)
    /// Genuine assertion failures (AssertionException) are not retried.
    /// </summary>
    public static async Task<T> RetryAsync<T>(
        Func<Task<T>> action,
        int maxAttempts = 3,
        string? operationName = null)
    {
        var pipeline = new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions
            {
                ShouldHandle      = new PredicateBuilder().Handle<PlaywrightException>(),
                MaxRetryAttempts  = maxAttempts - 1,
                BackoffType       = DelayBackoffType.Exponential,
                Delay             = TimeSpan.FromMilliseconds(500),
                UseJitter         = true,
                OnRetry           = args =>
                {
                    Log.Warning(
                        "Retry {Attempt}/{Max} for '{Operation}': {Error}",
                        args.AttemptNumber + 1,
                        maxAttempts,
                        operationName ?? "unnamed",
                        args.Outcome.Exception?.Message);
                    return default;
                }
            })
            .Build();

        return await pipeline.ExecuteAsync(async _ => await action(), CancellationToken.None);
    }

    public static async Task RetryAsync(
        Func<Task> action,
        int maxAttempts = 3,
        string? operationName = null)
    {
        await RetryAsync(async () => { await action(); return true; }, maxAttempts, operationName);
    }

    /// <summary>
    /// Polls a condition until it returns true or the timeout is exceeded.
    /// Useful for D365 async operations where there's no explicit element to wait for.
    /// </summary>
    public static async Task WaitUntilAsync(
        Func<Task<bool>> condition,
        int timeoutMs    = 15_000,
        int pollIntervalMs = 500,
        string? description  = null)
    {
        var deadline = DateTime.UtcNow.AddMilliseconds(timeoutMs);
        while (DateTime.UtcNow < deadline)
        {
            if (await condition())
                return;

            await Task.Delay(pollIntervalMs);
        }

        throw new TimeoutException(
            $"Condition '{description ?? "unnamed"}' was not met within {timeoutMs}ms.");
    }
}
