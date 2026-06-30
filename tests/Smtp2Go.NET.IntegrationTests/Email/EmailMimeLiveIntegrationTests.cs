namespace Smtp2Go.NET.IntegrationTests.Email;

using Fixtures;
using Helpers;
using Smtp2Go.NET.Models.Email;

/// <summary>
///   Live integration test for the <see cref="ISmtp2GoClient.SendMimeAsync" /> endpoint (<c>email/mime</c>) using the
///   live API key (the message is actually delivered).
/// </summary>
/// <remarks>
///   <para>
///     Sends a real, pre-built multipart/alternative message to the configured test recipient. Asserts the live API
///     accepted the verbatim MIME for delivery; it does not read the mailbox back (receipt-level verification — that
///     SMTP2GO delivered the multipart and the deterministic Message-ID intact — is owned by the consumer's delivery E2E,
///     AlosNotify <c>DR01</c>/<c>DR02</c>). Use with caution: the recipient must be a controlled mailbox.
///   </para>
/// </remarks>
[Trait("Category", "Integration.Live")]
public sealed class EmailMimeLiveIntegrationTests : IClassFixture<Smtp2GoLiveFixture>
{
  #region Properties & Fields - Non-Public

  /// <summary>The live-configured client fixture.</summary>
  private readonly Smtp2GoLiveFixture _fixture;

  #endregion


  #region Constructors

  /// <summary>Initializes a new instance of the <see cref="EmailMimeLiveIntegrationTests" /> class.</summary>
  public EmailMimeLiveIntegrationTests(Smtp2GoLiveFixture fixture)
  {
    _fixture = fixture;
  }

  #endregion


  #region Send MIME - Live Delivery

  [Fact]
  public async Task SendMime_WithLiveKey_DeliversToRecipient()
  {
    // Fail if live secrets are not configured.
    TestSecretValidator.AssertLiveSecretsPresent();

    // Arrange — a real multipart/alternative message submitted verbatim via email/mime.
    var mimeEmail = RawMimeBuilder.BuildMultipartAlternativeBase64(
      from: _fixture.TestSender,
      to: _fixture.TestRecipient,
      subject: $"Smtp2Go.NET Live MIME Integration Test - {DateTime.UtcNow:O}",
      messageId: $"{Guid.NewGuid():N}@smtp2go-net.test",
      textBody: "This is a live email/mime integration test from Smtp2Go.NET. No action required.",
      htmlBody: $"""
        <h2>Smtp2Go.NET Live email/mime Integration Test</h2>
        <p>This message was submitted verbatim through the email/mime endpoint.</p>
        <p>No action is required. It confirms live delivery via SendMimeAsync is working correctly.</p>
        <hr />
        <p style="color: #999; font-size: 12px;">Sent at {DateTime.UtcNow:O}</p>
        """);

    // Act
    var response = await _fixture.Client.SendMimeAsync(
      new EmailMimeRequest { MimeEmail = mimeEmail },
      TestContext.Current.CancellationToken);

    // Assert — the live API should accept and queue the email for delivery.
    response.Should().NotBeNull();
    response.RequestId.Should().NotBeNullOrWhiteSpace();
    response.Data.Should().NotBeNull();
    response.Data!.Succeeded.Should().Be(1, "the test recipient should succeed");
    response.Data.Failed.Should().Be(0, "no recipients should fail");
    response.Data.EmailId.Should().NotBeNullOrWhiteSpace("a live email should receive an email ID");
  }

  #endregion
}
