namespace YAM2RP;

public class CommandLineParser
{
	public IYam2rpAction? ParseArguments(string[] args)
	{
		return TryParsePatchArgs(args)
			?? TryParseExportArgs(args);
	}

	static IYam2rpAction? TryParsePatchArgs(string[] args)
	{
		var forceOverwrite = args.Contains("-f");
		var filteredArgs = args.Where(x => x != "-f").ToList();
		if (filteredArgs is [var dataPath, var yam2rpPath, var outPath])
		{
			return new PatchAction(yam2rpPath, dataPath, outPath, forceOverwrite);
		}
		return null;
	}

	static IYam2rpAction? TryParseExportArgs(string[] args)
	{
		return TryParseExportArgsWithNoOutPath(args)
			?? TryParseExportArgsWithOutPath(args);
	}

	static IYam2rpAction? TryParseExportArgsWithNoOutPath(string[] args)
	{
		if (args is ["export-from", var dataPath, var assetType, var pattern])
		{
			return new ExportAction(assetType, dataPath, pattern, "Exports");
		}
		return null;
	}

	static IYam2rpAction? TryParseExportArgsWithOutPath(string[] args)
	{
		if (args is ["export-from", var dataPath, var assetType, var pattern, var outPath])
		{
			return new ExportAction(assetType, dataPath, pattern, outPath);
		}
		return null;
	}
}
