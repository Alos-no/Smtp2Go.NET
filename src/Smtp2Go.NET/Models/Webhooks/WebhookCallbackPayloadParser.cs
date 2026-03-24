namespace Smtp2Go.NET.Models.Webhooks;

/// <summary>
///   Converts SMTP2GO form-encoded webhook fields into <see cref="WebhookCallbackPayload" />.
/// </summary>
/// <remarks>
///   <para>
///     SMTP2GO webhook callbacks are not guaranteed to arrive as JSON. When a callback is delivered
///     as <c>application/x-www-form-urlencoded</c> or <c>multipart/form-data</c>, callers can flatten
///     the inbound form values into key/value pairs and pass them to this parser.
///   </para>
///   <para>
///     Live captures showed that form callbacks also vary by event type. For example, processed events
///     carried <c>recipients</c> and <c>srchost</c>, while delivered events carried <c>rcpt</c>,
///     <c>context</c>, <c>host</c>, and <c>message</c>.
///   </para>
///   <para>
///     The resulting payload uses the same canonical model as JSON callbacks so downstream application
///     code does not need separate handling for the transport format.
///   </para>
/// </remarks>
public static class WebhookCallbackPayloadParser
{

  #region Methods - Public

  /// <summary>
  ///   Parses flattened SMTP2GO form values into a <see cref="WebhookCallbackPayload" />.
  /// </summary>
  /// <param name="formValues">
  ///   The flattened form values. Duplicate keys are allowed and are expected for repeated
  ///   fields such as <c>recipients</c>.
  /// </param>
  /// <returns>The parsed <see cref="WebhookCallbackPayload" />.</returns>
  public static WebhookCallbackPayload ParseFormValues(IEnumerable<KeyValuePair<string, string?>> formValues)
  {
    ArgumentNullException.ThrowIfNull(formValues);

    string? sourceHost = null;
    string? emailId = null;
    string? messageId = null;
    string? eventValue = null;
    string? time = null;
    string? sendTime = null;
    string? subject = null;
    string? eventId = null;
    string? auth = null;
    string? recipient = null;
    string? sender = null;
    string? from = null;
    string? fromAddress = null;
    string? fromName = null;
    string? bounceType = null;
    string? bounceContext = null;
    string? host = null;
    string? smtpResponse = null;
    string? clickUrl = null;
    string? link = null;
    List<string?>? recipients = null;

    foreach (var pair in formValues)
    {
      var normalizedKey = NormalizeKey(pair.Key);

      if (normalizedKey is null)
      {
        continue;
      }

      switch (normalizedKey)
      {
        case "srchost":
          sourceHost = pair.Value;
          break;

        case "email_id":
          emailId = pair.Value;
          break;

        case "message-id":
        case "message_id":
          messageId = pair.Value;
          break;

        case "event":
          eventValue = pair.Value;
          break;

        case "time":
          time = pair.Value;
          break;

        case "sendtime":
          sendTime = pair.Value;
          break;

        case "subject":
          subject = pair.Value;
          break;

        case "id":
          eventId = pair.Value;
          break;

        case "auth":
          auth = pair.Value;
          break;

        case "rcpt":
          recipient = pair.Value;
          break;

        case "sender":
          sender = pair.Value;
          break;

        case "from":
          from = pair.Value;
          break;

        case "from_address":
          fromAddress = pair.Value;
          break;

        case "from_name":
          fromName = pair.Value;
          break;

        case "recipients":
          recipients ??= [];
          recipients.Add(pair.Value);
          break;

        case "bounce":
          bounceType = pair.Value;
          break;

        case "context":
          bounceContext = pair.Value;
          break;

        case "host":
          host = pair.Value;
          break;

        case "message":
          smtpResponse = pair.Value;
          break;

        case "click_url":
          clickUrl = pair.Value;
          break;

        case "link":
          link = pair.Value;
          break;
      }
    }

    return new WebhookCallbackPayload
    {
      SourceHost = sourceHost,
      EmailId = emailId,
      MessageId = messageId,
      Event = WebhookCallbackValueParser.ParseCallbackEvent(eventValue),
      Time = WebhookCallbackValueParser.ParseDateTimeOffset(time),
      SendTime = WebhookCallbackValueParser.ParseDateTimeOffset(sendTime),
      Subject = subject,
      EventId = eventId,
      Auth = auth,
      Recipient = recipient,
      Sender = sender,
      From = from,
      FromAddress = fromAddress,
      FromName = fromName,
      Recipients = recipients is null
        ? null
        : WebhookCallbackValueParser.ParseRecipients(recipients),
      BounceType = WebhookCallbackValueParser.ParseBounceType(bounceType),
      BounceContext = bounceContext,
      Host = host,
      SmtpResponse = smtpResponse,
      ClickUrl = clickUrl,
      Link = link
    };
  }


  /// <summary>
  ///   Parses grouped SMTP2GO form values into a <see cref="WebhookCallbackPayload" />.
  /// </summary>
  /// <param name="formValues">
  ///   The grouped form values, where each key maps to zero or more submitted values.
  /// </param>
  /// <returns>The parsed <see cref="WebhookCallbackPayload" />.</returns>
  public static WebhookCallbackPayload ParseFormValues(IReadOnlyDictionary<string, string[]?> formValues)
  {
    ArgumentNullException.ThrowIfNull(formValues);

    return ParseFormValues(Flatten(formValues));
  }

  #endregion


  #region Methods - Private

  /// <summary>
  ///   Flattens grouped form values into duplicate-friendly key/value pairs.
  /// </summary>
  /// <param name="formValues">The grouped form values.</param>
  /// <returns>The flattened values.</returns>
  private static IEnumerable<KeyValuePair<string, string?>> Flatten(IReadOnlyDictionary<string, string[]?> formValues)
  {
    foreach (var pair in formValues)
    {
      if (pair.Value is null || pair.Value.Length == 0)
      {
        yield return new KeyValuePair<string, string?>(pair.Key, null);

        continue;
      }

      foreach (var value in pair.Value)
      {
        yield return new KeyValuePair<string, string?>(pair.Key, value);
      }
    }
  }


  /// <summary>
  ///   Normalizes a submitted form key for switch-based parsing.
  /// </summary>
  /// <param name="key">The raw form key.</param>
  /// <returns>The normalized key, or <c>null</c> when empty.</returns>
  private static string? NormalizeKey(string key)
  {
    return string.IsNullOrWhiteSpace(key)
      ? null
      : key.Trim().ToLowerInvariant();
  }

  #endregion
}
