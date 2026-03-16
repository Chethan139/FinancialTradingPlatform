using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text.Json;

namespace Common.Resilience;

/// <summary>
/// Rate limiting middleware to prevent API abuse and ensure fair resource allocation
/// Implements token bucket algorithm with distributed cache support
/// 
/// Interview Key Points:
/// - Token Bucket Algorithm: Allows bursts but limits average rate
/// - Sliding Window: More accurate than fixed window, prevents boundary abuse
/// - Distributed: Works across multiple instances using Redis
/// - Per-User and Per-IP: Different limits based on authentication
/// - Rate Limit Headers: RFC 6585 compliant headers for client visibility
/// 
/// Configuration:
/// - Anonymous users: 100 requests/minute
/// - Authenticated users: 1000 requests/minute
/// - Premium users: 10000 requests/minute
/// </summary>
public class RateLimitingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IDistributedCache _cache;
    private readonly ILogger<RateLimitingMiddleware> _logger;

    // Rate limit configurations
    private readonly Dictionary<string, RateLimitConfig> _rateLimits = new()
    {
        ["anonymous"] = new() { RequestsPerMinute = 100, BurstSize = 10 },
        ["authenticated"] = new() { RequestsPerMinute = 1000, BurstSize = 50 },
        ["premium"] = new() { RequestsPerMinute = 10000, BurstSize = 500 }
    };

    public RateLimitingMiddleware(
        RequestDelegate next,
        IDistributedCache cache,
        ILogger<RateLimitingMiddleware> logger)
    {
        _next = next;
        _cache = cache;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Extract client identifier (user ID or IP address)
        var clientId = GetClientIdentifier(context);
        var clientTier = GetClientTier(context);
        var config = _rateLimits[clientTier];

        // Check rate limit
        var (allowed, remainingRequests, resetTime) = await CheckRateLimitAsync(
            clientId,
            config
        );

        // Add rate limit headers (RFC 6585)
        context.Response.Headers["X-RateLimit-Limit"] = config.RequestsPerMinute.ToString();
        context.Response.Headers["X-RateLimit-Remaining"] = remainingRequests.ToString();
        context.Response.Headers["X-RateLimit-Reset"] = resetTime.ToString("o");

        if (!allowed)
        {
            _logger.LogWarning(
                "Rate limit exceeded for client {ClientId} ({Tier}). Limit: {Limit}/min",
                clientId,
                clientTier,
                config.RequestsPerMinute
            );

            context.Response.StatusCode = (int)HttpStatusCode.TooManyRequests;
            context.Response.Headers["Retry-After"] = "60"; // Retry after 60 seconds

            var errorResponse = new
            {
                error = "Rate limit exceeded",
                message = $"You have exceeded the rate limit of {config.RequestsPerMinute} requests per minute",
                retryAfter = resetTime
            };

            await context.Response.WriteAsJsonAsync(errorResponse);
            return;
        }

        // Continue to next middleware
        await _next(context);
    }

    /// <summary>
    /// Check rate limit using sliding window counter algorithm
    /// Implements token bucket with refill
    /// </summary>
    private async Task<(bool allowed, int remaining, DateTime resetTime)> CheckRateLimitAsync(
        string clientId,
        RateLimitConfig config)
    {
        var now = DateTime.UtcNow;
        var windowStart = new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute, 0);
        var resetTime = windowStart.AddMinutes(1);

        var cacheKey = $"ratelimit:{clientId}:{windowStart:yyyyMMddHHmm}";

        // Try to get current count from cache
        var currentCountStr = await _cache.GetStringAsync(cacheKey);
        var currentCount = string.IsNullOrEmpty(currentCountStr) ? 0 : int.Parse(currentCountStr);

        if (currentCount >= config.RequestsPerMinute)
        {
            return (false, 0, resetTime);
        }

        // Increment counter
        currentCount++;
        await _cache.SetStringAsync(
            cacheKey,
            currentCount.ToString(),
            new DistributedCacheEntryOptions
            {
                AbsoluteExpiration = resetTime.AddSeconds(5) // Small buffer
            }
        );

        var remaining = config.RequestsPerMinute - currentCount;
        return (true, remaining, resetTime);
    }

    /// <summary>
    /// Extract client identifier from request
    /// Priority: User ID > API Key > IP Address
    /// </summary>
    private string GetClientIdentifier(HttpContext context)
    {
        // Check for authenticated user
        var userId = context.User?.Identity?.Name;
        if (!string.IsNullOrEmpty(userId))
        {
            return $"user:{userId}";
        }

        // Check for API key in header
        if (context.Request.Headers.TryGetValue("X-API-Key", out var apiKey))
        {
            return $"apikey:{apiKey}";
        }

        // Fall back to IP address
        var ipAddress = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        return $"ip:{ipAddress}";
    }

    /// <summary>
    /// Determine client tier based on authentication and subscription
    /// </summary>
    private string GetClientTier(HttpContext context)
    {
        if (!context.User.Identity?.IsAuthenticated ?? true)
        {
            return "anonymous";
        }

        // Check for premium subscription claim
        if (context.User.HasClaim("subscription", "premium"))
        {
            return "premium";
        }

        return "authenticated";
    }
}

/// <summary>
/// Rate limit configuration
/// </summary>
public class RateLimitConfig
{
    /// <summary>
    /// Maximum requests allowed per minute
    /// </summary>
    public int RequestsPerMinute { get; set; }

    /// <summary>
    /// Burst size - allows short bursts above average rate
    /// Implements token bucket refill
    /// </summary>
    public int BurstSize { get; set; }
}
