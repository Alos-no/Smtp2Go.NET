namespace Smtp2Go.NET.Configuration;

/// <summary>
///   Configuration options for HTTP resilience policies (retries, circuit breaker, rate limiting).
/// </summary>
/// <remarks>
///   <para>
///     These options configure the resilience pipeline for HTTP requests, including:
///     <list type="bullet">
///       <item>Retry policies with exponential backoff</item>
///       <item>Circuit breaker to prevent cascading failures</item>
///       <item>Client-side rate limiting to respect API quotas</item>
///       <item>Timeouts for individual requests and total operation duration</item>
///     </list>
///   </para>
///   <para>
///     <b>Important:</b> SMTP2GO API endpoints use POST for all operations. Since POST is
///     non-idempotent, email send requests are NOT retried by default to prevent duplicate
///     sends. Only transient failures on non-send endpoints are retried.
///   </para>
/// </remarks>
/// <example>
///   <code>
///   {
///     "Smtp2Go": {
///       "Resilience": {
///         "MaxRetries": 3,
///         "RetryBaseDelay": "00:00:01",
///         "PerAttemptTimeout": "00:00:30",
///         "TotalRequestTimeout": "00:01:00",
///         "RateLimiting": {
///           "IsEnabled": true,
///           "PermitLimit": 20,
///           "QueueLimit": 50
///         }
///       }
///     }
///   }
///   </code>
/// </example>
public sealed class ResilienceOptions
{
  #region Constants & Statics

  /// <summary>The configuration section name.</summary>
  public const string SectionName = "Resilience";

  #endregion


  #region Properties & Fields - Public

  /// <summary>
  ///   Gets or sets the maximum number of retry attempts. Defaults to 3.
  /// </summary>
  /// <remarks>
  ///   <para>
  ///     Only idempotent HTTP methods (GET, HEAD, OPTIONS, TRACE, PUT, DELETE) are retried.
  ///     POST and PATCH requests are NOT retried to prevent duplicate operations.
  ///   </para>
  /// </remarks>
  public int MaxRetries { get; set; } = 3;

  /// <summary>
  ///   Gets or sets the base delay between retry attempts. Defaults to 1 second.
  /// </summary>
  /// <remarks>
  ///   <para>
  ///     Actual delay uses exponential backoff with jitter:
  ///     delay = baseDelay * 2^attemptNumber + random jitter
  ///   </para>
  /// </remarks>
  public TimeSpan RetryBaseDelay { get; set; } = TimeSpan.FromSeconds(1);

  /// <summary>
  ///   Gets or sets the timeout for each individual HTTP request attempt. Defaults to 30 seconds.
  /// </summary>
  public TimeSpan PerAttemptTimeout { get; set; } = TimeSpan.FromSeconds(30);

  /// <summary>
  ///   Gets or sets the total timeout covering all retry attempts. Defaults to 60 seconds.
  /// </summary>
  /// <remarks>
  ///   <para>
  ///     This is the outer timeout that covers all retry attempts combined.
  ///     If this timeout is reached, no more retries will be attempted.
  ///   </para>
  /// </remarks>
  public TimeSpan TotalRequestTimeout { get; set; } = TimeSpan.FromSeconds(60);

  /// <summary>
  ///   Gets or sets the circuit breaker failure threshold. Defaults to 0.1 (10%).
  /// </summary>
  /// <remarks>
  ///   <para>
  ///     When the failure rate exceeds this threshold within the sampling duration,
  ///     the circuit breaker opens and subsequent requests fail fast.
  ///   </para>
  /// </remarks>
  public double CircuitBreakerFailureThreshold { get; set; } = 0.1;

  /// <summary>
  ///   Gets or sets the circuit breaker sampling duration. Defaults to 30 seconds.
  /// </summary>
  public TimeSpan CircuitBreakerSamplingDuration { get; set; } = TimeSpan.FromSeconds(30);

  /// <summary>
  ///   Gets or sets the minimum throughput required before the circuit breaker can trip. Defaults to 10.
  /// </summary>
  public int CircuitBreakerMinimumThroughput { get; set; } = 10;

  /// <summary>
  ///   Gets or sets the duration the circuit breaker stays open before allowing a test request. Defaults to 30 seconds.
  /// </summary>
  public TimeSpan CircuitBreakerBreakDuration { get; set; } = TimeSpan.FromSeconds(30);

  /// <summary>
  ///   Gets or sets the rate limiting options.
  /// </summary>
  public RateLimitingOptions RateLimiting { get; set; } = new();

  #endregion
}


/// <summary>
///   Configuration options for client-side rate limiting.
/// </summary>
/// <remarks>
///   <para>
///     Client-side rate limiting helps prevent hitting server-side rate limits by
///     proactively throttling requests before they're sent. This is especially useful
///     for the SMTP2GO API which has rate limits on email sending.
///   </para>
/// </remarks>
public sealed class RateLimitingOptions
{
  /// <summary>
  ///   Gets or sets whether rate limiting is enabled. Defaults to true.
  /// </summary>
  public bool IsEnabled { get; set; } = true;

  /// <summary>
  ///   Gets or sets the maximum number of concurrent requests allowed. Defaults to 20.
  /// </summary>
  public int PermitLimit { get; set; } = 20;

  /// <summary>
  ///   Gets or sets the maximum number of requests that can be queued when at the permit limit. Defaults to 50.
  /// </summary>
  public int QueueLimit { get; set; } = 50;

  /// <summary>
  ///   Gets or sets whether to enable proactive throttling based on rate limit response headers. Defaults to true.
  /// </summary>
  /// <remarks>
  ///   <para>
  ///     When enabled, the client will respect <c>RateLimit-Remaining</c> and <c>Retry-After</c>
  ///     headers from server responses to slow down requests before hitting hard limits.
  ///   </para>
  /// </remarks>
  public bool EnableProactiveThrottling { get; set; } = true;

  /// <summary>
  ///   Gets or sets the quota threshold at which proactive throttling begins. Defaults to 0.1 (10%).
  /// </summary>
  /// <remarks>
  ///   <para>
  ///     When the remaining quota drops below this percentage, requests will be delayed
  ///     to spread usage more evenly over the quota reset period.
  ///   </para>
  /// </remarks>
  public double QuotaLowThreshold { get; set; } = 0.1;
}
