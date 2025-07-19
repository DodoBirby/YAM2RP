using System.Text.Json;
using System.Text.Json.Serialization;

namespace YAM2RP;

public class EventSubtypeJSONConverter : JsonConverter<string>
{
	public override string Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		switch (reader.TokenType)
		{
			case JsonTokenType.String:
				return reader.GetString() ?? throw new JsonException("Event subtype is null");
			case JsonTokenType.Number:
				return reader.GetInt32().ToString();
			default:
				throw new JsonException("Event subtype should be a string or a number");
		}
	}

	public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options)
	{
		writer.WriteStringValue(value);
	}
}
