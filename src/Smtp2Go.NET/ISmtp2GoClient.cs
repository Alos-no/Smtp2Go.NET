namespace Smtp2Go.NET;

using Models.Email;

/// <summary>
///   Provides the primary client interface for the SMTP2GO API.
/// </summary>
/// <remarks>
///   <para>
///     This interface defines the contract for interacting with the SMTP2GO v3 API.
///     Implementations are registered via <see cref="ServiceCollectionExtensions" /> extension methods.
///   </para>
///   <para>
///     Sub-client modules are accessible via properties:
///     <list type="bullet">
///       <item><see cref="Webhooks" /> — Webhook management (create, list, delete).</item>
///       <item><see cref="Statistics" /> — Email analytics and delivery metrics.</item>
///     </list>
///   </para>
/// </remarks>
/// <example>
///   <code>
///   // Inject ISmtp2GoClient via DI
///   public class EmailService(ISmtp2GoClient smtp2Go)
///   {
///       public async Task SendAsync(CancellationToken ct)
///       {
///           var request = new EmailSendRequest
///           {
///               Sender = "noreply@example.com",
///               To = ["user@example.com"],
///               Subject = "Hello",
///               HtmlBody = "&lt;h1&gt;Hello World&lt;/h1&gt;"
///           };
///
///           var response = await smtp2Go.SendEmailAsync(request, ct);
///       }
///   }
///   </code>
/// </example>
public interface ISmtp2GoClient
{
  /// <summary>
  ///   Gets the webhook management sub-client.
  /// </summary>
  /// <remarks>
  ///   <para>
  ///     Use this property to create, list, and delete webhooks for receiving
  ///     email event notifications from SMTP2GO.
  ///   </para>
  /// </remarks>
  ISmtp2GoWebhookClient Webhooks { get; }

  /// <summary>
  ///   Gets the statistics and analytics sub-client.
  /// </summary>
  /// <remarks>
  ///   <para>
  ///     Use this property to retrieve email delivery statistics and
  ///     analytics from the SMTP2GO <c>/stats/*</c> endpoints.
  ///   </para>
  /// </remarks>
  ISmtp2GoStatisticsClient Statistics { get; }

  /// <summary>
  ///   Sends an email via the SMTP2GO API.
  /// </summary>
  /// <param name="request">The email send request containing sender, recipients, subject, and body.</param>
  /// <param name="ct">The cancellation token.</param>
  /// <returns>The email send response containing success/failure counts and email ID.</returns>
  /// <exception cref="Exceptions.Smtp2GoApiException">Thrown when the SMTP2GO API returns an error.</exception>
  /// <exception cref="HttpRequestException">Thrown when the HTTP request fails.</exception>
  Task<EmailSendResponse> SendEmailAsync(EmailSendRequest request, CancellationToken ct = default);
}
