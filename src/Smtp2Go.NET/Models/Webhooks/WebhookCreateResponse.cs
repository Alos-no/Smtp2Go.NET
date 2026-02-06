namespace Smtp2Go.NET.Models.Webhooks;

using System.Text.Json.Serialization;

/// <summary>
///   Response model for the SMTP2GO webhook creation endpoint.
/// </summary>
/// <remarks>
///   <para>
///     Contains the unique identifier assigned to the newly created webhook.
///     This identifier is required for subsequent webhook management operations
///     (e.g., deletion).
///   </para>
/// </remarks>
public class WebhookCreateResponse : ApiResponse<WebhookCreateResponseData>;

/// <summary>
///   Data payload for the webhook creation response.
/// </summary>
public class WebhookCreateResponseData
{
  /// <summary>
  ///   Gets the unique identifier assigned to the newly created webhook.
  /// </summary>
  /// <remarks>
  ///   <para>
  ///     Store this identifier to manage the webhook later (e.g., deleting it
  ///     when it is no longer needed).
  ///   </para>
  /// </remarks>
  [JsonPropertyName("id")]
  public int? WebhookId { get; init; }
}
