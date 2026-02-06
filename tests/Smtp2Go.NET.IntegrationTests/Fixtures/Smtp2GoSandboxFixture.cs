namespace Smtp2Go.NET.IntegrationTests.Fixtures;

using Helpers;
using Microsoft.Extensions.Hosting;

/// <summary>
///   An xUnit class fixture that sets up a dependency injection container with the Smtp2Go.NET SDK
///   registered using the <strong>sandbox</strong> API key.
/// </summary>
/// <remarks>
///   <para>
///     Sandbox tests verify API contract behavior without actual email delivery.
///     The sandbox API key is configured via user secrets at <c>Smtp2Go:ApiKey:Sandbox</c>.
///   </para>
///   <para>
///     This fixture uses the library's <see cref="ServiceCollectionExtensions.AddSmtp2GoWithHttp"/>
///     extension method (via <see cref="Smtp2GoClientFactory"/>) to register the client via DI,
///     ensuring the actual DI configuration is tested.
///   </para>
/// </remarks>
public sealed class Smtp2GoSandboxFixture : IAsyncDisposable
{
  #region Properties & Fields - Non-Public

  /// <summary>The application host managing the DI container lifetime.</summary>
  private readonly IHost _host;

  #endregion


  #region Constructors

  /// <summary>
  ///   Initializes a new instance of the <see cref="Smtp2GoSandboxFixture" /> class.
  /// </summary>
  public Smtp2GoSandboxFixture()
  {
    (_host, Client) = Smtp2GoClientFactory.CreateHostedClient(TestConfiguration.Settings.ApiKey.Sandbox);
  }

  #endregion


  #region Properties & Fields - Public

  /// <summary>Gets the fully configured <see cref="ISmtp2GoClient" /> using the sandbox API key.</summary>
  public ISmtp2GoClient Client { get; }

  /// <summary>Gets the verified sender email address configured for tests.</summary>
  public string TestSender => TestConfiguration.Settings.TestSender;

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
