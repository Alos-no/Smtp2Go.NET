namespace Smtp2Go.NET.UnitTests.Models;

using System.Text.Json;
using Smtp2Go.NET.Internal;
using Smtp2Go.NET.Models.Webhooks;

/// <summary>
///   Verifies that SMTP2GO webhook callback payloads deserialize correctly,
///   including the custom JSON converters for <see cref="WebhookCallbackEvent" />
///   and <see cref="BounceType" />.
/// </summary>
/// <remarks>
///   JSON fixtures in these tests use the actual SMTP2GO webhook format,
///   captured from live webhook callbacks (not documentation — the docs are inaccurate).
///   Key differences from SMTP2GO docs:
///   <list type="bullet">
///     <item>Recipient field is <c>"rcpt"</c> (delivered/bounce), not <c>"email"</c>.</item>
///     <item>Recipients array is <c>"recipients"</c> (processed), not <c>"recipients_list"</c>.</item>
///     <item>Timestamp is <c>"time"</c> (ISO 8601 string), not <c>"timestamp"</c> (int).</item>
///     <item>Source host is <c>"srchost"</c>, not <c>"hostname"</c>.</item>
///   </list>
/// </remarks>
[Trait("Category", "Unit")]
public sealed class WebhookPayloadDeserializationTests
{
  #region Processed Event

  [Fact]
  public void Deserialize_ProcessedEvent_ParsesCorrectly()
  {
    // Arrange — Actual SMTP2GO processed event format. Note: processed events have "recipients"
    // array but NOT "rcpt".
    const string json = """
      {
        "srchost": "146.70.170.30",
        "email_id": "1vomg2-abc123",
        "event": "processed",
        "time": "2026-02-07T18:05:02Z",
        "sender": "noreply@example.com",
        "recipients": ["user@example.com", "user2@example.com"],
        "sendtime": "2026-02-07T18:05:02.199324+00:00"
      }
      """;

    // Act
    var payload = JsonSerializer.Deserialize<WebhookCallbackPayload>(json, Smtp2GoJsonDefaults.Options);

    // Assert
    payload.Should().NotBeNull();
    payload!.SourceHost.Should().Be("146.70.170.30");
    payload.EmailId.Should().Be("1vomg2-abc123");
    payload.Event.Should().Be(WebhookCallbackEvent.Processed);
    payload.Time.Should().Be(new DateTimeOffset(2026, 2, 7, 18, 5, 2, TimeSpan.Zero));
    payload.SendTime.Should().NotBeNull();
    payload.Sender.Should().Be("noreply@example.com");

    // Processed events have "recipients" array, not "rcpt".
    payload.Recipient.Should().BeNull();
    payload.Recipients.Should().HaveCount(2);
    payload.Recipients![0].Should().Be("user@example.com");

    payload.BounceType.Should().BeNull();
    payload.BounceContext.Should().BeNull();
  }

  #endregion


  #region Delivered Event

  [Fact]
  public void Deserialize_DeliveredEvent_ParsesCorrectly()
  {
    // Arrange — Actual SMTP2GO delivered event format. Note: delivered events have "rcpt"
    // (single recipient) but NOT "recipients" array.
    const string json = """
      {
        "srchost": "146.70.170.30",
        "email_id": "1vomg2-abc123",
        "event": "delivered",
        "time": "2026-02-07T18:05:06Z",
        "rcpt": "user@example.com",
        "sender": "noreply@alos.app",
        "host": "mail.protonmail.ch [176.119.200.128]",
        "context": "Unavailable",
        "message": "250 2.0.0 Ok: 2788 bytes queued as 4f7f4b3tWbzKy",
        "sendtime": "2026-02-07T18:05:06.215451+00:00"
      }
      """;

    // Act
    var payload = JsonSerializer.Deserialize<WebhookCallbackPayload>(json, Smtp2GoJsonDefaults.Options);

    // Assert
    payload.Should().NotBeNull();
    payload!.SourceHost.Should().Be("146.70.170.30");
    payload.EmailId.Should().Be("1vomg2-abc123");
    payload.Event.Should().Be(WebhookCallbackEvent.Delivered);
    payload.Time.Should().Be(new DateTimeOffset(2026, 2, 7, 18, 5, 6, TimeSpan.Zero));
    payload.SendTime.Should().NotBeNull();

    // Delivered events have "rcpt" (per-event recipient), not "recipients" array.
    payload.Recipient.Should().Be("user@example.com");
    payload.Recipients.Should().BeNull();

    payload.Sender.Should().Be("noreply@alos.app");
    payload.Host.Should().Be("mail.protonmail.ch [176.119.200.128]");
    payload.BounceContext.Should().Be("Unavailable");
    payload.SmtpResponse.Should().Be("250 2.0.0 Ok: 2788 bytes queued as 4f7f4b3tWbzKy");
    payload.BounceType.Should().BeNull();
  }

  #endregion


  #region Bounce Events

