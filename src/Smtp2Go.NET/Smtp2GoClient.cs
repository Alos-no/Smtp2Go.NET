namespace Smtp2Go.NET;

using System.Net;
using Configuration;
using Core;
using Internal;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Models;
using Models.Email;

/// <summary>
///   Default implementation of <see cref="ISmtp2GoClient" />.
/// </summary>
/// <remarks>
///   <para>
///     This client communicates with the SMTP2GO v3 API using HTTP POST requests.
///     Authentication is handled via the <c>X-Smtp2go-Api-Key</c> header, configured from
///     <see cref="Smtp2GoOptions.ApiKey" />.
///   </para>
///   <para>
///     The <see cref="Webhooks" /> and <see cref="Statistics" /> sub-clients are lazily
///     created and share the same <see cref="HttpClient" /> and logger.
///   </para>
/// </remarks>
internal sealed partial class Smtp2GoClient : Smtp2GoResource, ISmtp2GoClient
{
  #region Constants & Statics

  /// <summary>The SMTP2GO API header name for the API key.</summary>
  private const string ApiKeyHeaderName = "X-Smtp2go-Api-Key";

  /// <summary>API endpoint for sending emails.</summary>
  private const string EmailSendEndpoint = "email/send";

  #endregion


  #region Properties & Fields - Non-Public

  /// <summary>
  ///   Logger field required by <c>[LoggerMessage]</c> source generator.
  ///   Points to the same instance as the base class — see <see cref="Smtp2GoResource" /> remarks.
  /// </summary>
  // ReSharper disable once InconsistentNaming — required by LoggerMessage source generator convention.
  private readonly ILogger _logger;

  /// <summary>Lazily-created webhook sub-client.</summary>
  private Smtp2GoWebhookClient? _webhookClient;

  /// <summary>Lazily-created statistics sub-client.</summary>
  private Smtp2GoStatisticsClient? _statisticsClient;

  #endregion


  #region Constructors

  /// <summary>
  ///   Initializes a new instance of the <see cref="Smtp2GoClient" /> class.
  /// </summary>
  /// <param name="httpClient">The HTTP client (injected by IHttpClientFactory).</param>
  /// <param name="options">The SMTP2GO configuration options.</param>
  /// <param name="logger">The logger.</param>
  public Smtp2GoClient(
    HttpClient httpClient,
    IOptions<Smtp2GoOptions> options,
    ILogger<Smtp2GoClient> logger)
    : base(httpClient, logger)
  {
    _logger = logger;
    var opts = options.Value;

    // Set the base address from options if not already configured.
    if (HttpClient.BaseAddress is null)
    {
      HttpClient.BaseAddress = new Uri(opts.BaseUrl);
    }

    // Set the API key header.
    if (!string.IsNullOrWhiteSpace(opts.ApiKey))
    {
      HttpClient.DefaultRequestHeaders.Remove(ApiKeyHeaderName);
      HttpClient.DefaultRequestHeaders.Add(ApiKeyHeaderName, opts.ApiKey);
    }

    // Set the timeout from options.
    HttpClient.Timeout = opts.Timeout;
  }

  #endregion


  #region Properties - Public

  /// <inheritdoc />
  public ISmtp2GoWebhookClient Webhooks =>
    _webhookClient ??= new Smtp2GoWebhookClient(HttpClient, _logger);

  /// <inheritdoc />
  public ISmtp2GoStatisticsClient Statistics =>
    _statisticsClient ??= new Smtp2GoStatisticsClient(HttpClient, _logger);

  #endregion


  #region Methods - Public (ISmtp2GoClient)

  /// <inheritdoc />
  public async Task<EmailSendResponse> SendEmailAsync(EmailSendRequest request, CancellationToken ct = default)
  {
    ArgumentNullException.ThrowIfNull(request);

    LogEmailSendStarted(request.Sender, request.To?.Length ?? 0);

    var response = await PostAsync<EmailSendRequest, EmailSendResponse>(
      EmailSendEndpoint, request, ct).ConfigureAwait(false);

    LogEmailSendCompleted(response.Data?.Succeeded ?? 0, response.Data?.Failed ?? 0);

    return response;
  }

  #endregion


  #region Source-Generated Logging

  [LoggerMessage(LoggingConstants.EventIds.EmailSendStarted, LogLevel.Information,
    "Sending email from {Sender} to {RecipientCount} recipient(s)")]
  private partial void LogEmailSendStarted(string? sender, int recipientCount);

  [LoggerMessage(LoggingConstants.EventIds.EmailSendCompleted, LogLevel.Information,
    "Email send completed: {Succeeded} succeeded, {Failed} failed")]
  private partial void LogEmailSendCompleted(int succeeded, int failed);

  #endregion
}
