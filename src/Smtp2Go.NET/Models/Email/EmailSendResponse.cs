namespace Smtp2Go.NET.Models.Email;

using System.Text.Json.Serialization;

/// <summary>
///   Response model for the SMTP2GO <c>POST /email/send</c> endpoint.
/// </summary>
/// <remarks>
///   <para>
///     Contains the result of an email send operation, including counts of
///     successful and failed recipients, any failure messages, and the
///     unique email identifier assigned by SMTP2GO.
///   </para>
/// </remarks>
public class EmailSendResponse : ApiResponse<EmailSendResponseData>;

/// <summary>
///   Data payload for the email send response.
/// </summary>
/// <remarks>
///   <para>
///     The <see cref="Succeeded"/> and <see cref="Failed"/> counts represent the
///     number of recipients that were successfully accepted or rejected by the
///     SMTP2GO sending infrastructure. A succeeded count does not guarantee
///     final delivery — use webhooks to track delivery status.
///   </para>
/// </remarks>
public class EmailSendResponseData
{
  /// <summary>
  ///   Gets the number of recipients that were successfully accepted for sending.
  /// </summary>
  /// <remarks>
  ///   <para>
  ///     This indicates the message was accepted by SMTP2GO for delivery,
  ///     not that it has been delivered to the recipient's inbox.
  ///   </para>
  /// </remarks>
  [JsonPropertyName("succeeded")]
  public int Succeeded { get; init; }

  /// <summary>
  ///   Gets the number of recipients that failed to be accepted for sending.
  /// </summary>
  [JsonPropertyName("failed")]
  public int Failed { get; init; }

  /// <summary>
  ///   Gets the failure messages for recipients that could not be accepted.
  /// </summary>
  /// <remarks>
  ///   <para>
  ///     Each entry describes why a specific recipient was rejected (e.g.,
  ///     invalid email format, suppressed address).
  ///   </para>
  /// </remarks>
  [JsonPropertyName("failures")]
  public string[]? Failures { get; init; }

  /// <summary>
  ///   Gets the unique email identifier assigned by SMTP2GO.
  /// </summary>
  /// <remarks>
  ///   <para>
  ///     This identifier can be used to track the email through SMTP2GO's
  ///     dashboard, API, and webhook events.
  ///   </para>
  /// </remarks>
  [JsonPropertyName("email_id")]
  public string? EmailId { get; init; }
}
