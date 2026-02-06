namespace Smtp2Go.NET.Models.Email;

using System.Text.Json.Serialization;

/// <summary>
///   Represents a file attachment for an outgoing email.
/// </summary>
/// <remarks>
///   <para>
///     Attachments are included in the email send request as Base64-encoded blobs.
///     This model is used for both regular attachments (downloaded by the recipient)
///     and inline attachments (embedded in HTML via <c>cid:</c> references).
///   </para>
///   <para>
///     For inline attachments, the <see cref="Filename"/> is used as the
///     Content-ID reference in HTML (e.g., <c>&lt;img src="cid:logo.png" /&gt;</c>).
///   </para>
/// </remarks>
/// <example>
///   <code>
///     var attachment = new Attachment
///     {
///       Filename = "report.pdf",
///       Fileblob = Convert.ToBase64String(fileBytes),
///       Mimetype = "application/pdf"
///     };
///   </code>
/// </example>
public class Attachment
{
  /// <summary>
  ///   Gets or sets the file name of the attachment.
  /// </summary>
  /// <remarks>
  ///   <para>
  ///     The filename as it will appear to the recipient (e.g., <c>"report.pdf"</c>).
  ///     For inline attachments, this is also the Content-ID used in <c>cid:</c> references.
  ///   </para>
  /// </remarks>
  [JsonPropertyName("filename")]
  public required string Filename { get; set; }

  /// <summary>
  ///   Gets or sets the Base64-encoded file content.
  /// </summary>
  /// <remarks>
  ///   <para>
  ///     The raw file bytes must be encoded as a Base64 string before assignment.
  ///     Use <see cref="Convert.ToBase64String(byte[])"/> to encode file content.
  ///   </para>
  /// </remarks>
  [JsonPropertyName("fileblob")]
  public required string Fileblob { get; set; }

  /// <summary>
  ///   Gets or sets the MIME type of the attachment.
  /// </summary>
  /// <remarks>
  ///   <para>
  ///     The MIME type determines how the recipient's email client handles the file
  ///     (e.g., <c>"application/pdf"</c>, <c>"image/png"</c>, <c>"text/csv"</c>).
  ///   </para>
  /// </remarks>
  [JsonPropertyName("mimetype")]
  public required string Mimetype { get; set; }
}
