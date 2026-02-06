namespace Smtp2Go.NET.IntegrationTests.Fixtures;

using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Net.Sockets;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

/// <summary>
///   Manages a Cloudflare Quick Tunnel for exposing a local webhook receiver to the internet.
/// </summary>
/// <remarks>
///   <para>
///     This manager starts a <c>cloudflared tunnel --url</c> process pointing at a local port.
///     Cloudflare Quick Tunnels require no authentication — they generate a random
///     <c>https://{random}.trycloudflare.com</c> URL that proxies traffic to the local port.
///   </para>
///   <para>
///     The public URL is discovered by parsing cloudflared's stderr output, where it logs
///     <c>"https://xxx.trycloudflare.com"</c> once the tunnel is established.
///   </para>
///   <para>
///     <strong>DNS Caching Issue:</strong> Quick Tunnel subdomains on <c>trycloudflare.com</c>
///     do NOT use wildcard DNS — each tunnel gets its own DNS record created on-the-fly.
///     If the local resolver queries the hostname before the record exists, it caches
///     the NXDOMAIN response for up to 1800 seconds (the SOA minimum TTL). To work around
///     this, <see cref="WaitForTunnelReachableAsync"/> uses Cloudflare's DNS-over-HTTPS (DoH)
///     API at <c>cloudflare-dns.com/dns-query</c> to resolve DNS directly, bypassing the
///     Windows DNS cache entirely.
///   </para>
///   <para>
///     <strong>Prerequisites:</strong> <c>cloudflared</c> must be installed. If not on PATH,
///     the manager checks common install locations. Use <see cref="FindCloudflaredPath"/>
///     to locate the executable.
///   </para>
///   <para>
///     <strong>Advantages:</strong>
///     <list type="bullet">
///       <item>No authentication token required (Quick Tunnels are free and zero-config)</item>
///       <item>No interstitial page or request blocking for POST requests</item>
///       <item>No port conflict issues (no local API server)</item>
///     </list>
///   </para>
/// </remarks>
internal sealed partial class CloudflareTunnelManager : IAsyncDisposable
{
  #region Constants & Statics

  /// <summary>Maximum time to wait for cloudflared to start and expose a tunnel.</summary>
  private static readonly TimeSpan StartupTimeout = TimeSpan.FromSeconds(30);

  /// <summary>
  ///   Common install locations for cloudflared on Windows.
  ///   Checked when cloudflared is not on the system PATH.
  /// </summary>
  private static readonly string[] CommonWindowsPaths =
  [
    @"C:\Program Files (x86)\cloudflared\cloudflared.exe",
    @"C:\Program Files\cloudflared\cloudflared.exe",
    Path.Combine(
      Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
      "Programs", "cloudflared", "cloudflared.exe")
  ];

  /// <summary>
  ///   HTTP client for Cloudflare DNS-over-HTTPS (DoH) queries.
  ///   Used to resolve tunnel hostnames without touching the Windows DNS cache.
  /// </summary>
  private static readonly HttpClient DohClient = new() { Timeout = TimeSpan.FromSeconds(5) };

  #endregion


  #region Properties & Fields - Non-Public

  /// <summary>The cloudflared process.</summary>
  private Process? _cloudflaredProcess;

  #endregion


  #region Properties & Fields - Public

  /// <summary>Gets the public HTTPS URL of the active tunnel, or null if not started.</summary>
  public string? PublicUrl { get; private set; }

  #endregion


  #region Methods

