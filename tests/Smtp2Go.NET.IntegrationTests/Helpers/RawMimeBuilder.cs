namespace Smtp2Go.NET.IntegrationTests.Helpers;

using System.Text;

/// <summary>
///   Builds a raw RFC 5322 <c>multipart/alternative</c> MIME message and Base64-encodes it for the SMTP2GO
///   <c>email/mime</c> endpoint (<see cref="ISmtp2GoClient.SendMimeAsync" />).
/// </summary>
/// <remarks>
///   <para>
///     The SDK deliberately takes a pre-built MIME as an opaque Base64 string — it has no MIME library of its own — so
///     these integration tests construct a minimal valid message by hand rather than pulling one in. That also mirrors
///     the real contract the endpoint exists to serve: a caller hands the SDK a finished RFC 5322 message and SMTP2GO
///     transmits it verbatim (preserving its <c>Content-Type</c> and <c>Message-ID</c>) instead of reconstructing it from
///     structured fields.
///   </para>
/// </remarks>
internal static class RawMimeBuilder
{
  #region Methods

  /// <summary>
  ///   Builds a <c>multipart/alternative</c> message (a plain-text part and an HTML part) and returns its Base64
  ///   encoding, ready to assign to <see cref="Models.Email.EmailMimeRequest.MimeEmail" />.
  /// </summary>
  /// <param name="from">The From address; for live sends this must be a sender verified in the SMTP2GO account.</param>
  /// <param name="to">The recipient address.</param>
  /// <param name="subject">The subject line.</param>
  /// <param name="messageId">The Message-ID content; emitted wrapped in angle brackets.</param>
  /// <param name="textBody">The plain-text alternative.</param>
  /// <param name="htmlBody">The HTML alternative.</param>
  public static string BuildMultipartAlternativeBase64(
    string from,
    string to,
    string subject,
    string messageId,
    string textBody,
    string htmlBody)
  {
    var boundary = $"alt-{Guid.NewGuid():N}";

    var mime = new StringBuilder()
      .Append($"From: {from}\r\n")
      .Append($"To: {to}\r\n")
      .Append($"Subject: {subject}\r\n")
      .Append($"Message-ID: <{messageId}>\r\n")
      .Append("MIME-Version: 1.0\r\n")
      .Append($"Content-Type: multipart/alternative; boundary=\"{boundary}\"\r\n")
      .Append("\r\n")
      .Append($"--{boundary}\r\n")
      .Append("Content-Type: text/plain; charset=utf-8\r\n")
      .Append("\r\n")
      .Append(textBody).Append("\r\n")
      .Append($"--{boundary}\r\n")
      .Append("Content-Type: text/html; charset=utf-8\r\n")
      .Append("\r\n")
      .Append(htmlBody).Append("\r\n")
      .Append($"--{boundary}--\r\n")
      .ToString();

    return Convert.ToBase64String(Encoding.UTF8.GetBytes(mime));
  }

  #endregion
}
