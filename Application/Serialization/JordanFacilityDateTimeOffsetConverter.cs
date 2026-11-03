using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using Application.Validators;

namespace Application.Serialization;

/// <summary>
/// Enforces DVLD appointment time policy:
/// 1. A timestamp without an offset represents Jordan facility wall-clock time.
/// 2. A timestamp with 'Z' or an explicit offset represents an absolute instant and is converted explicitly to Jordan facility time.
/// 3. The hosting machine's local timezone never determines the meaning of an incoming timestamp.
/// </summary>
public sealed class JordanFacilityDateTimeOffsetConverter : JsonConverter<DateTimeOffset>
{
    public override DateTimeOffset Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.String)
        {
            throw new JsonException($"Expected string for appointment timestamp, but got {reader.TokenType}.");
        }

        var stringValue = reader.GetString();
        if (string.IsNullOrWhiteSpace(stringValue))
        {
            return default;
        }

        return Parse(stringValue);
    }

    public override void Write(
        Utf8JsonWriter writer,
        DateTimeOffset value,
        JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString("yyyy-MM-ddTHH:mm:sszzz", CultureInfo.InvariantCulture));
    }

    public static DateTimeOffset Parse(string stringValue)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(stringValue);

        var trimmed = stringValue.Trim();

        if (HasExplicitOffset(trimmed))
        {
            // Timestamp with Z or an explicit offset represents an absolute instant.
            return DateTimeOffset.Parse(trimmed, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
        }

        // Timestamp without an offset represents Jordan facility wall-clock time.
        // The hosting machine's local timezone must never determine the meaning of an incoming timestamp.
        var localDateTime = DateTime.Parse(trimmed, CultureInfo.InvariantCulture, DateTimeStyles.None);
        var jordanOffset = TestAppointmentValidator.JordanTimeZone.GetUtcOffset(localDateTime);
        return new DateTimeOffset(localDateTime, jordanOffset);
    }

    public static bool HasExplicitOffset(string dateString)
    {
        if (string.IsNullOrWhiteSpace(dateString))
        {
            return false;
        }

        var trimmed = dateString.Trim();
        if (trimmed.EndsWith('Z') || trimmed.EndsWith('z'))
        {
            return true;
        }

        var separatorIndex = trimmed.IndexOfAny(['T', 't', ' ']);
        if (separatorIndex >= 0)
        {
            var timePart = trimmed.Substring(separatorIndex);
            return timePart.Contains('+') || timePart.Contains('-');
        }

        return trimmed.Contains('+');
    }
}