  /// <summary>
  ///   Starts a Cloudflare Quick Tunnel to the specified local port.
  /// </summary>
  /// <remarks>
  ///   <para>
  ///     Quick Tunnels require no authentication — they create a temporary, randomly-named
  ///     tunnel that proxies HTTPS traffic to the specified local port.
  ///   </para>
  ///   <para>
  ///     <strong>DNS Propagation:</strong> Quick Tunnel URLs may not be immediately resolvable
  ///     after creation. Use <see cref="WaitForTunnelReachableAsync"/> after this method to
  ///     verify the tunnel is reachable before registering webhooks or expecting callbacks.
  ///   </para>
  /// </remarks>
  /// <param name="localPort">The local port to tunnel to.</param>
  /// <returns>The public HTTPS URL for the tunnel (e.g., <c>https://xxx.trycloudflare.com</c>).</returns>
  /// <exception cref="InvalidOperationException">
  ///   Thrown if cloudflared is not found, fails to start, or no tunnel URL is discovered.
  /// </exception>
  public async Task<string> StartTunnelAsync(int localPort)
  {
    if (_cloudflaredProcess != null)
      throw new InvalidOperationException("A tunnel is already running. Dispose first.");

    // Locate the cloudflared executable.
    var cloudflaredPath = FindCloudflaredPath()
      ?? throw new InvalidOperationException(
        "cloudflared is not installed. Install from https://developers.cloudflare.com/cloudflare-one/connections/connect-networks/downloads/");

    // Start cloudflared process with Quick Tunnel.
    // The --url flag tells cloudflared to create a Quick Tunnel (no account/auth required).
    var startInfo = new ProcessStartInfo
    {
      FileName = cloudflaredPath,
      Arguments = $"tunnel --url http://localhost:{localPort}",
      RedirectStandardOutput = true,
      RedirectStandardError = true,
      UseShellExecute = false,
      CreateNoWindow = true
    };

    _cloudflaredProcess = Process.Start(startInfo)
      ?? throw new InvalidOperationException("Failed to start cloudflared process.");

    // Consume stdout in the background — cloudflared may log connection info there.
    _ = Task.Run(async () =>
    {
      try
      {
        while (await _cloudflaredProcess.StandardOutput.ReadLineAsync() is { } line)
          Console.Error.WriteLine($"[cloudflared:stdout] {line}");
      }
      catch { /* Process exited or stream closed. */ }
    });

    // Parse stderr to discover the public tunnel URL.
    // cloudflared logs the tunnel URL to stderr once established.
    var publicUrl = await DiscoverTunnelUrlFromStderrAsync(_cloudflaredProcess);

    if (publicUrl != null)
    {
      PublicUrl = publicUrl;

      return publicUrl;
    }

    // Timeout — kill the process and throw.
    await DisposeAsync();

    throw new InvalidOperationException(
      $"cloudflared did not expose a tunnel within {StartupTimeout.TotalSeconds}s. " +
      "Ensure cloudflared is installed correctly.");
  }


  /// <summary>
  ///   Polls a tunnel URL until it responds with 200 OK, indicating the tunnel is reachable.
  /// </summary>
  /// <remarks>
  ///   <para>
  ///     Cloudflare Quick Tunnels may not be immediately reachable after the URL is reported
  ///     because the DNS record for the random subdomain needs time to propagate.
  ///   </para>
  ///   <para>
  ///     <strong>DNS Cache Bypass:</strong> The <c>trycloudflare.com</c> SOA minimum TTL is
  ///     1800 seconds, meaning NXDOMAIN responses are cached for up to 30 minutes by the
  ///     Windows DNS client. To avoid this, this method resolves DNS via Cloudflare's
  ///     DNS-over-HTTPS (DoH) API and connects directly to the resolved IP using a custom
  ///     <see cref="SocketsHttpHandler.ConnectCallback"/>.
  ///   </para>
  /// </remarks>
  /// <param name="healthUrl">The full URL to poll through the tunnel.</param>
  /// <returns><c>true</c> if the tunnel became reachable; <c>false</c> if the 60-second timeout expired.</returns>
  public async Task<bool> WaitForTunnelReachableAsync(string healthUrl)
  {
    // Allow up to 60 seconds for DNS propagation + tunnel readiness.
    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(60));

    // Create an HttpClient that bypasses the Windows DNS cache by resolving
    // hostnames via Cloudflare's DNS-over-HTTPS API.
    using var httpClient = CreateDnsBypassingHttpClient();
    var attempt = 0;

    while (!cts.Token.IsCancellationRequested)
    {
      try
      {
        var response = await httpClient.GetAsync(healthUrl, cts.Token);

        if (response.IsSuccessStatusCode)
        {
          Console.Error.WriteLine($"[CloudflareTunnelManager] Tunnel reachable after {attempt + 1} attempt(s).");

          return true;
        }

        Console.Error.WriteLine($"[CloudflareTunnelManager] Health check attempt {attempt + 1}: HTTP {(int)response.StatusCode}");
      }
      catch (TaskCanceledException) when (!cts.Token.IsCancellationRequested)
      {
        // Individual request timed out — retry.
        Console.Error.WriteLine($"[CloudflareTunnelManager] Health check attempt {attempt + 1}: request timed out");
      }
      catch (HttpRequestException ex) when (!cts.Token.IsCancellationRequested)
      {
        Console.Error.WriteLine($"[CloudflareTunnelManager] Health check attempt {attempt + 1}: {ex.Message}");
      }
      catch (Exception) when (!cts.Token.IsCancellationRequested)
      {
        // Tunnel not ready yet — retry.
      }

      attempt++;

      // Fixed 3-second interval between attempts.
      await Task.Delay(3000, cts.Token).ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
    }

    Console.Error.WriteLine($"[CloudflareTunnelManager] Tunnel not reachable after {attempt} attempts over 60 seconds.");

