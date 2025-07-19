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
		if (args is ["export", var assetType, var pattern, var dataPath])
		{
			return new ExportAction(assetType, pattern, dataPath);
		}
		return null;
	}
}
