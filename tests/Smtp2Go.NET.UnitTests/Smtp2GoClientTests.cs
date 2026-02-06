namespace Smtp2Go.NET.UnitTests;

using System.Net;
using System.Text.Json;
using Smtp2Go.NET.Configuration;
using Smtp2Go.NET.Exceptions;
using Smtp2Go.NET.Models.Email;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

/// <summary>
///   Unit tests for <see cref="Smtp2GoClient" />.
/// </summary>
[Trait("Category", "Unit")]
public sealed class Smtp2GoClientTests
{
  #region Constants & Statics

  /// <summary>The test API key used across all tests.</summary>
  private const string TestApiKey = "api-test-key-for-unit-tests";

  /// <summary>The API key header name set by the client.</summary>
  private const string ApiKeyHeaderName = "X-Smtp2go-Api-Key";

  #endregion


  #region Constructor & Configuration

  [Fact]
  public void Constructor_SetsBaseAddress_FromOptions()
  {
    // Arrange & Act
    var (client, httpClient, _) = CreateClient();

    // Assert
    httpClient.BaseAddress.Should().NotBeNull();
    httpClient.BaseAddress!.ToString().Should().Be("https://api.smtp2go.com/v3/");
  }


  [Fact]
  public void Constructor_SetsApiKeyHeader_FromOptions()
  {
    // Arrange & Act
    var (client, httpClient, _) = CreateClient();

    // Assert
    httpClient.DefaultRequestHeaders.Contains(ApiKeyHeaderName).Should().BeTrue();
    httpClient.DefaultRequestHeaders.GetValues(ApiKeyHeaderName).Should().ContainSingle()
      .Which.Should().Be(TestApiKey);
  }


  [Fact]
  public void Constructor_SetsTimeout_FromOptions()
  {
    // Arrange & Act
    var (client, httpClient, _) = CreateClient(timeout: TimeSpan.FromSeconds(45));

    // Assert
    httpClient.Timeout.Should().Be(TimeSpan.FromSeconds(45));
  }


