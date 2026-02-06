namespace Smtp2Go.NET.Models.Webhooks;

using System.Text.Json.Serialization;

/// <summary>
///   Request model for the SMTP2GO webhook creation endpoint.
/// </summary>
/// <remarks>
///   <para>
///     Creates a new webhook subscription that will receive HTTP POST callbacks
///     when the specified email events occur. Webhooks enable real-time notification
///     of delivery status changes without polling the SMTP2GO API.
///   </para>
///   <para>
///     Use the <see cref="WebhookCreateEvent"/> enum for the <see cref="Events"/> array
///     to ensure only valid subscription-level event names are used.
///   </para>
///   <para>
///     <strong>Webhook Authentication:</strong> To require Basic Auth on webhook callbacks,
///     embed credentials in the URL using RFC 3986 userinfo syntax:
///     <c>https://username:password@host/path</c>. SMTP2GO extracts the credentials
///     and sends them as an <c>Authorization: Basic</c> header when delivering callbacks.
///   </para>
/// </remarks>
/// <example>
///   <code>
///     var request = new WebhookCreateRequest
///     {
///       // Embed Basic Auth credentials directly in the URL.
///       WebhookUrl = "https://webhook-user:secure-password@api.alos.app/webhooks/smtp2go",
///       Events = [WebhookCreateEvent.Delivered, WebhookCreateEvent.Bounce]
///     };
///   </code>
/// </example>
public class WebhookCreateRequest
{
  /// <summary>
  ///   Gets or sets the URL that will receive webhook event callbacks.
  /// </summary>
  /// <remarks>
  ///   <para>
  ///     The URL must be publicly accessible and accept HTTP POST requests.
  ///     SMTP2GO will send JSON payloads to this URL when subscribed events occur.
  ///     HTTPS is strongly recommended for production use.
  ///   </para>
  ///   <para>
  ///     To require Basic Auth on callbacks, embed credentials in the URL:
  ///     <c>https://username:password@host/path</c>. SMTP2GO extracts the userinfo
  ///     component and sends it as an <c>Authorization: Basic</c> header.
  ///   </para>
  /// </remarks>
  [JsonPropertyName("url")]
  public required string WebhookUrl { get; set; }

  /// <summary>
  ///   Gets or sets the event types to subscribe to.
  /// </summary>
  /// <remarks>
  ///   <para>
  ///     Use <see cref="WebhookCreateEvent"/> enum values (e.g.,
  ///     <see cref="WebhookCreateEvent.Delivered"/>,
  ///     <see cref="WebhookCreateEvent.Bounce"/>).
  ///     If null or empty, the webhook may receive all event types depending
  ///     on the SMTP2GO API default behavior.
  ///   </para>
  ///   <para>
  ///     <strong>Warning:</strong> SMTP2GO silently ignores unrecognized event names.
  ///     Using the <see cref="WebhookCreateEvent"/> enum prevents this class of error.
  ///   </para>
  /// </remarks>
  [JsonPropertyName("events")]
  public WebhookCreateEvent[]? Events { get; set; }

  /// <summary>
  ///   Gets or sets the sender usernames to filter webhook events by.
  /// </summary>
  /// <remarks>
  ///   <para>
  ///     When specified, the webhook will only fire for emails sent by the
  ///     listed SMTP2GO sender usernames. If null, the webhook fires for
  ///     all senders in the account.
  ///   </para>
  /// </remarks>
  [JsonPropertyName("usernames")]
  public string[]? Usernames { get; set; }

  /// <summary>
  ///   Gets or sets the output format for webhook payloads.
  /// </summary>
  /// <remarks>
  ///   <para>
  ///     Controls the format of the webhook payload sent to the callback URL.
  ///     Typically left null to use the SMTP2GO default JSON format.
  ///   </para>
  /// </remarks>
  [JsonPropertyName("output")]
  public string? Output { get; set; }
}
