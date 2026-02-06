namespace Smtp2Go.NET.Models.Webhooks;

using System.Text.Json.Serialization;

/// <summary>
///   Represents the payload received from an SMTP2GO webhook callback.
/// </summary>
/// <remarks>
///   <para>
///     SMTP2GO sends HTTP POST requests to registered webhook URLs when email
///     events occur. This model deserializes the inbound webhook payload.
///   </para>
///   <para>
///     The fields populated depend on the event type:
///     <list type="bullet">
///       <item><see cref="BounceType"/>, <see cref="BounceContext"/>, and <see cref="Host"/> are only present for bounce events.</item>
///       <item><see cref="ClickUrl"/> and <see cref="Link"/> are only present for click events.</item>
///     </list>
///   </para>
/// </remarks>
/// <example>
///   <code>
///     // In an ASP.NET Core controller:
///     [HttpPost("webhooks/smtp2go")]
///     public IActionResult HandleWebhook([FromBody] WebhookCallbackPayload payload)
///     {
///       switch (payload.Event)
///       {
///         case WebhookCallbackEvent.Delivered:
///           // Handle delivery confirmation
///           break;
///         case WebhookCallbackEvent.Bounce:
///           // Handle bounce — check payload.BounceType for hard/soft
///           break;
///       }
///       return Ok();
///     }
///   </code>
/// </example>
public class WebhookCallbackPayload
{
  /// <summary>
  ///   Gets the hostname of the SMTP2GO sending server that processed the email.
  /// </summary>
  [JsonPropertyName("hostname")]
  public string? Hostname { get; init; }

  /// <summary>
  ///   Gets the unique SMTP2GO identifier for the email associated with this event.
  /// </summary>
  /// <remarks>
  ///   <para>
  ///     This corresponds to the <c>email_id</c> returned by the
  ///     <c>/email/send</c> endpoint and can be used to correlate webhook
  ///     events with sent emails.
  ///   </para>
  /// </remarks>
  [JsonPropertyName("email_id")]
  public string? EmailId { get; init; }

  /// <summary>
  ///   Gets the type of event that triggered this webhook callback.
  /// </summary>
  /// <remarks>
  ///   <para>
  ///     The event type determines which additional fields are populated
  ///     in the payload. See <see cref="WebhookCallbackEvent"/> for all possible values.
  ///   </para>
  /// </remarks>
  [JsonPropertyName("event")]
  [JsonConverter(typeof(WebhookCallbackEventJsonConverter))]
  public WebhookCallbackEvent Event { get; init; }

  /// <summary>
  ///   Gets the Unix timestamp (seconds since epoch) when the event occurred.
  /// </summary>
  /// <remarks>
  ///   <para>
  ///     Convert to <see cref="DateTimeOffset"/> using
  ///     <see cref="DateTimeOffset.FromUnixTimeSeconds"/>.
  ///   </para>
  /// </remarks>
  [JsonPropertyName("timestamp")]
  public int Timestamp { get; init; }

  /// <summary>
  ///   Gets the recipient email address associated with this event.
  /// </summary>
  /// <remarks>
  ///   <para>
  ///     The specific recipient that this event applies to. For example,
  ///     a delivered event for a multi-recipient email will generate one
  ///     webhook per recipient.
  ///   </para>
  /// </remarks>
  [JsonPropertyName("email")]
  public string? Email { get; init; }

  /// <summary>
  ///   Gets the sender email address of the original email.
  /// </summary>
  [JsonPropertyName("sender")]
  public string? Sender { get; init; }

  /// <summary>
  ///   Gets the list of all recipients of the original email.
  /// </summary>
  /// <remarks>
  ///   <para>
  ///     Contains all To, CC, and BCC recipients from the original send request.
  ///   </para>
  /// </remarks>
  [JsonPropertyName("recipients_list")]
  public string[]? RecipientsList { get; init; }

  /// <summary>
  ///   Gets the bounce type when the event is a bounce.
  /// </summary>
  /// <remarks>
  ///   <para>
  ///     Only populated for <see cref="WebhookCallbackEvent.Bounce"/> events.
  ///     <see cref="Models.Webhooks.BounceType.Hard"/> indicates a permanent delivery
  ///     failure; <see cref="Models.Webhooks.BounceType.Soft"/> indicates a temporary failure.
  ///   </para>
  ///   <para>
  ///     SMTP2GO sends the bounce type as a separate <c>"bounce"</c> JSON field
  ///     (value: <c>"hard"</c> or <c>"soft"</c>), distinct from the <c>"event": "bounce"</c> field.
  ///   </para>
  /// </remarks>
  [JsonPropertyName("bounce")]
  [JsonConverter(typeof(BounceTypeJsonConverter))]
  public BounceType? BounceType { get; init; }

  /// <summary>
  ///   Gets the bounce diagnostic context from the recipient's mail server.
  /// </summary>
  /// <remarks>
  ///   <para>
  ///     Only populated for <see cref="WebhookCallbackEvent.Bounce"/> events. Contains
  ///     the SMTP transaction context (e.g., <c>"RCPT TO:&lt;user@example.com&gt;"</c>).
  ///   </para>
  /// </remarks>
  [JsonPropertyName("context")]
  public string? BounceContext { get; init; }

  /// <summary>
  ///   Gets the mail server host that the email was delivered to (or bounced from).
  /// </summary>
  /// <remarks>
  ///   <para>
  ///     Only populated for <see cref="WebhookCallbackEvent.Bounce"/> events. Contains the
  ///     MX host and IP address (e.g., <c>"gmail-smtp-in.l.google.com [209.85.233.26]"</c>).
  ///   </para>
  /// </remarks>
  [JsonPropertyName("host")]
  public string? Host { get; init; }

  /// <summary>
  ///   Gets the URL that was clicked by the recipient.
  /// </summary>
  /// <remarks>
  ///   <para>
  ///     Only populated for <see cref="WebhookCallbackEvent.Clicked"/> events.
  ///     Contains the original URL (before SMTP2GO tracking redirect).
  ///   </para>
  /// </remarks>
  [JsonPropertyName("click_url")]
  public string? ClickUrl { get; init; }

  /// <summary>
  ///   Gets the tracked link URL associated with the click event.
  /// </summary>
  /// <remarks>
  ///   <para>
  ///     Only populated for <see cref="WebhookCallbackEvent.Clicked"/> events.
  ///     This may be the SMTP2GO tracking URL or the original link,
  ///     depending on the webhook configuration.
  ///   </para>
  /// </remarks>
  [JsonPropertyName("link")]
  public string? Link { get; init; }
}
