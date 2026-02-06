namespace Smtp2Go.NET;

using Models.Statistics;

/// <summary>
///   Provides the statistics sub-client interface for SMTP2GO API analytics endpoints.
/// </summary>
/// <remarks>
///   <para>
///     Access this interface via <see cref="ISmtp2GoClient.Statistics" />.
///     The statistics client covers the <c>/stats/*</c> family of SMTP2GO endpoints,
///     providing aggregate email analytics and delivery metrics.
///   </para>
/// </remarks>
/// <example>
///   <code>
///   // Get email statistics for a date range
///   var stats = await smtp2Go.Statistics.GetEmailSummaryAsync(
///     new EmailSummaryRequest
///     {
///       StartDate = "2025-01-01",
///       EndDate = "2025-01-31"
///     });
///   </code>
/// </example>
public interface ISmtp2GoStatisticsClient
{
  /// <summary>
  ///   Gets email statistics summary from the SMTP2GO API.
  /// </summary>
  /// <param name="request">Optional request with date range filters. Pass null for default statistics.</param>
  /// <param name="ct">The cancellation token.</param>
  /// <returns>The email summary response containing delivery statistics.</returns>
  /// <exception cref="Exceptions.Smtp2GoApiException">Thrown when the SMTP2GO API returns an error.</exception>
  /// <exception cref="HttpRequestException">Thrown when the HTTP request fails.</exception>
  Task<EmailSummaryResponse> GetEmailSummaryAsync(
    EmailSummaryRequest? request = null,
    CancellationToken ct = default);
}
