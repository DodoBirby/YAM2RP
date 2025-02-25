using UndertaleModLib;

class Program
{
	// Expected syntax: yam2rp.exe data.win yam2rp_project_path
	public static void Main(string[] args)
	{
		if (args.Length != 2)
		{
			throw new Exception("Expected 2 arguments");
		}
		var undertaleDataPath = args[0];
		var yam2rpPath = args[1];
		using (var fileStream = File.OpenRead(undertaleDataPath))
		{
			var data = UndertaleIO.Read(fileStream);
		}
	}
}