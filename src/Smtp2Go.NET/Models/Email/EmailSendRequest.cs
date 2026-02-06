namespace Smtp2Go.NET.Models.Email;

using System.Text.Json.Serialization;

/// <summary>
///   Request model for the SMTP2GO <c>POST /email/send</c> endpoint.
/// </summary>
/// <remarks>
///   <para>
///     Sends an email through the SMTP2GO API. At minimum, <see cref="Sender"/>,
///     <see cref="To"/>, and <see cref="Subject"/> are required. Either
///     <see cref="TextBody"/> or <see cref="HtmlBody"/> (or both) should be provided
///     unless using a <see cref="TemplateId"/>.
///   </para>
///   <para>
///     Attachments are Base64-encoded and included inline in the request body.
///     For inline images referenced via <c>cid:</c> in HTML bodies, use the
///     <see cref="Inlines"/> collection.
///   </para>
/// </remarks>
public class EmailSendRequest
{
  /// <summary>
  ///   Gets or sets the sender email address.
  /// </summary>
  /// <remarks>
  ///   <para>
  ///     The sender address must be verified in the SMTP2GO account.
  ///     Supports the format <c>"Display Name &lt;email@example.com&gt;"</c>.
  ///   </para>
  /// </remarks>
  /// <example><c>"Alos Notifications &lt;noreply@alos.app&gt;"</c></example>
  [JsonPropertyName("sender")]
  public required string Sender { get; set; }

  /// <summary>
  ///   Gets or sets the primary recipient email addresses.
  /// </summary>
  /// <remarks>
  ///   <para>
  ///     At least one recipient is required. Each entry supports the format
  ///     <c>"Display Name &lt;email@example.com&gt;"</c>.
  ///   </para>
  /// </remarks>
  [JsonPropertyName("to")]
  public required string[] To { get; set; }

  /// <summary>
  ///   Gets or sets the email subject line.
  /// </summary>
  [JsonPropertyName("subject")]
  public required string Subject { get; set; }

  /// <summary>
  ///   Gets or sets the plain text body of the email.
  /// </summary>
  /// <remarks>
  ///   <para>
  ///     If both <see cref="TextBody"/> and <see cref="HtmlBody"/> are provided,
  ///     the email is sent as a multipart/alternative message, allowing the
  ///     recipient's client to choose the preferred format.
  ///   </para>
  /// </remarks>
  [JsonPropertyName("text_body")]
  public string? TextBody { get; set; }

  /// <summary>
  ///   Gets or sets the HTML body of the email.
  /// </summary>
  /// <remarks>
  ///   <para>
  ///     When using inline images, reference them via <c>cid:filename</c> in the HTML
  ///     and include the corresponding files in the <see cref="Inlines"/> collection.
  ///   </para>
  /// </remarks>
  [JsonPropertyName("html_body")]
  public string? HtmlBody { get; set; }

  /// <summary>
  ///   Gets or sets the CC (carbon copy) recipient email addresses.
  /// </summary>
  /// <remarks>
  ///   <para>
  ///     CC recipients receive a copy of the email and are visible to all recipients.
  ///     Each entry supports the format <c>"Display Name &lt;email@example.com&gt;"</c>.
  ///   </para>
  /// </remarks>
  [JsonPropertyName("cc")]
  public string[]? Cc { get; set; }

  /// <summary>
  ///   Gets or sets the BCC (blind carbon copy) recipient email addresses.
  /// </summary>
  /// <remarks>
  ///   <para>
  ///     BCC recipients receive a copy of the email but are not visible to other recipients.
  ///     Each entry supports the format <c>"Display Name &lt;email@example.com&gt;"</c>.
  ///   </para>
  /// </remarks>
  [JsonPropertyName("bcc")]
  public string[]? Bcc { get; set; }

  /// <summary>
  ///   Gets or sets custom email headers to include in the message.
  /// </summary>
  /// <remarks>
  ///   <para>
  ///     Custom headers are useful for tracking and categorization. For example,
  ///     <c>X-Custom-Tag</c> headers can be used to group emails in SMTP2GO analytics.
  ///   </para>
  /// </remarks>
  [JsonPropertyName("custom_headers")]
  public CustomHeader[]? CustomHeaders { get; set; }

  /// <summary>
  ///   Gets or sets the file attachments to include with the email.
  /// </summary>
  /// <remarks>
  ///   <para>
  ///     Each attachment includes a filename, MIME type, and Base64-encoded content.
  ///     Attachments are delivered as downloadable files in the recipient's email client.
  ///   </para>
  /// </remarks>
  [JsonPropertyName("attachments")]
  public Attachment[]? Attachments { get; set; }

  /// <summary>
  ///   Gets or sets inline attachments for HTML body image references.
  /// </summary>
  /// <remarks>
  ///   <para>
  ///     Inline attachments are referenced in the HTML body via <c>cid:filename</c>.
  ///     Unlike regular <see cref="Attachments"/>, inline files are embedded within
  ///     the email body and are not shown as separate downloadable files.
  ///   </para>
  /// </remarks>
  [JsonPropertyName("inlines")]
  public Attachment[]? Inlines { get; set; }

  /// <summary>
  ///   Gets or sets the SMTP2GO template identifier to use for this email.
  /// </summary>
  /// <remarks>
  ///   <para>
  ///     When a template ID is specified, the email body is rendered from the template
  ///     with merge data from <see cref="TemplateData"/>. The <see cref="TextBody"/>
  ///     and <see cref="HtmlBody"/> properties are ignored when a template is used.
  ///   </para>
  /// </remarks>
  [JsonPropertyName("template_id")]
  public string? TemplateId { get; set; }

  /// <summary>
  ///   Gets or sets the template merge data for variable substitution.
  /// </summary>
  /// <remarks>
  ///   <para>
  ///     Used in conjunction with <see cref="TemplateId"/>. The keys in this dictionary
  ///     correspond to merge variables defined in the SMTP2GO template.
  ///   </para>
  /// </remarks>
  /// <example>
  ///   <code>
  ///     TemplateData = new Dictionary&lt;string, object&gt;
  ///     {
  ///       ["user_name"] = "John Doe",
  ///       ["verification_url"] = "https://alos.app/verify/abc123"
  ///     };
  ///   </code>
  /// </example>
  [JsonPropertyName("template_data")]
  public Dictionary<string, object>? TemplateData { get; set; }
}
