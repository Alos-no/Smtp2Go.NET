namespace Smtp2Go.NET;

using Configuration;
using Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

/// <summary>
///   Provides extension methods for setting up SMTP2GO services in an <see cref="IServiceCollection" />.
/// </summary>
public static class ServiceCollectionExtensions
{
  #region Methods

  /// <summary>
  ///   <para>Registers the <see cref="ISmtp2GoClient" /> and its dependencies using a configuration section.</para>
  ///   <para>
  ///     This is a convenience method that binds to the "Smtp2Go" section of the application's
  ///     <see cref="IConfiguration" />.
  ///   </para>
  /// </summary>
  /// <param name="services">The <see cref="IServiceCollection" /> to add the services to.</param>
  /// <param name="configuration">The application configuration.</param>
  /// <returns>The <see cref="IServiceCollection" /> so that additional calls can be chained.</returns>
  /// <exception cref="OptionsValidationException">
  ///   Thrown at application startup if required configuration is missing or invalid.
  /// </exception>
  /// <example>
  ///   <code>
  ///   // In Program.cs
  ///   builder.Services.AddSmtp2Go(builder.Configuration);
  ///   </code>
  /// </example>
  public static IServiceCollection AddSmtp2Go(
    this IServiceCollection services,
    IConfiguration configuration)
  {
    return services.AddSmtp2Go(options =>
      configuration.GetSection(Smtp2GoOptions.SectionName).Bind(options));
  }


  /// <summary>
  ///   <para>
  ///     Registers the <see cref="ISmtp2GoClient" /> and its dependencies, allowing for fine-grained
  ///     programmatic configuration.
  ///   </para>
  ///   <para>
  ///     Configuration is validated at application startup. If required settings are missing,
  ///     an <see cref="OptionsValidationException" /> is thrown with a clear error message
  ///     indicating what configuration is missing and how to fix it.
  ///   </para>
  /// </summary>
  /// <param name="services">The <see cref="IServiceCollection" /> to add the services to.</param>
  /// <param name="configureOptions">An action to configure the <see cref="Smtp2GoOptions" />.</param>
  /// <returns>The <see cref="IServiceCollection" /> so that additional calls can be chained.</returns>
  /// <exception cref="OptionsValidationException">
  ///   Thrown at application startup if required configuration is missing or invalid.
  /// </exception>
  /// <example>
  ///   <code>
  ///   // In Program.cs
  ///   builder.Services.AddSmtp2Go(options =>
  ///   {
  ///       options.ApiKey = "api-XXXXXXXXXX";
  ///   });
  ///   </code>
  /// </example>
  public static IServiceCollection AddSmtp2Go(
    this IServiceCollection services,
    Action<Smtp2GoOptions> configureOptions)
  {
    // Configure options with the provided delegate.
    services.Configure(configureOptions);

    // Register the validator for early failure with clear error messages.
    services.TryAddSingleton<IValidateOptions<Smtp2GoOptions>, Smtp2GoOptionsValidator>();

    // Add options validation at startup to fail fast with clear error messages.
    services
      .AddOptions<Smtp2GoOptions>()
      .ValidateOnStart();

    return services;
  }


  /// <summary>
  ///   <para>Registers the <see cref="ISmtp2GoClient" /> with HTTP client support for SMTP2GO API calls.</para>
  ///   <para>
  ///     This overload configures an HTTP client with production-ready resilience including:
  ///     <list type="bullet">
  ///       <item>Retry with exponential backoff (idempotent methods only; POST is NOT retried)</item>
  ///       <item>Circuit breaker to prevent cascading failures</item>
  ///       <item>Per-attempt and total request timeouts</item>
  ///       <item>Client-side rate limiting</item>
  ///     </list>
  ///   </para>
  /// </summary>
  /// <param name="services">The <see cref="IServiceCollection" /> to add the services to.</param>
  /// <param name="configuration">The application configuration.</param>
  /// <param name="configureHttpClient">Optional action to further configure the HTTP client.</param>
  /// <returns>The <see cref="IServiceCollection" /> so that additional calls can be chained.</returns>
  /// <example>
  ///   <code>
  ///   // In Program.cs
  ///   builder.Services.AddSmtp2GoWithHttp(builder.Configuration);
  ///   </code>
  /// </example>
  public static IServiceCollection AddSmtp2GoWithHttp(
    this IServiceCollection services,
    IConfiguration configuration,
    Action<HttpClient>? configureHttpClient = null)
  {
    // Register base services (options, validation).
    services.AddSmtp2Go(configuration);

    // Add the typed HTTP client with resilience pipeline.
    // This registers ISmtp2GoClient -> Smtp2GoClient with a configured HttpClient.
    var httpClientBuilder = services.AddHttpClient<ISmtp2GoClient, Smtp2GoClient>();

    // Add resilience pipeline to the HTTP client.
    httpClientBuilder.AddResilienceHandler("Smtp2GoPipeline", (pipelineBuilder, context) =>
    {
      var options = context.ServiceProvider
        .GetRequiredService<IOptionsMonitor<Smtp2GoOptions>>()
        .Get(Options.DefaultName);

      HttpClientExtensions.ConfigureResiliencePipeline(pipelineBuilder, options.Resilience);
    });

    // Allow additional HTTP client configuration.
    if (configureHttpClient != null)
    {
      httpClientBuilder.ConfigureHttpClient(configureHttpClient);
    }

    return services;
  }


  /// <summary>
  ///   <para>Registers the <see cref="ISmtp2GoClient" /> with HTTP client support and programmatic configuration.</para>
  /// </summary>
  /// <param name="services">The <see cref="IServiceCollection" /> to add the services to.</param>
  /// <param name="configureOptions">An action to configure the <see cref="Smtp2GoOptions" />.</param>
  /// <param name="configureHttpClient">Optional action to further configure the HTTP client.</param>
  /// <returns>The <see cref="IServiceCollection" /> so that additional calls can be chained.</returns>
  public static IServiceCollection AddSmtp2GoWithHttp(
    this IServiceCollection services,
    Action<Smtp2GoOptions> configureOptions,
    Action<HttpClient>? configureHttpClient = null)
  {
    // Register base services (options, validation).
    services.AddSmtp2Go(configureOptions);

    // Add the typed HTTP client with resilience pipeline.
    var httpClientBuilder = services.AddHttpClient<ISmtp2GoClient, Smtp2GoClient>();

    // Add resilience pipeline to the HTTP client.
    httpClientBuilder.AddResilienceHandler("Smtp2GoPipeline", (pipelineBuilder, context) =>
    {
      var options = context.ServiceProvider
        .GetRequiredService<IOptionsMonitor<Smtp2GoOptions>>()
        .Get(Options.DefaultName);

      HttpClientExtensions.ConfigureResiliencePipeline(pipelineBuilder, options.Resilience);
    });

    // Allow additional HTTP client configuration.
    if (configureHttpClient != null)
    {
      httpClientBuilder.ConfigureHttpClient(configureHttpClient);
    }

    return services;
  }

  #endregion
}
