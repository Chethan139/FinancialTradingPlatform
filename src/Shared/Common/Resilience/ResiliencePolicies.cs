using Microsoft.Extensions.Logging;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;
using Polly.Timeout;

namespace Common.Resilience;

/// <summary>
/// Resilience policies for handling transient failures and preventing cascade failures
/// Implements Circuit Breaker, Retry, and Timeout patterns
/// 
/// Interview Key Points:
/// - Circuit Breaker prevents cascade failures by failing fast when service is unhealthy
/// - Exponential backoff with jitter prevents thundering herd problem
/// - Bulkhead isolation prevents one service from consuming all resources
/// - Timeout prevents indefinite hanging on slow services
/// </summary>
public class ResiliencePolicies
{
    private readonly ILogger<ResiliencePolicies> _logger;

    public ResiliencePolicies(ILogger<ResiliencePolicies> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Standard retry policy with exponential backoff
    /// Used for transient failures (network blips, temporary service unavailability)
    /// 
    /// Retry Strategy:
    /// - Attempt 1: Immediate
    /// - Attempt 2: ~2 seconds (with jitter)
    /// - Attempt 3: ~4 seconds (with jitter)
    /// - Total max time: ~6 seconds
    /// </summary>
    public AsyncRetryPolicy GetRetryPolicy(string policyName = "StandardRetry")
    {
        return Policy
            .Handle<HttpRequestException>()
            .Or<TimeoutException>()
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: retryAttempt =>
                {
                    // Exponential backoff with jitter
                    var baseDelay = TimeSpan.FromSeconds(Math.Pow(2, retryAttempt));
                    var jitter = TimeSpan.FromMilliseconds(Random.Shared.Next(0, 1000));
                    return baseDelay + jitter;
                },
                onRetry: (exception, timespan, retryCount, context) =>
                {
                    _logger.LogWarning(
                        "[{PolicyName}] Retry {RetryCount} after {Delay}ms due to {ExceptionType}: {ExceptionMessage}",
                        policyName,
                        retryCount,
                        timespan.TotalMilliseconds,
                        exception.GetType().Name,
                        exception.Message
                    );
                }
            );
    }

    /// <summary>
    /// Circuit breaker policy to prevent cascade failures
    /// 
    /// How it works:
    /// - CLOSED: Normal operation, requests pass through
    /// - OPEN: After 5 consecutive failures, circuit opens for 30 seconds (fail fast)
    /// - HALF-OPEN: After break duration, allows one test request
    ///   - If succeeds: Circuit closes, normal operation resumes
    ///   - If fails: Circuit opens again for another 30 seconds
    /// 
    /// Benefits:
    /// - Prevents resource exhaustion on failing services
    /// - Gives failing services time to recover
    /// - Provides immediate feedback to clients instead of hanging
    /// </summary>
    public AsyncCircuitBreakerPolicy GetCircuitBreakerPolicy(
        string serviceName,
        int failureThreshold = 5,
        TimeSpan? breakDuration = null)
    {
        breakDuration ??= TimeSpan.FromSeconds(30);

        return Policy
            .Handle<HttpRequestException>()
            .Or<TimeoutException>()
            .CircuitBreakerAsync(
                exceptionsAllowedBeforeBreaking: failureThreshold,
                durationOfBreak: breakDuration.Value,
                onBreak: (exception, duration) =>
                {
                    _logger.LogError(
                        "[CircuitBreaker] {ServiceName} circuit opened for {Duration}s due to {ExceptionType}: {ExceptionMessage}",
                        serviceName,
                        duration.TotalSeconds,
                        exception.GetType().Name,
                        exception.Message
                    );
                },
                onReset: () =>
                {
                    _logger.LogInformation(
                        "[CircuitBreaker] {ServiceName} circuit closed, normal operation resumed",
                        serviceName
                    );
                },
                onHalfOpen: () =>
                {
                    _logger.LogInformation(
                        "[CircuitBreaker] {ServiceName} circuit half-open, testing service health",
                        serviceName
                    );
                }
            );
    }

    /// <summary>
    /// Timeout policy to prevent indefinite hangs
    /// Critical for maintaining SLA and preventing thread pool exhaustion
    /// </summary>
    public AsyncTimeoutPolicy GetTimeoutPolicy(TimeSpan timeout, string policyName = "StandardTimeout")
    {
        return Policy.TimeoutAsync(
            timeout,
            TimeoutStrategy.Pessimistic,
            onTimeoutAsync: (context, timeSpan, task) =>
            {
                _logger.LogWarning(
                    "[{PolicyName}] Request timed out after {Timeout}ms",
                    policyName,
                    timeSpan.TotalMilliseconds
                );
                return Task.CompletedTask;
            }
        );
    }

    /// <summary>
    /// Combined policy: Timeout -> Retry -> Circuit Breaker
    /// This is the recommended pattern for external service calls
    /// 
    /// Order matters:
    /// 1. Timeout: Prevents individual requests from hanging
    /// 2. Retry: Handles transient failures
    /// 3. Circuit Breaker: Prevents cascade failures
    /// </summary>
    public IAsyncPolicy GetCombinedPolicy(
        string serviceName,
        TimeSpan timeout,
        int retryCount = 3,
        int circuitBreakerThreshold = 5)
    {
        var timeoutPolicy = GetTimeoutPolicy(timeout, $"{serviceName}Timeout");
        var retryPolicy = GetRetryPolicy($"{serviceName}Retry");
        var circuitBreakerPolicy = GetCircuitBreakerPolicy(serviceName, circuitBreakerThreshold);

        // Wrap policies: innermost (timeout) to outermost (circuit breaker)
        return Policy.WrapAsync(circuitBreakerPolicy, retryPolicy, timeoutPolicy);
    }

    /// <summary>
    /// Advanced retry policy with custom conditions
    /// Used for specific scenarios like rate limiting (HTTP 429)
    /// </summary>
    public AsyncRetryPolicy GetAdvancedRetryPolicy(
        Func<Exception, bool> shouldRetry,
        int maxRetries = 3,
        string policyName = "AdvancedRetry")
    {
        return Policy
            .Handle<Exception>(shouldRetry)
            .WaitAndRetryAsync(
                retryCount: maxRetries,
                sleepDurationProvider: retryAttempt =>
                {
                    // Exponential backoff: 2^retryAttempt seconds
                    return TimeSpan.FromSeconds(Math.Pow(2, retryAttempt));
                },
                onRetry: (exception, timespan, retryCount, context) =>
                {
                    _logger.LogWarning(
                        "[{PolicyName}] Advanced retry {RetryCount} after {Delay}s",
                        policyName,
                        retryCount,
                        timespan.TotalSeconds
                    );
                }
            );
    }
}
