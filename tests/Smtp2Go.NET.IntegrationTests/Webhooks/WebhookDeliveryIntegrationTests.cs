namespace Smtp2Go.NET.IntegrationTests.Webhooks;

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
      var processedPayload = await receiver.WaitForPayloadAsync(
        p => p.Event == WebhookCallbackEvent.Processed,
        timeout: TimeSpan.FromSeconds(180));

      var deliveredPayload = await receiver.WaitForPayloadAsync(
        p => p.Event == WebhookCallbackEvent.Delivered,
        timeout: TimeSpan.FromSeconds(180));

      // Diagnostic: Log all received payloads and raw bodies for debugging.
      LogReceivedPayloads("WebhookDeliveryTest", receiver);

      // Assert: At minimum, we should receive a 'processed' or 'delivered' event.
      processedPayload.Should().NotBeNull("a processed webhook event should be received within 180 seconds");
      deliveredPayload.Should().NotBeNull("a delivered webhook event should be received within 180 seconds after SMTP2GO accepts the email");

      // Log which event we received.
      Console.Error.WriteLine($"[WebhookDeliveryTest] Received processed webhook event: {processedPayload!.Event}");
      Console.Error.WriteLine($"[WebhookDeliveryTest] Received delivered webhook event: {deliveredPayload!.Event}");
      deliveredPayload.Event.Should().Be(WebhookCallbackEvent.Delivered);
    }
    finally
    {
      await WebhookPipelineHelper.CleanupWebhookAsync(_fixture.Client, webhookId, ct);
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
      await WebhookPipelineHelper.CleanupWebhookAsync(_fixture.Client, webhookId, ct);
    }
  }

  #endregion


  #region Methods - Private

  /// <summary>
  ///   Sets up the full webhook delivery pipeline: establishes a reachable tunnel-backed receiver
  ///   URL (via <see cref="WebhookPipelineHelper"/>) and registers a webhook with SMTP2GO for the
  ///   specified events.
  /// </summary>
  /// <param name="receiver">The webhook receiver fixture (must be freshly created, not yet started).</param>
  /// <param name="tunnel">The tunnel manager (must be freshly created, not yet started).</param>
  /// <param name="events">The subscription-level events to register the webhook for.</param>
  /// <param name="ct">Cancellation token.</param>
  /// <returns>The SMTP2GO webhook ID for cleanup via <see cref="WebhookPipelineHelper.CleanupWebhookAsync"/>.</returns>
  private async Task<int> SetupWebhookPipelineAsync(
    WebhookReceiverFixture receiver,
    CloudflareTunnelManager tunnel,
    WebhookCreateEvent[] events,
    CancellationToken ct)
  {
    // Stand up the local receiver behind a Cloudflare Quick Tunnel and get the reachable URL.
    var webhookUrl = await WebhookPipelineHelper.EstablishReachableWebhookUrlAsync(receiver, tunnel);

    // Delete any stale webhooks from previous runs.
    // SMTP2GO free tier allows only 1 webhook — a stale webhook from a failed run blocks creation.
    await WebhookPipelineHelper.DeleteAllExistingWebhooksAsync(_fixture.Client, ct);

    // Register the webhook with SMTP2GO for the requested events.
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
  ///   Logs all received payloads and raw bodies for debugging failed webhook delivery tests.
  /// </summary>
  /// <param name="testName">A short label for the log prefix (e.g., <c>"HardBounceTest"</c>).</param>
  /// <param name="receiver">The webhook receiver containing the captured payloads.</param>
  private static void LogReceivedPayloads(string testName, WebhookReceiverFixture receiver)
  {
    Console.Error.WriteLine($"[{testName}] Received {receiver.ReceivedPayloads.Count} payload(s), {receiver.RawBodies.Count} raw body(ies).");

    foreach (var raw in receiver.RawBodies)
      Console.Error.WriteLine($"[{testName}] Raw body: {raw}");
  }

  #endregion
}
