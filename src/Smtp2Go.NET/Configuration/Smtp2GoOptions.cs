namespace Smtp2Go.NET.Configuration;

/// <summary>
///   Configuration options for the SMTP2GO API client.
/// </summary>
/// <remarks>
///   <para>
///     Configure these options in your <c>appsettings.json</c> under the "Smtp2Go" section,
///     or programmatically via the <see cref="ServiceCollectionExtensions" /> extension methods.
///   </para>
/// </remarks>
/// <example>
///   <code>
///   {
///     "Smtp2Go": {
///       "ApiKey": "api-XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX",
///       "BaseUrl": "https://api.smtp2go.com/v3/",
///       "Timeout": "00:00:30",
///       "Resilience": {
///         "MaxRetries": 3,
///         "PerAttemptTimeout": "00:00:30"
///       }
///     }
///   }
///   </code>
/// </example>
public sealed class Smtp2GoOptions
{
  #region Constants & Statics

  /// <summary>The configuration section name.</summary>
  public const string SectionName = "Smtp2Go";

  /// <summary>The default SMTP2GO API base URL.</summary>
  public const string DefaultBaseUrl = "https://api.smtp2go.com/v3/";

  #endregion


  #region Properties & Fields - Public

  /// <summary>
  ///   Gets or sets the SMTP2GO API key. This value must be provided.
  /// </summary>
  /// <remarks>
  ///   <para>
  ///     The API key is sent via the <c>X-Smtp2go-Api-Key</c> header on every request.
  ///     Obtain an API key from your SMTP2GO dashboard at https://app.smtp2go.com/settings/apikeys.
  ///   </para>
  /// </remarks>
  public string? ApiKey { get; set; }

  /// <summary>
  ///   Gets or sets the SMTP2GO API base URL. Defaults to <c>https://api.smtp2go.com/v3/</c>.
  /// </summary>
  public string BaseUrl { get; set; } = DefaultBaseUrl;

  /// <summary>
  ///   Gets or sets the HTTP request timeout. Defaults to 30 seconds.
  /// </summary>
  public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);

  /// <summary>
  ///   Gets or sets the HTTP resilience options for API calls.
  /// </summary>
  /// <remarks>
  ///   <para>
  ///     Configure retry policies, circuit breaker, and rate limiting for HTTP clients.
  ///     These settings apply when the library makes calls to the SMTP2GO API.
  ///   </para>
  /// </remarks>
  public ResilienceOptions Resilience { get; set; } = new();

  #endregion
}
