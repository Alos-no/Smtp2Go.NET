namespace Smtp2Go.NET.Models.Webhooks;

using System.Text.Json.Serialization;

/// <summary>
///   Response model for the SMTP2GO webhook deletion endpoint.
/// </summary>
/// <remarks>
///   <para>
///     The webhook deletion response is a simple envelope containing only
///     the request identifier. A successful HTTP 200 response indicates
///     the webhook was deleted. Unlike other responses, this does not
///     inherit from <see cref="ApiResponse{TData}"/> because the SMTP2GO
///     API returns no data payload for delete operations.
///   </para>
/// </remarks>
public class WebhookDeleteResponse
{
  /// <summary>
  ///   Gets the unique request identifier assigned by the SMTP2GO API.
  /// </summary>
  /// <remarks>
  ///   <para>
  ///     This identifier can be used when contacting SMTP2GO support to trace
  ///     the deletion request.
  ///   </para>
  /// </remarks>
  [JsonPropertyName("request_id")]
  public string? RequestId { get; init; }
}
