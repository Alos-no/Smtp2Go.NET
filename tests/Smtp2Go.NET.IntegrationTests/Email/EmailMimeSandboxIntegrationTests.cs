namespace Smtp2Go.NET.IntegrationTests.Email;

using Fixtures;
using Helpers;
using Smtp2Go.NET.Exceptions;
using Smtp2Go.NET.Models.Email;

/// <summary>
///   Integration tests for the <see cref="ISmtp2GoClient.SendMimeAsync" /> endpoint (<c>email/mime</c>) using the
///   sandbox API key (the message is accepted but not delivered).
/// </summary>
/// <remarks>
///   <para>
///     Unlike <c>email/send</c> (which reconstructs the MIME from structured fields), <c>email/mime</c> takes a pre-built
///     RFC 5322 message as Base64 and sends it verbatim. These tests prove the SDK posts that payload to the real endpoint
///     and the API accepts it; end-to-end preservation of the verbatim <c>Content-Type</c>/<c>Message-ID</c> at the
///     recipient is owned by the consumer's delivery E2E (AlosNotify <c>DR01</c>/<c>DR02</c>).
///   </para>
/// </remarks>
[Trait("Category", "Integration")]
public sealed class EmailMimeSandboxIntegrationTests : IClassFixture<Smtp2GoSandboxFixture>
{
  #region Properties & Fields - Non-Public

  /// <summary>The sandbox-configured client fixture.</summary>
  private readonly Smtp2GoSandboxFixture _fixture;

  #endregion


  #region Constructors

  /// <summary>Initializes a new instance of the <see cref="EmailMimeSandboxIntegrationTests" /> class.</summary>
  public EmailMimeSandboxIntegrationTests(Smtp2GoSandboxFixture fixture)
  {
    _fixture = fixture;
  }

  #endregion


  #region Send MIME - Success

  [Fact]
  public async Task SendMime_WithSandboxKey_ReturnsSuccessResponse()
  {
    // Fail if sandbox secrets are not configured.
    TestSecretValidator.AssertSandboxSecretsPresent();

    // Arrange — a pre-built multipart/alternative message submitted verbatim.
    var mimeEmail = RawMimeBuilder.BuildMultipartAlternativeBase64(
      from: _fixture.TestSender,
      to: "sandbox-recipient@example.com",
      subject: $"Smtp2Go.NET MIME Integration Test - {DateTime.UtcNow:O}",
      messageId: $"{Guid.NewGuid():N}@smtp2go-net.test",
      textBody: "This is the plain-text alternative of an automated email/mime integration test. No action needed.",
      htmlBody: "<html><body><h1>email/mime Integration Test</h1><p>Automated test — no action needed.</p></body></html>");

    // Act
    var response = await _fixture.Client.SendMimeAsync(
      new EmailMimeRequest { MimeEmail = mimeEmail },
      TestContext.Current.CancellationToken);

    // Assert — the sandbox API should accept the verbatim MIME and return a success response.
    response.Should().NotBeNull();
    response.RequestId.Should().NotBeNullOrWhiteSpace("the API should return a request ID");
    response.Data.Should().NotBeNull("the response should contain data");
    response.Data!.Succeeded.Should().BeGreaterThanOrEqualTo(1, "the sandbox API should accept the MIME message");
    response.Data.EmailId.Should().NotBeNullOrWhiteSpace("the API should return an email ID");
  }

  #endregion


  #region Send MIME - Error Handling

  [Fact]
  public async Task SendMime_WithInvalidApiKey_ThrowsSmtp2GoApiException()
  {
    // Arrange — a correctly-formatted but nonexistent key triggers an auth error (not a format error).
    var invalidClient = Smtp2GoClientFactory.CreateClient("api-00000000000000000000000000000000");

    var mimeEmail = RawMimeBuilder.BuildMultipartAlternativeBase64(
      from: _fixture.TestSender,
      to: "recipient@example.com",
      subject: "Invalid Key MIME Test",
      messageId: $"{Guid.NewGuid():N}@smtp2go-net.test",
      textBody: "This should fail.",
      htmlBody: "<p>This should fail.</p>");

    // Act
    var act = async () => await invalidClient.SendMimeAsync(
      new EmailMimeRequest { MimeEmail = mimeEmail },
      TestContext.Current.CancellationToken);

    // Assert
    await act.Should().ThrowAsync<Smtp2GoApiException>();
  }

  #endregion
}
