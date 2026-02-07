# Smtp2Go.NET

[![CI](https://github.com/Alos-no/Smtp2Go.NET/actions/workflows/CI.yml/badge.svg)](https://github.com/Alos-no/Smtp2Go.NET/actions/workflows/CI.yml)
[![NuGet](https://img.shields.io/nuget/v/Smtp2Go.NET?color=27ae60)](https://www.nuget.org/packages/Smtp2Go.NET/)
[![License: Apache-2.0](https://img.shields.io/badge/license-Apache--2.0-27ae60)](LICENSE.txt)

**Smtp2Go.NET** is a strongly-typed .NET client library for the [SMTP2GO](https://www.smtp2go.com/) transactional email API. It supports sending emails, webhook management, and email statistics with built-in HTTP resilience.

## Installation

```bash
dotnet add package Smtp2Go.NET
```

## Quick Start

### Configuration (appsettings.json)

```json
{
  "Smtp2Go": {
    "ApiKey": "api-YOUR-KEY-HERE",
    "BaseUrl": "https://api.smtp2go.com/v3/",
    "Timeout": "00:00:30"
  }
}
```

### Registration (Program.cs)

```csharp
// With HttpClient + resilience pipeline (recommended for production)
builder.Services.AddSmtp2GoWithHttp(builder.Configuration);

// Or with programmatic configuration
builder.Services.AddSmtp2GoWithHttp(options =>
{
    options.ApiKey = "api-YOUR-KEY-HERE";
});
```

### Sending Email

```csharp
public class EmailService(ISmtp2GoClient smtp2Go)
{
    public async Task SendWelcomeAsync(string recipientEmail)
    {
        var request = new EmailSendRequest
        {
            Sender = "noreply@yourdomain.com",
            To = [recipientEmail],
            Subject = "Welcome!",
            HtmlBody = "<h1>Welcome to our platform</h1>"
        };

        var response = await smtp2Go.SendEmailAsync(request);
        // response.Data.Succeeded == 1
    }
}
```

### Managing Webhooks

```csharp
// Create a webhook with Basic Auth (credentials embedded in URL)
var request = new WebhookCreateRequest
{
    WebhookUrl = "https://user:pass@api.yourdomain.com/webhooks/smtp2go",
    Events = [WebhookCreateEvent.Delivered, WebhookCreateEvent.Bounce]
};

var response = await smtp2Go.Webhooks.CreateAsync(request);

// List all webhooks
var webhooks = await smtp2Go.Webhooks.ListAsync();

// Delete a webhook
await smtp2Go.Webhooks.DeleteAsync(webhookId);
```

### Receiving Webhook Callbacks

SMTP2GO sends HTTP POST requests to your registered webhook URL when email events occur. The `WebhookCallbackPayload` model deserializes the inbound payload:

```csharp
[HttpPost("webhooks/smtp2go")]
public IActionResult HandleWebhook([FromBody] WebhookCallbackPayload payload)
{
    switch (payload.Event)
    {
        case WebhookCallbackEvent.Delivered:
            logger.LogInformation("Delivered to {Recipient}", payload.Recipient);
            break;

        case WebhookCallbackEvent.Bounce:
            logger.LogWarning("Bounce ({Type}) for {Recipient}: {Context}",
                payload.BounceType, payload.Recipient, payload.BounceContext);
            break;

        case WebhookCallbackEvent.SpamComplaint:
            logger.LogWarning("Spam complaint from {Recipient}", payload.Recipient);
            break;
    }

    return Ok();
}
```

#### Webhook Event Types

SMTP2GO uses different event names for **subscriptions** vs **callback payloads**:

| Subscription (`WebhookCreateEvent`) | Callback (`WebhookCallbackEvent`) | Description |
|--------------------------------------|-------------------------------------|-------------|
| `Processed` | `Processed` | Email accepted and queued by SMTP2GO |
| `Delivered` | `Delivered` | Email delivered to recipient's mail server |
| `Bounce` | `Bounce` | Email bounced (check `BounceType` for hard/soft) |
| `Open` | `Opened` | Recipient opened the email |
| `Click` | `Clicked` | Recipient clicked a tracked link |
| `Spam` | `SpamComplaint` | Recipient marked the email as spam |
| `Unsubscribe` | `Unsubscribed` | Recipient unsubscribed |
| `Resubscribe` | — | Recipient re-subscribed |
| `Reject` | — | Email rejected before delivery |

#### Callback Payload Fields

| Field | Type | Description |
|-------|------|-------------|
| `Event` | `WebhookCallbackEvent` | The event type that triggered this callback |
| `EmailId` | `string?` | SMTP2GO email identifier (correlates with send response) |
| `Recipient` | `string?` | Per-event recipient (`rcpt`); present for delivered/bounce events |
| `Recipients` | `string[]?` | All recipients from the original send; present for processed events |
| `Sender` | `string?` | Sender email address |
| `Time` | `DateTimeOffset?` | ISO 8601 timestamp when the event occurred |
| `SendTime` | `DateTimeOffset?` | ISO 8601 timestamp when the email was sent by SMTP2GO |
| `SourceHost` | `string?` | Source host IP of the SMTP2GO server that processed the email |
| `BounceType` | `BounceType?` | `Hard` or `Soft` (bounce events only) |
| `BounceContext` | `string?` | SMTP transaction context (bounce and delivered events) |
| `Host` | `string?` | Target mail server host and IP (bounce and delivered events) |
| `SmtpResponse` | `string?` | SMTP 250 response from receiving server (delivered events only) |
| `ClickUrl` | `string?` | Original URL clicked (click events only) |
| `Link` | `string?` | Tracked link URL (click events only) |

### Querying Statistics

```csharp
var request = new EmailSummaryRequest
{
    StartDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-7)),
    EndDate = DateOnly.FromDateTime(DateTime.UtcNow)
};

var summary = await smtp2Go.Statistics.GetEmailSummaryAsync(request);
```

## Features

- **Email Sending** - Send transactional emails with attachments, CC/BCC, custom headers, and inline images
- **Webhook Management** - Create, list, and delete webhook subscriptions for delivery events
- **Webhook Callbacks** - Strongly-typed models for receiving and processing webhook payloads
- **Email Statistics** - Query email delivery summaries and metrics
- **Built-in Resilience** - Production-grade HTTP pipeline with retry, circuit breaker, rate limiting, and timeouts
- **Strongly Typed** - Full request/response models with XML documentation
- **Source-Generated Logging** - Zero-reflection `[LoggerMessage]` for high-performance diagnostics
- **DI Integration** - First-class `IServiceCollection` registration with `IHttpClientFactory`

## HTTP Resilience Pipeline

The `AddSmtp2GoWithHttp` registration includes a production-grade resilience pipeline:

| Layer | Behavior |
|-------|----------|
| Rate Limiter | Concurrency limiter (20 permits, 50 queue) |
| Total Timeout | Outer timeout (60s) covering all retries |
| Retry | Exponential backoff (max 3 attempts). **POST is not retried** to prevent duplicate sends |
| Circuit Breaker | Opens at 10% failure rate over 30s sampling window |
| Per-Attempt Timeout | Individual request timeout (30s) |

## Configuration Options

| Option | Type | Default | Description |
|--------|------|---------|-------------|
| `ApiKey` | `string` | *required* | SMTP2GO API key |
| `BaseUrl` | `string` | `https://api.smtp2go.com/v3/` | API base URL |
| `Timeout` | `TimeSpan` | `00:00:30` | Default request timeout |

## Supported Frameworks

| Framework | Supported |
|-----------|:---------:|
| .NET 8 (LTS) | Yes |
| .NET 9 | Yes |
| .NET 10 (LTS) | Yes |

All packages are **strong-named** for use in strong-named assemblies.

## Development

### Prerequisites

- .NET 10 SDK
- SMTP2GO account with API keys (for integration tests)

### Building

```bash
dotnet build Smtp2Go.NET.slnx
```

### Testing

```bash
# Unit tests (74 tests, no network required)
tests/Smtp2Go.NET.UnitTests/bin/Debug/net10.0/Smtp2Go.NET.UnitTests

# Integration tests (15 tests, requires API keys configured via user secrets)
tests/Smtp2Go.NET.IntegrationTests/bin/Debug/net10.0/Smtp2Go.NET.IntegrationTests
```

> **Note:** xUnit v3 test projects are standalone executables.

### Configuring Test Secrets

```bash
cd tests/Smtp2Go.NET.IntegrationTests
dotnet user-secrets set "Smtp2Go:ApiKey:Sandbox" "api-YOUR-SANDBOX-KEY"
dotnet user-secrets set "Smtp2Go:ApiKey:Live" "api-YOUR-LIVE-KEY"
dotnet user-secrets set "Smtp2Go:TestSender" "verified-sender@yourdomain.com"
dotnet user-secrets set "Smtp2Go:TestRecipient" "test@yourmailbox.com"
```

Or use the interactive setup script: `pwsh -File scripts/setup-secrets.ps1`

## Project Structure

```
Smtp2Go.NET/
├── src/Smtp2Go.NET/                    # Library source
│   ├── Core/Smtp2GoResource.cs         # Base class (shared PostAsync)
│   ├── Models/                         # Request/response DTOs
│   │   ├── Email/                      # Email send models
│   │   ├── Statistics/                 # Statistics query models
│   │   └── Webhooks/                   # Webhook CRUD + payload models
│   ├── ISmtp2GoClient.cs              # Main client interface
│   ├── Smtp2GoClient.cs              # Main client implementation
│   └── ServiceCollectionExtensions.cs # DI registration
└── tests/
    ├── Smtp2Go.NET.UnitTests/         # 77 unit tests (Moq-based)
    └── Smtp2Go.NET.IntegrationTests/  # 15 integration tests (live API)
```

## License

This project is licensed under the [Apache 2.0 License](LICENSE.txt).
