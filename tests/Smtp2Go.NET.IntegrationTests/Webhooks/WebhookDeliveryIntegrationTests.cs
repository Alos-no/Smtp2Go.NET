namespace Smtp2Go.NET.IntegrationTests.Webhooks;

using System.Net.Http.Headers;
using System.Text;
using Fixtures;
using Helpers;
using Smtp2Go.NET.Models.Email;
using Smtp2Go.NET.Models.Webhooks;

/// <summary>
///   End-to-end webhook delivery integration tests using the live API key,
///   a local webhook receiver, and a Cloudflare Quick Tunnel.
/// </summary>
/// <remarks>
///   <para>
///     These tests verify the full webhook delivery pipeline:
///     <list type="number">
///       <item>Start a local webhook receiver on a random port</item>
///       <item>Create a Cloudflare Quick Tunnel to expose the receiver publicly</item>
///       <item>Verify the tunnel accepts POST requests (self-test through the tunnel)</item>
///       <item>Register a webhook with SMTP2GO pointing to the tunnel URL</item>
///       <item>Send an email to trigger the webhook</item>
///       <item>Wait for the webhook payload to arrive at the receiver</item>
///       <item>Clean up: delete the webhook, stop tunnel, stop the receiver</item>
///     </list>
///   </para>
///   <para>
///     <strong>Prerequisites:</strong> <c>cloudflared</c> must be installed, and the live
///     API key must be configured. Webhook Basic Auth credentials are arbitrary test constants
///     defined below — they are NOT external secrets, since we define them when creating the webhook.
///   </para>
/// </remarks>
[Collection("Webhook")]
[Trait("Category", "Integration.Webhook")]
public sealed class WebhookDeliveryIntegrationTests : IClassFixture<Smtp2GoLiveFixture>
{
  #region Constants & Statics

  /// <summary>
  ///   Arbitrary Basic Auth username for the webhook receiver.
  ///   We define this when creating the webhook — it is NOT an external secret.
  /// </summary>
  private const string WebhookUsername = "test-webhook-user";

  /// <summary>
  ///   Arbitrary Basic Auth password for the webhook receiver.
  ///   We define this when creating the webhook — it is NOT an external secret.
  /// </summary>
  private const string WebhookPassword = "test-webhook-pass";

  #endregion


  #region Properties & Fields - Non-Public

  /// <summary>The live-configured client fixture.</summary>
  private readonly Smtp2GoLiveFixture _fixture;

  #endregion


  #region Constructors

  /// <summary>
  ///   Initializes a new instance of the <see cref="WebhookDeliveryIntegrationTests" /> class.
  /// </summary>
  public WebhookDeliveryIntegrationTests(Smtp2GoLiveFixture fixture)
  {
    _fixture = fixture;
  }

  #endregion


  #region Webhook Delivery

  [Fact]
  public async Task SendEmail_ReceivesDeliveredWebhook()
  {
    // Fail if live secrets are not configured (live key + sender + recipient).
    TestSecretValidator.AssertLiveSecretsPresent();

    // Fail if cloudflared is not installed.
    TestSecretValidator.AssertCloudflaredInstalled();

    var ct = TestContext.Current.CancellationToken;
    int? webhookId = null;

    await using var receiver = new WebhookReceiverFixture();
    await using var tunnel = new CloudflareTunnelManager();

    try
    {
      // Set up the full pipeline: receiver → tunnel → DNS → POST verify → webhook registration.
      // Subscribe to both 'processed' and 'delivered' events to catch the earliest callback.
      // 'processed' fires when SMTP2GO accepts the email; 'delivered' fires when the
      // recipient MTA accepts it.
      webhookId = await SetupWebhookPipelineAsync(
        receiver, tunnel,
        [WebhookCreateEvent.Processed, WebhookCreateEvent.Delivered],
        ct);

      // Send an email to trigger the webhook.
      var emailRequest = new EmailSendRequest
      {
        Sender = _fixture.TestSender,
        To = [_fixture.TestRecipient],
        Subject = $"Webhook Delivery Test - {Guid.NewGuid():N}",
        TextBody = "This email triggers a webhook delivery event."
      };

      var emailResponse = await _fixture.Client.SendEmailAsync(emailRequest, ct);
      emailResponse.Data.Should().NotBeNull();
      emailResponse.Data!.Succeeded.Should().BeGreaterThanOrEqualTo(1);

      Console.Error.WriteLine($"[WebhookDeliveryTest] Email sent successfully. Waiting for webhook callback...");

      // Wait for any webhook payload to arrive.
      // SMTP2GO sends one payload per event per recipient (WebhookCallbackPayload.Event is singular).
      // We accept any event type — 'processed' arrives first, 'delivered' later.
      // 180-second timeout accounts for email delivery delay and SMTP2GO processing time.
      var payload = await receiver.WaitForPayloadAsync(
        _ => true,
        timeout: TimeSpan.FromSeconds(180));

      // Diagnostic: Log all received payloads and raw bodies for debugging.
      LogReceivedPayloads("WebhookDeliveryTest", receiver);

      // Assert: At minimum, we should receive a 'processed' or 'delivered' event.
      payload.Should().NotBeNull("a webhook event (processed or delivered) should be received within 180 seconds");

      // Log which event we received.
      Console.Error.WriteLine($"[WebhookDeliveryTest] Received webhook event: {payload!.Event}");
      payload.Event.Should().BeOneOf(WebhookCallbackEvent.Processed, WebhookCallbackEvent.Delivered);
    }
    finally
    {
      await CleanupWebhookAsync(webhookId, ct);
    }
  }

