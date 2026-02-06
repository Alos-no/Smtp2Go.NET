namespace Smtp2Go.NET.Http;

using System.Net;
using System.Threading.RateLimiting;
using Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Retry;
using Polly.Timeout;

/// <summary>
///   Extension methods for configuring HTTP clients with resilience policies.
/// </summary>
/// <remarks>
///   <para>
///     These methods configure HTTP clients with production-ready resilience including:
///     <list type="bullet">
///       <item>Retry with exponential backoff (idempotent methods only)</item>
///       <item>Circuit breaker to prevent cascading failures</item>
///       <item>Per-attempt and total request timeouts</item>
///       <item>Client-side rate limiting</item>
///     </list>
///   </para>
///   <para>
///     <b>Note:</b> SMTP2GO API uses POST for all endpoints. POST requests are NOT retried
///     to prevent duplicate email sends. This is by design — retrying a POST /email/send
///     could result in the recipient receiving the same email multiple times.
///   </para>
/// </remarks>
public static class HttpClientExtensions
{
  #region Constants & Statics

  /// <summary>
  ///   The default HTTP client name prefix for named clients.
  /// </summary>
  internal const string HttpClientNamePrefix = "Smtp2GoClient";

  /// <summary>
  ///   HTTP methods that are safe to retry (idempotent methods).
  /// </summary>
  private static readonly HashSet<HttpMethod> IdempotentMethods =
  [
    HttpMethod.Get,
    HttpMethod.Head,
    HttpMethod.Options,
    HttpMethod.Trace,
    HttpMethod.Put,
    HttpMethod.Delete
  ];

  #endregion


  #region Methods - Public

  /// <summary>
  ///   Gets the full HTTP client name for a named client.
  /// </summary>
  /// <param name="clientName">The client name, or null for the default client.</param>
  /// <returns>The full HTTP client name.</returns>
  public static string GetHttpClientName(string? clientName = null)
  {
    return string.IsNullOrEmpty(clientName)
      ? HttpClientNamePrefix
      : $"{HttpClientNamePrefix}:{clientName}";
  }


  /// <summary>
  ///   Adds an HTTP client with resilience policies configured from options.
  /// </summary>
  /// <param name="services">The service collection.</param>
  /// <param name="clientName">Optional client name for named clients.</param>
  /// <returns>The HTTP client builder for further configuration.</returns>
  public static IHttpClientBuilder AddSmtp2GoHttpClient(
    this IServiceCollection services,
    string? clientName = null)
  {
    var httpClientName = GetHttpClientName(clientName);

    // Create the HTTP client builder.
    var builder = services.AddHttpClient(httpClientName);

    // Add the resilience handler to the builder.
    // Note: AddResilienceHandler returns IHttpResiliencePipelineBuilder, not IHttpClientBuilder,
    // so we call it for its side effect and return the original builder.
    AddResilienceHandler(builder, clientName);

    return builder;
  }

  #endregion


  #region Methods - Private

  /// <summary>
  ///   Adds a resilience handler to the HTTP client builder.
  /// </summary>
  /// <param name="builder">The HTTP client builder.</param>
  /// <param name="clientName">Optional client name for options resolution.</param>
  private static void AddResilienceHandler(IHttpClientBuilder builder, string? clientName)
  {
    var pipelineName = clientName is null ? "Smtp2GoPipeline" : $"Smtp2GoPipeline:{clientName}";

    builder.AddResilienceHandler(pipelineName, (pipelineBuilder, context) =>
    {
      // Resolve options at runtime to allow configuration changes.
      var options = context.ServiceProvider
        .GetRequiredService<IOptionsMonitor<Smtp2GoOptions>>()
        .Get(clientName ?? Options.DefaultName);

      ConfigureResiliencePipeline(pipelineBuilder, options.Resilience, clientName);
    });
  }


