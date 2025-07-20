using System.Text.Json;
using UndertaleModLib.Models;

namespace YAM2RP;

public class RoomExporter
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

	public static void ExportRoom(UndertaleRoom room, string outPath)
	{
		var roomJson = new RoomJSON()
		{
			Name = room.Name.Content,
			Caption = room.Caption?.Content,
			Width = room.Width,
			Height = room.Height,
			Speed = room.Speed,
			Persistent = room.Persistent,
			BackgroundColor = 0x00FFFFFF & room.BackgroundColor, // Remove alpha channel
			DrawBackgroundColor = room.DrawBackgroundColor,
			CreationCodeId = room.CreationCodeId?.Name.Content,
			Flags = (int)room.Flags,
			World = room.World,
			Top = room.Top,
			Left = room.Left,
			Right = room.Right,
			Bottom = room.Bottom,
			GravityX = room.GravityX,
			GravityY = room.GravityY,
			MetersPerPixel = room.MetersPerPixel,
			Backgrounds = room.Backgrounds.Select(Background.ConvertFromUnderBackground).ToList(),
			GameObjects = room.GameObjects.Select(RoomGameObject.ConvertFromUnderObject).ToList(),
			Tiles = room.Tiles.Select(RoomTile.ConvertFromUnderTile).ToList(),
			Views = room.Views.Select(View.ConvertFromUnderView).ToList()
		};

		using var fs = File.Create(outPath);
		JsonSerializer.Serialize(fs, roomJson, CreateSerializerOptions());
	}
}
