namespace Smtp2Go.NET.Models.Webhooks;

using System.Text.Json.Serialization;

/// <summary>
///   Represents the payload received from an SMTP2GO webhook callback.
/// </summary>
/// <remarks>
///   <para>
///     SMTP2GO sends HTTP POST requests to registered webhook URLs when email
///     events occur. This model is the canonical in-memory representation of
///     JSON and form-encoded webhook callbacks.
///   </para>
///   <para>
///     Live callbacks captured in the integration test suite showed that SMTP2GO does not emit one
///     stable callback shape. The fields populated depend on the event type:
///     <list type="bullet">
///       <item><see cref="Recipient"/> (<c>rcpt</c>) is present for delivered and bounce events.</item>
///       <item><see cref="Recipients"/> is present for processed events (array of all recipients).</item>
///       <item><see cref="BounceType"/>, <see cref="BounceContext"/>, and <see cref="Host"/>
///             are present for bounce and delivered events.</item>
///       <item><see cref="ClickUrl"/> and <see cref="Link"/> are only present for click events.</item>
///       <item>
///         Processed and delivered callbacks include additional provider metadata such as
///         <c>id</c>, <c>auth</c>, <c>message-id</c>/<c>Message-Id</c>, <c>subject</c>/<c>Subject</c>,
///         <c>from</c>, <c>from_address</c>, and <c>from_name</c>.
///       </item>
///     </list>
///   </para>
/// </remarks>
/// <example>
///   <code>
///     // In an ASP.NET Core controller:
///     [HttpPost("webhooks/smtp2go")]
///     public async Task&lt;IActionResult&gt; HandleWebhook(CancellationToken cancellationToken)
///     {
///       WebhookCallbackPayload payload;
///
///       if (Request.HasFormContentType)
///       {
///         var form = await Request.ReadFormAsync(cancellationToken);
///         payload = WebhookCallbackPayloadParser.ParseFormValues(
///           form.SelectMany(pair => pair.Value.Select(value => new KeyValuePair&lt;string, string?&gt;(pair.Key, value))));
///       }
///       else
///       {
///         payload = await Request.ReadFromJsonAsync&lt;WebhookCallbackPayload&gt;(cancellationToken: cancellationToken)
///           ?? new WebhookCallbackPayload();
///       }
///
///       switch (payload.Event)
///       {
///         case WebhookCallbackEvent.Delivered:
///           // Handle delivery confirmation — payload.Recipient has the recipient
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
  ///   Gets the source host IP address of the SMTP2GO server that processed the email.
  /// </summary>
  /// <remarks>
  ///   Maps to the <c>srchost</c> field in the SMTP2GO webhook JSON payload.
  /// </remarks>
  [JsonPropertyName("srchost")]
  public string? SourceHost { get; init; }

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
  ///   Gets the SMTP message identifier observed in the webhook callback.
  /// </summary>
  /// <remarks>
  ///   <para>
  ///     This is distinct from <see cref="EmailId" />. <see cref="EmailId" /> is SMTP2GO's provider-side
  ///     correlation ID returned by the send API, while this property carries the RFC 5322 Message-ID style
  ///     value observed in webhook callbacks.
  ///   </para>
  ///   <para>
  ///     Live callbacks emitted this field with inconsistent casing (<c>Message-Id</c> and <c>message-id</c>).
  ///   </para>
  /// </remarks>
  [JsonPropertyName("message-id")]
  public string? MessageId { get; init; }

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
  ///   Gets the ISO 8601 timestamp when the event occurred.
  /// </summary>
  /// <remarks>
  ///   Maps to the <c>time</c> field in the SMTP2GO webhook JSON payload.
  ///   Format example: <c>2026-02-07T18:05:02Z</c>.
  /// </remarks>
  [JsonPropertyName("time")]
  public DateTimeOffset? Time { get; init; }

  /// <summary>
  ///   Gets the ISO 8601 timestamp when the email was sent by SMTP2GO.
  /// </summary>
  /// <remarks>
  ///   Maps to the <c>sendtime</c> field in the SMTP2GO webhook JSON payload.
  ///   Format example: <c>2026-02-07T18:05:02.199324+00:00</c>.
  /// </remarks>
  [JsonPropertyName("sendtime")]
  public DateTimeOffset? SendTime { get; init; }

  /// <summary>
  ///   Gets the message subject observed in the webhook callback.
  /// </summary>
  /// <remarks>
  ///   Live callbacks emitted this field with inconsistent casing (<c>Subject</c> and <c>subject</c>).
  /// </remarks>
  [JsonPropertyName("subject")]
  public string? Subject { get; init; }

  /// <summary>
  ///   Gets the provider-specific callback event identifier.
  /// </summary>
  /// <remarks>
  ///   This maps to the raw <c>id</c> field observed in live callbacks. Different events for the same
  ///   <see cref="EmailId" /> can carry different values, so this is treated as an event-level identifier.
  /// </remarks>
  [JsonPropertyName("id")]
  public string? EventId { get; init; }

  /// <summary>
  ///   Gets the opaque provider auth marker observed in live callbacks.
  /// </summary>
  /// <remarks>
  ///   The exact semantics are undocumented. Live callbacks included values such as a truncated API key
  ///   prefix, so the library preserves the field as opaque diagnostic metadata.
  /// </remarks>
  [JsonPropertyName("auth")]
  public string? Auth { get; init; }

  /// <summary>
  ///   Gets the per-event recipient email address.
  /// </summary>
  /// <remarks>
  ///   <para>
  ///     Maps to the <c>rcpt</c> field in the SMTP2GO webhook JSON payload.
  ///     Present for delivered and bounce events (one webhook per recipient).
  ///     Not present for processed events — use <see cref="Recipients"/> instead.
  ///   </para>
  /// </remarks>
  [JsonPropertyName("rcpt")]
  public string? Recipient { get; init; }

  /// <summary>
  ///   Gets the sender email address of the original email.
  /// </summary>
  [JsonPropertyName("sender")]
  public string? Sender { get; init; }

  /// <summary>
  ///   Gets the raw <c>from</c> field observed in live callbacks.
  /// </summary>
  /// <remarks>
  ///   Live callbacks included <c>sender</c>, <c>from</c>, and <c>from_address</c>. They carried the same
  ///   address in the captured delivered and processed payloads, so this property is preserved separately
  ///   to avoid losing transport detail.
  /// </remarks>
  [JsonPropertyName("from")]
  public string? From { get; init; }

  /// <summary>
  ///   Gets the raw <c>from_address</c> field observed in live callbacks.
  /// </summary>
  [JsonPropertyName("from_address")]
  public string? FromAddress { get; init; }

  /// <summary>
  ///   Gets the raw <c>from_name</c> field observed in live callbacks.
  /// </summary>
  [JsonPropertyName("from_name")]
  public string? FromName { get; init; }

  /// <summary>
  ///   Gets the list of all recipients of the original email.
  /// </summary>
  /// <remarks>
  ///   <para>
  ///     Maps to the <c>recipients</c> field in the SMTP2GO webhook JSON payload.
  ///     Present for processed events. For delivered/bounce events, use
  ///     <see cref="Recipient"/> (<c>rcpt</c>) which has the per-event recipient.
  ///   </para>
  /// </remarks>
  [JsonPropertyName("recipients")]
  public string[]? Recipients { get; init; }

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
  ///   Gets the diagnostic context from the recipient's mail server.
  /// </summary>
  /// <remarks>
  ///   <para>
  ///     Present for bounce and delivered events. For bounce events, contains
  ///     the SMTP transaction context (e.g., <c>"RCPT TO:&lt;user@example.com&gt;"</c>).
  ///     For delivered events, may contain <c>"Unavailable"</c>.
  ///   </para>
  /// </remarks>
  [JsonPropertyName("context")]
  public string? BounceContext { get; init; }

  /// <summary>
  ///   Gets the mail server host that the email was delivered to (or bounced from).
  /// </summary>
  /// <remarks>
  ///   <para>
  ///     Present for bounce and delivered events. Contains the MX host and IP address
  ///     (e.g., <c>"mail.protonmail.ch [176.119.200.128]"</c>).
  ///   </para>
  /// </remarks>
  [JsonPropertyName("host")]
  public string? Host { get; init; }

  /// <summary>
  ///   Gets the SMTP response message from the receiving mail server.
  /// </summary>
  /// <remarks>
  ///   <para>
  ///     Present for delivered events. Contains the SMTP 250 response
  ///     (e.g., <c>"250 2.0.0 Ok: 2788 bytes queued as 4f7f4b3tWbzKy"</c>).
  ///   </para>
  /// </remarks>
  [JsonPropertyName("message")]
  public string? SmtpResponse { get; init; }

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
