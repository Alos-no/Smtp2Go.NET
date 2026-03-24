namespace Smtp2Go.NET.Models.Webhooks;

using System.Globalization;

/// <summary>
///   Centralizes normalization and parsing for SMTP2GO webhook callback values.
/// </summary>
/// <remarks>
///   <para>
///     This helper is intentionally internal so the library can keep one source of truth for
///     callback value parsing across JSON converters and form payload conversion.
///   </para>
///   <para>
///     The accepted values reflect the currently supported library behavior, including the
///     compatibility aliases already handled by AlosNotify's SMTP2GO form workaround.
///   </para>
/// </remarks>
internal static class WebhookCallbackValueParser
{

  #region Methods - Internal

  /// <summary>
  ///   Parses a callback event string into a <see cref="WebhookCallbackEvent" /> value.
  /// </summary>
  /// <param name="value">The raw callback event string.</param>
  /// <returns>The parsed <see cref="WebhookCallbackEvent" /> value.</returns>
  internal static WebhookCallbackEvent ParseCallbackEvent(string? value)
  {
    return Normalize(value) switch
    {
      "processed" => WebhookCallbackEvent.Processed,
      "delivered" => WebhookCallbackEvent.Delivered,
      "bounce" => WebhookCallbackEvent.Bounce,
      "open" or "opened" => WebhookCallbackEvent.Opened,
      "click" or "clicked" => WebhookCallbackEvent.Clicked,
      "unsubscribe" or "unsubscribed" => WebhookCallbackEvent.Unsubscribed,
      "spam" or "spam_complaint" => WebhookCallbackEvent.SpamComplaint,
      _ => WebhookCallbackEvent.Unknown
    };
  }


  /// <summary>
  ///   Formats a callback event enum as the canonical SMTP2GO wire string.
  /// </summary>
  /// <param name="value">The callback event value.</param>
  /// <returns>The SMTP2GO wire string.</returns>
  internal static string FormatCallbackEvent(WebhookCallbackEvent value)
  {
    return value switch
    {
      WebhookCallbackEvent.Processed => "processed",
      WebhookCallbackEvent.Delivered => "delivered",
      WebhookCallbackEvent.Bounce => "bounce",
      WebhookCallbackEvent.Opened => "opened",
      WebhookCallbackEvent.Clicked => "clicked",
      WebhookCallbackEvent.Unsubscribed => "unsubscribed",
      WebhookCallbackEvent.SpamComplaint => "spam_complaint",
      _ => "unknown"
    };
  }


  /// <summary>
  ///   Parses a bounce type string into a nullable <see cref="BounceType" /> value.
  /// </summary>
  /// <param name="value">The raw bounce type string.</param>
  /// <returns>The parsed bounce type, or <c>null</c> when the value is absent.</returns>
  internal static BounceType? ParseBounceType(string? value)
  {
    return Normalize(value) switch
    {
      null => null,
      "hard" => BounceType.Hard,
      "soft" => BounceType.Soft,
      _ => BounceType.Unknown
    };
  }


  /// <summary>
  ///   Formats a bounce type enum as the canonical SMTP2GO wire string.
  /// </summary>
  /// <param name="value">The bounce type value.</param>
  /// <returns>The SMTP2GO wire string.</returns>
  internal static string FormatBounceType(BounceType value)
  {
    return value switch
    {
      BounceType.Hard => "hard",
      BounceType.Soft => "soft",
      _ => "unknown"
    };
  }


  /// <summary>
  ///   Parses an ISO 8601 timestamp string into a <see cref="DateTimeOffset" />.
  /// </summary>
  /// <param name="value">The raw timestamp value.</param>
  /// <returns>The parsed timestamp, or <c>null</c> when absent or invalid.</returns>
  internal static DateTimeOffset? ParseDateTimeOffset(string? value)
  {
    if (string.IsNullOrWhiteSpace(value))
    {
      return null;
    }

    return DateTimeOffset.TryParse(
      value,
      CultureInfo.InvariantCulture,
      DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
      out var parsed)
      ? parsed
      : null;
  }


  /// <summary>
  ///   Normalizes recipient list values from repeated or delimiter-separated fields.
  /// </summary>
  /// <param name="values">The raw recipient values.</param>
  /// <returns>The normalized recipients, or <c>null</c> when none are present.</returns>
  internal static string[]? ParseRecipients(IEnumerable<string?> values)
  {
    ArgumentNullException.ThrowIfNull(values);

    var recipients = values.Where(static value => !string.IsNullOrWhiteSpace(value))
                           .SelectMany(static value => value!.Split(
                               [',', ';'],
                               StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                           .Where(static value => !string.IsNullOrWhiteSpace(value))
                           .ToArray();

    return recipients.Length > 0 ? recipients : null;
  }

  #endregion


  #region Methods - Private

  /// <summary>
  ///   Normalizes a raw SMTP2GO callback string for switch-based parsing.
  /// </summary>
  /// <param name="value">The raw string value.</param>
  /// <returns>The normalized string, or <c>null</c> when empty.</returns>
  private static string? Normalize(string? value)
  {
    return string.IsNullOrWhiteSpace(value)
      ? null
      : value.Trim().ToLowerInvariant();
  }

  #endregion
}
