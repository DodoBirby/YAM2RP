using System.Text.Json;
using UndertaleModLib;
using UndertaleModLib.Models;

namespace YAM2RP;

public class ObjectImporter
{
	static readonly JsonSerializerOptions serializerOptions = CreateSerializerOptions();
	
	static readonly List<GameObjectJSON> gameObjectJSONs = [];
	// TODO: Clean up these functions
	static JsonSerializerOptions CreateSerializerOptions()
	{
		var options = new JsonSerializerOptions
		{
			PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
		};
		return options;
	}

	public static void ImportObjectBodies(UndertaleData data)
	{
		foreach (var gameObject in gameObjectJSONs)
		{
			var existingObject = data.GameObjects.ByName(gameObject.Name) ?? throw new Exception("Object should exist at this point");
			existingObject.Visible = gameObject.Visible;
			existingObject.Solid = gameObject.Solid;
			existingObject.Depth = gameObject.Depth;
			existingObject.Persistent = gameObject.Persistent;
			existingObject.UsesPhysics = gameObject.UsesPhysics;
			existingObject.IsSensor = gameObject.IsSensor;
			existingObject.CollisionShape = (CollisionShapeFlags)gameObject.CollisionShape;
			existingObject.Density = gameObject.Density;
			existingObject.Restitution = gameObject.Restitution;
			existingObject.Group = gameObject.Group;
			existingObject.LinearDamping = gameObject.LinearDamping;
			existingObject.AngularDamping = gameObject.AngularDamping;
			existingObject.Friction = gameObject.Friction;
			existingObject.Awake = gameObject.Awake;
			existingObject.Kinematic = gameObject.Kinematic;
			existingObject.Sprite = data.Sprites.NameLookupIfNotNull(gameObject.Sprite);
			existingObject.ParentId = data.GameObjects.NameLookupIfNotNull(gameObject.ParentId);
			existingObject.TextureMaskId = data.Sprites.NameLookupIfNotNull(gameObject.TextureMaskId);
			existingObject.PhysicsVertices.Clear();
			foreach (var vert in gameObject.PhysicsVertices)
			{
				existingObject.PhysicsVertices.Add(vert.ConvertToUnderVertex());
			}
		}
	}

	public static void ImportObjectNamesAndMethods(UndertaleData data, string objectPath)
	{
		var underGameObjects = new List<UndertaleGameObject>();
		foreach (var file in Directory.EnumerateFiles(objectPath, "*.json", SearchOption.AllDirectories))
		{
			var stream = File.OpenRead(file);
			var gameObject = JsonSerializer.Deserialize<GameObjectJSON>(stream, serializerOptions) ?? throw new Exception($"Failed to deserialise game object from {file}");
			gameObjectJSONs.Add(gameObject);
			var underGameObject = AddObjectIfNewName(gameObject);
			underGameObjects.Add(underGameObject);
		}

		foreach ((var gameObject, var underGameObject) in gameObjectJSONs.Zip(underGameObjects))
		{
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
				result = new UndertaleGameObject
				{
					Name = data.Strings.MakeString(name)
				};
				data.GameObjects.Add(result);
			}
			return result;
		}
	}
}
