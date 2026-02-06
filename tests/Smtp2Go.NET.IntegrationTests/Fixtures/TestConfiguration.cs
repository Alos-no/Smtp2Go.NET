namespace Smtp2Go.NET.IntegrationTests.Fixtures;

using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.UserSecrets;

/// <summary>
///   A helper class to build configuration from multiple sources (JSON, Environment, User Secrets)
///   for use in integration tests.
/// </summary>
/// <remarks>
///   <para>
///     Configuration sources are loaded in priority order (lowest to highest):
///     <list type="number">
///       <item><c>appsettings.json</c> — template/placeholder values (checked into source control)</item>
///       <item>Environment variables — CI/CD pipelines or container configuration</item>
///       <item>User Secrets — local developer secrets (not checked into source control)</item>
///     </list>
///   </para>
/// </remarks>
public static class TestConfiguration
{
  #region Constants & Statics

  /// <summary>Gets the lazily-initialized configuration root.</summary>
  public static IConfigurationRoot Configuration { get; }

  /// <summary>Gets the SMTP2GO test settings loaded from the configuration.</summary>
  public static TestSmtp2GoSettings Settings { get; }

  #endregion


  #region Constructors

  /// <summary>Initializes the static TestConfiguration by building the configuration sources.</summary>
  static TestConfiguration()
  {
    // Build configuration from appsettings.json, environment variables, and user secrets.
    var builder = new ConfigurationBuilder()
      .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
      .AddEnvironmentVariables();

    // Dynamically find the assembly containing the user secrets. This is necessary because
    // the UserSecretsId is defined in the test project, not the source library. We scan
    // the loaded assemblies to find one that has the attribute and use it as the anchor.
    var testAssemblyWithSecrets = AppDomain.CurrentDomain.GetAssemblies()
      .FirstOrDefault(a => a.GetCustomAttribute<UserSecretsIdAttribute>() != null);

    if (testAssemblyWithSecrets != null)
      builder.AddUserSecrets(testAssemblyWithSecrets);

    Configuration = builder.Build();

    // Bind the configuration to a strongly-typed settings object.
    Settings = new TestSmtp2GoSettings();
    Configuration.GetSection("Smtp2Go").Bind(Settings);
  }

  #endregion
}


/// <summary>
///   Represents the configuration options required for SMTP2GO integration tests.
/// </summary>
/// <remarks>
///   <para>
///     Contains real secrets (API keys, sender/recipient addresses) that must be configured
///     via user secrets or environment variables. Webhook Basic Auth credentials are NOT
///     included here — they are arbitrary test constants defined by the tests themselves.
///   </para>
/// </remarks>
public class TestSmtp2GoSettings
{
  #region Properties & Fields - Public

  /// <summary>Gets the API key settings (sandbox and live).</summary>
  public ApiKeySettings ApiKey { get; set; } = new();

  /// <summary>
  ///   Gets or sets the verified sender email address for all integration tests.
  ///   Must be a sender verified on the SMTP2GO account (e.g., <c>noreply@yourdomain.com</c>).
  /// </summary>
  public string TestSender { get; set; } = string.Empty;

  /// <summary>Gets or sets the real email address for live delivery tests.</summary>
  public string TestRecipient { get; set; } = string.Empty;

  /// <summary>Gets or sets the SMTP2GO API base URL.</summary>
  public string BaseUrl { get; set; } = "https://api.smtp2go.com/v3/";

  #endregion


  #region Nested Types

  /// <summary>API key configuration with separate sandbox and live keys.</summary>
  public class ApiKeySettings
  {
    /// <summary>
    ///   Gets or sets the sandbox API key. Emails are accepted but not delivered.
    ///   Used for API contract testing without incurring delivery costs.
    /// </summary>
    public string Sandbox { get; set; } = string.Empty;

    /// <summary>
    ///   Gets or sets the live API key. Emails are actually delivered.
    ///   Used for end-to-end delivery and webhook tests.
    /// </summary>
    public string Live { get; set; } = string.Empty;
  }

  #endregion
}
