namespace Smtp2Go.NET.Exceptions;

/// <summary>
///   Base exception for all Smtp2Go.NET library errors.
/// </summary>
/// <remarks>
///   <para>
///     This base exception allows callers to catch all library-specific errors
///     while still enabling specific exception handling for derived types.
///   </para>
/// </remarks>
public class Smtp2GoException : Exception
{
  /// <summary>
  ///   Initializes a new instance of the <see cref="Smtp2GoException" /> class.
  /// </summary>
  public Smtp2GoException()
  {
  }


  /// <summary>
  ///   Initializes a new instance of the <see cref="Smtp2GoException" /> class with a specified error message.
  /// </summary>
  /// <param name="message">The message that describes the error.</param>
  public Smtp2GoException(string message)
    : base(message)
  {
  }


  /// <summary>
  ///   Initializes a new instance of the <see cref="Smtp2GoException" /> class with a specified error message
  ///   and a reference to the inner exception that caused this exception.
  /// </summary>
  /// <param name="message">The message that describes the error.</param>
  /// <param name="innerException">
  ///   The exception that is the cause of the current exception, or a null reference if no inner exception is specified.
  /// </param>
  public Smtp2GoException(string message, Exception innerException)
    : base(message, innerException)
  {
  }
}
