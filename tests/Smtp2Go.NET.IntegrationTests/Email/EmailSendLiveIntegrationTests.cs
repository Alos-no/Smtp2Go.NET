namespace Smtp2Go.NET.IntegrationTests.Email;

using Fixtures;
using Helpers;
using Smtp2Go.NET.Models.Email;

/// <summary>
///   Live integration tests for the <see cref="ISmtp2GoClient.SendEmailAsync"/> endpoint
///   using the live API key (emails are actually delivered).
/// </summary>
/// <remarks>
///   <para>
///     These tests send real emails to the configured test recipient. Use with caution
///     and ensure the test recipient is a controlled mailbox to avoid spamming.
///   </para>
/// </remarks>
[Trait("Category", "Integration.Live")]
public sealed class EmailSendLiveIntegrationTests : IClassFixture<Smtp2GoLiveFixture>
{
  #region Properties & Fields - Non-Public

  /// <summary>The live-configured client fixture.</summary>
  private readonly Smtp2GoLiveFixture _fixture;

  #endregion


  #region Constructors

  /// <summary>
  ///   Initializes a new instance of the <see cref="EmailSendLiveIntegrationTests" /> class.
  /// </summary>
  public EmailSendLiveIntegrationTests(Smtp2GoLiveFixture fixture)
  {
    _fixture = fixture;
  }

  #endregion


  #region Send Email - Live Delivery

  [Fact]
  public async Task SendEmail_WithLiveKey_DeliversToRecipient()
  {
    // Fail if live secrets are not configured.
    TestSecretValidator.AssertLiveSecretsPresent();

    // Arrange
    var request = new EmailSendRequest
    {
      Sender = _fixture.TestSender,
      To = [_fixture.TestRecipient],
      Subject = $"Smtp2Go.NET Live Integration Test - {DateTime.UtcNow:O}",
      HtmlBody = $"""
        <h2>Smtp2Go.NET Live Integration Test</h2>
        <p>This email was sent by the Smtp2Go.NET integration test suite.</p>
        <p>No action is required. This email confirms live delivery is working correctly.</p>
        <hr />
        <p style="color: #999; font-size: 12px;">Sent at {DateTime.UtcNow:O}</p>
        """,
      TextBody = "This is a live integration test email from Smtp2Go.NET. No action required."
    };

    // Act
    var response = await _fixture.Client.SendEmailAsync(request, TestContext.Current.CancellationToken);

    // Assert — The live API should accept and queue the email for delivery.
    response.Should().NotBeNull();
    response.RequestId.Should().NotBeNullOrWhiteSpace();
    response.Data.Should().NotBeNull();
    response.Data!.Succeeded.Should().Be(1, "the test recipient should succeed");
    response.Data.Failed.Should().Be(0, "no recipients should fail");
    response.Data.EmailId.Should().NotBeNullOrWhiteSpace("a live email should receive an email ID");
  }

  #endregion
}
