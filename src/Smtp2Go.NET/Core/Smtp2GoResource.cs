namespace Smtp2Go.NET.Core;

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Exceptions;
using Internal;
using Microsoft.Extensions.Logging;
using Models;

/// <summary>
///   Base class for SMTP2GO API resource clients, providing shared HTTP infrastructure.
/// </summary>
/// <remarks>
///   <para>
///     All SMTP2GO API endpoints use POST requests. This base class provides a single
///     <see cref="PostAsync{TRequest,TResponse}" /> implementation that handles serialization,
///     error parsing, and deserialization — eliminating duplication across sub-clients.
///   </para>
///   <para>
///     Modeled after the <c>Cloudflare.NET.Core.ApiResource</c> pattern, but simplified
///     for the SMTP2GO API which is exclusively POST-based.
///   </para>
/// </remarks>
internal abstract partial class Smtp2GoResource
{
  #region Properties & Fields - Non-Public

  /// <summary>The configured HttpClient for making API requests.</summary>
  protected readonly HttpClient HttpClient;

  /// <summary>
  ///   The logger for this API resource. Required by the <c>[LoggerMessage]</c> source generator
  ///   which looks for a field of type <see cref="ILogger" /> in the declaring class.
  /// </summary>
  /// <remarks>
  ///   Subclasses that use <c>[LoggerMessage]</c> must declare their own <c>_logger</c> field
  ///   (pointing to the same instance) because the source generator only inspects the immediate type.
  /// </remarks>
  // ReSharper disable once InconsistentNaming — required by LoggerMessage source generator convention.
  private readonly ILogger _logger;

  #endregion


  #region Constructors

  /// <summary>
  ///   Initializes a new instance of the <see cref="Smtp2GoResource" /> class.
  /// </summary>
  /// <param name="httpClient">The HttpClient to use for requests.</param>
  /// <param name="logger">The logger for this API resource.</param>
  protected Smtp2GoResource(HttpClient httpClient, ILogger logger)
  {
    HttpClient = httpClient;
    _logger = logger;
  }

  #endregion


  #region Methods - Protected (Shared POST Helper)

  /// <summary>
  ///   Sends a POST request to the SMTP2GO API and deserializes the response.
  /// </summary>
  /// <typeparam name="TRequest">The request body type.</typeparam>
  /// <typeparam name="TResponse">The response type.</typeparam>
  /// <param name="endpoint">The API endpoint (relative to BaseAddress).</param>
  /// <param name="request">The request body.</param>
  /// <param name="ct">The cancellation token.</param>
  /// <returns>The deserialized response.</returns>
  /// <exception cref="Smtp2GoApiException">Thrown when the API returns a non-success response.</exception>
  protected async Task<TResponse> PostAsync<TRequest, TResponse>(
    string endpoint,
    TRequest request,
    CancellationToken ct)
    where TResponse : class
  {
    // Serialize and send the request.
    using var httpResponse = await HttpClient.PostAsJsonAsync(
      endpoint, request, Smtp2GoJsonDefaults.Options, ct).ConfigureAwait(false);

    // Handle non-success HTTP status codes.
    if (!httpResponse.IsSuccessStatusCode)
    {
      var errorBody = await httpResponse.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
      var errorMessage = ParseErrorMessage(errorBody);
      var requestId = ParseRequestId(errorBody);

      LogApiError(endpoint, (int)httpResponse.StatusCode, errorMessage);

      throw new Smtp2GoApiException(
        $"SMTP2GO API request to '{endpoint}' failed with status {(int)httpResponse.StatusCode}: {errorMessage}",
        httpResponse.StatusCode,
        errorMessage,
        requestId);
    }

    // Deserialize the response body.
    var result = await httpResponse.Content.ReadFromJsonAsync<TResponse>(
      Smtp2GoJsonDefaults.Options, ct).ConfigureAwait(false);

    if (result is null)
    {
      throw new Smtp2GoApiException(
        $"SMTP2GO API returned null response for '{endpoint}'.",
        httpResponse.StatusCode);
    }

    // Check for API-level errors in the response envelope.
    if (result is ApiResponse<object> apiResponse && apiResponse.Data is null && httpResponse.StatusCode == HttpStatusCode.OK)
    {
      // Some SMTP2GO endpoints return 200 with error data — check for these.
      LogApiError(endpoint, 200, "Response data is null");
    }

    return result;
  }

  #endregion


  #region Methods - Private (Error Parsing)

  /// <summary>
  ///   Attempts to parse an error message from the SMTP2GO API error response body.
  /// </summary>
  /// <param name="responseBody">The raw response body.</param>
  /// <returns>The extracted error message, or the raw body if parsing fails.</returns>
  private static string? ParseErrorMessage(string? responseBody)
  {
    if (string.IsNullOrWhiteSpace(responseBody))
    {
      return null;
    }

    try
    {
      using var doc = JsonDocument.Parse(responseBody);

      // Try "data.error" (common SMTP2GO error format).
      if (doc.RootElement.TryGetProperty("data", out var data) &&
          data.TryGetProperty("error", out var error))
      {
        return error.GetString();
      }

      // Try "data.error_code".
      if (doc.RootElement.TryGetProperty("data", out var data2) &&
          data2.TryGetProperty("error_code", out var errorCode))
      {
        return errorCode.GetString();
      }

      return responseBody;
    }
    catch (JsonException)
    {
      return responseBody;
    }
  }


  /// <summary>
  ///   Attempts to parse the request ID from the SMTP2GO API response body.
  /// </summary>
  /// <param name="responseBody">The raw response body.</param>
  /// <returns>The request ID, or null if not found.</returns>
  private static string? ParseRequestId(string? responseBody)
  {
    if (string.IsNullOrWhiteSpace(responseBody))
    {
      return null;
    }

    try
    {
      using var doc = JsonDocument.Parse(responseBody);

      if (doc.RootElement.TryGetProperty("request_id", out var requestId))
      {
        return requestId.GetString();
      }

      return null;
    }
    catch (JsonException)
    {
      return null;
    }
  }

  #endregion


  #region Source-Generated Logging

  /// <summary>Logs an SMTP2GO API error with endpoint, status code, and error message.</summary>
  [LoggerMessage(LoggingConstants.EventIds.ApiError, LogLevel.Error,
    "SMTP2GO API error on {Endpoint}: HTTP {StatusCode} - {ErrorMessage}")]
  private partial void LogApiError(string endpoint, int statusCode, string? errorMessage);

  #endregion
}
