using UndertaleModLib;

namespace YAM2RP;

public class ExportAction(string assetType, string pattern, string dataPath) : IYam2rpAction
{
	public int Run()
	{
		if (assetType != "room" && assetType != "object")
		{
			Console.WriteLine($"Cannot export asset type {assetType}! Only room and object are supported");
			return 1;
		}
		UndertaleData data;
		using (var fs = File.OpenRead(dataPath))
		{
			data = UndertaleIO.Read(fs);
		}
		switch (assetType)
		{
			case "object":
				ExportObjects(pattern, data);
				break;
			case "room":
				ExportRooms(pattern, data);
				break;
		}
		return 0;
	}

	void ExportRooms(string pattern, UndertaleData data)
	{
		foreach (var room in data.Rooms)
		{
			if (room.Name.Content == pattern)
			{
				Console.WriteLine($"Exporting {room.Name.Content} to {room.Name.Content}.json");
				RoomExporter.ExportRoom(room, $"{room.Name.Content}.json");
			}
		}
	}

	void ExportObjects(string pattern, UndertaleData data)
	{
		foreach (var obj in data.GameObjects)
		{
			if (obj.Name.Content == pattern)
			{
				Console.WriteLine($"Exporting {obj.Name.Content} to {obj.Name.Content}.json");
				ObjectExporter.ExportGameObject(data, obj, $"{obj.Name.Content}.json");
			}
		}
	}
}
