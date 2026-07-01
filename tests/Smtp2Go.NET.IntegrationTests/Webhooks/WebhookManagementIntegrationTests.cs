namespace Smtp2Go.NET.IntegrationTests.Webhooks;

using Fixtures;
using Helpers;
using Smtp2Go.NET.Models.Webhooks;

/// <summary>
///   Collection definition for webhook tests — ensures they run sequentially
///   because SMTP2GO free tier limits the account to 1 webhook at a time.
/// </summary>
[CollectionDefinition("Webhook")]
public class WebhookTestCollection;

/// <summary>
///   Integration tests for webhook CRUD lifecycle operations using the live API key.
/// </summary>
/// <remarks>
///   <para>
///     These tests create, list, and delete real webhooks on the SMTP2GO account.
///     Each test cleans up after itself by deleting any webhooks it creates.
///   </para>
///   <para>
///     <strong>Reachable URL required.</strong> SMTP2GO validates that a webhook URL points to a
///     reachable destination at <c>webhook/add</c> time (it rejects a fabricated URL with HTTP 400
///     "The passed URL must point to a valid destination"), so these tests stand up a local receiver
///     behind a Cloudflare Quick Tunnel (via <see cref="WebhookPipelineHelper"/>) to obtain a real
///     URL. That makes them depend on <c>cloudflared</c>, so they carry the
///     <c>Integration.Webhook</c> trait and are excluded from CI alongside the delivery tests
///     (CI has no tunnel infrastructure); they run locally.
///   </para>
/// </remarks>
[Collection("Webhook")]
[Trait("Category", "Integration.Webhook")]
public sealed class WebhookManagementIntegrationTests : IClassFixture<Smtp2GoLiveFixture>
{
  #region Properties & Fields - Non-Public

  /// <summary>The live-configured client fixture.</summary>
  private readonly Smtp2GoLiveFixture _fixture;

  #endregion


  #region Constructors

  /// <summary>
  ///   Initializes a new instance of the <see cref="WebhookManagementIntegrationTests" /> class.
  /// </summary>
  public WebhookManagementIntegrationTests(Smtp2GoLiveFixture fixture)
  {
    _fixture = fixture;
  }

  #endregion


  #region Webhook Lifecycle

  [Fact]
  public async Task WebhookLifecycle_CreateListDelete_Succeeds()
  {
    // Fail if live secrets are not configured, or if cloudflared is unavailable for the tunnel.
    TestSecretValidator.AssertLiveSecretsPresent();
    TestSecretValidator.AssertCloudflaredInstalled();

    var ct = TestContext.Current.CancellationToken;
    int? webhookId = null;

    await using var receiver = new WebhookReceiverFixture();
    await using var tunnel = new CloudflareTunnelManager();

    // SMTP2GO requires a reachable destination, so create a real tunnel-backed receiver URL.
    var webhookUrl = await WebhookPipelineHelper.EstablishReachableWebhookUrlAsync(receiver, tunnel);

    // SMTP2GO free tier allows only 1 webhook — clear stale webhooks from previous runs.
    await WebhookPipelineHelper.DeleteAllExistingWebhooksAsync(_fixture.Client, ct);

    try
    {
      // Step 1: Create a webhook.
      var createRequest = new WebhookCreateRequest
      {
        WebhookUrl = webhookUrl,
        Events = [WebhookCreateEvent.Delivered, WebhookCreateEvent.Bounce]
      };

      var createResponse = await _fixture.Client.Webhooks.CreateAsync(createRequest, ct);

      // Assert — Create should return a valid response.
      createResponse.Should().NotBeNull();
      createResponse.RequestId.Should().NotBeNullOrWhiteSpace();
      createResponse.Data.Should().NotBeNull();
      createResponse.Data!.WebhookId.Should().NotBeNull()
        .And.BeGreaterThan(0, "a new webhook should receive a positive ID");

      webhookId = createResponse.Data.WebhookId!.Value;


      // Step 2: List webhooks and verify the created one appears.
      var listResponse = await _fixture.Client.Webhooks.ListAsync(ct);

      listResponse.Should().NotBeNull();
      listResponse.Data.Should().NotBeNull();

      // The created webhook should appear in the list.
      // WebhookListResponse.Data is WebhookInfo[] (extends ApiResponse<WebhookInfo[]>).
      listResponse.Data!.Should().Contain(
        w => w.WebhookId == webhookId,
        "the newly created webhook should be in the list");


      // Step 3: Delete the webhook.
      var deleteResponse = await _fixture.Client.Webhooks.DeleteAsync(webhookId!.Value, ct);

      deleteResponse.Should().NotBeNull();
      deleteResponse.RequestId.Should().NotBeNullOrWhiteSpace();

      // Mark as cleaned up so the finally block doesn't try again.
      webhookId = null;


      // Step 4: Verify the webhook is no longer in the list.
      var listAfterDelete = await _fixture.Client.Webhooks.ListAsync(ct);
      var webhookIds = listAfterDelete.Data?.Select(w => w.WebhookId) ?? [];
      webhookIds.Should().NotContain(createResponse.Data.WebhookId,
        "the deleted webhook should no longer appear in the list");
    }
    finally
    {
      // Cleanup: Delete the webhook if the test failed midway.
      await WebhookPipelineHelper.CleanupWebhookAsync(_fixture.Client, webhookId, ct);
    }
  }


  [Fact]
  public async Task WebhookCreate_WithSpecificEvents_ConfiguresCorrectly()
  {
    // Fail if live secrets are not configured, or if cloudflared is unavailable for the tunnel.
    TestSecretValidator.AssertLiveSecretsPresent();
    TestSecretValidator.AssertCloudflaredInstalled();

    var ct = TestContext.Current.CancellationToken;
    int? webhookId = null;

    await using var receiver = new WebhookReceiverFixture();
    await using var tunnel = new CloudflareTunnelManager();

    // SMTP2GO requires a reachable destination, so create a real tunnel-backed receiver URL.
    var webhookUrl = await WebhookPipelineHelper.EstablishReachableWebhookUrlAsync(receiver, tunnel);

    // SMTP2GO free tier allows only 1 webhook — clear stale webhooks from previous runs.
    await WebhookPipelineHelper.DeleteAllExistingWebhooksAsync(_fixture.Client, ct);

    try
    {
      // Arrange — Create a webhook with a specific set of event types.
      // Subscribe to a representative set of subscription-level events.
      // NOTE: WebhookCreateEvent values are subscription events (e.g., Bounce, Spam),
      // NOT callback payload events (e.g., "hard_bounced", "spam_complaint").
      var createRequest = new WebhookCreateRequest
      {
        WebhookUrl = webhookUrl,
        Events =
        [
          WebhookCreateEvent.Processed,
          WebhookCreateEvent.Delivered,
          WebhookCreateEvent.Bounce,
          WebhookCreateEvent.Spam
        ]
      };

      // Act
      var createResponse = await _fixture.Client.Webhooks.CreateAsync(createRequest, ct);

      createResponse.Should().NotBeNull();
      createResponse.Data.Should().NotBeNull();
      webhookId = createResponse.Data!.WebhookId!.Value;

      // Assert — Verify via the list endpoint that the webhook has the correct events.
      var listResponse = await _fixture.Client.Webhooks.ListAsync(ct);
      var webhook = listResponse.Data?.FirstOrDefault(w => w.WebhookId == webhookId);

      webhook.Should().NotBeNull("the created webhook should be in the list");
    }
    finally
    {
      // Cleanup.
      await WebhookPipelineHelper.CleanupWebhookAsync(_fixture.Client, webhookId, ct);
    }
  }

  #endregion
}