    return false;
  }


  /// <summary>
  ///   Finds the cloudflared executable path by checking PATH and common install locations.
  /// </summary>
  /// <returns>The full path to the cloudflared executable, or <c>null</c> if not found.</returns>
  public static string? FindCloudflaredPath()
  {
    // First, check if cloudflared is on the system PATH.
    try
    {
      using var process = Process.Start(new ProcessStartInfo
      {
        FileName = "cloudflared",
        Arguments = "version",
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        UseShellExecute = false,
        CreateNoWindow = true
      });

      process?.WaitForExit(5000);

      if (process is { ExitCode: 0 })
        return "cloudflared";
    }
    catch
    {
      // Not on PATH — check common install locations.
    }

    // Check common Windows install locations.
    foreach (var path in CommonWindowsPaths)
    {
      if (File.Exists(path))
        return path;
    }

    return null;
  }


  /// <summary>
  ///   Checks whether cloudflared is installed and available.
  /// </summary>
  /// <returns><c>true</c> if cloudflared is found; otherwise, <c>false</c>.</returns>
  public static bool IsCloudflaredInstalled()
  {
    return FindCloudflaredPath() != null;
  }

  #endregion


  #region Methods - Private

  /// <summary>
  ///   Reads cloudflared's stderr output to discover the public tunnel URL.
  /// </summary>
  /// <remarks>
  ///   <para>
  ///     cloudflared logs tunnel information to stderr. The public URL appears in a line like:
  ///     <c>... | https://random-words.trycloudflare.com</c>
  ///   </para>
  ///   <para>
  ///     After discovering the URL, stderr consumption continues in a background task
  ///     to capture connection registration and diagnostic messages.
  ///   </para>
  /// </remarks>
  private async Task<string?> DiscoverTunnelUrlFromStderrAsync(Process process)
  {
    using var cts = new CancellationTokenSource(StartupTimeout);

    try
    {
      // Read stderr line by line — cloudflared logs tunnel info there.
      while (!cts.Token.IsCancellationRequested)
      {
        var line = await process.StandardError.ReadLineAsync(cts.Token);

        // Process exited or stream ended.
        if (line == null)
          break;

        // Log to console for debugging (visible in test output).
        Console.Error.WriteLine($"[cloudflared] {line}");

        // Look for the trycloudflare.com URL.
        var match = TryCloudflareUrlRegex().Match(line);

        if (match.Success)
        {
          // Continue reading stderr in the background for diagnostics.
          _ = Task.Run(async () =>
          {
            try
            {
              while (await process.StandardError.ReadLineAsync() is { } stderrLine)
                Console.Error.WriteLine($"[cloudflared] {stderrLine}");
            }
            catch { /* Process exited or stream closed. */ }
          });

          return match.Value;
        }

        // Also check for fatal errors to fail fast.
        if (line.Contains("ERR", StringComparison.OrdinalIgnoreCase) &&
            line.Contains("failed", StringComparison.OrdinalIgnoreCase))
        {
          Console.Error.WriteLine($"[CloudflareTunnelManager] Possible error detected: {line}");
        }
      }
    }
    catch (OperationCanceledException)
    {
      // Timeout — fall through to return null.
    }

    return null;
  }


  /// <summary>
  ///   Creates an <see cref="HttpClient"/> that resolves DNS via Cloudflare's DNS-over-HTTPS
  ///   (DoH) API, completely bypassing the Windows DNS cache.
  /// </summary>
  /// <remarks>
  ///   <para>
  ///     The <c>trycloudflare.com</c> zone has an SOA minimum TTL of 1800 seconds, causing
  ///     Windows to cache NXDOMAIN responses for up to 30 minutes. This makes it impossible
  ///     to reach a newly-created Quick Tunnel using the system DNS resolver.
  ///   </para>
  ///   <para>
  ///     This client uses a <see cref="SocketsHttpHandler.ConnectCallback"/> that:
  ///     <list type="number">
  ///       <item>Resolves the hostname via <c>https://cloudflare-dns.com/dns-query</c> (JSON API)</item>
  ///       <item>Connects a TCP socket directly to the resolved IP address</item>
  ///     </list>
  ///     Since Cloudflare is the authoritative DNS provider for <c>trycloudflare.com</c>,
  ///     their resolver will have the record available as soon as it's created.
  ///   </para>
  /// </remarks>
  internal static HttpClient CreateDnsBypassingHttpClient()
  {
    var handler = new SocketsHttpHandler
    {
      // Disable connection pooling — each request gets a fresh DNS lookup.
      PooledConnectionLifetime = TimeSpan.Zero,

      ConnectCallback = async (context, ct) =>
      {
        // Resolve DNS via Cloudflare's DoH API instead of the system resolver.
        var ip = await ResolveDnsViaCloudflareAsync(context.DnsEndPoint.Host, ct);

        if (ip == null)
        {
          throw new HttpRequestException(
            $"DNS resolution via Cloudflare DoH failed for {context.DnsEndPoint.Host}");
        }

        Console.Error.WriteLine(
          $"[CloudflareTunnelManager] DoH resolved {context.DnsEndPoint.Host} → {ip}");

        var socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
        socket.NoDelay = true;

        try
        {
          await socket.ConnectAsync(new IPEndPoint(ip, context.DnsEndPoint.Port), ct);

          return new NetworkStream(socket, ownsSocket: true);
        }
        catch
        {
          socket.Dispose();

          throw;
        }
      }
    };

    return new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(10) };
  }


  /// <summary>
  ///   Resolves a hostname to an IPv4 address using Cloudflare's DNS-over-HTTPS (DoH) JSON API.
  /// </summary>
  /// <remarks>
  ///   <para>
  ///     Queries <c>https://cloudflare-dns.com/dns-query?name={hostname}&amp;type=A</c>
  ///     with the <c>Accept: application/dns-json</c> header. Returns the first A record
  ///     (type 1) from the response, or <c>null</c> if no record is found (NXDOMAIN).
  ///   </para>
  /// </remarks>
  /// <param name="hostname">The hostname to resolve.</param>
  /// <param name="ct">Cancellation token.</param>
  /// <returns>The resolved IPv4 address, or <c>null</c> if the record doesn't exist yet.</returns>
  private static async Task<IPAddress?> ResolveDnsViaCloudflareAsync(string hostname, CancellationToken ct)
  {
    try
    {
      using var request = new HttpRequestMessage(
        HttpMethod.Get,
        $"https://cloudflare-dns.com/dns-query?name={Uri.EscapeDataString(hostname)}&type=A");

      request.Headers.Add("Accept", "application/dns-json");

      var response = await DohClient.SendAsync(request, ct);
      response.EnsureSuccessStatusCode();

      var dohResponse = await response.Content.ReadFromJsonAsync<DohResponse>(ct);

      // Find the first A record (type 1) in the answers.
      var aRecord = dohResponse?.Answer?.FirstOrDefault(a => a.Type == 1);

      return aRecord?.Data is { } ip ? IPAddress.Parse(ip) : null;
    }
    catch (Exception ex)
    {
      Console.Error.WriteLine($"[CloudflareTunnelManager] DoH query for {hostname} failed: {ex.Message}");

      return null;
    }
  }


  /// <summary>
  ///   Compiled regex to extract the trycloudflare.com URL from cloudflared log output.
  /// </summary>
  /// <remarks>
  ///   Matches URLs like <c>https://random-words-here.trycloudflare.com</c>.
  /// </remarks>
  [GeneratedRegex(@"https://[\w-]+\.trycloudflare\.com", RegexOptions.Compiled)]
  private static partial Regex TryCloudflareUrlRegex();

  #endregion


  #region IAsyncDisposable

  /// <inheritdoc />
  public async ValueTask DisposeAsync()
  {
    if (_cloudflaredProcess is { HasExited: false })
    {
      try
      {
        _cloudflaredProcess.Kill(entireProcessTree: true);
        await _cloudflaredProcess.WaitForExitAsync();
      }
      catch
      {
        // Best-effort cleanup; process may have already exited.
      }
    }

    _cloudflaredProcess?.Dispose();
    _cloudflaredProcess = null;
    PublicUrl = null;
  }

  #endregion


  #region Inner Types

  /// <summary>
  ///   Minimal DTO for Cloudflare DNS-over-HTTPS JSON responses.
  ///   See: https://developers.cloudflare.com/1.1.1.1/encryption/dns-over-https/make-api-requests/dns-json/
  /// </summary>
  private sealed class DohResponse
  {
    /// <summary>DNS response status code (0 = NOERROR, 3 = NXDOMAIN).</summary>
    [JsonPropertyName("Status")]
    public int Status { get; init; }

    /// <summary>DNS answer records.</summary>
    [JsonPropertyName("Answer")]
    public DohAnswer[]? Answer { get; init; }
  }


  /// <summary>
  ///   Represents a single DNS answer record in a DoH JSON response.
  /// </summary>
  private sealed class DohAnswer
  {
    /// <summary>The record owner name.</summary>
    [JsonPropertyName("name")]
    public string? Name { get; init; }

    /// <summary>The DNS record type (1 = A, 28 = AAAA, 5 = CNAME).</summary>
    [JsonPropertyName("type")]
    public int Type { get; init; }

    /// <summary>The record TTL in seconds.</summary>
    [JsonPropertyName("TTL")]
    public int Ttl { get; init; }

    /// <summary>The record value (e.g., an IP address for A records).</summary>
    [JsonPropertyName("data")]
    public string? Data { get; init; }
  }

  #endregion
}