  [Fact]
  [Trait("Category", "Integration.LongRunning")]
  public async Task SendEmail_ToNonExistentDomain_ReceivesHardBounceWebhook()
  {
    // Fail if live secrets are not configured (live key + sender + recipient).
    TestSecretValidator.AssertLiveSecretsPresent();

    // Fail if cloudflared is not installed.
    TestSecretValidator.AssertCloudflaredInstalled();

    var ct = TestContext.Current.CancellationToken;
    int? webhookId = null;

    await using var receiver = new WebhookReceiverFixture();
    await using var tunnel = new CloudflareTunnelManager();

    try
    {
      // Set up the full pipeline: receiver → tunnel → DNS → POST verify → webhook registration.
      // Subscribe to 'bounce' (the subscription-level event name) to receive both
      // hard and soft bounce payload events.
      // Also subscribe to 'processed' to confirm SMTP2GO accepted the email.
      webhookId = await SetupWebhookPipelineAsync(
        receiver, tunnel,
        [WebhookCreateEvent.Processed, WebhookCreateEvent.Bounce],
        ct);

      // Send an email to a nonexistent mailbox on a real domain to trigger a hard bounce.
      // We use @gmail.com because Gmail immediately rejects unknown recipients at SMTP level
      // with "550 5.1.1 The email account that you tried to reach does not exist", which
      // SMTP2GO classifies as a hard bounce. This is faster than using a non-existent domain
      // (like .invalid) where DNS resolution failure causes SMTP2GO to retry for hours/days
      // before eventually bouncing.
      var bounceRecipient = $"smtp2go-bounce-test-{Guid.NewGuid():N}@gmail.com";
      var emailRequest = new EmailSendRequest
      {
        Sender = _fixture.TestSender,
        To = [bounceRecipient],
        Subject = $"Hard Bounce Test - {Guid.NewGuid():N}",
        TextBody = "This email is sent to a non-existent domain to trigger a hard bounce webhook event."
      };

      var emailResponse = await _fixture.Client.SendEmailAsync(emailRequest, ct);
      emailResponse.Data.Should().NotBeNull();
      emailResponse.Data!.Succeeded.Should().BeGreaterThanOrEqualTo(1);

      Console.Error.WriteLine($"[HardBounceTest] Email sent to {bounceRecipient}. Waiting for hard bounce webhook callback...");

      // Wait for the bounce webhook payload to arrive.
      // SMTP2GO sends "event": "bounce" (not "hard_bounced") with a separate "bounce" field
      // containing "hard" or "soft". Gmail rejects unknown recipients immediately at SMTP level,
      // so the bounce webhook typically arrives within seconds of the email send.
      // 30-minute timeout ensures we capture the bounce even on slow runs.
      var payload = await receiver.WaitForPayloadAsync(
        p => p.Event == WebhookCallbackEvent.Bounce,
        timeout: TimeSpan.FromMinutes(30));

      // Diagnostic: Log all received payloads and raw bodies for debugging.
      LogReceivedPayloads("HardBounceTest", receiver);

      // Assert: We should receive a bounce event.
      payload.Should().NotBeNull("a bounce webhook event should be received within 30 minutes for a non-existent recipient");

      // Assert: Verify the event type and bounce-specific fields are correctly deserialized.
      Console.Error.WriteLine($"[HardBounceTest] Received webhook event: {payload!.Event}, BounceType: {payload.BounceType}, BounceContext: {payload.BounceContext}, Host: {payload.Host}");
      payload.Event.Should().Be(WebhookCallbackEvent.Bounce);
      payload.BounceType.Should().Be(BounceType.Hard, "a Gmail rejection (550 5.1.1) should classify as BounceType.Hard");
      payload.BounceContext.Should().NotBeNullOrWhiteSpace("a bounce event should include the SMTP transaction context");
      payload.Host.Should().NotBeNullOrWhiteSpace("a bounce event should include the target mail server host");

      // Assert: Common payload fields should still be populated on bounce events.
      payload.EmailId.Should().NotBeNullOrWhiteSpace("the SMTP2GO email ID should be present on bounce events");
    }
    finally
    {
      await CleanupWebhookAsync(webhookId, ct);
    }
  }

