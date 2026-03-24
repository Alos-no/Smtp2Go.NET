namespace Smtp2Go.NET.UnitTests.Models;

using Smtp2Go.NET.Models.Webhooks;

/// <summary>
///   Verifies conversion of SMTP2GO form-encoded webhook callbacks into
///   <see cref="WebhookCallbackPayload" />.
/// </summary>
[Trait("Category", "Unit")]
public sealed class WebhookPayloadFormParsingTests
{
  #region Delivered Event

  [Fact]
  public void ParseFormValues_LiveDeliveredPayload_ParsesObservedFields()
  {
    // Arrange
    // This payload was captured from a real SMTP2GO delivered callback on 2026-03-24.
    var formValues = new Dictionary<string, string[]?>
    {
      ["Message-Id"] = ["<E1w4x2g-FnQW0hPru7M-NRRC@message-id.smtpcorp.com>"],
      ["Subject"] = ["Webhook Delivery Test - b83d60289ef94e028a45a905198ad9b7"],
      ["auth"] = ["api-597435AE4E55"],
      ["email_id"] = ["1w4x2g-FnQW0hPru7M-NRRC"],
      ["event"] = ["delivered"],
      ["from"] = ["testing@dev.mjosdrone.no"],
      ["from_address"] = ["testing@dev.mjosdrone.no"],
      ["from_name"] = [""],
      ["host"] = ["mail.protonmail.ch [185.205.70.128]"],
      ["id"] = ["6dfa7d3b4514c1f5f0e916bc0cc0395c"],
      ["context"] = ["Unavailable"],
      ["message"] = ["250 2.0.0 Ok: 2780 bytes queued as 4fg32Y2xRcz3T"],
      ["message-id"] = ["<E1w4x2g-FnQW0hPru7M-NRRC@message-id.smtpcorp.com>"],
      ["rcpt"] = ["alexis.pujo@pm.me"],
      ["sender"] = ["testing@dev.mjosdrone.no"],
      ["sendtime"] = ["2026-03-24T08:23:19.052765+00:00"],
      ["subject"] = ["Webhook Delivery Test - b83d60289ef94e028a45a905198ad9b7"],
      ["time"] = ["2026-03-24T08:23:19Z"]
    };

    // Act
    var payload = WebhookCallbackPayloadParser.ParseFormValues(formValues);

    // Assert
    payload.Event.Should().Be(WebhookCallbackEvent.Delivered);
    payload.EmailId.Should().Be("1w4x2g-FnQW0hPru7M-NRRC");
    payload.MessageId.Should().Be("<E1w4x2g-FnQW0hPru7M-NRRC@message-id.smtpcorp.com>");
    payload.Subject.Should().Be("Webhook Delivery Test - b83d60289ef94e028a45a905198ad9b7");
    payload.EventId.Should().Be("6dfa7d3b4514c1f5f0e916bc0cc0395c");
    payload.Auth.Should().Be("api-597435AE4E55");
    payload.Recipient.Should().Be("alexis.pujo@pm.me");
    payload.Sender.Should().Be("testing@dev.mjosdrone.no");
    payload.From.Should().Be("testing@dev.mjosdrone.no");
    payload.FromAddress.Should().Be("testing@dev.mjosdrone.no");
    payload.FromName.Should().BeEmpty();
    payload.Time.Should().Be(new DateTimeOffset(2026, 3, 24, 8, 23, 19, TimeSpan.Zero));
    payload.SendTime.Should().Be(new DateTimeOffset(2026, 3, 24, 8, 23, 19, 52, TimeSpan.Zero).AddTicks(7650));
    payload.Host.Should().Be("mail.protonmail.ch [185.205.70.128]");
    payload.BounceContext.Should().Be("Unavailable");
    payload.SmtpResponse.Should().Be("250 2.0.0 Ok: 2780 bytes queued as 4fg32Y2xRcz3T");
    payload.Recipients.Should().BeNull();
  }


  [Fact]
  public void ParseFormValues_LiveProcessedPayload_ParsesObservedFields()
  {
    // Arrange
    // This payload was captured from a real SMTP2GO processed callback on 2026-03-24.
    var formValues = new Dictionary<string, string[]?>
    {
      ["Message-Id"] = ["<E1w4x2g-FnQW0hPru7M-NRRC@message-id.smtpcorp.com>"],
      ["Subject"] = ["Webhook Delivery Test - b83d60289ef94e028a45a905198ad9b7"],
      ["auth"] = ["api-597435AE4E55"],
      ["email_id"] = ["1w4x2g-FnQW0hPru7M-NRRC"],
      ["event"] = ["processed"],
      ["from"] = ["testing@dev.mjosdrone.no"],
      ["from_address"] = ["testing@dev.mjosdrone.no"],
      ["from_name"] = [""],
      ["id"] = ["e57b42854a69ee377c4221c22e08e5e7"],
      ["message-id"] = ["<E1w4x2g-FnQW0hPru7M-NRRC@message-id.smtpcorp.com>"],
      ["recipients"] = ["alexis.pujo@pm.me"],
      ["sender"] = ["testing@dev.mjosdrone.no"],
      ["sendtime"] = ["2026-03-24T08:23:14.828376+00:00"],
      ["srchost"] = ["146.70.170.19"],
      ["subject"] = ["Webhook Delivery Test - b83d60289ef94e028a45a905198ad9b7"],
      ["time"] = ["2026-03-24T08:23:14Z"]
    };

    // Act
    var payload = WebhookCallbackPayloadParser.ParseFormValues(formValues);

    // Assert
    payload.Event.Should().Be(WebhookCallbackEvent.Processed);
    payload.EmailId.Should().Be("1w4x2g-FnQW0hPru7M-NRRC");
    payload.MessageId.Should().Be("<E1w4x2g-FnQW0hPru7M-NRRC@message-id.smtpcorp.com>");
    payload.Subject.Should().Be("Webhook Delivery Test - b83d60289ef94e028a45a905198ad9b7");
    payload.EventId.Should().Be("e57b42854a69ee377c4221c22e08e5e7");
    payload.Auth.Should().Be("api-597435AE4E55");
    payload.SourceHost.Should().Be("146.70.170.19");
    payload.Sender.Should().Be("testing@dev.mjosdrone.no");
    payload.From.Should().Be("testing@dev.mjosdrone.no");
    payload.FromAddress.Should().Be("testing@dev.mjosdrone.no");
    payload.FromName.Should().BeEmpty();
    payload.Time.Should().Be(new DateTimeOffset(2026, 3, 24, 8, 23, 14, TimeSpan.Zero));
    payload.SendTime.Should().Be(new DateTimeOffset(2026, 3, 24, 8, 23, 14, 828, TimeSpan.Zero).AddTicks(3760));
    payload.Recipient.Should().BeNull();
    payload.Recipients.Should().BeEquivalentTo("alexis.pujo@pm.me");
    payload.BounceContext.Should().BeNull();
    payload.SmtpResponse.Should().BeNull();
  }

