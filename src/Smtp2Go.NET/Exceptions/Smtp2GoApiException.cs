namespace Smtp2Go.NET.Exceptions;

using System.Net;

/// <summary>
///   Exception thrown when the SMTP2GO API returns an error response.
/// </summary>
/// <remarks>
///   <para>
///     This exception carries context about the failed API call, including the HTTP status code,
///     the API's error message, and the request ID for troubleshooting with SMTP2GO support.
///   </para>
/// </remarks>
public class Smtp2GoApiException : Smtp2GoException
{
  /// <summary>
  ///   Initializes a new instance of the <see cref="Smtp2GoApiException" /> class.
  /// </summary>
  public Smtp2GoApiException()
  {
  }


  /// <summary>
  ///   Initializes a new instance of the <see cref="Smtp2GoApiException" /> class with a specified error message.
  /// </summary>
  /// <param name="message">The message that describes the error.</param>
  public Smtp2GoApiException(string message)
    : base(message)
  {
  }


  /// <summary>
  ///   Initializes a new instance of the <see cref="Smtp2GoApiException" /> class with a specified error message
  ///   and a reference to the inner exception that caused this exception.
  /// </summary>
  /// <param name="message">The message that describes the error.</param>
  /// <param name="innerException">
  ///   The exception that is the cause of the current exception, or a null reference if no inner exception is specified.
  /// </param>
  public Smtp2GoApiException(string message, Exception innerException)
    : base(message, innerException)
  {
  }


  /// <summary>
  ///   Initializes a new instance of the <see cref="Smtp2GoApiException" /> class
  ///   with API error context.
  /// </summary>
  /// <param name="message">The message that describes the error.</param>
  /// <param name="statusCode">The HTTP status code from the SMTP2GO API response.</param>
  /// <param name="errorMessage">The error message from the SMTP2GO API response body.</param>
  /// <param name="requestId">The request ID from the SMTP2GO API response for troubleshooting.</param>
  /// <param name="innerException">The inner exception, if any.</param>
  public Smtp2GoApiException(
    string message,
    HttpStatusCode statusCode,
    string? errorMessage = null,
    string? requestId = null,
    Exception? innerException = null)
    : base(message, innerException!)
  {
    StatusCode = statusCode;
    ErrorMessage = errorMessage;
    RequestId = requestId;
  }


  /// <summary>
  ///   Gets the HTTP status code from the SMTP2GO API response.
  /// </summary>
  public HttpStatusCode? StatusCode { get; }

  /// <summary>
  ///   Gets the error message from the SMTP2GO API response body.
  /// </summary>
  public string? ErrorMessage { get; }

  /// <summary>
  ///   Gets the request ID from the SMTP2GO API response, useful for troubleshooting with SMTP2GO support.
  /// </summary>
  public string? RequestId { get; }
}
