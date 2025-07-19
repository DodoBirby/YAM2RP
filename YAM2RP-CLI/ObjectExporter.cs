using System.Text.Json;
using UndertaleModLib;
using UndertaleModLib.Models;

namespace YAM2RP;

public class ObjectExporter
{
	static JsonSerializerOptions CreateSerializerOptions()
	{
		var options = new JsonSerializerOptions
		{
			PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
			WriteIndented = true,
		};
		return options;
	}

	public static void ExportGameObject(UndertaleData data, UndertaleGameObject obj, string outPath)
	{
		var jsonObject = new GameObjectJSON()
		{
			Name = obj.Name.Content,
			Sprite = obj.Sprite?.Name.Content,
			Visible = obj.Visible,
			Solid = obj.Solid,
			Depth = obj.Depth,
			Persistent = obj.Persistent,
			ParentId = obj.ParentId?.Name.Content,
			TextureMaskId = obj.TextureMaskId?.Name.Content,
			UsesPhysics = obj.UsesPhysics,
			IsSensor = obj.IsSensor,
			CollisionShape = (int)obj.CollisionShape,
			Density = obj.Density,
			Restitution = obj.Restitution,
			Group = obj.Group,
			LinearDamping = obj.LinearDamping,
			AngularDamping = obj.AngularDamping,
			Friction = obj.Friction,
			Awake = obj.Awake,
			Kinematic = obj.Kinematic,
			PhysicsVertices = obj.PhysicsVertices.Select(PhysicsVertex.ConvertFromUnderVertex).ToList(),
			Events = obj.Events.Select((x, eventIndex) =>
				x.Select(y => ObjectEvent.ConvertFromUnderEvent(y, eventIndex, data))
				.ToList())
			.ToList()
		};

		using var fs = File.Create(outPath);
		JsonSerializer.Serialize(fs, jsonObject, CreateSerializerOptions());
	}
}