  [Fact]
  public void Constructor_DoesNotOverrideBaseAddress_WhenAlreadySet()
  {
    // Arrange — Pre-set the base address on the HttpClient.
    var existingBaseAddress = new Uri("https://custom.api.test/v3/");
    var handler = new MockHttpMessageHandler(
      new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{}") });

    var httpClient = new HttpClient(handler)
    {
      BaseAddress = existingBaseAddress
    };

    var options = CreateOptions();

    // Act
    var client = new Smtp2GoClient(
      httpClient,
      Options.Create(options),
      NullLogger<Smtp2GoClient>.Instance);

    // Assert — BaseAddress should remain the pre-set value.
    httpClient.BaseAddress.Should().Be(existingBaseAddress);
  }

  #endregion


  #region SendEmailAsync

  [Fact]
  public async Task SendEmailAsync_WithValidRequest_ReturnsResponse()
  {
    // Arrange
    var responseJson = JsonSerializer.Serialize(new
    {
      request_id = "req-123",
      data = new
      {
        succeeded = 1,
        failed = 0,
        email_id = "email-abc-123"
      }
    });

    var (client, _, _) = CreateClient(
      new HttpResponseMessage(HttpStatusCode.OK)
      {
        Content = new StringContent(responseJson, System.Text.Encoding.UTF8, "application/json")
      });

    var request = new EmailSendRequest
    {
      Sender = "test@example.com",
      To = ["recipient@example.com"],
      Subject = "Test",
      TextBody = "Hello"
    };

    // Act
    var response = await client.SendEmailAsync(request, TestContext.Current.CancellationToken);

    // Assert
    response.Should().NotBeNull();
    response.RequestId.Should().Be("req-123");
    response.Data.Should().NotBeNull();
    response.Data!.Succeeded.Should().Be(1);
    response.Data.Failed.Should().Be(0);
    response.Data.EmailId.Should().Be("email-abc-123");
  }


  [Fact]
  public async Task SendEmailAsync_WithNullRequest_ThrowsArgumentNullException()
  {
    // Arrange
    var (client, _, _) = CreateClient();

    // Act
    var act = async () => await client.SendEmailAsync(null!);

    // Assert
    await act.Should().ThrowAsync<ArgumentNullException>();
  }


  [Fact]
  public async Task SendEmailAsync_WithApiError_ThrowsSmtp2GoApiException()
  {
    // Arrange
    var errorJson = JsonSerializer.Serialize(new
    {
      request_id = "req-error-456",
      data = new
      {
        error = "Invalid API key",
        error_code = "E_ApiKey"
      }
    });

    var (client, _, _) = CreateClient(
      new HttpResponseMessage(HttpStatusCode.Unauthorized)
      {
        Content = new StringContent(errorJson, System.Text.Encoding.UTF8, "application/json")
      });

    var request = new EmailSendRequest
    {
      Sender = "test@example.com",
      To = ["recipient@example.com"],
      Subject = "Test",
      TextBody = "Hello"
    };

    // Act
    var act = async () => await client.SendEmailAsync(request);

    // Assert
    var ex = (await act.Should().ThrowAsync<Smtp2GoApiException>()).Which;
    ex.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    ex.ErrorMessage.Should().Be("Invalid API key");
    ex.RequestId.Should().Be("req-error-456");
  }


  [Fact]
  public async Task SendEmailAsync_WithServerError_ThrowsSmtp2GoApiException()
  {
    // Arrange
    var (client, _, _) = CreateClient(
      new HttpResponseMessage(HttpStatusCode.InternalServerError)
      {
        Content = new StringContent("Internal Server Error", System.Text.Encoding.UTF8, "text/plain")
      });

    var request = new EmailSendRequest
    {
      Sender = "test@example.com",
      To = ["recipient@example.com"],
      Subject = "Test",
      TextBody = "Hello"
    };

    // Act
    var act = async () => await client.SendEmailAsync(request);

    // Assert
    var ex = (await act.Should().ThrowAsync<Smtp2GoApiException>()).Which;
    ex.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
  }

  #endregion


  #region Statistics.GetEmailSummaryAsync

  [Fact]
  public async Task Statistics_GetEmailSummaryAsync_WithNoRequest_ReturnsResponse()
  {
    // Arrange
    var responseJson = JsonSerializer.Serialize(new
    {
      request_id = "req-summary-789",
      data = new
      {
        email_count = 100,
        hardbounces = 3,
        softbounces = 2,
        opens = 50,
        clicks = 10
      }
    });

    var (client, _, _) = CreateClient(
      new HttpResponseMessage(HttpStatusCode.OK)
      {
        Content = new StringContent(responseJson, System.Text.Encoding.UTF8, "application/json")
      });

    // Act — Statistics is now a sub-client module.
    var response = await client.Statistics.GetEmailSummaryAsync(ct: TestContext.Current.CancellationToken);

    // Assert
    response.Should().NotBeNull();
    response.RequestId.Should().Be("req-summary-789");
    response.Data.Should().NotBeNull();
    response.Data!.Emails.Should().Be(100);
    response.Data.HardBounces.Should().Be(3);
    response.Data.SoftBounces.Should().Be(2);
  }

  #endregion


  #region Sub-Client Properties

  [Fact]
  public void Webhooks_ReturnsNonNull()
  {
    // Arrange
    var (client, _, _) = CreateClient();

    // Act
    var webhooks = client.Webhooks;

    // Assert
    webhooks.Should().NotBeNull();
  }


  [Fact]
  public void Webhooks_ReturnsSameInstanceOnMultipleCalls()
  {
    // Arrange — The webhook sub-client is lazily created and should be reused.
    var (client, _, _) = CreateClient();

    // Act
    var first = client.Webhooks;
    var second = client.Webhooks;

    // Assert
    first.Should().BeSameAs(second);
  }


  [Fact]
  public void Statistics_ReturnsNonNull()
  {
    // Arrange
    var (client, _, _) = CreateClient();

    // Act
    var statistics = client.Statistics;

    // Assert
    statistics.Should().NotBeNull();
  }


  [Fact]
  public void Statistics_ReturnsSameInstanceOnMultipleCalls()
  {
    // Arrange — The statistics sub-client is lazily created and should be reused.
    var (client, _, _) = CreateClient();

    // Act
    var first = client.Statistics;
    var second = client.Statistics;

    // Assert
    first.Should().BeSameAs(second);
  }

  #endregion


  #region Helpers

  /// <summary>
  ///   Creates an <see cref="Smtp2GoClient" /> with a mock HTTP message handler.
  /// </summary>
  /// <param name="response">
  ///   The HTTP response to return from all requests. If null, a default 200 OK response is used.
  /// </param>
  /// <param name="timeout">The timeout to configure. Defaults to 30 seconds.</param>
  /// <returns>A tuple of the client, the underlying HttpClient (for header/configuration assertions), and the handler.</returns>
  private static (ISmtp2GoClient Client, HttpClient HttpClient, MockHttpMessageHandler Handler) CreateClient(
    HttpResponseMessage? response = null,
    TimeSpan? timeout = null)
  {
    var handler = new MockHttpMessageHandler(
      response ?? new HttpResponseMessage(HttpStatusCode.OK)
      {
        Content = new StringContent("{\"request_id\":\"test\",\"data\":{}}", System.Text.Encoding.UTF8,
          "application/json")
      });

    var httpClient = new HttpClient(handler);
    var options = CreateOptions(timeout);

    var client = new Smtp2GoClient(
      httpClient,
      Options.Create(options),
      NullLogger<Smtp2GoClient>.Instance);

    return (client, httpClient, handler);
  }


  /// <summary>
  ///   Creates a valid <see cref="Smtp2GoOptions" /> for testing.
  /// </summary>
  private static Smtp2GoOptions CreateOptions(TimeSpan? timeout = null)
  {
    return new Smtp2GoOptions
    {
      ApiKey = TestApiKey,
      BaseUrl = "https://api.smtp2go.com/v3/",
      Timeout = timeout ?? TimeSpan.FromSeconds(30)
    };
  }

  #endregion


  #region Mock HTTP Message Handler

  /// <summary>
  ///   A simple mock <see cref="HttpMessageHandler" /> that returns a predefined response.
  /// </summary>
  private sealed class MockHttpMessageHandler(HttpResponseMessage response) : HttpMessageHandler
  {
    /// <summary>
    ///   Gets the last request that was sent through this handler.
    /// </summary>
    public HttpRequestMessage? LastRequest { get; private set; }

    /// <inheritdoc />
    protected override Task<HttpResponseMessage> SendAsync(
      HttpRequestMessage request,
      CancellationToken cancellationToken)
    {
      LastRequest = request;

      return Task.FromResult(response);
    }
  }

  #endregion
}
