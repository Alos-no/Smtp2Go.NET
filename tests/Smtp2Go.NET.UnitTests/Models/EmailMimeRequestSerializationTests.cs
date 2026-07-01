namespace Smtp2Go.NET.UnitTests.Models;

using System.Text.Json;
using Smtp2Go.NET.Internal;
using Smtp2Go.NET.Models.Email;

/// <summary>
///   Verifies that <see cref="EmailMimeRequest" /> serializes to the SMTP2GO <c>POST /email/mime</c> body shape: a
///   single snake_case <c>mime_email</c> field carrying the base64-encoded raw MIME message. The endpoint takes only
///   this field (the API key travels in the <c>X-Smtp2go-Api-Key</c> header set by the client); all addressing and
///   headers live inside the MIME itself.
/// </summary>
[Trait("Category", "Unit")]
public sealed class EmailMimeRequestSerializationTests
{
  #region Serialization

  [Fact]
  public void Serialize_ProducesSnakeCaseMimeEmailField()
  {
    // Arrange — the payload is an opaque base64 string as far as the wire contract is concerned.
    var request = new EmailMimeRequest { MimeEmail = "TUlNRS1ib2R5" };

    // Act
    var json = JsonSerializer.Serialize(request, Smtp2GoJsonDefaults.Options);
    using var doc = JsonDocument.Parse(json);
    var root = doc.RootElement;

    // Assert — the single field is emitted as snake_case mime_email carrying the payload.
    root.GetProperty("mime_email").GetString().Should().Be("TUlNRS1ib2R5");
  }


  [Fact]
  public void Serialize_DoesNotEmitSendEnvelopeFields()
  {
    // Arrange — email/mime carries no envelope; sender/to/subject/headers all live inside the MIME blob.
    var request = new EmailMimeRequest { MimeEmail = "TUlNRS1ib2R5" };

    // Act
    var json = JsonSerializer.Serialize(request, Smtp2GoJsonDefaults.Options);
    using var doc = JsonDocument.Parse(json);
    var root = doc.RootElement;

    // Assert — none of the email/send envelope fields leak into the mime request.
    root.TryGetProperty("sender", out _).Should().BeFalse();
    root.TryGetProperty("to", out _).Should().BeFalse();
    root.TryGetProperty("subject", out _).Should().BeFalse();
    root.TryGetProperty("html_body", out _).Should().BeFalse();
    root.TryGetProperty("custom_headers", out _).Should().BeFalse();
  }

  #endregion
}
