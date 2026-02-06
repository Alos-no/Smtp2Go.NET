namespace Smtp2Go.NET.Models.Webhooks;

using System.Text.Json;
using System.Text.Json.Serialization;

/// <summary>
///   Defines the event types that can be subscribed to when creating an SMTP2GO webhook.
/// </summary>
/// <remarks>
///   <para>
///     These are <strong>subscription-level</strong> event names — used in
///     <see cref="WebhookCreateRequest.Events"/> when registering a webhook via the
///     SMTP2GO <c>webhook/add</c> API endpoint.
///   </para>
///   <para>
///     <strong>Important:</strong> Subscription event names differ from the event names
///     returned in webhook callback payloads (<see cref="WebhookCallbackEvent"/>).
///     For example, subscribing to <see cref="Bounce"/> produces callback payloads with
///     <see cref="WebhookCallbackEvent.Bounce"/> and a separate
///     <see cref="WebhookCallbackPayload.BounceType"/> field for hard/soft classification.
///   </para>
///   <para>
///     Using an incorrect event name in the subscription is <strong>silently ignored</strong>
///     by SMTP2GO — no error is returned, and no webhook callbacks are delivered for that event.
///   </para>
/// </remarks>
/// <example>
///   <code>
///     var request = new WebhookCreateRequest
///     {
///       WebhookUrl = "https://user:pass@api.alos.app/webhooks/smtp2go",
///       Events =
///       [
///         WebhookCreateEvent.Delivered,
///         WebhookCreateEvent.Bounce,
///         WebhookCreateEvent.Spam
///       ]
///     };
///   </code>
/// </example>
[JsonConverter(typeof(WebhookCreateEventJsonConverter))]
public enum WebhookCreateEvent
{
  /// <summary>
  ///   The email was accepted and queued for delivery by SMTP2GO.
  /// </summary>
  Processed,

  /// <summary>
  ///   The email was successfully delivered to the recipient's mail server.
  /// </summary>
  Delivered,

  /// <summary>
  ///   The email bounced (hard or soft).
  /// </summary>
  /// <remarks>
  ///   <para>
  ///     Subscribing to this event produces callback payloads with
  ///     <see cref="WebhookCallbackEvent.Bounce"/>. Use <see cref="WebhookCallbackPayload.BounceType"/>
  ///     to distinguish <see cref="Models.Webhooks.BounceType.Hard"/> from
  ///     <see cref="Models.Webhooks.BounceType.Soft"/>.
  ///   </para>
  /// </remarks>
  Bounce,

  /// <summary>
  ///   The recipient opened the email.
  /// </summary>
  /// <remarks>
  ///   <para>
  ///     Subscription name: <c>"open"</c>.<br/>
  ///     Callback payload event: <see cref="WebhookCallbackEvent.Opened"/> (<c>"opened"</c>).
  ///   </para>
  /// </remarks>
  Open,

  /// <summary>
  ///   The recipient clicked a tracked link in the email.
  /// </summary>
  /// <remarks>
  ///   <para>
  ///     Subscription name: <c>"click"</c>.<br/>
  ///     Callback payload event: <see cref="WebhookCallbackEvent.Clicked"/> (<c>"clicked"</c>).
  ///   </para>
  /// </remarks>
  Click,

  /// <summary>
  ///   The recipient marked the email as spam/junk.
  /// </summary>
  /// <remarks>
  ///   <para>
  ///     Subscription name: <c>"spam"</c>.<br/>
  ///     Callback payload event: <see cref="WebhookCallbackEvent.SpamComplaint"/> (<c>"spam_complaint"</c>).
  ///   </para>
  /// </remarks>
  Spam,

  /// <summary>
  ///   The recipient unsubscribed via the email's unsubscribe mechanism.
  /// </summary>
  /// <remarks>
  ///   <para>
  ///     Subscription name: <c>"unsubscribe"</c>.<br/>
  ///     Callback payload event: <see cref="WebhookCallbackEvent.Unsubscribed"/> (<c>"unsubscribed"</c>).
  ///   </para>
  /// </remarks>
  Unsubscribe,

  /// <summary>
  ///   The recipient re-subscribed after a previous unsubscribe.
  /// </summary>
  Resubscribe,

  /// <summary>
  ///   The email was rejected by SMTP2GO before delivery.
  /// </summary>
  Reject
}


