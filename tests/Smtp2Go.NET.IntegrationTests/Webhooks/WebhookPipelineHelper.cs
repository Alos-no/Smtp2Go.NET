namespace Smtp2Go.NET.IntegrationTests.Webhooks;

using System.Net.Http.Headers;
using System.Text;
using Fixtures;
using Smtp2Go.NET.Models.Webhooks;

/// <summary>
///   Shared setup for live webhook integration tests: brings up a local receiver behind a
///   Cloudflare Quick Tunnel and yields a publicly reachable webhook URL, plus account-level
///   webhook cleanup helpers.
/// </summary>
/// <remarks>
///   <para>
///     SMTP2GO validates that a webhook URL points to a reachable destination when the webhook is
///     created (<c>webhook/add</c> returns HTTP 400 "The passed URL must point to a valid
///     destination" otherwise), so both the delivery tests and the management/CRUD tests need a
///     real, reachable endpoint — a fabricated URL is rejected. This helper is the single source of
///     truth for standing that endpoint up, so the two webhook test classes do not duplicate the
///     receiver/tunnel pipeline.
///   </para>
///   <para>
///     <strong>Prerequisites:</strong> <c>cloudflared</c> must be installed and the live API key
///     configured. The webhook Basic Auth credentials below are arbitrary test constants — we define
///     them when creating the webhook, so they are NOT external secrets.
///   </para>
/// </remarks>
internal static class WebhookPipelineHelper
{
  #region Constants & Statics

  /// <summary>
  ///   Arbitrary Basic Auth username for the webhook receiver.
  ///   We define this when creating the webhook — it is NOT an external secret.
  /// </summary>
  public const string WebhookUsername = "test-webhook-user";

  /// <summary>
  ///   Arbitrary Basic Auth password for the webhook receiver.
  ///   We define this when creating the webhook — it is NOT an external secret.
  /// </summary>
  public const string WebhookPassword = "test-webhook-pass";

  #endregion


  #region Methods

  /// <summary>
  ///   Starts the local receiver, opens a Cloudflare Quick Tunnel to it, waits for the tunnel to
  ///   become reachable, verifies it accepts POSTs, and returns the reachable webhook URL with the
  ///   Basic Auth credentials embedded (RFC 3986 userinfo) — the form SMTP2GO accepts at
  ///   <c>webhook/add</c>.
  /// </summary>
  /// <remarks>
  ///   <para>
  ///     This consolidates the common setup sequence:
  ///     <list type="number">
  ///       <item>Start the local webhook receiver on a random port</item>
  ///       <item>Create a Cloudflare Quick Tunnel to the receiver</item>
  ///       <item>Wait for DNS propagation so the tunnel is reachable</item>
  ///       <item>Verify the tunnel accepts POST requests (self-test through the tunnel)</item>
  ///       <item>Clear self-test payloads so they don't interfere with <c>WaitForPayloadAsync</c></item>
  ///       <item>Build the webhook URL with Basic Auth credentials embedded (RFC 3986 userinfo)</item>
  ///     </list>
  ///   </para>
  /// </remarks>
  /// <param name="receiver">The webhook receiver fixture (must be freshly created, not yet started).</param>
  /// <param name="tunnel">The tunnel manager (must be freshly created, not yet started).</param>
  /// <returns>The reachable webhook URL with Basic Auth credentials embedded.</returns>
  public static async Task<string> EstablishReachableWebhookUrlAsync(
    WebhookReceiverFixture receiver,
    CloudflareTunnelManager tunnel)
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
    var webhookUri = new UriBuilder(new Uri(publicUrl))
    {
      UserName = Uri.EscapeDataString(WebhookUsername),
      Password = Uri.EscapeDataString(WebhookPassword),
      Path = WebhookReceiverFixture.WebhookPath
    };

    return webhookUri.Uri.AbsoluteUri;
  }


  /// <summary>
  ///   Deletes all existing webhooks on the SMTP2GO account.
  ///   SMTP2GO free tier limits accounts to 1 webhook — stale webhooks from
  ///   previous failed runs block creation of new ones.
  /// </summary>
  /// <param name="client">The live-configured SMTP2GO client.</param>
  /// <param name="ct">Cancellation token.</param>
  public static async Task DeleteAllExistingWebhooksAsync(ISmtp2GoClient client, CancellationToken ct)
  {
    var listResponse = await client.Webhooks.ListAsync(ct);

    if (listResponse.Data is not { Length: > 0 })
      return;

    foreach (var webhook in listResponse.Data)
    {
      if (webhook.WebhookId is { } id)
      {
        try
        {
          await client.Webhooks.DeleteAsync(id, ct);
        }
        catch
        {
          // Best-effort cleanup — continue with remaining webhooks.
        }
      }
    }
  }


  /// <summary>
  ///   Best-effort webhook cleanup. Silently ignores errors to prevent masking test failures.
  /// </summary>
  /// <param name="client">The live-configured SMTP2GO client.</param>
  /// <param name="webhookId">The webhook ID to delete, or <c>null</c> if no webhook was created.</param>
  /// <param name="ct">Cancellation token.</param>
  public static async Task CleanupWebhookAsync(ISmtp2GoClient client, int? webhookId, CancellationToken ct)
  {
    if (webhookId == null)
      return;

    try
    {
      await client.Webhooks.DeleteAsync(webhookId.Value, ct);
    }
    catch
    {
      // Best-effort cleanup.
    }
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

    Console.Error.WriteLine($"[WebhookPipeline] Self-POST verification: HTTP {(int)response.StatusCode}");

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