  #endregion


  #region Methods - Private

  /// <summary>
  ///   Sets up the full webhook delivery pipeline: starts the local receiver, creates a
  ///   Cloudflare Quick Tunnel, verifies POST reachability, and registers a webhook with SMTP2GO.
  /// </summary>
  /// <remarks>
  ///   <para>
  ///     This method consolidates the common setup sequence shared by all webhook delivery tests:
  ///     <list type="number">
  ///       <item>Start the local webhook receiver on a random port</item>
  ///       <item>Create a Cloudflare Quick Tunnel to the receiver</item>
  ///       <item>Wait for DNS propagation so the tunnel is reachable</item>
  ///       <item>Verify the tunnel accepts POST requests (self-test through the tunnel)</item>
  ///       <item>Clear self-test payloads to prevent interference with <c>WaitForPayloadAsync</c></item>
  ///       <item>Build the webhook URL with Basic Auth credentials embedded (RFC 3986 userinfo)</item>
  ///       <item>Register the webhook with SMTP2GO for the specified events</item>
  ///     </list>
  ///   </para>
  /// </remarks>
  /// <param name="receiver">The webhook receiver fixture (must be freshly created, not yet started).</param>
  /// <param name="tunnel">The tunnel manager (must be freshly created, not yet started).</param>
  /// <param name="events">The subscription-level events to register the webhook for.</param>
  /// <param name="ct">Cancellation token.</param>
  /// <returns>The SMTP2GO webhook ID for cleanup via <see cref="CleanupWebhookAsync"/>.</returns>
  private async Task<int> SetupWebhookPipelineAsync(
    WebhookReceiverFixture receiver,
    CloudflareTunnelManager tunnel,
    WebhookCreateEvent[] events,
    CancellationToken ct)
  {
    // Step 1: Start the local webhook receiver.
    await receiver.StartAsync(WebhookUsername, WebhookPassword);

    // Step 2: Create a Cloudflare Quick Tunnel to the receiver.
    var publicUrl = await tunnel.StartTunnelAsync(receiver.Port);

    // Step 2b: Wait for the tunnel to become reachable via DNS propagation.
    // Quick Tunnels need time for DNS records to propagate globally.
    var healthUrl = $"{publicUrl}{WebhookReceiverFixture.HealthPath}";
    var isReachable = await tunnel.WaitForTunnelReachableAsync(healthUrl);

    if (!isReachable)
      Assert.Fail($"Cloudflare tunnel {publicUrl} did not become reachable within 60 seconds (DNS propagation timeout).");

    // Step 2c: Verify the tunnel accepts POST requests by sending a self-test POST
    // through the tunnel. This confirms the full chain works for POST (not just GET).
    // Cloudflare Quick Tunnels may have WAF/Bot protection that blocks POSTs from
    // external services, so this step isolates tunnel-vs-SMTP2GO issues.
    var webhookPathUrl = $"{publicUrl}{WebhookReceiverFixture.WebhookPath}";
    await VerifyTunnelAcceptsPostAsync(webhookPathUrl);

    // Clear the self-test payload so it doesn't interfere with WaitForPayloadAsync.
    receiver.ClearReceivedPayloads();

    // Build the webhook URL with Basic Auth credentials embedded in the URI.
    // SMTP2GO requires credentials in the URL itself (RFC 3986 userinfo component),
    // NOT as separate API fields. The webhook_username/webhook_password API fields
    // are silently ignored — SMTP2GO extracts credentials from the URL and sends them
    // as an Authorization: Basic header when delivering webhook callbacks.
    var tunnelUri = new Uri(publicUrl);
    var webhookUri = new UriBuilder(tunnelUri)
    {
      UserName = Uri.EscapeDataString(WebhookUsername),
      Password = Uri.EscapeDataString(WebhookPassword),
      Path = WebhookReceiverFixture.WebhookPath
    };
    var webhookUrl = webhookUri.Uri.AbsoluteUri;

    // Step 3: Register the webhook with SMTP2GO.
    var createRequest = new WebhookCreateRequest
    {
      WebhookUrl = webhookUrl,
      Events = events
    };

    var createResponse = await _fixture.Client.Webhooks.CreateAsync(createRequest, ct);
    createResponse.Data.Should().NotBeNull();

    var webhookId = createResponse.Data!.WebhookId!.Value;

    Console.Error.WriteLine($"[WebhookDeliveryTest] Webhook created: ID={webhookId}, URL={webhookUrl}");

    return webhookId;
  }


