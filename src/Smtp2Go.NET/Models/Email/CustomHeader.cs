namespace Smtp2Go.NET.Models.Email;

using System.Text.Json.Serialization;

/// <summary>
///   Represents a custom email header to include in an outgoing message.
/// </summary>
/// <remarks>
///   <para>
///     Custom headers are useful for tracking, categorization, and integration
///     with external systems. Common examples include <c>X-Custom-Tag</c> for
///     analytics grouping and <c>Reply-To</c> for directing replies.
///   </para>
///   <para>
///     Header names should follow RFC 5322 conventions. Custom headers typically
///     use the <c>X-</c> prefix by convention.
///   </para>
/// </remarks>
/// <example>
///   <code>
///     var header = new CustomHeader
///     {
///       Header = "X-Custom-Tag",
///       Value = "password-reset"
///     };
///   </code>
/// </example>
public class CustomHeader
{
  /// <summary>
  ///   Gets or sets the header name.
  /// </summary>
  /// <remarks>
  ///   <para>
  ///     The header name (e.g., <c>"X-Custom-Tag"</c>, <c>"Reply-To"</c>).
  ///     Must conform to RFC 5322 header field name syntax.
  ///   </para>
  /// </remarks>
  [JsonPropertyName("header")]
  public required string Header { get; set; }

  /// <summary>
  ///   Gets or sets the header value.
  /// </summary>
  /// <remarks>
  ///   <para>
  ///     The header value (e.g., <c>"password-reset"</c>, <c>"support@alos.app"</c>).
  ///   </para>
  /// </remarks>
  [JsonPropertyName("value")]
  public required string Value { get; set; }
}
