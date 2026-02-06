namespace Smtp2Go.NET.IntegrationTests.Helpers;

using Fixtures;

/// <summary>
///   Provides helper methods for validating that the necessary test configuration
///   and secrets are present before running integration tests.
/// </summary>
public static class TestSecretValidator
{
  #region Methods

  /// <summary>
  ///   Checks if a configuration value is null, empty, or still has its placeholder value.
  /// </summary>
  /// <param name="value">The configuration value to check.</param>
  /// <returns><c>true</c> if the secret is missing or has a placeholder value; otherwise, <c>false</c>.</returns>
  public static bool IsSecretMissing(string? value)
  {
    return string.IsNullOrWhiteSpace(value) ||
           value.Equals("from-user-secrets", StringComparison.OrdinalIgnoreCase);
  }


  /// <summary>
  ///   Gets a list of all required secrets for sandbox integration tests that are currently missing.
  /// </summary>
  /// <returns>A list of missing secret names. Empty if all sandbox secrets are present.</returns>
  public static List<string> GetMissingSandboxSecrets()
  {
    var settings = TestConfiguration.Settings;
    var missing = new List<string>();

    if (IsSecretMissing(settings.ApiKey.Sandbox))
      missing.Add("Smtp2Go:ApiKey:Sandbox");

    if (IsSecretMissing(settings.TestSender))
      missing.Add("Smtp2Go:TestSender");

    return missing;
  }


  /// <summary>
  ///   Gets a list of all required secrets for live integration tests that are currently missing.
  /// </summary>
  /// <remarks>
  ///   Webhook delivery tests also use this — webhook Basic Auth credentials are arbitrary
  ///   test constants (we define them when creating the webhook), not external secrets.
  /// </remarks>
  /// <returns>A list of missing secret names. Empty if all live secrets are present.</returns>
  public static List<string> GetMissingLiveSecrets()
  {
    var settings = TestConfiguration.Settings;
    var missing = new List<string>();

    if (IsSecretMissing(settings.ApiKey.Live))
      missing.Add("Smtp2Go:ApiKey:Live");

    if (IsSecretMissing(settings.TestSender))
      missing.Add("Smtp2Go:TestSender");

    if (IsSecretMissing(settings.TestRecipient))
      missing.Add("Smtp2Go:TestRecipient");

    return missing;
  }


  /// <summary>
  ///   Asserts that all required sandbox secrets are present.
  ///   Fails the test with a descriptive message if any are missing.
  /// </summary>
  public static void AssertSandboxSecretsPresent()
  {
    var missing = GetMissingSandboxSecrets();

    if (missing.Count > 0)
      Assert.Fail($"Missing required secrets: {string.Join(", ", missing)}. Configure via user secrets or environment variables.");
  }


  /// <summary>
  ///   Asserts that all required live secrets are present.
  ///   Fails the test with a descriptive message if any are missing.
  /// </summary>
  public static void AssertLiveSecretsPresent()
  {
    var missing = GetMissingLiveSecrets();

    if (missing.Count > 0)
      Assert.Fail($"Missing required secrets: {string.Join(", ", missing)}. Configure via user secrets or environment variables.");
  }


  /// <summary>
  ///   Asserts that cloudflared is installed (on PATH or at a known install location).
  ///   Fails the test with a descriptive message if cloudflared is not found.
  /// </summary>
  public static void AssertCloudflaredInstalled()
  {
    if (!CloudflareTunnelManager.IsCloudflaredInstalled())
      Assert.Fail("cloudflared is not installed. Install from https://developers.cloudflare.com/cloudflare-one/connections/connect-networks/downloads/");
  }

  #endregion
}