  /// <summary>
  ///   Configures the resilience pipeline with retries, circuit breaker, and rate limiting.
  /// </summary>
  /// <param name="builder">The resilience pipeline builder.</param>
  /// <param name="options">The resilience options.</param>
  /// <param name="clientName">The client name for logging/metrics.</param>
  /// <remarks>
  ///   <para>
  ///     The pipeline order follows Microsoft's recommended standard pipeline:
  ///     Rate Limiter -> Total Timeout -> Retry -> Circuit Breaker -> Attempt Timeout
  ///   </para>
  ///   <para>
  ///     Ref: https://learn.microsoft.com/en-us/dotnet/core/resilience/http-resilience#standard-pipeline
  ///   </para>
  /// </remarks>
  internal static void ConfigureResiliencePipeline(
    ResiliencePipelineBuilder<HttpResponseMessage> builder,
    ResilienceOptions options,
    string? clientName = null)
  {
    var namePrefix = string.IsNullOrEmpty(clientName) ? "Smtp2Go" : $"Smtp2Go:{clientName}";

    // 1. OUTERMOST: Client-side rate limiting (if enabled).
    if (options.RateLimiting.IsEnabled)
    {
      builder.AddRateLimiter(new HttpRateLimiterStrategyOptions
      {
        Name = $"{namePrefix}:RateLimiter",
        DefaultRateLimiterOptions = new ConcurrencyLimiterOptions
        {
          PermitLimit = options.RateLimiting.PermitLimit,
          QueueLimit = options.RateLimiting.QueueLimit,
          QueueProcessingOrder = QueueProcessingOrder.OldestFirst
        }
      });
    }

    // 2. Total request timeout (outer timeout covering all retries).
    builder.AddTimeout(new HttpTimeoutStrategyOptions
    {
      Name = $"{namePrefix}:TotalTimeout",
      Timeout = options.TotalRequestTimeout
    });

    // 3. Retry strategy with exponential backoff.
    // Only idempotent methods (GET, PUT, DELETE, etc.) are retried.
    // POST requests (all SMTP2GO API calls) are NOT retried to prevent duplicate emails.
    if (options.MaxRetries > 0)
    {
      builder.AddRetry(new HttpRetryStrategyOptions
      {
        Name = $"{namePrefix}:Retry",
        MaxRetryAttempts = options.MaxRetries,
        Delay = options.RetryBaseDelay,
        BackoffType = DelayBackoffType.Exponential,
        UseJitter = true,
        ShouldHandle = args => ShouldRetry(args, options)
      });
    }

    // 4. Circuit breaker to prevent cascading failures.
    builder.AddCircuitBreaker(new HttpCircuitBreakerStrategyOptions
    {
      Name = $"{namePrefix}:CircuitBreaker",
      FailureRatio = options.CircuitBreakerFailureThreshold,
      SamplingDuration = options.CircuitBreakerSamplingDuration,
      MinimumThroughput = options.CircuitBreakerMinimumThroughput,
      BreakDuration = options.CircuitBreakerBreakDuration,
      ShouldHandle = args => ValueTask.FromResult(IsTransientFailure(args.Outcome))
    });

    // 5. INNERMOST: Per-attempt timeout (inner timeout for each request).
    builder.AddTimeout(new HttpTimeoutStrategyOptions
    {
      Name = $"{namePrefix}:AttemptTimeout",
      Timeout = options.PerAttemptTimeout
    });
  }


  /// <summary>
  ///   Determines whether a request should be retried.
  /// </summary>
  private static ValueTask<bool> ShouldRetry(
    RetryPredicateArguments<HttpResponseMessage> args,
    ResilienceOptions options)
  {
    // Don't retry non-idempotent methods (POST, PATCH) to prevent duplicate operations.
    // The request message is available via the response's RequestMessage property.
    var method = args.Outcome.Result?.RequestMessage?.Method;

    if (method is not null && !IdempotentMethods.Contains(method))
    {
      return new ValueTask<bool>(false);
    }

    // Retry transient exceptions (network errors, timeouts).
    if (args.Outcome.Exception is HttpRequestException or TimeoutRejectedException)
    {
      return new ValueTask<bool>(true);
    }

    // Check HTTP response status codes.
    if (args.Outcome.Result is not { } response)
    {
      return new ValueTask<bool>(false);
    }

    var statusCode = response.StatusCode;

    // Retry on 408 (Request Timeout) and 5xx server errors.
    if (statusCode == HttpStatusCode.RequestTimeout || (int)statusCode >= 500)
    {
      return new ValueTask<bool>(true);
    }

    // Retry on 429 (Too Many Requests) only when rate limiting is enabled.
    if (statusCode == HttpStatusCode.TooManyRequests)
    {
      return new ValueTask<bool>(options.RateLimiting.IsEnabled);
    }

    return new ValueTask<bool>(false);
  }


  /// <summary>
  ///   Determines whether an outcome represents a transient failure that may succeed on retry.
  /// </summary>
  /// <remarks>
  ///   <para>
  ///     This method is used by the circuit breaker to determine whether a failure should
  ///     count towards opening the circuit.
  ///   </para>
  /// </remarks>
  private static bool IsTransientFailure(Outcome<HttpResponseMessage> outcome)
  {
    // Exception-based failures (network errors, Polly timeouts).
    // TimeoutRejectedException is thrown by Polly when the timeout strategy triggers.
    if (outcome.Exception is HttpRequestException or TimeoutRejectedException)
    {
      return true;
    }

    // Response-based failures (server errors).
    if (outcome.Result is null)
    {
      return false;
    }

    return outcome.Result.StatusCode is
      HttpStatusCode.RequestTimeout or        // 408
      HttpStatusCode.InternalServerError or   // 500
      HttpStatusCode.BadGateway or            // 502
      HttpStatusCode.ServiceUnavailable or    // 503
      HttpStatusCode.GatewayTimeout;          // 504
  }

  #endregion
}
