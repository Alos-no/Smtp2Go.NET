namespace Smtp2Go.NET.Models.Webhooks;

using System.Text.Json.Serialization;

/// <summary>
///   Response model for the SMTP2GO webhook listing endpoint.
/// </summary>
/// <remarks>
///   <para>
///     Contains an array of all webhook subscriptions configured for the account.
///     Each <see cref="WebhookInfo"/> entry describes a single webhook including
///     its URL, subscribed events, and creation date.
///   </para>
/// </remarks>
public class WebhookListResponse : ApiResponse<WebhookInfo[]>;

/// <summary>
///   Represents a single webhook subscription in the SMTP2GO account.
/// </summary>
/// <remarks>
///   <para>
///     This model describes an existing webhook configuration, including
///     the events it is subscribed to and the URL that receives callbacks.
///   </para>
/// </remarks>
public class WebhookInfo
{
  /// <summary>
  ///   Gets the unique identifier of the webhook.
  /// </summary>
  [JsonPropertyName("id")]
  public int? WebhookId { get; init; }

  /// <summary>
  ///   Gets the URL that receives webhook event callbacks.
  /// </summary>
  [JsonPropertyName("url")]
  public string? WebhookUrl { get; init; }

  /// <summary>
  ///   Gets the event types this webhook is subscribed to.
  /// </summary>
  /// <remarks>
  ///   <para>
  ///     Values correspond to SMTP2GO subscription-level event names
  ///     (see <see cref="WebhookCreateEvent"/> for the strongly-typed equivalent).
  ///   </para>
  /// </remarks>
  [JsonPropertyName("events")]
  public string[]? Events { get; init; }

  /// <summary>
  ///   Gets the sender usernames this webhook is filtered by.
  /// </summary>
  /// <remarks>
  ///   <para>
  ///     If null or empty, the webhook fires for all senders in the account.
  ///   </para>
  /// </remarks>
  [JsonPropertyName("usernames")]
  public string[]? Usernames { get; init; }

  /// <summary>
  ///   Gets the output format configured for this webhook's payloads.
  /// </summary>
  [JsonPropertyName("output_format")]
  public string? OutputFormat { get; init; }
}
