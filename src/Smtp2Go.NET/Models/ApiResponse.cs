namespace Smtp2Go.NET.Models;

using System.Text.Json.Serialization;

/// <summary>
///   Generic API response envelope for all SMTP2GO API responses.
/// </summary>
/// <remarks>
///   <para>
///     The SMTP2GO API wraps all responses in a standard envelope containing a
///     <c>request_id</c> for troubleshooting and a <c>data</c> object with the
///     response-specific payload.
///   </para>
///   <para>
///     Example JSON:
///     <code>
///     {
///       "request_id": "abc-123",
///       "data": { ... }
///     }
///     </code>
///   </para>
/// </remarks>
/// <typeparam name="TData">
///   The type of the response data payload. Each API endpoint defines its own data shape.
/// </typeparam>
public class ApiResponse<TData>
{
  /// <summary>
  ///   Gets the unique request identifier assigned by the SMTP2GO API.
  /// </summary>
  /// <remarks>
  ///   <para>
  ///     This identifier can be used when contacting SMTP2GO support to trace
  ///     a specific API call. It is returned in every API response.
  ///   </para>
  /// </remarks>
  [JsonPropertyName("request_id")]
  public string? RequestId { get; init; }

  /// <summary>
  ///   Gets the response data payload.
  /// </summary>
  /// <remarks>
  ///   <para>
  ///     The structure of the data object varies by endpoint. For example,
  ///     <c>/email/send</c> returns send results while <c>/stats/email</c>
  ///     returns summary statistics.
  ///   </para>
  /// </remarks>
  [JsonPropertyName("data")]
  public TData? Data { get; init; }
}
