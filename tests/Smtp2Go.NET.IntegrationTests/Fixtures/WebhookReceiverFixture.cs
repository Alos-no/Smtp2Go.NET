namespace Smtp2Go.NET.IntegrationTests.Fixtures;

using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Smtp2Go.NET.Models.Webhooks;

/// <summary>
///   A minimal Kestrel web server that captures incoming SMTP2GO webhook payloads
///   for verification in integration tests.
/// </summary>
/// <remarks>
///   <para>
///     This fixture starts a Kestrel web server on a random available port using
///     <see cref="WebApplication.CreateSlimBuilder()"/>, validates Basic Auth credentials,
///     deserializes incoming webhook payloads, and stores them for assertion by test methods.
///   </para>
///   <para>
///     Incoming payloads are matched to registered waiters via
///     <see cref="TaskCompletionSource{TResult}"/>, providing event-driven notification
///     instead of polling.
///   </para>
///   <para>
///     Designed to be used in conjunction with <see cref="CloudflareTunnelManager"/> to
///     expose the local receiver to the internet for SMTP2GO webhook callbacks.
///   </para>
/// </remarks>
internal sealed class WebhookReceiverFixture : IAsyncDisposable
{
  #region Constants & Statics

  /// <summary>The path that the webhook receiver listens on.</summary>
  public const string WebhookPath = "/webhook";

  /// <summary>The health check path for tunnel reachability verification.</summary>
  public const string HealthPath = "/health";

  /// <summary>Maximum time to wait for a matching payload in <see cref="WaitForPayloadAsync"/>.</summary>
  private static readonly TimeSpan DefaultWaitTimeout = TimeSpan.FromSeconds(60);

  #endregion


  #region Properties & Fields - Non-Public

  /// <summary>The Kestrel web application serving webhook callbacks.</summary>
  private WebApplication? _app;

  /// <summary>Thread-safe collection of received webhook payloads.</summary>
  private readonly ConcurrentBag<WebhookCallbackPayload> _receivedPayloads = new();

  /// <summary>Thread-safe collection of raw JSON bodies received (for debugging).</summary>
  private readonly ConcurrentBag<string> _rawBodies = new();

  /// <summary>Registered waiters notified via <see cref="TaskCompletionSource{TResult}"/> when a matching payload arrives.</summary>
  private readonly ConcurrentBag<PayloadWaiter> _waiters = new();

  #endregion


  #region Properties & Fields - Public

  /// <summary>Gets the local port the webhook receiver is listening on.</summary>
  public int Port { get; private set; }

  /// <summary>Gets all received webhook payloads.</summary>
  public IReadOnlyCollection<WebhookCallbackPayload> ReceivedPayloads => _receivedPayloads.ToArray();

  /// <summary>Gets all raw JSON bodies received (useful for debugging deserialization issues).</summary>
  public IReadOnlyCollection<string> RawBodies => _rawBodies.ToArray();

  #endregion


  #region Methods

  /// <summary>
  ///   Clears all received payloads and raw bodies.
  ///   Used after self-test POST verification to prevent test payloads from
  ///   interfering with WaitForPayloadAsync matches.
  /// </summary>
  public void ClearReceivedPayloads()
  {
    _receivedPayloads.Clear();
    _rawBodies.Clear();
  }


  /// <summary>
  ///   Starts the webhook receiver on a random available port with Basic Auth validation.
  /// </summary>
  /// <param name="username">The expected Basic Auth username.</param>
  /// <param name="password">The expected Basic Auth password.</param>
  public async Task StartAsync(string username, string password)
  {
    // Encode the expected Basic Auth credentials for comparison.
    var expectedAuth = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{username}:{password}"));

    // Build a minimal Kestrel server on a random available port.
    var builder = WebApplication.CreateSlimBuilder();
    builder.WebHost.UseUrls("http://127.0.0.1:0");

    // Suppress Kestrel startup logging noise in test output.
    builder.Logging.ClearProviders();

    _app = builder.Build();

    // Map the health check endpoint for tunnel reachability verification.
    _app.MapGet(HealthPath, () => Results.Ok("healthy"));

    // Map the webhook endpoint using minimal API routing.
    _app.MapPost(WebhookPath, async (HttpContext ctx) =>
    {
      // Log incoming webhook request for diagnostics — BEFORE auth check so we see all requests.
      Console.Error.WriteLine($"[WebhookReceiver] Received POST {ctx.Request.Path} from {ctx.Connection.RemoteIpAddress}");

      // Validate Basic Auth header.
      var authHeader = ctx.Request.Headers.Authorization.ToString();

      if (string.IsNullOrEmpty(authHeader)
          || !authHeader.StartsWith("Basic ", StringComparison.OrdinalIgnoreCase))
      {
        Console.Error.WriteLine($"[WebhookReceiver] Auth REJECTED: header is empty or not Basic (got: '{authHeader}')");

        return Results.Unauthorized();
      }

      var providedAuth = authHeader["Basic ".Length..];

      if (providedAuth != expectedAuth)
      {
        Console.Error.WriteLine($"[WebhookReceiver] Auth REJECTED: credentials mismatch");

        return Results.StatusCode(StatusCodes.Status403Forbidden);
      }

      Console.Error.WriteLine($"[WebhookReceiver] Auth OK");

      // Read and store the raw body.
      using var reader = new StreamReader(ctx.Request.Body, Encoding.UTF8);
      var body = await reader.ReadToEndAsync();
      _rawBodies.Add(body);

      Console.Error.WriteLine($"[WebhookReceiver] Body length: {body.Length} chars");

      // Attempt to deserialize the webhook payload.
      try
      {
        var payload = JsonSerializer.Deserialize<WebhookCallbackPayload>(body, new JsonSerializerOptions
        {
          PropertyNameCaseInsensitive = true
        });

        if (payload != null)
        {
          _receivedPayloads.Add(payload);
          NotifyWaiters(payload);
        }
      }
      catch (JsonException ex)
      {
        Console.Error.WriteLine($"[WebhookReceiver] Failed to deserialize webhook payload: {ex.Message}");
        Console.Error.WriteLine($"[WebhookReceiver] Raw body: {body}");
      }

      // Respond with 200 OK to acknowledge receipt.
      return Results.Ok();
    });

    // Start the server and discover the assigned port.
    await _app.StartAsync();

    var server = _app.Services.GetRequiredService<IServer>();
    var addressFeature = server.Features.Get<IServerAddressesFeature>()!;
    var address = addressFeature.Addresses.First();
    Port = new Uri(address).Port;
  }