  #endregion


  #region Bounce Event

  [Fact]
  public void ParseFormValues_BouncePayload_ParsesCorrectly()
  {
    // Arrange
    var formValues = new[]
    {
      new KeyValuePair<string, string?>("event", "bounce"),
      new KeyValuePair<string, string?>("email_id", "provider-bounce-123"),
      new KeyValuePair<string, string?>("rcpt", "recipient@example.com"),
      new KeyValuePair<string, string?>("bounce", "soft"),
      new KeyValuePair<string, string?>("context", "DATA: 452 Mailbox full")
    };

    // Act
    var payload = WebhookCallbackPayloadParser.ParseFormValues(formValues);

    // Assert
    payload.Event.Should().Be(WebhookCallbackEvent.Bounce);
    payload.EmailId.Should().Be("provider-bounce-123");
    payload.Recipient.Should().Be("recipient@example.com");
    payload.BounceType.Should().Be(BounceType.Soft);
    payload.BounceContext.Should().Be("DATA: 452 Mailbox full");
  }

  #endregion


  #region Recipients

  [Fact]
  public void ParseFormValues_RecipientsRepeatedAndDelimited_NormalizesRecipients()
  {
    // Arrange
    var formValues = new[]
    {
      new KeyValuePair<string, string?>("event", "processed"),
      new KeyValuePair<string, string?>("recipients", "one@example.com"),
      new KeyValuePair<string, string?>("recipients", "two@example.com; three@example.com"),
      new KeyValuePair<string, string?>("recipients", "four@example.com,five@example.com")
    };

    // Act
    var payload = WebhookCallbackPayloadParser.ParseFormValues(formValues);

    // Assert
    payload.Event.Should().Be(WebhookCallbackEvent.Processed);
    payload.Recipients.Should().BeEquivalentTo(
      "one@example.com",
      "two@example.com",
      "three@example.com",
      "four@example.com",
      "five@example.com");
  }

  #endregion


  #region Compatibility Aliases

  [Theory]
  [InlineData("open", WebhookCallbackEvent.Opened)]
  [InlineData("opened", WebhookCallbackEvent.Opened)]
  [InlineData("click", WebhookCallbackEvent.Clicked)]
  [InlineData("clicked", WebhookCallbackEvent.Clicked)]
  [InlineData("spam", WebhookCallbackEvent.SpamComplaint)]
  [InlineData("spam_complaint", WebhookCallbackEvent.SpamComplaint)]
  [InlineData("unsubscribe", WebhookCallbackEvent.Unsubscribed)]
  [InlineData("unsubscribed", WebhookCallbackEvent.Unsubscribed)]
  public void ParseFormValues_CompatibilityEventAliases_NormalizesToCanonicalEnum(
    string               rawValue,
    WebhookCallbackEvent expected)
  {
    // Arrange
    var formValues = new[]
    {
      new KeyValuePair<string, string?>("event", rawValue)
    };

    // Act
    var payload = WebhookCallbackPayloadParser.ParseFormValues(formValues);

    // Assert
    payload.Event.Should().Be(expected);
  }

  #endregion


  #region Invalid Inputs

  [Fact]
  public void ParseFormValues_InvalidTimestamp_ReturnsNullTimestamp()
  {
    // Arrange
    var formValues = new[]
    {
      new KeyValuePair<string, string?>("event", "delivered"),
      new KeyValuePair<string, string?>("time", "not-a-timestamp")
    };

    // Act
    var payload = WebhookCallbackPayloadParser.ParseFormValues(formValues);

    // Assert
    payload.Event.Should().Be(WebhookCallbackEvent.Delivered);
    payload.Time.Should().BeNull();
  }


  [Fact]
  public void ParseFormValues_UnknownEvent_ReturnsUnknown()
  {
    // Arrange
    var formValues = new[]
    {
      new KeyValuePair<string, string?>("event", "totally_new_event")
    };

    // Act
    var payload = WebhookCallbackPayloadParser.ParseFormValues(formValues);

    // Assert
    payload.Event.Should().Be(WebhookCallbackEvent.Unknown);
  }

  #endregion
}