  [Fact]
  public void Deserialize_BounceEvent_HardBounce_ParsesBounceFields()
  {
    // Arrange — Actual SMTP2GO bounce payload format observed in live integration tests.
    // SMTP2GO sends "event": "bounce" with a separate "bounce" field for hard/soft classification,
    // and "context" for the SMTP transaction context.
    const string json = """
      {
        "email_id": "bounce-456",
        "event": "bounce",
        "time": "2026-02-07T18:15:00Z",
        "rcpt": "invalid@nonexistent.com",
        "sender": "noreply@alos.app",
        "bounce": "hard",
        "context": "RCPT TO:<invalid@nonexistent.com>",
        "host": "gmail-smtp-in.l.google.com [209.85.233.26]"
      }
      """;

    // Act
    var payload = JsonSerializer.Deserialize<WebhookCallbackPayload>(json, Smtp2GoJsonDefaults.Options);

    // Assert
    payload.Should().NotBeNull();
    payload!.Event.Should().Be(WebhookCallbackEvent.Bounce);
    payload.BounceType.Should().Be(BounceType.Hard);
    payload.BounceContext.Should().Be("RCPT TO:<invalid@nonexistent.com>");
    payload.Host.Should().Be("gmail-smtp-in.l.google.com [209.85.233.26]");
    payload.Recipient.Should().Be("invalid@nonexistent.com");
  }


  [Fact]
  public void Deserialize_BounceEvent_SoftBounce_ParsesBounceFields()
  {
    // Arrange — Soft bounce in actual SMTP2GO payload format.
    const string json = """
      {
        "event": "bounce",
        "time": "2026-02-07T18:18:00Z",
        "rcpt": "user@example.com",
        "bounce": "soft",
        "context": "DATA: 452 Mailbox full"
      }
      """;

    // Act
    var payload = JsonSerializer.Deserialize<WebhookCallbackPayload>(json, Smtp2GoJsonDefaults.Options);

    // Assert
    payload.Should().NotBeNull();
    payload!.Event.Should().Be(WebhookCallbackEvent.Bounce);
    payload.BounceType.Should().Be(BounceType.Soft);
    payload.BounceContext.Should().Be("DATA: 452 Mailbox full");
    payload.Recipient.Should().Be("user@example.com");
  }

  #endregion


  #region Click Events

  [Fact]
  public void Deserialize_ClickedEvent_ParsesClickFields()
  {
    // Arrange
    const string json = """
      {
        "event": "clicked",
        "time": "2026-02-07T18:20:00Z",
        "rcpt": "user@example.com",
        "click_url": "https://alos.app/dashboard",
        "link": "https://track.smtp2go.com/abc123"
      }
      """;

    // Act
    var payload = JsonSerializer.Deserialize<WebhookCallbackPayload>(json, Smtp2GoJsonDefaults.Options);

    // Assert
    payload.Should().NotBeNull();
    payload!.Event.Should().Be(WebhookCallbackEvent.Clicked);
    payload.ClickUrl.Should().Be("https://alos.app/dashboard");
    payload.Link.Should().Be("https://track.smtp2go.com/abc123");
    payload.Recipient.Should().Be("user@example.com");
  }

  #endregion


  #region WebhookCallbackEvent Converter

  [Theory]
  [InlineData("processed", WebhookCallbackEvent.Processed)]
  [InlineData("delivered", WebhookCallbackEvent.Delivered)]
  [InlineData("bounce", WebhookCallbackEvent.Bounce)]
  [InlineData("opened", WebhookCallbackEvent.Opened)]
  [InlineData("clicked", WebhookCallbackEvent.Clicked)]
  [InlineData("unsubscribed", WebhookCallbackEvent.Unsubscribed)]
  [InlineData("spam_complaint", WebhookCallbackEvent.SpamComplaint)]
  public void CallbackEventConverter_DeserializesKnownEvents(string jsonValue, WebhookCallbackEvent expected)
  {
    // Arrange
    var json = $$"""{"event": "{{jsonValue}}"}""";

    // Act
    var payload = JsonSerializer.Deserialize<WebhookCallbackPayload>(json, Smtp2GoJsonDefaults.Options);

    // Assert
    payload.Should().NotBeNull();
    payload!.Event.Should().Be(expected);
  }


  [Theory]
  [InlineData("some_future_event")]
  [InlineData("hard_bounced")]
  [InlineData("soft_bounced")]
  public void CallbackEventConverter_DeserializesUnknownEvent_AsUnknown(string jsonValue)
  {
    // Arrange — The API may introduce new event types in the future.
    // Also verifies that the removed legacy values ("hard_bounced", "soft_bounced")
    // now correctly fall through to Unknown instead of being mapped to dead enum values.
    var json = $$"""{"event": "{{jsonValue}}"}""";

    // Act
    var payload = JsonSerializer.Deserialize<WebhookCallbackPayload>(json, Smtp2GoJsonDefaults.Options);

    // Assert
    payload.Should().NotBeNull();
    payload!.Event.Should().Be(WebhookCallbackEvent.Unknown);
  }


