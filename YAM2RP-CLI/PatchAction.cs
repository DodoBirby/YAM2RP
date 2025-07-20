using System.Diagnostics;
using UndertaleModLib;

namespace YAM2RP;

public class PatchAction(string yam2rpPath, string dataPath, string outputPath, bool forceOverwrite) : IYam2rpAction
{
	public int Run()
	{
		Console.WriteLine("Running Patch command...");

		if (!File.Exists(dataPath))
		{
			Console.Error.WriteLine($"data.win path {dataPath} does not exist! exiting...");
			return 1;
		}

		if (!Directory.Exists(yam2rpPath))
		{
			Console.Error.WriteLine($"YAM2RP project path {yam2rpPath} does not exist! exiting...");
			return 1;
		}

		if (File.Exists(outputPath) && !forceOverwrite)
		{
			Console.WriteLine($"WARNING: {outputPath} already exists! Would you like to overwrite it? Y/N");
			if (!GetYesOrNoFromUser())
			{
				Console.Error.WriteLine("User chose not to overwrite, exiting...");
				return 1;
			}
		}

		if (File.Exists(outputPath))
		{
			Console.WriteLine($"{outputPath} will be overwritten");
		}

		UndertaleData data;
		using (var fileStream = File.OpenRead(dataPath))
		{
			data = UndertaleIO.Read(fileStream);
		}
		var sw = new Stopwatch();
		sw.Start();
		Patcher.Patch(data, yam2rpPath);
		using (var fileStream = File.Open(outputPath, FileMode.Create))
		{
			Console.WriteLine($"Writing modified data to {outputPath}");
			UndertaleIO.Write(fileStream, data);
			Console.WriteLine("Finished writing");
		}
		sw.Stop();
		Console.WriteLine($"Patching Complete in {sw.Elapsed.TotalSeconds}s");
		return 0;
	}

	static bool GetYesOrNoFromUser()
	{
		while (true)
		{
			var key = Console.ReadKey();
			if (key.KeyChar == 'y' || key.KeyChar == 'Y')
			{
				return true;
			}
			if (key.KeyChar == 'n' || key.KeyChar == 'N')
			{
				return false;
			}
			Console.WriteLine("Must type a Y or N");
		}
	}

	public IYam2rpAction Chain(Func<IYam2rpAction> next)
	{
		return this;
	}
}
