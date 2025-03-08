using System.Text.Json;
using UndertaleModLib;
using UndertaleModLib.Models;

namespace YAM2RP;

public class RoomImporter
{
	static readonly JsonSerializerOptions serializerOptions = CreateSerializerOptions();

	static readonly List<RoomJSON> roomJSONs = [];

	static JsonSerializerOptions CreateSerializerOptions()
	{
		var options = new JsonSerializerOptions
		{
			PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
		};
		return options;
	}

	public static void ImportRoomBodies(UndertaleData data)
	{
		foreach (var room in roomJSONs)
		{
			var existingRoom = data.Rooms.ByName(room.Name) ?? throw new Exception("Room should exist by this point");
			existingRoom.Width = room.Width;
			existingRoom.Height = room.Height;
			existingRoom.Speed = room.Speed;
			existingRoom.Persistent = room.Persistent;
			existingRoom.BackgroundColor = 0xFF000000 | room.BackgroundColor;
			existingRoom.DrawBackgroundColor = room.DrawBackgroundColor;
			existingRoom.Flags = (UndertaleRoom.RoomEntryFlags)room.Flags;
			existingRoom.World = room.World;
			existingRoom.Top = room.Top;
			existingRoom.Left = room.Left;
			existingRoom.Right = room.Right;
			existingRoom.Bottom = room.Bottom;
			existingRoom.GravityX = room.GravityX;
			existingRoom.GravityY = room.GravityY;
			existingRoom.MetersPerPixel = room.MetersPerPixel;
			existingRoom.Caption = string.IsNullOrEmpty(room.Caption) ? null : data.Strings.MakeString(room.Caption);
			existingRoom.CreationCodeId = data.Code.NameLookupIfNotNull(room.CreationCodeId);
			existingRoom.Backgrounds.Clear();
			foreach (var background in room.Backgrounds)
			{
				existingRoom.Backgrounds.Add(background.ConvertToUnderBackground(data, existingRoom));
			}
			existingRoom.Views.Clear();
			foreach (var view in room.Views)
			{
				existingRoom.Views.Add(view.ConvertToUnderView(data));
			}
			existingRoom.GameObjects.Clear();
			foreach (var obj in room.GameObjects)
			{
				existingRoom.GameObjects.Add(obj.ConvertToUnderObject(data));
			}
			existingRoom.Tiles.Clear();
			foreach (var tile in room.Tiles)
			{
				existingRoom.Tiles.Add(tile.ConvertToUnderTile(data));
			}
			// TODO: Layers here
		}
	}

	static void AddRoomIfNewName(UndertaleData data, RoomJSON room)
	{
		if (data.Rooms.ByName(room.Name) != null)
		{
			return;
		}
		var newRoom = new UndertaleRoom
		{
			Name = data.Strings.MakeString(room.Name)
		};
		data.Rooms.Add(newRoom);
	}

	public static void ImportRoomNames(UndertaleData data, string roomPath)
	{
		foreach (var file in Directory.EnumerateFiles(roomPath, "*.json", SearchOption.AllDirectories))
		{
			var stream = File.OpenRead(file);
			var room = JsonSerializer.Deserialize<RoomJSON>(stream, serializerOptions) ?? throw new Exception($"Failed to deserialize room from {file}");
			roomJSONs.Add(room);
			AddRoomIfNewName(data, room);
		}
	}
}