  [Theory]
  [InlineData(WebhookCallbackEvent.Processed, "processed")]
  [InlineData(WebhookCallbackEvent.Delivered, "delivered")]
  [InlineData(WebhookCallbackEvent.Bounce, "bounce")]
  [InlineData(WebhookCallbackEvent.Opened, "opened")]
  [InlineData(WebhookCallbackEvent.Clicked, "clicked")]
  [InlineData(WebhookCallbackEvent.Unsubscribed, "unsubscribed")]
  [InlineData(WebhookCallbackEvent.SpamComplaint, "spam_complaint")]
  public void CallbackEventConverter_SerializesToSnakeCase(WebhookCallbackEvent value, string expected)
  {
    // Arrange — Serialize via a wrapper to trigger the converter.
    var options = new JsonSerializerOptions();
    options.Converters.Add(new WebhookCallbackEventJsonConverter());

    // Act
    var json = JsonSerializer.Serialize(value, options);

    // Assert — The value should be a quoted snake_case string.
    json.Should().Be($"\"{expected}\"");
  }

  #endregion


  #region BounceType Converter

  [Theory]
  [InlineData("hard", BounceType.Hard)]
  [InlineData("soft", BounceType.Soft)]
  public void BounceTypeConverter_DeserializesKnownTypes(string jsonValue, BounceType expected)
  {
    // Arrange — The "bounce" field contains the bounce classification (hard/soft).
    var json = $$"""{"event": "bounce", "bounce": "{{jsonValue}}"}""";

    // Act
    var payload = JsonSerializer.Deserialize<WebhookCallbackPayload>(json, Smtp2GoJsonDefaults.Options);

    // Assert
    payload.Should().NotBeNull();
    payload!.BounceType.Should().Be(expected);
  }


  [Fact]
  public void BounceTypeConverter_DeserializesUnknownType_AsUnknown()
  {
    // Arrange
    const string json = """{"event": "bounce", "bounce": "future_type"}""";

    // Act
    var payload = JsonSerializer.Deserialize<WebhookCallbackPayload>(json, Smtp2GoJsonDefaults.Options);

    // Assert
    payload.Should().NotBeNull();
    payload!.BounceType.Should().Be(BounceType.Unknown);
  }


  [Fact]
  public void BounceTypeConverter_DeserializesNull_AsNull()
  {
    // Arrange — Non-bounce events have no "bounce" field.
    const string json = """{"event": "delivered"}""";

    // Act
    var payload = JsonSerializer.Deserialize<WebhookCallbackPayload>(json, Smtp2GoJsonDefaults.Options);

    // Assert
    payload.Should().NotBeNull();
    payload!.BounceType.Should().BeNull();
  }

  #endregion


  #region WebhookCreateEvent Converter

  [Theory]
  [InlineData(WebhookCreateEvent.Processed, "processed")]
  [InlineData(WebhookCreateEvent.Delivered, "delivered")]
  [InlineData(WebhookCreateEvent.Bounce, "bounce")]
  [InlineData(WebhookCreateEvent.Open, "open")]
  [InlineData(WebhookCreateEvent.Click, "click")]
  [InlineData(WebhookCreateEvent.Spam, "spam")]
  [InlineData(WebhookCreateEvent.Unsubscribe, "unsubscribe")]
  [InlineData(WebhookCreateEvent.Resubscribe, "resubscribe")]
  [InlineData(WebhookCreateEvent.Reject, "reject")]
  public void CreateEventConverter_SerializesToApiStrings(WebhookCreateEvent value, string expected)
  {
    // Arrange — WebhookCreateEvent has [JsonConverter] on the enum type itself,
    // so it auto-serializes without additional options.
    // Act
    var json = JsonSerializer.Serialize(value);

    // Assert — The value should be a quoted subscription event string.
    json.Should().Be($"\"{expected}\"");
  }


  [Theory]
  [InlineData("processed", WebhookCreateEvent.Processed)]
  [InlineData("delivered", WebhookCreateEvent.Delivered)]
  [InlineData("bounce", WebhookCreateEvent.Bounce)]
  [InlineData("open", WebhookCreateEvent.Open)]
  [InlineData("click", WebhookCreateEvent.Click)]
  [InlineData("spam", WebhookCreateEvent.Spam)]
  [InlineData("unsubscribe", WebhookCreateEvent.Unsubscribe)]
  [InlineData("resubscribe", WebhookCreateEvent.Resubscribe)]
  [InlineData("reject", WebhookCreateEvent.Reject)]
  public void CreateEventConverter_DeserializesFromApiStrings(string jsonValue, WebhookCreateEvent expected)
  {
    // Arrange
    var json = $"\"{jsonValue}\"";

    // Act
    var result = JsonSerializer.Deserialize<WebhookCreateEvent>(json);

    // Assert
    result.Should().Be(expected);
  }

  #endregion
}