  /// <summary>
  ///   Waits for a webhook payload matching the specified predicate using event-driven
  ///   notification via <see cref="TaskCompletionSource{TResult}"/>.
  /// </summary>
  /// <remarks>
  ///   <para>
  ///     This method first checks all previously received payloads. If no match is found,
  ///     a waiter is registered and notified when a matching payload arrives. A second
  ///     check is performed after registration to guard against the race condition where
  ///     a payload arrives between the initial check and the waiter registration.
  ///   </para>
  /// </remarks>
  /// <param name="predicate">A predicate to match against received payloads.</param>
  /// <param name="timeout">
  ///   The maximum time to wait. Defaults to <see cref="DefaultWaitTimeout"/> (60 seconds).
  /// </param>
  /// <returns>The first matching payload, or <c>null</c> if the timeout was reached.</returns>
  public async Task<WebhookCallbackPayload?> WaitForPayloadAsync(
    Func<WebhookCallbackPayload, bool> predicate,
    TimeSpan? timeout = null)
  {
    // Check existing payloads first (payload may have already arrived).
    var existing = _receivedPayloads.FirstOrDefault(predicate);

    if (existing != null)
      return existing;

    // Register a waiter for new payloads.
    var tcs = new TaskCompletionSource<WebhookCallbackPayload?>(TaskCreationOptions.RunContinuationsAsynchronously);
    var waiter = new PayloadWaiter(predicate, tcs);
    _waiters.Add(waiter);

    // Check again after registration — guards against the race condition where a payload
    // arrived between the initial check and the waiter registration.
    existing = _receivedPayloads.FirstOrDefault(predicate);

    if (existing != null)
    {
      tcs.TrySetResult(existing);

      return existing;
    }

    // Wait for a matching payload with timeout.
    var effectiveTimeout = timeout ?? DefaultWaitTimeout;
    using var cts = new CancellationTokenSource(effectiveTimeout);

    // When the timeout fires, resolve the TCS with null so the caller isn't stuck forever.
    cts.Token.Register(() => tcs.TrySetResult(null));

    return await tcs.Task;
  }

  #endregion


  #region Methods - Non-Public

  /// <summary>
  ///   Notifies all registered waiters whose predicate matches the received payload.
  /// </summary>
  /// <param name="payload">The received webhook payload.</param>
  private void NotifyWaiters(WebhookCallbackPayload payload)
  {
    foreach (var waiter in _waiters.ToArray())
    {
      if (waiter.Predicate(payload))
        waiter.Tcs.TrySetResult(payload);
    }
  }

  #endregion


  #region IAsyncDisposable

  /// <inheritdoc />
  public async ValueTask DisposeAsync()
  {
    if (_app != null)
    {
      try
      {
        await _app.StopAsync();
        await _app.DisposeAsync();
      }
      catch
      {
        // Best-effort cleanup.
      }

      _app = null;
    }

    // Cancel any waiters still pending so tests don't hang on disposal.
    foreach (var waiter in _waiters.ToArray())
      waiter.Tcs.TrySetResult(null);
  }

  #endregion


  #region Inner Types

  /// <summary>
  ///   Represents a registered waiter for a webhook payload matching a predicate.
  /// </summary>
  /// <param name="Predicate">The predicate to match against incoming payloads.</param>
  /// <param name="Tcs">The <see cref="TaskCompletionSource{TResult}"/> to signal when a match is found.</param>
  private sealed record PayloadWaiter(
    Func<WebhookCallbackPayload, bool> Predicate,
    TaskCompletionSource<WebhookCallbackPayload?> Tcs);

  #endregion
}
