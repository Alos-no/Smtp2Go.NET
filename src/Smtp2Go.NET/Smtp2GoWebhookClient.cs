namespace Smtp2Go.NET;

using Core;
using Internal;
using Microsoft.Extensions.Logging;
using Models.Webhooks;

/// <summary>
///   Default implementation of <see cref="ISmtp2GoWebhookClient" />.
/// </summary>
/// <remarks>
///   <para>
///     This sub-client handles webhook management operations (create, list, delete) by
///     inheriting the shared <see cref="Smtp2GoResource.PostAsync{TRequest,TResponse}" />
///     helper from the base class.
///   </para>
/// </remarks>
internal sealed partial class Smtp2GoWebhookClient : Smtp2GoResource, ISmtp2GoWebhookClient
{
  #region Constants & Statics

  /// <summary>API endpoint for creating webhooks.</summary>
  private const string WebhookCreateEndpoint = "webhook/add";

  /// <summary>API endpoint for listing webhooks.</summary>
  private const string WebhookListEndpoint = "webhook/view";

  /// <summary>API endpoint for deleting webhooks.</summary>
  private const string WebhookDeleteEndpoint = "webhook/remove";

  #endregion


  #region Properties & Fields - Non-Public

  /// <summary>
  ///   Logger field required by <c>[LoggerMessage]</c> source generator.
  ///   Points to the same instance as the base class — see <see cref="Smtp2GoResource" /> remarks.
  /// </summary>
  // ReSharper disable once InconsistentNaming — required by LoggerMessage source generator convention.
  private readonly ILogger _logger;

  #endregion


  #region Constructors

  /// <summary>
  ///   Initializes a new instance of the <see cref="Smtp2GoWebhookClient" /> class.
  /// </summary>
  /// <param name="httpClient">The shared HTTP client from the parent <see cref="Smtp2GoClient" />.</param>
  /// <param name="logger">The shared logger from the parent <see cref="Smtp2GoClient" />.</param>
  internal Smtp2GoWebhookClient(HttpClient httpClient, ILogger logger)
    : base(httpClient, logger)
  {
    _logger = logger;
  }

  #endregion


  #region Methods - Public (ISmtp2GoWebhookClient)

  /// <inheritdoc />
  public async Task<WebhookCreateResponse> CreateAsync(
    WebhookCreateRequest request,
    CancellationToken ct = default)
  {
    ArgumentNullException.ThrowIfNull(request);

    LogWebhookCreateStarted(request.WebhookUrl);

    var response = await PostAsync<WebhookCreateRequest, WebhookCreateResponse>(
      WebhookCreateEndpoint, request, ct).ConfigureAwait(false);

    LogWebhookCreateCompleted(response.Data?.WebhookId);

    return response;
  }


  /// <inheritdoc />
  public async Task<WebhookListResponse> ListAsync(CancellationToken ct = default)
  {
    LogWebhookListRequested();

    // POST with empty body — the API requires a POST but no specific parameters for listing.
    var response = await PostAsync<object, WebhookListResponse>(
      WebhookListEndpoint, new { }, ct).ConfigureAwait(false);

    return response;
  }


  /// <inheritdoc />
  public async Task<WebhookDeleteResponse> DeleteAsync(int webhookId, CancellationToken ct = default)
  {
    LogWebhookDeleteStarted(webhookId);

    var request = new { id = webhookId };

    var response = await PostAsync<object, WebhookDeleteResponse>(
      WebhookDeleteEndpoint, request, ct).ConfigureAwait(false);

    LogWebhookDeleteCompleted(webhookId);

    return response;
  }

  #endregion


  #region Source-Generated Logging

  [LoggerMessage(LoggingConstants.EventIds.WebhookCreateStarted, LogLevel.Information,
    "Creating webhook for URL: {WebhookUrl}")]
  private partial void LogWebhookCreateStarted(string? webhookUrl);

  [LoggerMessage(LoggingConstants.EventIds.WebhookCreateCompleted, LogLevel.Information,
    "Webhook created with ID: {WebhookId}")]
  private partial void LogWebhookCreateCompleted(int? webhookId);

  [LoggerMessage(LoggingConstants.EventIds.WebhookListRequested, LogLevel.Debug,
    "Listing configured webhooks")]
  private partial void LogWebhookListRequested();

  [LoggerMessage(LoggingConstants.EventIds.WebhookDeleteStarted, LogLevel.Information,
    "Deleting webhook: {WebhookId}")]
  private partial void LogWebhookDeleteStarted(int webhookId);

  [LoggerMessage(LoggingConstants.EventIds.WebhookDeleteCompleted, LogLevel.Information,
    "Webhook deleted: {WebhookId}")]
  private partial void LogWebhookDeleteCompleted(int webhookId);

  #endregion
}
