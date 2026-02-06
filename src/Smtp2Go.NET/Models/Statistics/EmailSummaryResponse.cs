namespace Smtp2Go.NET.Models.Statistics;

using System.Text.Json.Serialization;

/// <summary>
///   Response model for the SMTP2GO <c>POST /stats/email_summary</c> endpoint.
/// </summary>
/// <remarks>
///   <para>
///     Contains aggregate email sending statistics for the requested date range,
///     including delivery, bounce, open, click, and unsubscribe counts.
///   </para>
/// </remarks>
public class EmailSummaryResponse : ApiResponse<EmailSummaryResponseData>;

/// <summary>
///   Data payload for the email summary response.
/// </summary>
/// <remarks>
///   <para>
///     Maps to the SMTP2GO <c>POST /stats/email_summary</c> response which includes
///     billing cycle information, email counts, bounce/spam statistics, and engagement metrics.
///     All counts are nullable to handle cases where the SMTP2GO API does not
///     return a particular statistic.
///   </para>
/// </remarks>
public class EmailSummaryResponseData
{
  /// <summary>
  ///   Gets the start of the current billing cycle.
  /// </summary>
  [JsonPropertyName("cycle_start")]
  public string? CycleStart { get; init; }

  /// <summary>
  ///   Gets the end of the current billing cycle.
  /// </summary>
  [JsonPropertyName("cycle_end")]
  public string? CycleEnd { get; init; }

  /// <summary>
  ///   Gets the number of emails used in the current billing cycle.
  /// </summary>
  [JsonPropertyName("cycle_used")]
  public int? CycleUsed { get; init; }

  /// <summary>
  ///   Gets the number of emails remaining in the current billing cycle.
  /// </summary>
  [JsonPropertyName("cycle_remaining")]
  public int? CycleRemaining { get; init; }

  /// <summary>
  ///   Gets the maximum number of emails allowed in the current billing cycle.
  /// </summary>
  [JsonPropertyName("cycle_max")]
  public int? CycleMax { get; init; }

  /// <summary>
  ///   Gets the total number of emails sent during the period.
  /// </summary>
  [JsonPropertyName("email_count")]
  public int? Emails { get; init; }

  /// <summary>
  ///   Gets the number of emails rejected before delivery (format/policy violations).
  /// </summary>
  [JsonPropertyName("rejects")]
  public int? Rejects { get; init; }

  /// <summary>
  ///   Gets the number of emails rejected due to bounce policies.
  /// </summary>
  [JsonPropertyName("bounce_rejects")]
  public int? BounceRejects { get; init; }

  /// <summary>
  ///   Gets the number of hard bounces (permanent delivery failures).
  /// </summary>
  /// <remarks>
  ///   <para>
  ///     Hard bounces indicate permanent delivery failures (e.g., invalid address).
  ///   </para>
  /// </remarks>
  [JsonPropertyName("hardbounces")]
  public int? HardBounces { get; init; }

  /// <summary>
  ///   Gets the number of soft bounces (temporary delivery failures).
  /// </summary>
  /// <remarks>
  ///   <para>
  ///     Soft bounces indicate temporary failures (e.g., full mailbox).
  ///   </para>
  /// </remarks>
  [JsonPropertyName("softbounces")]
  public int? SoftBounces { get; init; }

  /// <summary>
  ///   Gets the bounce percentage as a string (e.g., "0.00").
  /// </summary>
  [JsonPropertyName("bounce_percent")]
  public string? BouncePercent { get; init; }

  /// <summary>
  ///   Gets the number of emails rejected due to spam policies.
  /// </summary>
  [JsonPropertyName("spam_rejects")]
  public int? SpamRejects { get; init; }

  /// <summary>
  ///   Gets the number of emails flagged as spam by recipients.
  /// </summary>
  [JsonPropertyName("spam_emails")]
  public int? SpamEmails { get; init; }

  /// <summary>
  ///   Gets the spam percentage as a string (e.g., "0.00").
  /// </summary>
  [JsonPropertyName("spam_percent")]
  public string? SpamPercent { get; init; }

  /// <summary>
  ///   Gets the number of times emails were opened by recipients.
  /// </summary>
  /// <remarks>
  ///   <para>
  ///     Open tracking relies on a tracking pixel embedded in HTML emails.
  ///     Plain text emails and recipients with image loading disabled will not
  ///     be counted.
  ///   </para>
  /// </remarks>
  [JsonPropertyName("opens")]
  public int? Opens { get; init; }

  /// <summary>
  ///   Gets the number of link clicks tracked in emails.
  /// </summary>
  /// <remarks>
  ///   <para>
  ///     Click tracking requires link rewriting to be enabled in the SMTP2GO account.
  ///     Each unique link click per recipient is counted.
  ///   </para>
  /// </remarks>
  [JsonPropertyName("clicks")]
  public int? Clicks { get; init; }

  /// <summary>
  ///   Gets the number of recipients who unsubscribed via the email's unsubscribe mechanism.
  /// </summary>
  [JsonPropertyName("unsubscribes")]
  public int? Unsubscribes { get; init; }

  /// <summary>
  ///   Gets the unsubscribe percentage as a string (e.g., "0.00").
  /// </summary>
  [JsonPropertyName("unsubscribe_percent")]
  public string? UnsubscribePercent { get; init; }
}
