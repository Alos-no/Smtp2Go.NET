namespace Smtp2Go.NET.IntegrationTests.Helpers;

using Fixtures;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

/// <summary>
///   Factory for creating <see cref="ISmtp2GoClient"/> instances via the library's DI extension method.
///   Centralizes host + client construction so fixtures and tests share a single code path.
/// </summary>
internal static class Smtp2GoClientFactory
{
  /// <summary>
  ///   Creates an <see cref="IHost"/> with <see cref="ISmtp2GoClient"/> registered via DI,
  ///   configured with the specified API key and test-appropriate logging.
  /// </summary>
  /// <remarks>
  ///   <para>
  ///     The returned host owns the DI container lifetime. Callers that need the client for
  ///     the duration of a test class (fixtures) should store and dispose the host. Callers
  ///     that need a throwaway client (e.g., invalid key tests) can use <see cref="CreateClient"/>.
  ///   </para>
  /// </remarks>
  /// <param name="apiKey">The SMTP2GO API key to use.</param>
  /// <returns>A tuple of the built host and the resolved client.</returns>
  public static (IHost Host, ISmtp2GoClient Client) CreateHostedClient(string apiKey)
  {
    var settings = TestConfiguration.Settings;

    var builder = Host.CreateApplicationBuilder();

    // Configure test-appropriate logging: concise single-line output,
    // debug-level for Smtp2Go, suppress framework noise.
    builder.Logging.ClearProviders();
    builder.Logging.AddSimpleConsole(o =>
    {
      o.SingleLine = true;
      o.IncludeScopes = true;
      o.TimestampFormat = "HH:mm:ss.fff ";
    });
    builder.Logging.SetMinimumLevel(LogLevel.Debug);
    builder.Logging.AddFilter("Microsoft", LogLevel.Warning);
    builder.Logging.AddFilter("System", LogLevel.Warning);

    // Use the SDK's own DI extension method — ensures the actual DI configuration is tested.
    builder.Services.AddSmtp2GoWithHttp(options =>
    {
      options.ApiKey = apiKey;
      options.BaseUrl = settings.BaseUrl;
    });

    var host = builder.Build();
    var client = host.Services.GetRequiredService<ISmtp2GoClient>();

    return (host, client);
  }


  /// <summary>
  ///   Creates a standalone <see cref="ISmtp2GoClient"/> configured with a specific API key.
  ///   The underlying host is not tracked — suitable for short-lived test scenarios
  ///   (e.g., verifying behavior with an invalid API key).
  /// </summary>
  /// <param name="apiKey">The API key to use.</param>
  /// <returns>A configured <see cref="ISmtp2GoClient"/> instance.</returns>
  public static ISmtp2GoClient CreateClient(string apiKey)
  {
    var (_, client) = CreateHostedClient(apiKey);

    return client;
  }
}
