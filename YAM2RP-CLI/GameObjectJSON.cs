using System.Text.Json.Serialization;
using UndertaleModLib;
using UndertaleModLib.Models;

namespace YAM2RP;

public class GameObjectJSON
{
	public string Name { get; set; } = "";
	public string? Sprite { get; set; }
	public bool Visible { get; set; }
	public bool Solid { get; set; }
	public int Depth { get; set; }
	public bool Persistent { get; set; }
	public string? ParentId { get; set; }
	public string? TextureMaskId { get; set; }
	public bool UsesPhysics { get; set; }
	public bool IsSensor { get; set; }
	public int CollisionShape { get; set; }
	public float Density { get; set; }
	public float Restitution {  get; set; }
	public uint Group {  get; set; }
	public float LinearDamping { get; set; }
	public float AngularDamping { get; set; }
	public float Friction { get; set; }
	public bool Awake { get; set; }
	public bool Kinematic { get; set; }
	public List<PhysicsVertex> PhysicsVertices { get; set; } = [];
	public List<List<ObjectEvent>> Events { get; set; } = [];
}

public class ObjectAction
{
	public int LibId { get; set; }
	public int Id { get; set; }
	public int Kind { get; set; }
	public bool UseRelative { get; set; }
	public bool IsQuestion { get; set; }
	public bool UseApplyTo { get; set; }
	public int ExeType { get; set; }
	public string ActionName { get; set; } = "";
	public string? CodeId { get; set; }
	public int ArgumentCount { get; set; }
	public int Who { get; set; }
	public bool Relative { get; set; }
	public bool IsNot { get; set; }

	public UndertaleGameObject.EventAction ConvertToUnderAction(UndertaleData data)
	{
		var newAction = new UndertaleGameObject.EventAction
		{
			LibID = (uint)LibId,
			ID = (uint)Id,
			Kind = (uint)Kind,
			UseRelative = UseRelative,
			IsQuestion = IsQuestion,
			UseApplyTo = UseApplyTo,
			ExeType = (uint)ExeType,
			ArgumentCount = (uint)ArgumentCount,
			Who = Who,
			Relative = Relative,
			IsNot = IsNot
		};

		var actionName = ActionName;
		if (string.IsNullOrEmpty(actionName))
		{
			newAction.ActionName = null;
		}
		else
		{
			newAction.ActionName = data.Strings.MakeString(actionName);
		}
		var codeId = CodeId;
		if (codeId == null)
		{
			newAction.CodeId = null;
		}
		else
		{
			newAction.CodeId = data.Code.ByName(codeId);
		}
		return newAction;
	}

	public static ObjectAction ConvertFromUnderAction(UndertaleGameObject.EventAction eventAction)
	{
		var newAction = new ObjectAction()
		{
			LibId = (int)eventAction.LibID,
			Id = (int)eventAction.ID,
			Kind = (int)eventAction.Kind,
			UseRelative = eventAction.UseRelative,
			IsQuestion = eventAction.IsQuestion,
			UseApplyTo= eventAction.UseApplyTo,
			ExeType = (int)eventAction.ExeType,
			ArgumentCount = (int)eventAction.ArgumentCount,
			Who = eventAction.Who,
			Relative = eventAction.Relative,
			IsNot = eventAction.IsNot,
			ActionName = eventAction.ActionName.Content,
			CodeId = eventAction.CodeId.Name.Content
		};

		return newAction;
	}
}

public class ObjectEvent
{
	[JsonConverter(typeof(EventSubtypeJSONConverter))]
	public string EventSubtype { get; set; } = "";
	public List<ObjectAction> Actions { get; set; } = [];

	public UndertaleGameObject.Event ConvertToUnderEvent(UndertaleData data)
	{
		var newEvent = new UndertaleGameObject.Event();
		if (uint.TryParse(EventSubtype, out var num))
		{
			newEvent.EventSubtype = num;
		}
		else
		{
			newEvent.EventSubtype = (uint)data.GameObjects.FindIndex(x => x.Name.Content == EventSubtype);
		}
		newEvent.Actions.Clear();
		foreach (var action in Actions)
		{
			newEvent.Actions.Add(action.ConvertToUnderAction(data));
		}
		return newEvent;
	}

	const int CollisionEventIndex = 4;

	public static ObjectEvent ConvertFromUnderEvent(UndertaleGameObject.Event underEvent, int eventIndex, UndertaleData data)
	{
		var newEvent = new ObjectEvent();
		if (eventIndex == CollisionEventIndex)
		{
			newEvent.EventSubtype = data.GameObjects[(int)underEvent.EventSubtype].Name.Content;
		}
		else
		{
			newEvent.EventSubtype = underEvent.EventSubtype.ToString();
		}
		newEvent.Actions = underEvent.Actions.Select(ObjectAction.ConvertFromUnderAction).ToList();
		return newEvent;
	}
}

public class PhysicsVertex
{
	public float X { get; set; }
	public float Y { get; set; }

	public UndertaleGameObject.UndertalePhysicsVertex ConvertToUnderVertex()
	{
		var newVert = new UndertaleGameObject.UndertalePhysicsVertex()
		{
			X = X,
			Y = Y
		};
		return newVert;
	}

	public static PhysicsVertex ConvertFromUnderVertex(UndertaleGameObject.UndertalePhysicsVertex vert)
	{
		var newVert = new PhysicsVertex()
		{
			X = vert.X,
			Y = vert.Y
		};
		return newVert;
	}
}
