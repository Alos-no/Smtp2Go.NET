namespace Smtp2Go.NET.IntegrationTests.Fixtures;

using Helpers;
using Microsoft.Extensions.Hosting;

/// <summary>
///   An xUnit class fixture that sets up a dependency injection container with the Smtp2Go.NET SDK
///   registered using the <strong>live</strong> API key.
/// </summary>
/// <remarks>
///   <para>
///     Live tests perform actual email delivery and webhook operations against the real SMTP2GO API.
///     The live API key is configured via user secrets at <c>Smtp2Go:ApiKey:Live</c>.
///   </para>
///   <para>
///     <strong>Warning:</strong> Live tests will send real emails and create/delete real webhooks.
///     Use with caution and ensure the test recipient is a controlled mailbox.
///   </para>
/// </remarks>
public sealed class Smtp2GoLiveFixture : IAsyncDisposable
{
  #region Properties & Fields - Non-Public

  /// <summary>The application host managing the DI container lifetime.</summary>
  private readonly IHost _host;

  #endregion


  #region Constructors

  /// <summary>
  ///   Initializes a new instance of the <see cref="Smtp2GoLiveFixture" /> class.
  /// </summary>
  public Smtp2GoLiveFixture()
  {
    (_host, Client) = Smtp2GoClientFactory.CreateHostedClient(TestConfiguration.Settings.ApiKey.Live);
  }

  #endregion


  #region Properties & Fields - Public

  /// <summary>Gets the fully configured <see cref="ISmtp2GoClient" /> using the live API key.</summary>
  public ISmtp2GoClient Client { get; }

  /// <summary>Gets the verified sender email address configured for tests.</summary>
  public string TestSender => TestConfiguration.Settings.TestSender;

  /// <summary>Gets the test recipient email address for live delivery tests.</summary>
  public string TestRecipient => TestConfiguration.Settings.TestRecipient;

  #endregion


  #region Methods Impl

  /// <inheritdoc />
  public async ValueTask DisposeAsync()
  {
    _host.Dispose();
    await Task.CompletedTask;
  }

  #endregion
}
