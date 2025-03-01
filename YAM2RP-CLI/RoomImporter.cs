using System.Text.Json;
using UndertaleModLib;
using UndertaleModLib.Models;
using UndertaleModLib.Scripting;

namespace YAM2RP;

public class RoomImporter
{
	public static void ImportRoomNames(UndertaleData data, string roomPath)
	{
		foreach (var file in Directory.EnumerateFiles(roomPath, "*.json", SearchOption.AllDirectories))
		{
			var stream = File.OpenRead(file);
			var jsonUtf8Bytes = new byte[stream.Length];

			stream.Read(jsonUtf8Bytes, 0, jsonUtf8Bytes.Length);
			stream.Close();

			var options = new JsonReaderOptions
			{
				AllowTrailingCommas = true,
				CommentHandling = JsonCommentHandling.Skip
			};

			var reader = new Utf8JsonReader(jsonUtf8Bytes, options);

			ReadAnticipateJSONObject(ref reader, JsonTokenType.StartObject);

			AddRoomIfNewName(ref reader);
		}
		
		void AddRoomIfNewName(ref Utf8JsonReader reader)
		{
			var name = ReadString(ref reader) ?? throw new Exception("ERROR: Object name was null - object name must be defined!");
			if (data.Rooms.ByName(name) != null)
			{
				return;
			}
			else
			{
				var newRoom = new UndertaleRoom
				{
					Name = new UndertaleString(name)
				};
				data.Strings.Add(newRoom.Name);
				data.Rooms.Add(newRoom);
			}
		}

		string? ReadString(ref Utf8JsonReader reader)
		{
			while (reader.Read())
			{
				switch (reader.TokenType)
				{
					case JsonTokenType.PropertyName: continue;
					case JsonTokenType.String: return reader.GetString()!;
					case JsonTokenType.Null: return null;
					default: throw new Exception($"ERROR: Unexpected token type. Expected String - found {reader.TokenType}");
				}
			}

			throw new ScriptException("ERROR: Did not find value of expected type. Expected String.");
		}

		void ReadAnticipateJSONObject(ref Utf8JsonReader reader, JsonTokenType allowedTokenType)
		{
			while (reader.Read())
			{
				if (reader.TokenType == JsonTokenType.PropertyName)
					continue;
				if (reader.TokenType == allowedTokenType)
					return;
				throw new Exception($"ERROR: Unexpected token type. Expected {allowedTokenType} - found {reader.TokenType}");
			}

			throw new Exception("ERROR: Did not find value of expected type. Expected String.");
		}
	}
}
