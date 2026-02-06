namespace Smtp2Go.NET;

using Models.Webhooks;

/// <summary>
///   Provides the client interface for SMTP2GO webhook management operations.
/// </summary>
/// <remarks>
///   <para>
///     This sub-client handles webhook lifecycle operations: creating, listing, and deleting
///     webhooks that receive email event notifications from SMTP2GO.
///   </para>
///   <para>
///     Access this interface via <see cref="ISmtp2GoClient.Webhooks" />.
///   </para>
/// </remarks>
public interface ISmtp2GoWebhookClient
{
  /// <summary>
  ///   Creates a new webhook subscription.
  /// </summary>
  /// <param name="request">The webhook creation request containing URL, events, and optional authentication.</param>
  /// <param name="ct">The cancellation token.</param>
  /// <returns>The webhook creation response containing the new webhook ID.</returns>
  /// <exception cref="Exceptions.Smtp2GoApiException">Thrown when the SMTP2GO API returns an error.</exception>
  Task<WebhookCreateResponse> CreateAsync(WebhookCreateRequest request, CancellationToken ct = default);

  /// <summary>
  ///   Lists all configured webhooks.
  /// </summary>
  /// <param name="ct">The cancellation token.</param>
  /// <returns>The response containing an array of webhook information.</returns>
  /// <exception cref="Exceptions.Smtp2GoApiException">Thrown when the SMTP2GO API returns an error.</exception>
  Task<WebhookListResponse> ListAsync(CancellationToken ct = default);

  /// <summary>
  ///   Deletes a webhook by its ID.
  /// </summary>
  /// <param name="webhookId">The ID of the webhook to delete.</param>
  /// <param name="ct">The cancellation token.</param>
  /// <returns>The deletion response.</returns>
  /// <exception cref="Exceptions.Smtp2GoApiException">Thrown when the SMTP2GO API returns an error.</exception>
  Task<WebhookDeleteResponse> DeleteAsync(int webhookId, CancellationToken ct = default);
}