/// <summary>
///   JSON converter for <see cref="WebhookCreateEvent"/> that handles SMTP2GO's
///   subscription-level event name strings.
/// </summary>
/// <remarks>
///   <para>
///     The SMTP2GO <c>webhook/add</c> API expects lowercase event names:
///     <list type="bullet">
///       <item><c>"processed"</c> -> <see cref="WebhookCreateEvent.Processed"/></item>
///       <item><c>"delivered"</c> -> <see cref="WebhookCreateEvent.Delivered"/></item>
///       <item><c>"bounce"</c> -> <see cref="WebhookCreateEvent.Bounce"/></item>
///       <item><c>"open"</c> -> <see cref="WebhookCreateEvent.Open"/></item>
///       <item><c>"click"</c> -> <see cref="WebhookCreateEvent.Click"/></item>
///       <item><c>"spam"</c> -> <see cref="WebhookCreateEvent.Spam"/></item>
///       <item><c>"unsubscribe"</c> -> <see cref="WebhookCreateEvent.Unsubscribe"/></item>
///       <item><c>"resubscribe"</c> -> <see cref="WebhookCreateEvent.Resubscribe"/></item>
///       <item><c>"reject"</c> -> <see cref="WebhookCreateEvent.Reject"/></item>
///     </list>
///   </para>
/// </remarks>
public class WebhookCreateEventJsonConverter : JsonConverter<WebhookCreateEvent>
{
  #region Constants & Statics

  /// <summary>SMTP2GO API string for the "processed" subscription event.</summary>
  private const string ProcessedValue = "processed";

  /// <summary>SMTP2GO API string for the "delivered" subscription event.</summary>
  private const string DeliveredValue = "delivered";

  /// <summary>SMTP2GO API string for the "bounce" subscription event.</summary>
  private const string BounceValue = "bounce";

  /// <summary>SMTP2GO API string for the "open" subscription event.</summary>
  private const string OpenValue = "open";

  /// <summary>SMTP2GO API string for the "click" subscription event.</summary>
  private const string ClickValue = "click";

  /// <summary>SMTP2GO API string for the "spam" subscription event.</summary>
  private const string SpamValue = "spam";

  /// <summary>SMTP2GO API string for the "unsubscribe" subscription event.</summary>
  private const string UnsubscribeValue = "unsubscribe";

  /// <summary>SMTP2GO API string for the "resubscribe" subscription event.</summary>
  private const string ResubscribeValue = "resubscribe";

  /// <summary>SMTP2GO API string for the "reject" subscription event.</summary>
  private const string RejectValue = "reject";

  #endregion


  #region Methods - Public

  /// <summary>
  ///   Reads and converts a JSON string to a <see cref="WebhookCreateEvent"/> value.
  /// </summary>
  /// <param name="reader">The JSON reader.</param>
  /// <param name="typeToConvert">The type to convert.</param>
  /// <param name="options">The serializer options.</param>
  /// <returns>The deserialized <see cref="WebhookCreateEvent"/> value.</returns>
  /// <exception cref="JsonException">Thrown when the JSON string is not a recognized subscription event.</exception>
  public override WebhookCreateEvent Read(
    ref Utf8JsonReader reader,
    Type typeToConvert,
    JsonSerializerOptions options)
  {
    var value = reader.GetString();

    return value switch
    {
      ProcessedValue => WebhookCreateEvent.Processed,
      DeliveredValue => WebhookCreateEvent.Delivered,
      BounceValue => WebhookCreateEvent.Bounce,
      OpenValue => WebhookCreateEvent.Open,
      ClickValue => WebhookCreateEvent.Click,
      SpamValue => WebhookCreateEvent.Spam,
      UnsubscribeValue => WebhookCreateEvent.Unsubscribe,
      ResubscribeValue => WebhookCreateEvent.Resubscribe,
      RejectValue => WebhookCreateEvent.Reject,
      _ => throw new JsonException($"Unknown SMTP2GO subscription event: '{value}'.")
    };
  }

  /// <summary>
  ///   Writes a <see cref="WebhookCreateEvent"/> value as a JSON string.
  /// </summary>
  /// <param name="writer">The JSON writer.</param>
  /// <param name="value">The <see cref="WebhookCreateEvent"/> value to write.</param>
  /// <param name="options">The serializer options.</param>
  public override void Write(
    Utf8JsonWriter writer,
    WebhookCreateEvent value,
    JsonSerializerOptions options)
  {
    var stringValue = value switch
    {
      WebhookCreateEvent.Processed => ProcessedValue,
      WebhookCreateEvent.Delivered => DeliveredValue,
      WebhookCreateEvent.Bounce => BounceValue,
      WebhookCreateEvent.Open => OpenValue,
      WebhookCreateEvent.Click => ClickValue,
      WebhookCreateEvent.Spam => SpamValue,
      WebhookCreateEvent.Unsubscribe => UnsubscribeValue,
      WebhookCreateEvent.Resubscribe => ResubscribeValue,
      WebhookCreateEvent.Reject => RejectValue,
      _ => throw new JsonException($"Unknown WebhookCreateEvent value: '{value}'.")
    };

    writer.WriteStringValue(stringValue);
  }

  #endregion
}
