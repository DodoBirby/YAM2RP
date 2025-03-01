﻿using System.Text.Json.Serialization;
using UndertaleModLib;
using UndertaleModLib.Models;
using System.Linq;

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
	public double Density { get; set; }
	public double Restitution {  get; set; }
	public int Group {  get; set; }
	public double LinearDamping { get; set; }
	public double AngularDamping { get; set; }
	public double Friction { get; set; }
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
		if (actionName == "")
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
}

public class PhysicsVertex
{
	public double X { get; set; }
	public double Y { get; set; }
}
