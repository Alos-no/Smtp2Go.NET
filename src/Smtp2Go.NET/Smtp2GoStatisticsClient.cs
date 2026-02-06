namespace Smtp2Go.NET;

using Core;
using Internal;
using Microsoft.Extensions.Logging;
using Models.Statistics;

/// <summary>
///   Default implementation of <see cref="ISmtp2GoStatisticsClient" />.
/// </summary>
/// <remarks>
///   <para>
///     This sub-client handles statistics/analytics operations by inheriting the shared
///     <see cref="Smtp2GoResource.PostAsync{TRequest,TResponse}" /> helper from the base class.
///     It covers the <c>/stats/*</c> family of SMTP2GO API endpoints.
///   </para>
/// </remarks>
internal sealed partial class Smtp2GoStatisticsClient : Smtp2GoResource, ISmtp2GoStatisticsClient
{
  #region Constants & Statics

  /// <summary>API endpoint for email statistics summary.</summary>
  private const string EmailSummaryEndpoint = "stats/email_summary";

  #endregion


  #region Properties & Fields - Non-Public

  /// <summary>
  ///   Logger field required by <c>[LoggerMessage]</c> source generator.
  ///   Points to the same instance as the base class — see <see cref="Smtp2GoResource" /> remarks.
  /// </summary>
  // ReSharper disable once InconsistentNaming — required by LoggerMessage source generator convention.
  private readonly ILogger _logger;

  #endregion


  #region Constructors

  /// <summary>
  ///   Initializes a new instance of the <see cref="Smtp2GoStatisticsClient" /> class.
  /// </summary>
  /// <param name="httpClient">The shared HTTP client from the parent <see cref="Smtp2GoClient" />.</param>
  /// <param name="logger">The shared logger from the parent <see cref="Smtp2GoClient" />.</param>
  internal Smtp2GoStatisticsClient(HttpClient httpClient, ILogger logger)
    : base(httpClient, logger)
  {
    _logger = logger;
  }

  #endregion


  #region Methods - Public (ISmtp2GoStatisticsClient)

  /// <inheritdoc />
  public async Task<EmailSummaryResponse> GetEmailSummaryAsync(
    EmailSummaryRequest? request = null,
    CancellationToken ct = default)
  {
    LogEmailSummaryRequested();

    // Use an empty object if no request is specified (API requires a POST body).
    var body = request ?? new EmailSummaryRequest();

    var response = await PostAsync<EmailSummaryRequest, EmailSummaryResponse>(
      EmailSummaryEndpoint, body, ct).ConfigureAwait(false);

    return response;
  }

  #endregion


  #region Source-Generated Logging

  [LoggerMessage(LoggingConstants.EventIds.EmailSummaryRequested, LogLevel.Debug,
    "Requesting email statistics summary")]
  private partial void LogEmailSummaryRequested();

  #endregion
}
