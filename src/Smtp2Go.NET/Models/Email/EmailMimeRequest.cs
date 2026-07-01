namespace Smtp2Go.NET.Models.Email;

using System.Text.Json.Serialization;

/// <summary>
///   Request model for the SMTP2GO <c>POST /email/mime</c> endpoint.
/// </summary>
/// <remarks>
///   <para>
///     Unlike <see cref="EmailSendRequest" /> — which sends structured fields (<c>sender</c>, <c>to</c>,
///     <c>subject</c>, <c>html_body</c>, …) that SMTP2GO assembles into a MIME message server-side — this endpoint
///     takes a complete, pre-built MIME message and transmits it verbatim. That gives the caller full control over the
///     MIME structure and headers: a true <c>multipart/alternative</c> body, and a caller-supplied <c>Message-ID</c>
///     that SMTP2GO preserves rather than overwriting.
///   </para>
///   <para>
///     The API key travels in the <c>X-Smtp2go-Api-Key</c> request header (set by the client), so the only body field
///     is <see cref="MimeEmail" />. The sender, recipients, subject, and every header are taken from inside the MIME
///     message itself.
///   </para>
/// </remarks>
public class EmailMimeRequest
{
  /// <summary>
  ///   Gets or sets the complete MIME message to send, Base64-encoded.
  /// </summary>
  /// <remarks>
  ///   <para>
  ///     The value must be a valid RFC 5322 / MIME message (headers + body) that has then been Base64-encoded.
  ///     SMTP2GO sends this exact message; the <c>From</c>, <c>To</c>, <c>Subject</c>, and all headers are read from
  ///     within it.
  ///   </para>
  /// </remarks>
  [JsonPropertyName("mime_email")]
  public required string MimeEmail { get; set; }
}
