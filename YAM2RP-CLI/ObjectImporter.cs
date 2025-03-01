using System.Text.Json;
using UndertaleModLib;
using UndertaleModLib.Models;

namespace YAM2RP;

public class ObjectImporter
{
	static readonly JsonSerializerOptions serializerOptions = CreateSerializerOptions();

	// TODO: Clean up these functions
	static JsonSerializerOptions CreateSerializerOptions()
	{
		var options = new JsonSerializerOptions();
		options.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower;
		return options;
	}

	public static void ImportObjectNamesAndMethods(UndertaleData data, string objectPath)
	{
		foreach (var file in Directory.EnumerateFiles(objectPath, "*.json", SearchOption.AllDirectories))
		{
			var stream = File.OpenRead(file);
			var gameObject = JsonSerializer.Deserialize<GameObjectJSON>(stream, serializerOptions) ?? throw new Exception($"Failed to deserialise game object from {file}");
			var underGameObject = AddObjectIfNewName(gameObject);
			AddObjectMethods(underGameObject, gameObject);
		}

		void AddObjectMethods(UndertaleGameObject underGameObject, GameObjectJSON gameObject)
		{
			var i = -1;
			foreach (var objectEventList in gameObject.Events)
			{
				i++;
				underGameObject.Events[i].Clear();
				foreach (var eventToAdd in objectEventList)
				{
					underGameObject.Events[i].Add(eventToAdd.ConvertToUnderEvent(data));
				}
			}
		}

		UndertaleGameObject AddObjectIfNewName(GameObjectJSON gameObject)
		{
			var name = gameObject.Name;
			var result = data.GameObjects.ByName(name);
			if (result == null)
			{
				result = new UndertaleGameObject();
				result.Name = data.Strings.MakeString(name);
				data.GameObjects.Add(result);
			}
			return result;
		}
	}
}
