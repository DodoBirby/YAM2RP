using CommandLine;
using CommandLine.Text;
using System.Diagnostics;
using UndertaleModLib;

namespace YAM2RP;

class Program
{
	public static void Main(string[] args)
	{
		var parser = new Parser(with => with.HelpWriter = null);
		var result = parser.ParseArguments<Options>(args);
		result
			.WithParsed(Run)
			.WithNotParsed(errs => DisplayHelpMenu(result, errs));
	}

	static void DisplayHelpMenu(ParserResult<Options> result, IEnumerable<Error> errors)
	{
		var helpText = HelpText.AutoBuild(result, h =>
		{
			h.Heading = "YAM2RP";
			h.Copyright = "";
			h.AddDashesToOption = true;
			h.AdditionalNewLineAfterOption = false;
			h.AddPreOptionsLine("Usage: yam2rp [-f] <Data Path> <YAM2RP Project Path> <Output Path>");
			return h;
		});
		Console.WriteLine(helpText);
	}

	static void Run(Options options)
	{
		var undertaleDataPath = options.DataPath;
		var yam2rpPath = options.Yam2rpFolder;
		var outputPath = options.OutputPath;
		var forceOverwrite = options.ForceOverWrite;
		if (!File.Exists(undertaleDataPath))
		{
			Console.Error.WriteLine($"{undertaleDataPath} does not exist! exiting...");
			Environment.Exit(1);
		}
		if (!Directory.Exists(yam2rpPath))
		{
			Console.Error.WriteLine($"{yam2rpPath} does not exist! exiting...");
			Environment.Exit(1);
		}
		if (File.Exists(outputPath) && !forceOverwrite)
		{
			Console.WriteLine($"WARNING: {outputPath} already exists! Would you like to overwrite it? Y/N");
			if (!GetYesOrNoFromUser())
			{
				Console.Error.WriteLine("User chose not to overwrite, exiting...");
				Environment.Exit(1);
			}
		}
		if (File.Exists(outputPath))
		{
			Console.WriteLine($"{outputPath} will be overwritten");
		}
		UndertaleData data;
		using (var fileStream = File.OpenRead(undertaleDataPath))
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

class Options
{
	[Value(0, Required = true, HelpText = "Path to base data.win", MetaName = "Data Path")]
	public string DataPath { get; set; } = "";

	[Value(1, Required = true, HelpText = "Path to YAM2RP project folder", MetaName = "Yam2rp Project Path")]
	public string Yam2rpFolder { get; set; } = "";

	[Value(2, Required = true, HelpText = "Path to write output data.win", MetaName = "Output Path")]
	public string OutputPath { get; set; } = "";

	[Option('f', HelpText = "Overwrite the output file without asking")]
	public bool ForceOverWrite { get; set; }
}