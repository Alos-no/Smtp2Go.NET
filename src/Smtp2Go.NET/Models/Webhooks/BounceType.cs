namespace Smtp2Go.NET.Models.Webhooks;

using System.Text.Json;
using System.Text.Json.Serialization;

/// <summary>
///   Defines the types of email bounces reported by SMTP2GO.
/// </summary>
/// <remarks>
///   <para>
///     Bounce classification determines how the recipient address should be handled:
///     <list type="bullet">
///       <item><see cref="Hard"/> — Permanent failure; remove the address from mailing lists.</item>
///       <item><see cref="Soft"/> — Temporary failure; the address may be retried later.</item>
///     </list>
///     The SMTP2GO API transmits these as lowercase strings (<c>"hard"</c>, <c>"soft"</c>);
///     the <see cref="BounceTypeJsonConverter"/> handles conversion.
///   </para>
/// </remarks>
public enum BounceType
{
  /// <summary>
  ///   An unrecognized or unmapped bounce type.
  /// </summary>
  /// <remarks>
  ///   <para>
  ///     Used as a fallback when the API returns a bounce type not yet
  ///     defined in this enum. Consumers should log and handle gracefully.
  ///   </para>
  /// </remarks>
  Unknown = 0,

  /// <summary>
  ///   A permanent delivery failure (hard bounce).
  /// </summary>
  /// <remarks>
  ///   <para>
  ///     Hard bounces indicate the email address is permanently undeliverable.
  ///     Common causes include: invalid address, non-existent domain, or
  ///     permanently rejected sender. The address should be suppressed from
  ///     all future mailings.
  ///   </para>
  /// </remarks>
  Hard,

  /// <summary>
  ///   A temporary delivery failure (soft bounce).
  /// </summary>
  /// <remarks>
  ///   <para>
  ///     Soft bounces indicate a temporary issue that may resolve on its own.
  ///     Common causes include: full mailbox, server temporarily unavailable,
  ///     message too large, or greylisting. SMTP2GO may automatically retry.
  ///   </para>
  /// </remarks>
  Soft
}


/// <summary>
///   JSON converter for <see cref="BounceType"/> that handles SMTP2GO's
///   lowercase string representation.
/// </summary>
/// <remarks>
///   <para>
///     The SMTP2GO API uses lowercase strings for bounce types:
///     <list type="bullet">
///       <item><c>"hard"</c> -> <see cref="BounceType.Hard"/></item>
///       <item><c>"soft"</c> -> <see cref="BounceType.Soft"/></item>
///     </list>
///     Unrecognized values are deserialized as <see cref="BounceType.Unknown"/>.
///   </para>
/// </remarks>
public class BounceTypeJsonConverter : JsonConverter<BounceType>
{
  #region Constants & Statics

  /// <summary>
  ///   The SMTP2GO API string for hard bounces.
  /// </summary>
  private const string HardValue = "hard";

  /// <summary>
  ///   The SMTP2GO API string for soft bounces.
  /// </summary>
  private const string SoftValue = "soft";

  #endregion


  #region Methods - Public

  /// <summary>
  ///   Reads and converts a JSON string to a <see cref="BounceType"/> value.
  /// </summary>
  /// <param name="reader">The JSON reader.</param>
  /// <param name="typeToConvert">The type to convert.</param>
  /// <param name="options">The serializer options.</param>
  /// <returns>The deserialized <see cref="BounceType"/> value.</returns>
  public override BounceType Read(
    ref Utf8JsonReader reader,
    Type typeToConvert,
    JsonSerializerOptions options)
  {
    var value = reader.GetString();

    return value switch
    {
      HardValue => BounceType.Hard,
      SoftValue => BounceType.Soft,
      _ => BounceType.Unknown
    };
  }

  /// <summary>
  ///   Writes a <see cref="BounceType"/> value as a JSON lowercase string.
  /// </summary>
  /// <param name="writer">The JSON writer.</param>
  /// <param name="value">The <see cref="BounceType"/> value to write.</param>
  /// <param name="options">The serializer options.</param>
  public override void Write(
    Utf8JsonWriter writer,
    BounceType value,
    JsonSerializerOptions options)
  {
    var stringValue = value switch
    {
      BounceType.Hard => HardValue,
      BounceType.Soft => SoftValue,
      _ => "unknown"
    };

    writer.WriteStringValue(stringValue);
  }

  #endregion
}


/// <summary>
///   JSON converter for nullable <see cref="BounceType"/> that handles SMTP2GO's
///   lowercase string representation and JSON null values.
/// </summary>
/// <remarks>
///   <para>
///     This converter extends <see cref="BounceTypeJsonConverter"/> to support
///     nullable <see cref="BounceType"/> properties. JSON null values are
///     deserialized as C# null rather than <see cref="BounceType.Unknown"/>.
///   </para>
/// </remarks>
public class NullableBounceTypeJsonConverter : JsonConverter<BounceType?>
{
  #region Properties & Fields - Non-Public

  /// <summary>
  ///   The inner converter for non-nullable <see cref="BounceType"/> values.
  /// </summary>
  private readonly BounceTypeJsonConverter _inner = new();

  #endregion


  #region Methods - Public

  /// <summary>
  ///   Reads and converts a JSON string or null to a nullable <see cref="BounceType"/> value.
  /// </summary>
  /// <param name="reader">The JSON reader.</param>
  /// <param name="typeToConvert">The type to convert.</param>
  /// <param name="options">The serializer options.</param>
  /// <returns>The deserialized nullable <see cref="BounceType"/> value, or null.</returns>
  public override BounceType? Read(
    ref Utf8JsonReader reader,
    Type typeToConvert,
    JsonSerializerOptions options)
  {
    if (reader.TokenType == JsonTokenType.Null)
    {
      return null;
    }

    return _inner.Read(ref reader, typeof(BounceType), options);
  }

  /// <summary>
  ///   Writes a nullable <see cref="BounceType"/> value as a JSON string or null.
  /// </summary>
  /// <param name="writer">The JSON writer.</param>
  /// <param name="value">The nullable <see cref="BounceType"/> value to write.</param>
  /// <param name="options">The serializer options.</param>
  public override void Write(
    Utf8JsonWriter writer,
    BounceType? value,
    JsonSerializerOptions options)
  {
    if (value is null)
    {
      writer.WriteNullValue();

      return;
    }

    _inner.Write(writer, value.Value, options);
  }

  #endregion
}
