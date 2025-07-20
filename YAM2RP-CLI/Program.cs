namespace YAM2RP;

class Program
{
	public static int Main(string[] args)
	{
		var parser = new CommandLineParser();
		var action = parser.ParseArguments(args);
		if (action is null)
		{
			WriteUsageText();
			return 1;
		}
		return action.Run();
	}

	static void WriteUsageText()
	{
		Console.WriteLine("YAM2RP: Yet Another Metroid 2 Remake Patcher");
		Console.WriteLine();
		Console.WriteLine("Expected usage:");
		Console.WriteLine("yam2rp <dataPath> <yam2rpPath> <outPath> [-f]");
		Console.WriteLine("yam2rp export <assetType> <pattern> <dataPath>");
		Console.WriteLine();
		Console.WriteLine("Definitions:");
		Console.WriteLine("<dataPath>: Path to unpatched data.win");
		Console.WriteLine("<yam2rpPath>: Path to YAM2RP project");
		Console.WriteLine("<outPath>: Path to place the patched data.win");
		Console.WriteLine("[-f]: Optional, overwrites the output path even if it already exists");
		Console.WriteLine("<assetType>: Type of asset to export, object or room");
		Console.WriteLine("<pattern>: Name of the asset to export");
	}
}