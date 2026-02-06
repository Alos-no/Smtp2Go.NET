namespace Smtp2Go.NET.Configuration;

using Microsoft.Extensions.Options;

/// <summary>
///   Validates <see cref="Smtp2GoOptions" /> to ensure all required configuration is present and valid.
///   This validator is invoked at startup when using <c>ValidateOnStart()</c>, providing immediate feedback
///   for configuration issues rather than waiting for the first API call to fail.
/// </summary>
/// <remarks>
///   <para>
///     The validation errors are designed to be clear and actionable, mentioning the configuration
///     section name explicitly so developers can quickly identify the source of configuration issues.
///   </para>
/// </remarks>
public sealed class Smtp2GoOptionsValidator : IValidateOptions<Smtp2GoOptions>
{
  #region Methods Impl

  /// <inheritdoc />
  public ValidateOptionsResult Validate(string? name, Smtp2GoOptions options)
  {
    var failures = new List<string>();

    // Validate ApiKey is provided.
    if (string.IsNullOrWhiteSpace(options.ApiKey))
    {
      failures.Add(
        $"{Smtp2GoOptions.SectionName}:ApiKey is required. " +
        $"Set '{Smtp2GoOptions.SectionName}:ApiKey' in your configuration. " +
        "Obtain an API key from https://app.smtp2go.com/settings/apikeys.");
    }

    // Validate BaseUrl is a valid absolute URI.
    if (string.IsNullOrWhiteSpace(options.BaseUrl))
    {
      failures.Add(
        $"{Smtp2GoOptions.SectionName}:BaseUrl is required. " +
        $"Set '{Smtp2GoOptions.SectionName}:BaseUrl' in your configuration.");
    }
    else if (!Uri.TryCreate(options.BaseUrl, UriKind.Absolute, out var uri) ||
             (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
    {
      failures.Add(
        $"{Smtp2GoOptions.SectionName}:BaseUrl must be a valid HTTP or HTTPS URL. " +
        $"Current value: '{options.BaseUrl}'");
    }

    // Validate Timeout is positive.
    if (options.Timeout <= TimeSpan.Zero)
    {
      failures.Add(
        $"{Smtp2GoOptions.SectionName}:Timeout must be a positive duration. " +
        $"Current value: {options.Timeout}");
    }

    // Return validation result.
    return failures.Count > 0
      ? ValidateOptionsResult.Fail(failures)
      : ValidateOptionsResult.Success;
  }

  #endregion
}
