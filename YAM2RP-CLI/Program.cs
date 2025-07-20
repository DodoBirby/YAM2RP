namespace YAM2RP;

class Program
{
	public static int Main(string[] args)
	{
		if (args.Length == 0)
		{
			WriteUsageText();
			return 1;
		}
		var parser = new CommandLineParser();
		var action = parser.ParseArguments(args);
		return action.Run();
	}

	static void WriteUsageText()
	{
		Console.WriteLine("YAM2RP: Yet Another Metroid 2 Remake Patcher");
		Console.WriteLine();
		Console.WriteLine("Expected usage:");
		Console.WriteLine("Patch Mode: Patches a given data.win");
		Console.WriteLine("yam2rp <dataPath> <yam2rpPath> <outPath> [-f]");
		Console.WriteLine();
		Console.WriteLine("Export Mode: Exports objects or rooms from a data.win into a given folder");
		Console.WriteLine("yam2rp export-from <dataPath> <assetType> <pattern> [<outPath>]");
		Console.WriteLine();
		Console.WriteLine("Definitions:");
		Console.WriteLine("Patch Mode:");
		Console.WriteLine("<dataPath>: Path to unpatched data.win");
		Console.WriteLine("<yam2rpPath>: Path to YAM2RP project");
		Console.WriteLine("<outPath>: Path to place the patched data.win");
		Console.WriteLine("[-f]: Optional, overwrites the output path even if it already exists");
		Console.WriteLine();
		Console.WriteLine("Export Mode:");
		Console.WriteLine("<dataPath>: Path to data.win that should be exported from");
		Console.WriteLine("<assetType>: Type of asset to export, object or room");
		Console.WriteLine("<pattern>: Name of the assets to export, supports matching prefixes/suffixes with * e.g 'rm_a0h*' matches rooms beginning with rm_a0h, while * would match all rooms");
		Console.WriteLine("[<outPath>]: Optional (Defaults to \"Exports\"), Path to folder where exported assets will be placed, if it doesn't exist it will be created, if it does exist then files may be replaced inside the folder.");
	}
}