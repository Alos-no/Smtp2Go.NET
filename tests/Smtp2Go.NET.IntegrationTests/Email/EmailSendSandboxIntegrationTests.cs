namespace Smtp2Go.NET.IntegrationTests.Email;

using Fixtures;
using Helpers;
using Smtp2Go.NET.Exceptions;
using Smtp2Go.NET.Models.Email;

/// <summary>
///   Integration tests for the <see cref="ISmtp2GoClient.SendEmailAsync"/> endpoint
///   using the sandbox API key (emails accepted but not delivered).
/// </summary>
[Trait("Category", "Integration")]
public sealed class EmailSendSandboxIntegrationTests : IClassFixture<Smtp2GoSandboxFixture>
{
  #region Properties & Fields - Non-Public

  /// <summary>The sandbox-configured client fixture.</summary>
  private readonly Smtp2GoSandboxFixture _fixture;

  #endregion


  #region Constructors

  /// <summary>
  ///   Initializes a new instance of the <see cref="EmailSendSandboxIntegrationTests" /> class.
  /// </summary>
  public EmailSendSandboxIntegrationTests(Smtp2GoSandboxFixture fixture)
  {
    _fixture = fixture;
  }

  #endregion


  #region Send Email - Success

  [Fact]
  public async Task SendEmail_WithSandboxKey_ReturnsSuccessResponse()
  {
    // Fail if sandbox secrets are not configured.
    TestSecretValidator.AssertSandboxSecretsPresent();

    // Arrange
    var request = new EmailSendRequest
    {
      Sender = _fixture.TestSender,
      To = ["sandbox-recipient@example.com"],
      Subject = $"Smtp2Go.NET Integration Test - {DateTime.UtcNow:O}",
      TextBody = "This is an automated integration test. No action needed."
    };

    // Act
    var response = await _fixture.Client.SendEmailAsync(request, TestContext.Current.CancellationToken);

    // Assert — The sandbox API should accept the email and return a success response.
    response.Should().NotBeNull();
    response.RequestId.Should().NotBeNullOrWhiteSpace("the API should return a request ID");
    response.Data.Should().NotBeNull("the response should contain data");
    response.Data!.Succeeded.Should().BeGreaterThanOrEqualTo(1, "at least one recipient should succeed");
    response.Data.EmailId.Should().NotBeNullOrWhiteSpace("the API should return an email ID");
  }


  [Fact]
  public async Task SendEmail_WithHtmlBody_ReturnsSuccessResponse()
  {
    // Fail if sandbox secrets are not configured.
    TestSecretValidator.AssertSandboxSecretsPresent();

    // Arrange
    var request = new EmailSendRequest
    {
      Sender = _fixture.TestSender,
      To = ["sandbox-recipient@example.com"],
      Subject = $"HTML Test - {DateTime.UtcNow:O}",
      HtmlBody = "<h1>Integration Test</h1><p>This is an automated test with HTML body.</p>"
    };

    // Act
    var response = await _fixture.Client.SendEmailAsync(request, TestContext.Current.CancellationToken);

    // Assert
    response.Should().NotBeNull();
    response.Data.Should().NotBeNull();
    response.Data!.Succeeded.Should().BeGreaterThanOrEqualTo(1);
  }


  [Fact]
  public async Task SendEmail_WithMultipleRecipients_ReturnsSuccessResponse()
  {
    // Fail if sandbox secrets are not configured.
    TestSecretValidator.AssertSandboxSecretsPresent();

    // Arrange
    var request = new EmailSendRequest
    {
      Sender = _fixture.TestSender,
      To = ["recipient1@example.com", "recipient2@example.com"],
      Subject = $"Multi-Recipient Test - {DateTime.UtcNow:O}",
      TextBody = "This email was sent to multiple recipients."
    };

    // Act
    var response = await _fixture.Client.SendEmailAsync(request, TestContext.Current.CancellationToken);

    // Assert
    response.Should().NotBeNull();
    response.Data.Should().NotBeNull();
    // SMTP2GO sandbox may count multiple recipients differently — assert at least 1 succeeded.
    response.Data!.Succeeded.Should().BeGreaterThanOrEqualTo(1, "at least one recipient should succeed");
  }


