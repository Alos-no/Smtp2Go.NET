namespace Smtp2Go.NET.Models.Statistics;

using System.Text.Json.Serialization;

/// <summary>
///   Request model for the SMTP2GO <c>POST /stats/email_summary</c> endpoint.
/// </summary>
/// <remarks>
///   <para>
///     Retrieves aggregate email sending statistics for the account, optionally
///     filtered by a date range. If no dates are specified, the API returns
///     statistics for the default period (typically the last 30 days).
///   </para>
/// </remarks>
public class EmailSummaryRequest
{
  /// <summary>
  ///   Gets or sets the start date for the statistics query.
  /// </summary>
  /// <remarks>
  ///   <para>
  ///     The date must be in <c>yyyy-MM-dd</c> format (e.g., <c>"2024-01-01"</c>).
  ///     If omitted, the API uses its default start date.
  ///   </para>
  /// </remarks>
  [JsonPropertyName("start_date")]
  public string? StartDate { get; set; }

  /// <summary>
  ///   Gets or sets the end date for the statistics query.
  /// </summary>
  /// <remarks>
  ///   <para>
  ///     The date must be in <c>yyyy-MM-dd</c> format (e.g., <c>"2024-12-31"</c>).
  ///     If omitted, the API uses the current date as the end date.
  ///   </para>
  /// </remarks>
  [JsonPropertyName("end_date")]
  public string? EndDate { get; set; }
}
