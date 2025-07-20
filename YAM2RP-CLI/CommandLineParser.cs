namespace YAM2RP;

public class CommandLineParser
{
	public IYam2rpAction ParseArguments(string[] args)
	{
		return TryParseExportArgs(args).Chain(() => TryParsePatchArgs(args));
	}

	static IYam2rpAction TryParsePatchArgs(string[] args)
	{
		var forceOverwrite = args.Contains("-f");
		var filteredArgs = args.Where(x => x != "-f").ToList();
		if (filteredArgs is [var dataPath, var yam2rpPath, var outPath])
		{
			return new PatchAction(yam2rpPath, dataPath, outPath, forceOverwrite);
		}

		if (filteredArgs.Count > 3)
		{
			return new ErrorAction($"Couldn't parse as Patch Command: Unexpected argument for patch command! Got: {filteredArgs[3]}");
		}
		else
		{
			return new ErrorAction("Couldn't parse as Patch Command: Too few arguments for patch command!");
		}
	}

	static IYam2rpAction TryParseExportArgs(string[] args)
	{
		if (args.Length < 4)
		{
			return new ErrorAction("Couldn't parse as Export Command: Too few arguments for export command!");
		}

		if (args.Length > 5)
		{
			return new ErrorAction($"Couldn't parse as Export Command: Unexpected argument for export command! Got: {args[5]}");
		}

		return TryParseExportArgsWithNoOutPath(args).Chain(() => TryParseExportArgsWithOutPath(args));
	}

	static IYam2rpAction TryParseExportArgsWithNoOutPath(string[] args)
	{
		if (args is ["export-from", var dataPath, var assetType, var pattern])
		{
			if (assetType != "room" && assetType != "object")
			{
				return new ErrorAction($"Couldn't parse as Export Command: Invalid assetType {assetType}! Only room and object are supported");
			}

			if (pattern.Count(x => x == '*') > 1)
			{
				return new ErrorAction("Couldn't parse as Export Command: Only 1 '*' character is supported in patterns");
			}
			return new ExportAction(assetType, dataPath, pattern, "Exports");
		}
		return new ErrorAction("");
	}

	static IYam2rpAction TryParseExportArgsWithOutPath(string[] args)
	{
		if (args is ["export-from", var dataPath, var assetType, var pattern, var outPath])
		{
			if (assetType != "room" && assetType != "object")
			{
				return new ErrorAction($"Couldn't parse as Export Command: Invalid assetType {assetType}! Only room and object are supported");
			}

			if (pattern.Count(x => x == '*') > 1)
			{
				return new ErrorAction("Couldn't parse as Export Command: Only 1 '*' character is supported in patterns");
			}
			return new ExportAction(assetType, dataPath, pattern, outPath);
		}
		return new ErrorAction("");
	}
}
