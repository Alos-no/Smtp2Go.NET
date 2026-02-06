namespace Smtp2Go.NET.Exceptions;

/// <summary>
///   Exception thrown when SMTP2GO configuration is invalid or missing.
/// </summary>
/// <remarks>
///   <para>
///     This exception is typically thrown during application startup when options validation fails,
///     or at runtime when a named client configuration cannot be resolved.
///   </para>
/// </remarks>
public sealed class Smtp2GoConfigurationException : Smtp2GoException
{
  /// <summary>
  ///   Initializes a new instance of the <see cref="Smtp2GoConfigurationException" /> class.
  /// </summary>
  public Smtp2GoConfigurationException()
  {
  }


  /// <summary>
  ///   Initializes a new instance of the <see cref="Smtp2GoConfigurationException" /> class with a specified error message.
  /// </summary>
  /// <param name="message">The message that describes the configuration error.</param>
  public Smtp2GoConfigurationException(string message)
    : base(message)
  {
  }


  /// <summary>
  ///   Initializes a new instance of the <see cref="Smtp2GoConfigurationException" /> class with a specified error message
  ///   and a reference to the inner exception that caused this exception.
  /// </summary>
  /// <param name="message">The message that describes the error.</param>
  /// <param name="innerException">
  ///   The exception that is the cause of the current exception, or a null reference if no inner exception is specified.
  /// </param>
  public Smtp2GoConfigurationException(string message, Exception innerException)
    : base(message, innerException)
  {
  }


  /// <summary>
  ///   Initializes a new instance of the <see cref="Smtp2GoConfigurationException" /> class
  ///   with the name of the configuration that failed and a list of validation errors.
  /// </summary>
  /// <param name="configurationName">The name of the configuration that failed validation.</param>
  /// <param name="errors">The list of validation errors.</param>
  public Smtp2GoConfigurationException(string configurationName, IReadOnlyList<string> errors)
    : base(FormatMessage(configurationName, errors))
  {
    ConfigurationName = configurationName;
    ValidationErrors = errors;
  }


  /// <summary>
  ///   Gets the name of the configuration that failed validation.
  /// </summary>
  public string? ConfigurationName { get; }

  /// <summary>
  ///   Gets the list of validation errors, if any.
  /// </summary>
  public IReadOnlyList<string>? ValidationErrors { get; }


  private static string FormatMessage(string configurationName, IReadOnlyList<string> errors)
  {
    var configPart = string.IsNullOrEmpty(configurationName)
      ? "Smtp2Go configuration"
      : $"Smtp2Go configuration '{configurationName}'";

    return errors.Count == 1
      ? $"{configPart} is invalid: {errors[0]}"
      : $"{configPart} has {errors.Count} validation errors:\n- " + string.Join("\n- ", errors);
  }
}
