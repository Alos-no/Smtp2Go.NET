namespace Smtp2Go.NET.Internal;

using System.Text.Json;
using System.Text.Json.Serialization;

/// <summary>
///   Default JSON serialization options for the SMTP2GO API.
/// </summary>
/// <remarks>
///   <para>
///     The SMTP2GO API uses snake_case naming convention for all JSON properties.
///     Null values are omitted from serialization to keep requests minimal.
///   </para>
///   <para>
///     Deserialization is case-insensitive because live webhook captures showed mixed casing for
///     some callback keys (for example <c>Message-Id</c> and <c>Subject</c>).
///   </para>
/// </remarks>
internal static class Smtp2GoJsonDefaults
{
  /// <summary>
  ///   Standard JSON options for serializing/deserializing SMTP2GO API payloads.
  /// </summary>
  public static readonly JsonSerializerOptions Options = new()
  {
    PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
    PropertyNameCaseInsensitive = true,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
  };
}
