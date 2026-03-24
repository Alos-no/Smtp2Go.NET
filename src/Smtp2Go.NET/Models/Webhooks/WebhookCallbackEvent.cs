namespace Smtp2Go.NET.Models.Webhooks;

using System.Text.Json;
using System.Text.Json.Serialization;

/// <summary>
///   Defines the event types returned in SMTP2GO webhook callback payloads.
/// </summary>
/// <remarks>
///   <para>
///     These are <strong>callback-level</strong> event names — received in
///     <see cref="WebhookCallbackPayload.Event"/> when SMTP2GO delivers a webhook
///     POST to the registered URL.
///   </para>
///   <para>
///     <strong>Important:</strong> Callback event names differ from subscription event
///     names (<see cref="WebhookCreateEvent"/>). For example, subscribing to
///     <see cref="WebhookCreateEvent.Open"/> produces callbacks with
///     <see cref="Opened"/> (<c>"opened"</c>).
///   </para>
///   <para>
///     The SMTP2GO API transmits these as snake_case strings (e.g.,
///     <c>"spam_complaint"</c>); the <see cref="WebhookCallbackEventJsonConverter"/>
///     handles conversion.
///   </para>
/// </remarks>
public enum WebhookCallbackEvent
{
  /// <summary>
  ///   An unrecognized or unmapped event type.
  /// </summary>
  /// <remarks>
  ///   <para>
  ///     Used as a fallback when the API returns an event type not yet
  ///     defined in this enum. Consumers should log and handle gracefully.
  ///   </para>
  /// </remarks>
  Unknown = 0,

  /// <summary>
  ///   The email was accepted and queued for delivery by SMTP2GO.
  /// </summary>
  Processed,

  /// <summary>
  ///   The email was successfully delivered to the recipient's mail server.
  /// </summary>
  Delivered,

  /// <summary>
  ///   The email bounced (hard or soft). Use <see cref="WebhookCallbackPayload.BounceType"/>
  ///   to distinguish between <see cref="Models.Webhooks.BounceType.Hard"/> and
  ///   <see cref="Models.Webhooks.BounceType.Soft"/>.
  /// </summary>
  /// <remarks>
  ///   <para>
  ///     SMTP2GO sends <c>"event": "bounce"</c> with a separate <c>"bounce"</c> field
  ///     containing <c>"hard"</c> or <c>"soft"</c>. The bounce diagnostic message is in
  ///     the <c>"context"</c> field.
  ///   </para>
  /// </remarks>
  Bounce,

  /// <summary>
  ///   The recipient opened the email.
  /// </summary>
  /// <remarks>
  ///   <para>
  ///     Open tracking relies on a tracking pixel and may not capture all opens
  ///     (e.g., plain text readers, image blocking).
  ///   </para>
  /// </remarks>
  Opened,

  /// <summary>
  ///   The recipient clicked a tracked link in the email.
  /// </summary>
  Clicked,

  /// <summary>
  ///   The recipient unsubscribed via the email's unsubscribe mechanism.
  /// </summary>
  Unsubscribed,

  /// <summary>
  ///   The recipient marked the email as spam/junk.
  /// </summary>
  /// <remarks>
  ///   <para>
  ///     Spam complaints can negatively impact sender reputation. The recipient
  ///     address should be immediately suppressed from future mailings.
  ///   </para>
  /// </remarks>
  SpamComplaint
}


/// <summary>
///   JSON converter for <see cref="WebhookCallbackEvent"/> that handles SMTP2GO's
///   snake_case string representation in webhook callback payloads.
/// </summary>
/// <remarks>
///   <para>
///     The SMTP2GO API uses snake_case strings for callback event types:
///     <list type="bullet">
///       <item><c>"processed"</c> -> <see cref="WebhookCallbackEvent.Processed"/></item>
///       <item><c>"delivered"</c> -> <see cref="WebhookCallbackEvent.Delivered"/></item>
///       <item><c>"bounce"</c> -> <see cref="WebhookCallbackEvent.Bounce"/></item>
///       <item><c>"opened"</c> -> <see cref="WebhookCallbackEvent.Opened"/></item>
///       <item><c>"clicked"</c> -> <see cref="WebhookCallbackEvent.Clicked"/></item>
///       <item><c>"unsubscribed"</c> -> <see cref="WebhookCallbackEvent.Unsubscribed"/></item>
///       <item><c>"spam_complaint"</c> -> <see cref="WebhookCallbackEvent.SpamComplaint"/></item>
///     </list>
///     Unrecognized values are deserialized as <see cref="WebhookCallbackEvent.Unknown"/>.
///   </para>
/// </remarks>
public class WebhookCallbackEventJsonConverter : JsonConverter<WebhookCallbackEvent>
{
  #region Methods - Public

  /// <summary>
  ///   Reads and converts a JSON string to a <see cref="WebhookCallbackEvent"/> value.
  /// </summary>
  /// <param name="reader">The JSON reader.</param>
  /// <param name="typeToConvert">The type to convert.</param>
  /// <param name="options">The serializer options.</param>
  /// <returns>The deserialized <see cref="WebhookCallbackEvent"/> value.</returns>
  public override WebhookCallbackEvent Read(
    ref Utf8JsonReader reader,
    Type typeToConvert,
    JsonSerializerOptions options)
  {
    return WebhookCallbackValueParser.ParseCallbackEvent(reader.GetString());
  }

  /// <summary>
  ///   Writes a <see cref="WebhookCallbackEvent"/> value as a JSON snake_case string.
  /// </summary>
  /// <param name="writer">The JSON writer.</param>
  /// <param name="value">The <see cref="WebhookCallbackEvent"/> value to write.</param>
  /// <param name="options">The serializer options.</param>
  public override void Write(
    Utf8JsonWriter writer,
    WebhookCallbackEvent value,
    JsonSerializerOptions options)
  {
    writer.WriteStringValue(WebhookCallbackValueParser.FormatCallbackEvent(value));
  }

  #endregion
}
