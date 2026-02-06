namespace Smtp2Go.NET.IntegrationTests.Statistics;

using Fixtures;
using Helpers;
using Smtp2Go.NET.Models.Statistics;

/// <summary>
///   Integration tests for the <see cref="ISmtp2GoStatisticsClient.GetEmailSummaryAsync"/> endpoint
///   using the sandbox API key.
/// </summary>
[Trait("Category", "Integration")]
public sealed class EmailSummaryIntegrationTests : IClassFixture<Smtp2GoSandboxFixture>
{
  #region Properties & Fields - Non-Public

  /// <summary>The sandbox-configured client fixture.</summary>
  private readonly Smtp2GoSandboxFixture _fixture;

  #endregion


  #region Constructors

  /// <summary>
  ///   Initializes a new instance of the <see cref="EmailSummaryIntegrationTests" /> class.
  /// </summary>
  public EmailSummaryIntegrationTests(Smtp2GoSandboxFixture fixture)
  {
    _fixture = fixture;
  }

  #endregion


  #region Get Email Summary

  [Fact]
  public async Task GetEmailSummary_WithNoRequest_ReturnsStatistics()
  {
    // Fail if sandbox secrets are not configured.
    TestSecretValidator.AssertSandboxSecretsPresent();

    // Act — Call with no request parameters for default date range.
    var response = await _fixture.Client.Statistics.GetEmailSummaryAsync(
      ct: TestContext.Current.CancellationToken);

    // Assert — The API should return a valid statistics response.
    response.Should().NotBeNull();
    response.RequestId.Should().NotBeNullOrWhiteSpace("the API should return a request ID");
    response.Data.Should().NotBeNull("the response should contain statistics data");

    // Statistics values should be non-negative (may be zero for sandbox accounts).
    response.Data!.Emails.Should().BeGreaterThanOrEqualTo(0);
    response.Data.HardBounces.Should().BeGreaterThanOrEqualTo(0);
    response.Data.SoftBounces.Should().BeGreaterThanOrEqualTo(0);
  }


  [Fact]
  public async Task GetEmailSummary_WithDateRange_ReturnsFilteredStatistics()
  {
    // Fail if sandbox secrets are not configured.
    TestSecretValidator.AssertSandboxSecretsPresent();

    // Arrange — Query for the last 7 days.
    var request = new EmailSummaryRequest
    {
      StartDate = DateTime.UtcNow.AddDays(-7).ToString("yyyy-MM-dd"),
      EndDate = DateTime.UtcNow.ToString("yyyy-MM-dd")
    };

    // Act
    var response = await _fixture.Client.Statistics.GetEmailSummaryAsync(
      request,
      TestContext.Current.CancellationToken);

    // Assert
    response.Should().NotBeNull();
    response.RequestId.Should().NotBeNullOrWhiteSpace();
    response.Data.Should().NotBeNull();

    // Values should be non-negative.
    response.Data!.Emails.Should().BeGreaterThanOrEqualTo(0);
    response.Data.HardBounces.Should().BeGreaterThanOrEqualTo(0);
  }

  #endregion
}