  /// <summary>
  ///   Best-effort webhook cleanup. Silently ignores errors to prevent masking test failures.
  /// </summary>
  /// <param name="webhookId">The webhook ID to delete, or <c>null</c> if no webhook was created.</param>
  /// <param name="ct">Cancellation token.</param>
  private async Task CleanupWebhookAsync(int? webhookId, CancellationToken ct)
  {
    if (webhookId == null)
      return;

    try
    {
      await _fixture.Client.Webhooks.DeleteAsync(webhookId.Value, ct);
    }
    catch
    {
      // Best-effort cleanup.
    }
  }


  /// <summary>
  ///   Logs all received payloads and raw bodies for debugging failed webhook delivery tests.
  /// </summary>
  /// <param name="testName">A short label for the log prefix (e.g., <c>"HardBounceTest"</c>).</param>
  /// <param name="receiver">The webhook receiver containing the captured payloads.</param>
  private static void LogReceivedPayloads(string testName, WebhookReceiverFixture receiver)
  {
    Console.Error.WriteLine($"[{testName}] Received {receiver.ReceivedPayloads.Count} payload(s), {receiver.RawBodies.Count} raw body(ies).");

    foreach (var raw in receiver.RawBodies)
      Console.Error.WriteLine($"[{testName}] Raw body: {raw[..Math.Min(raw.Length, 500)]}");
  }


  /// <summary>
  ///   Sends a test POST through the Cloudflare tunnel to verify that POST requests
  ///   are proxied correctly. Uses the DoH-bypassing HTTP client to avoid DNS cache issues.
  /// </summary>
  /// <remarks>
  ///   This self-test isolates tunnel configuration issues from SMTP2GO delivery issues.
  ///   If this step fails, the tunnel does not support POSTs (e.g., Cloudflare WAF blocking).
  ///   If this step succeeds but SMTP2GO never calls back, the issue is on SMTP2GO's side.
  /// </remarks>
  private static async Task VerifyTunnelAcceptsPostAsync(string webhookUrl)
  {
    using var client = CloudflareTunnelManager.CreateDnsBypassingHttpClient();

    // Build a Basic Auth header matching the test credentials.
    var authValue = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{WebhookUsername}:{WebhookPassword}"));
    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authValue);

    // Send a minimal JSON POST body — the receiver will attempt to deserialize it.
    var content = new StringContent(
      """{"event": "test", "hostname": "self-test"}""",
      Encoding.UTF8,
      "application/json");

    var response = await client.PostAsync(webhookUrl, content);

    Console.Error.WriteLine($"[WebhookDeliveryTest] Self-POST verification: HTTP {(int)response.StatusCode}");

    if (!response.IsSuccessStatusCode)
    {
      Assert.Fail(
        $"Cloudflare tunnel does not accept POST requests. " +
        $"Self-POST to {webhookUrl} returned HTTP {(int)response.StatusCode}. " +
        $"This may indicate Cloudflare WAF/Bot protection is blocking POSTs.");
    }
  }

  #endregion
}
