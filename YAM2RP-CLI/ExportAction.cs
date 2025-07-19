using UndertaleModLib;

namespace YAM2RP;

public class ExportAction(string assetType, string pattern, string dataPath) : IYam2rpAction
{
	public void Run()
	{
		if (assetType != "room" && assetType != "object")
		{
			Console.WriteLine($"Cannot export asset type {assetType}! Only room and object are supported");
			return;
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
