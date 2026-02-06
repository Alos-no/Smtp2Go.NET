namespace Smtp2Go.NET.Internal;

/// <summary>
///   Centralized logging category constants for the Smtp2Go.NET library.
/// </summary>
/// <remarks>
///   <para>
///     Using centralized category names ensures:
///     <list type="bullet">
///       <item>DRY principle - single source of truth for category names</item>
///       <item>Consistent logging across all library components</item>
///       <item>Independent verbosity tuning per component via logging configuration</item>
///       <item>Structured log filtering and analysis</item>
///     </list>
///   </para>
/// </remarks>
/// <example>
///   <code>
///   // In appsettings.json, configure per-category log levels:
///   {
///     "Logging": {
///       "LogLevel": {
///         "Smtp2Go.NET.Core": "Information",
///         "Smtp2Go.NET.Http": "Warning",
///         "Smtp2Go.NET.Http.Resilience": "Debug"
///       }
///     }
///   }
///   </code>
/// </example>
internal static class LoggingConstants
{
  /// <summary>
  ///   Logging category names for different library components.
  /// </summary>
  public static class Categories
  {
    /// <summary>Core client logging category.</summary>
    public const string Core = "Smtp2Go.NET.Core";

    /// <summary>HTTP client logging category.</summary>
    public const string Http = "Smtp2Go.NET.Http";

    /// <summary>HTTP resilience pipeline logging category (retries, circuit breaker, etc.).</summary>
    public const string HttpResilience = "Smtp2Go.NET.Http.Resilience";

    /// <summary>Configuration and options logging category.</summary>
    public const string Configuration = "Smtp2Go.NET.Configuration";

    /// <summary>Webhook sub-client logging category.</summary>
    public const string Webhooks = "Smtp2Go.NET.Webhooks";

    /// <summary>Statistics sub-client logging category.</summary>
    public const string Statistics = "Smtp2Go.NET.Statistics";
  }


  /// <summary>
  ///   Event IDs for structured logging.
  /// </summary>
  /// <remarks>
  ///   <para>
  ///     Event IDs enable structured log filtering and alerting.
  ///     Reserve ranges for different components:
  ///     <list type="bullet">
  ///       <item>100-199: Email send events</item>
  ///       <item>200-299: HTTP client events</item>
  ///       <item>300-399: Configuration events</item>
  ///       <item>400-499: Webhook events</item>
  ///       <item>500-599: Error events</item>
  ///     </list>
  ///   </para>
  /// </remarks>
  public static class EventIds
  {
    // Email send events (100-199)
    public const int EmailSendStarted = 100;
    public const int EmailSendCompleted = 101;
    public const int EmailSendFailed = 102;
    public const int EmailSummaryRequested = 110;

    // HTTP client events (200-299)
    public const int HttpRequestStarted = 200;
    public const int HttpRequestCompleted = 201;
    public const int HttpRequestFailed = 202;
    public const int HttpRetryAttempt = 210;
    public const int HttpCircuitBreakerOpened = 220;
    public const int HttpCircuitBreakerClosed = 221;
    public const int HttpRateLimited = 230;

    // Configuration events (300-399)
    public const int ConfigurationLoaded = 300;
    public const int ConfigurationValidationFailed = 301;

    // Webhook events (400-499)
    public const int WebhookCreateStarted = 400;
    public const int WebhookCreateCompleted = 401;
    public const int WebhookListRequested = 410;
    public const int WebhookDeleteStarted = 420;
    public const int WebhookDeleteCompleted = 421;

    // Error events (500-599)
    public const int UnexpectedError = 500;
    public const int OperationCancelled = 501;
    public const int ApiError = 510;
  }
}