  [Fact]
  public async Task SendEmail_WithCcAndBcc_ReturnsSuccessResponse()
  {
    // Fail if sandbox secrets are not configured.
    TestSecretValidator.AssertSandboxSecretsPresent();

    // Arrange
    var request = new EmailSendRequest
    {
      Sender = _fixture.TestSender,
      To = ["to@example.com"],
      Cc = ["cc@example.com"],
      Bcc = ["bcc@example.com"],
      Subject = $"CC/BCC Test - {DateTime.UtcNow:O}",
      TextBody = "This email includes CC and BCC recipients."
    };

    // Act
    var response = await _fixture.Client.SendEmailAsync(request, TestContext.Current.CancellationToken);

    // Assert
    response.Should().NotBeNull();
    response.Data.Should().NotBeNull();
    response.Data!.Succeeded.Should().BeGreaterThanOrEqualTo(1);
  }


  [Fact]
  public async Task SendEmail_WithCustomHeaders_ReturnsSuccessResponse()
  {
    // Fail if sandbox secrets are not configured.
    TestSecretValidator.AssertSandboxSecretsPresent();

    // Arrange
    var request = new EmailSendRequest
    {
      Sender = _fixture.TestSender,
      To = ["sandbox-recipient@example.com"],
      Subject = $"Custom Headers Test - {DateTime.UtcNow:O}",
      TextBody = "This email includes custom headers.",
      CustomHeaders =
      [
        new CustomHeader { Header = "X-Test-Id", Value = Guid.NewGuid().ToString() },
        new CustomHeader { Header = "X-Source", Value = "Smtp2Go.NET.IntegrationTests" }
      ]
    };

    // Act
    var response = await _fixture.Client.SendEmailAsync(request, TestContext.Current.CancellationToken);

    // Assert
    response.Should().NotBeNull();
    response.Data.Should().NotBeNull();
    response.Data!.Succeeded.Should().BeGreaterThanOrEqualTo(1);
  }

  #endregion


  #region Send Email - Attachments

  [Fact]
  public async Task SendEmail_WithAttachment_ReturnsSuccessResponse()
  {
    // Fail if sandbox secrets are not configured.
    TestSecretValidator.AssertSandboxSecretsPresent();

    // Arrange — Create a small text file attachment.
    var fileContent = "This is a test attachment file content."u8.ToArray();

    var request = new EmailSendRequest
    {
      Sender = _fixture.TestSender,
      To = ["sandbox-recipient@example.com"],
      Subject = $"Attachment Test - {DateTime.UtcNow:O}",
      TextBody = "This email includes a file attachment.",
      Attachments =
      [
        new Attachment
        {
          Filename = "test-report.txt",
          Fileblob = Convert.ToBase64String(fileContent),
          Mimetype = "text/plain"
        }
      ]
    };

    // Act
    var response = await _fixture.Client.SendEmailAsync(request, TestContext.Current.CancellationToken);

    // Assert
    response.Should().NotBeNull();
    response.Data.Should().NotBeNull();
    response.Data!.Succeeded.Should().BeGreaterThanOrEqualTo(1);
  }


