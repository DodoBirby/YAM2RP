using System.Diagnostics;
using UndertaleModLib;

namespace YAM2RP;

public class PatchAction(string yam2rpPath, string dataPath, string outputPath, bool forceOverwrite) : IYam2rpAction
{
	public void Run()
	{
		if (!File.Exists(dataPath))
		{
			Console.Error.WriteLine($"{dataPath} does not exist! exiting...");
			return;
		}

		if (!Directory.Exists(yam2rpPath))
		{
			Console.Error.WriteLine($"{yam2rpPath} does not exist! exiting...");
			return;
		}

		if (File.Exists(outputPath) && !forceOverwrite)
		{
			Console.WriteLine($"WARNING: {outputPath} already exists! Would you like to overwrite it? Y/N");
			if (!GetYesOrNoFromUser())
			{
				Console.Error.WriteLine("User chose not to overwrite, exiting...");
				return;
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
}
