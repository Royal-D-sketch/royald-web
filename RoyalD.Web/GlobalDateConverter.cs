using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace RoyalD.Web;

/// <summary>
/// JSON converter that serializes and deserializes DateTime values using the "dd/MM/yyyy" format.
/// This ensures all API responses return dates in the required format.
/// </summary>
public class GlobalDateConverter : JsonConverter<DateTime>
{
    private const string DateFormat = "dd/MM/yyyy";

    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.String)
            throw new JsonException($"Unexpected token parsing date. Expected String, got {reader.TokenType}.");

        var dateString = reader.GetString();
        if (DateTime.TryParseExact(dateString, DateFormat, null, System.Globalization.DateTimeStyles.None, out var date))
            return date;
        // Fallback to default parsing for compatibility
        if (DateTime.TryParse(dateString, out date))
            return date;
        throw new JsonException($"Invalid date format. Expected {DateFormat}.");
    }

    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString(DateFormat));
    }
}