  [Fact]
  public async Task SendEmail_WithMultipleAttachments_ReturnsSuccessResponse()
  {
    // Fail if sandbox secrets are not configured.
    TestSecretValidator.AssertSandboxSecretsPresent();

    // Arrange — Create multiple attachments of different MIME types.
    var textContent = "Plain text attachment."u8.ToArray();
    var csvContent = "Name,Value\nTest,123\n"u8.ToArray();

    var request = new EmailSendRequest
    {
      Sender = _fixture.TestSender,
      To = ["sandbox-recipient@example.com"],
      Subject = $"Multiple Attachments Test - {DateTime.UtcNow:O}",
      TextBody = "This email includes multiple file attachments.",
      Attachments =
      [
        new Attachment
        {
          Filename = "notes.txt",
          Fileblob = Convert.ToBase64String(textContent),
          Mimetype = "text/plain"
        },
        new Attachment
        {
          Filename = "data.csv",
          Fileblob = Convert.ToBase64String(csvContent),
          Mimetype = "text/csv"
        }
      ]
    };

    // Act
    var response = await _fixture.Client.SendEmailAsync(request, TestContext.Current.CancellationToken);

    // Assert
    response.Should().NotBeNull();
    response.Data.Should().NotBeNull();
    response.Data!.Succeeded.Should().BeGreaterThanOrEqualTo(1);
  }


  [Fact]
  public async Task SendEmail_WithInlineAttachment_ReturnsSuccessResponse()
  {
    // Fail if sandbox secrets are not configured.
    TestSecretValidator.AssertSandboxSecretsPresent();

    // Arrange — Create a minimal 1x1 red PNG for inline embedding.
    // This is the smallest valid PNG: 8-byte signature + IHDR + IDAT + IEND.
    byte[] pixelPng =
    [
      0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, // PNG signature
      0x00, 0x00, 0x00, 0x0D, 0x49, 0x48, 0x44, 0x52, // IHDR chunk length + type
      0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x01, // 1x1 pixels
      0x08, 0x02, 0x00, 0x00, 0x00, 0x90, 0x77, 0x53, // 8-bit RGB + CRC
      0xDE, 0x00, 0x00, 0x00, 0x0C, 0x49, 0x44, 0x41, // IDAT chunk
      0x54, 0x08, 0xD7, 0x63, 0xF8, 0xCF, 0xC0, 0x00, // compressed pixel data
      0x00, 0x00, 0x02, 0x00, 0x01, 0xE2, 0x21, 0xBC, // Adler32 + CRC
      0x33, 0x00, 0x00, 0x00, 0x00, 0x49, 0x45, 0x4E, // IEND chunk
      0x44, 0xAE, 0x42, 0x60, 0x82                      // IEND CRC
    ];

    var request = new EmailSendRequest
    {
      Sender = _fixture.TestSender,
      To = ["sandbox-recipient@example.com"],
      Subject = $"Inline Attachment Test - {DateTime.UtcNow:O}",
      HtmlBody = """
        <h2>Inline Image Test</h2>
        <p>The image below is embedded via cid: reference:</p>
        <img src="cid:test-logo.png" alt="Test Logo" />
        """,
      Inlines =
      [
        new Attachment
        {
          Filename = "test-logo.png",
          Fileblob = Convert.ToBase64String(pixelPng),
          Mimetype = "image/png"
        }
      ]
    };

    // Act
    var response = await _fixture.Client.SendEmailAsync(request, TestContext.Current.CancellationToken);

    // Assert
    response.Should().NotBeNull();
    response.Data.Should().NotBeNull();
    response.Data!.Succeeded.Should().BeGreaterThanOrEqualTo(1);
  }

  #endregion


  #region Send Email - Error Handling

  [Fact]
  public async Task SendEmail_WithInvalidApiKey_ThrowsSmtp2GoApiException()
  {
    // Arrange — Create a client with a deliberately invalid API key.
    // SMTP2GO requires API keys in format 'api-[A-Za-z0-9]{32}' (36 chars total).
    // Use a correctly-formatted but nonexistent key to trigger an auth error (not a format error).
    var invalidClient = Smtp2GoClientFactory.CreateClient("api-00000000000000000000000000000000");

    var request = new EmailSendRequest
    {
      Sender = _fixture.TestSender,
      To = ["recipient@example.com"],
      Subject = "Invalid Key Test",
      TextBody = "This should fail."
    };

    // Act
    var act = async () => await invalidClient.SendEmailAsync(request, TestContext.Current.CancellationToken);

    // Assert
    await act.Should().ThrowAsync<Smtp2GoApiException>();
  }

  #endregion
}
