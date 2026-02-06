namespace Smtp2Go.NET.UnitTests.Models;

using System.Text.Json;
using Smtp2Go.NET.Internal;
using Smtp2Go.NET.Models.Email;

/// <summary>
///   Verifies that <see cref="EmailSendRequest" /> serializes to JSON
///   matching the SMTP2GO API's expected format (snake_case, null omission).
/// </summary>
[Trait("Category", "Unit")]
public sealed class EmailSendRequestSerializationTests
{
  #region Serialization

  [Fact]
  public void Serialize_MinimalRequest_ProducesCorrectSnakeCaseJson()
  {
    // Arrange
    var request = new EmailSendRequest
    {
      Sender = "noreply@alos.app",
      To = ["user@example.com"],
      Subject = "Welcome",
      TextBody = "Hello, World!"
    };

    // Act
    var json = JsonSerializer.Serialize(request, Smtp2GoJsonDefaults.Options);
    using var doc = JsonDocument.Parse(json);
    var root = doc.RootElement;

    // Assert — Properties should use snake_case naming.
    root.GetProperty("sender").GetString().Should().Be("noreply@alos.app");
    root.GetProperty("to").GetArrayLength().Should().Be(1);
    root.GetProperty("to")[0].GetString().Should().Be("user@example.com");
    root.GetProperty("subject").GetString().Should().Be("Welcome");
    root.GetProperty("text_body").GetString().Should().Be("Hello, World!");
  }


  [Fact]
  public void Serialize_MinimalRequest_OmitsNullProperties()
  {
    // Arrange
    var request = new EmailSendRequest
    {
      Sender = "noreply@alos.app",
      To = ["user@example.com"],
      Subject = "Test"
    };

    // Act
    var json = JsonSerializer.Serialize(request, Smtp2GoJsonDefaults.Options);
    using var doc = JsonDocument.Parse(json);
    var root = doc.RootElement;

    // Assert — Null optional fields should not appear in the output.
    root.TryGetProperty("text_body", out _).Should().BeFalse();
    root.TryGetProperty("html_body", out _).Should().BeFalse();
    root.TryGetProperty("cc", out _).Should().BeFalse();
    root.TryGetProperty("bcc", out _).Should().BeFalse();
    root.TryGetProperty("custom_headers", out _).Should().BeFalse();
    root.TryGetProperty("attachments", out _).Should().BeFalse();
    root.TryGetProperty("inlines", out _).Should().BeFalse();
    root.TryGetProperty("template_id", out _).Should().BeFalse();
    root.TryGetProperty("template_data", out _).Should().BeFalse();
  }


  [Fact]
  public void Serialize_FullRequest_IncludesAllProperties()
  {
    // Arrange
    var request = new EmailSendRequest
    {
      Sender = "Alos <noreply@alos.app>",
      To = ["user1@example.com", "user2@example.com"],
      Subject = "Full Test",
      TextBody = "Plain text",
      HtmlBody = "<h1>HTML</h1>",
      Cc = ["cc@example.com"],
      Bcc = ["bcc@example.com"],
      CustomHeaders =
      [
        new CustomHeader { Header = "X-Tag", Value = "test" }
      ],
      Attachments =
      [
        new Attachment { Filename = "report.pdf", Fileblob = "base64data", Mimetype = "application/pdf" }
      ],
      TemplateId = "tmpl_123",
      TemplateData = new Dictionary<string, object>
      {
        ["user_name"] = "John"
      }
    };

    // Act
    var json = JsonSerializer.Serialize(request, Smtp2GoJsonDefaults.Options);
    using var doc = JsonDocument.Parse(json);
    var root = doc.RootElement;

    // Assert
    root.GetProperty("sender").GetString().Should().Be("Alos <noreply@alos.app>");
    root.GetProperty("to").GetArrayLength().Should().Be(2);
    root.GetProperty("subject").GetString().Should().Be("Full Test");
    root.GetProperty("text_body").GetString().Should().Be("Plain text");
    root.GetProperty("html_body").GetString().Should().Be("<h1>HTML</h1>");
    root.GetProperty("cc").GetArrayLength().Should().Be(1);
    root.GetProperty("bcc").GetArrayLength().Should().Be(1);
    root.GetProperty("custom_headers").GetArrayLength().Should().Be(1);
    root.GetProperty("attachments").GetArrayLength().Should().Be(1);
    root.GetProperty("template_id").GetString().Should().Be("tmpl_123");
    root.GetProperty("template_data").GetProperty("user_name").GetString().Should().Be("John");
  }

  #endregion


  #region Deserialization

  [Fact]
  public void Deserialize_EmailSendResponse_ParsesCorrectly()
  {
    // Arrange — Simulate a raw SMTP2GO API response.
    const string json = """
      {
        "request_id": "aa253464-0bd0-467a-b24b-6159dcd7be60",
        "data": {
          "succeeded": 1,
          "failed": 0,
          "failures": [],
          "email_id": "1234567890abcdef"
        }
      }
      """;

    // Act
    var response = JsonSerializer.Deserialize<EmailSendResponse>(json, Smtp2GoJsonDefaults.Options);

    // Assert
    response.Should().NotBeNull();
    response!.RequestId.Should().Be("aa253464-0bd0-467a-b24b-6159dcd7be60");
    response.Data.Should().NotBeNull();
    response.Data!.Succeeded.Should().Be(1);
    response.Data.Failed.Should().Be(0);
    response.Data.Failures.Should().BeEmpty();
    response.Data.EmailId.Should().Be("1234567890abcdef");
  }

  #endregion
}
