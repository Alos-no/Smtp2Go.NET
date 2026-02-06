namespace Smtp2Go.NET.UnitTests.Configuration;

using Smtp2Go.NET.Configuration;

/// <summary>
///   Unit tests for <see cref="Smtp2GoOptionsValidator" />.
/// </summary>
[Trait("Category", "Unit")]
public sealed class Smtp2GoOptionsValidatorTests
{
  #region Properties & Fields - Non-Public

  private readonly Smtp2GoOptionsValidator _validator = new();

  #endregion


  #region Validate - Success

  [Fact]
  public void Validate_WithValidOptions_ReturnsSuccess()
  {
    // Arrange
    var options = new Smtp2GoOptions
    {
      ApiKey = "api-test-key-XXXXXXXXXXXXXXXX",
      BaseUrl = "https://api.smtp2go.com/v3/",
      Timeout = TimeSpan.FromSeconds(30)
    };

    // Act
    var result = _validator.Validate(null, options);

    // Assert
    result.Succeeded.Should().BeTrue();
  }


  [Fact]
  public void Validate_WithCustomHttpBaseUrl_ReturnsSuccess()
  {
    // Arrange — HTTP is allowed (e.g., for local development or testing).
    var options = new Smtp2GoOptions
    {
      ApiKey = "api-test-key",
      BaseUrl = "http://localhost:5000/v3/",
      Timeout = TimeSpan.FromSeconds(10)
    };

    // Act
    var result = _validator.Validate(null, options);

    // Assert
    result.Succeeded.Should().BeTrue();
  }

  #endregion


  #region Validate - ApiKey

  [Theory]
  [InlineData(null)]
  [InlineData("")]
  [InlineData("   ")]
  public void Validate_WithMissingApiKey_ReturnsFailed(string? apiKey)
  {
    // Arrange
    var options = new Smtp2GoOptions
    {
      ApiKey = apiKey,
      BaseUrl = "https://api.smtp2go.com/v3/",
      Timeout = TimeSpan.FromSeconds(30)
    };

    // Act
    var result = _validator.Validate(null, options);

    // Assert
    result.Failed.Should().BeTrue();
    result.FailureMessage.Should().Contain("ApiKey");
    result.FailureMessage.Should().Contain("is required");
  }

  #endregion


  #region Validate - BaseUrl

  [Fact]
  public void Validate_WithEmptyBaseUrl_ReturnsFailed()
  {
    // Arrange
    var options = new Smtp2GoOptions
    {
      ApiKey = "api-test-key",
      BaseUrl = "",
      Timeout = TimeSpan.FromSeconds(30)
    };

    // Act
    var result = _validator.Validate(null, options);

    // Assert
    result.Failed.Should().BeTrue();
    result.FailureMessage.Should().Contain("BaseUrl");
    result.FailureMessage.Should().Contain("is required");
  }


  [Fact]
  public void Validate_WithInvalidBaseUrl_ReturnsFailed()
  {
    // Arrange
    var options = new Smtp2GoOptions
    {
      ApiKey = "api-test-key",
      BaseUrl = "not-a-url",
      Timeout = TimeSpan.FromSeconds(30)
    };

    // Act
    var result = _validator.Validate(null, options);

    // Assert
    result.Failed.Should().BeTrue();
    result.FailureMessage.Should().Contain("BaseUrl");
    result.FailureMessage.Should().Contain("valid HTTP or HTTPS URL");
  }


  [Fact]
  public void Validate_WithFtpBaseUrl_ReturnsFailed()
  {
    // Arrange — Only HTTP/HTTPS schemes are accepted.
    var options = new Smtp2GoOptions
    {
      ApiKey = "api-test-key",
      BaseUrl = "ftp://api.smtp2go.com/v3/",
      Timeout = TimeSpan.FromSeconds(30)
    };

    // Act
    var result = _validator.Validate(null, options);

    // Assert
    result.Failed.Should().BeTrue();
    result.FailureMessage.Should().Contain("BaseUrl");
    result.FailureMessage.Should().Contain("valid HTTP or HTTPS URL");
  }

  #endregion


  #region Validate - Timeout

  [Fact]
  public void Validate_WithZeroTimeout_ReturnsFailed()
  {
    // Arrange
    var options = new Smtp2GoOptions
    {
      ApiKey = "api-test-key",
      BaseUrl = "https://api.smtp2go.com/v3/",
      Timeout = TimeSpan.Zero
    };

    // Act
    var result = _validator.Validate(null, options);

    // Assert
    result.Failed.Should().BeTrue();
    result.FailureMessage.Should().Contain("Timeout");
    result.FailureMessage.Should().Contain("positive");
  }


  [Fact]
  public void Validate_WithNegativeTimeout_ReturnsFailed()
  {
    // Arrange
    var options = new Smtp2GoOptions
    {
      ApiKey = "api-test-key",
      BaseUrl = "https://api.smtp2go.com/v3/",
      Timeout = TimeSpan.FromSeconds(-1)
    };

    // Act
    var result = _validator.Validate(null, options);

    // Assert
    result.Failed.Should().BeTrue();
    result.FailureMessage.Should().Contain("Timeout");
  }

  #endregion


  #region Validate - Multiple Failures

  [Fact]
  public void Validate_WithMultipleInvalidSettings_ReportsAllFailures()
  {
    // Arrange
    var options = new Smtp2GoOptions
    {
      ApiKey = null,
      BaseUrl = "not-a-url",
      Timeout = TimeSpan.Zero
    };

    // Act
    var result = _validator.Validate(null, options);

    // Assert
    result.Failed.Should().BeTrue();
    result.FailureMessage.Should().Contain("ApiKey");
    result.FailureMessage.Should().Contain("BaseUrl");
    result.FailureMessage.Should().Contain("Timeout");
  }

  #endregion


  #region Validate - Defaults

  [Fact]
  public void DefaultOptions_HaveExpectedDefaults()
  {
    // Arrange & Act
    var options = new Smtp2GoOptions();

    // Assert — ApiKey defaults to null (must be configured), other properties have sensible defaults.
    options.ApiKey.Should().BeNull();
    options.BaseUrl.Should().Be(Smtp2GoOptions.DefaultBaseUrl);
    options.Timeout.Should().Be(TimeSpan.FromSeconds(30));
    options.Resilience.Should().NotBeNull();
  }


  [Fact]
  public void SectionName_IsSmtp2Go()
  {
    // Assert
    Smtp2GoOptions.SectionName.Should().Be("Smtp2Go");
  }

  #endregion
}
