using UndertaleModLib;

namespace YAM2RP;

public class ExportAction(string assetType, string dataPath, string pattern, string outPath) : IYam2rpAction
{
	public int Run()
	{
		Console.WriteLine("Running Export command...");
		UndertaleData data;
		using (var fs = File.OpenRead(dataPath))
		{
			data = UndertaleIO.Read(fs);
		}
		Directory.CreateDirectory(outPath);
		switch (assetType)
		{
			case "object":
				ExportObjects(pattern, data, outPath);
				break;
			case "room":
				ExportRooms(pattern, data, outPath);
				break;
		}
		return 0;
	}

	static bool PatternMatch(string pattern, string assetName)
	{
		if (!pattern.Contains('*'))
		{
			return pattern == assetName;
		}

		var splitPattern = pattern.Split('*', StringSplitOptions.TrimEntries);

		if (splitPattern is not [var prefix, var suffix])
		{
			throw new ArgumentException("Only 1 '*' character is supported in patterns");
		}

		return assetName.StartsWith(prefix) && assetName.EndsWith(suffix);
	}

	void ExportRooms(string pattern, UndertaleData data, string outPath)
	{
		foreach (var room in data.Rooms)
		{
			var roomName = room.Name.Content;
			if (PatternMatch(pattern, roomName))
			{
				var combinedOutPath = Path.Combine(outPath, $"{roomName}.json");
				Console.WriteLine($"Exporting {roomName} to {combinedOutPath}");
				RoomExporter.ExportRoom(room, combinedOutPath);
			}
		}
	}

	void ExportObjects(string pattern, UndertaleData data, string outPath)
	{
		foreach (var obj in data.GameObjects)
		{
			var objName = obj.Name.Content;
			if (PatternMatch(pattern, objName))
			{
				var combinedOutPath = Path.Combine(outPath, $"{objName}.json");
				Console.WriteLine($"Exporting {objName} to {combinedOutPath}");
				ObjectExporter.ExportGameObject(data, obj, combinedOutPath);
			}
		}
	}

	public IYam2rpAction Chain(Func<IYam2rpAction> next)
	{
		return this;
	}
}
