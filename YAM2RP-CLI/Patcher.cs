using UndertaleModLib;
using UndertaleModLib.Models;
using UndertaleModLib.Scripting;

namespace YAM2RP
{
	public class Patcher
	{
		public static void Patch(UndertaleData data, string yam2rpPath)
		{
			var soundPath = Path.Combine(yam2rpPath, "Sounds");
			var graphicsPath = Path.Combine(yam2rpPath, "Graphics");
			var maskPath = Path.Combine(yam2rpPath, "Masks");
			var objectPath = Path.Combine(yam2rpPath, "Objects");
			var roomPath = Path.Combine(yam2rpPath, "Rooms");
			var scriptPath = Path.Combine(yam2rpPath, "Code");

			var nameReplacePath = Path.Combine(yam2rpPath, "Replace.txt");
			var spriteOptionsPath = Path.Combine(yam2rpPath, "SpriteOptions.txt");

			if (File.Exists(nameReplacePath))
			{
				Console.WriteLine("Replacing names...");
				ReplaceNames(data, File.ReadAllLines(nameReplacePath));
				Console.WriteLine("Finished replacing names");
			}

			if (Directory.Exists(graphicsPath))
			{

			}

			if (Directory.Exists(maskPath))
			{
				Console.WriteLine("Importing collision masks");
				MaskImporter.ImportMasks(data, maskPath);
				Console.WriteLine("Finished importing collision masks");
			}
		}

		static void ReplaceNames(UndertaleData data, string[] lines)
		{
			var lineCount = 0;
			foreach (var line in lines)
			{
				lineCount++;
				var trimmedLine = line.Trim();
				if (trimmedLine == "" || trimmedLine[0] == '#')
				{
					continue;
				}
				var splitLine = trimmedLine.Split("->", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
				if (splitLine.Length != 2)
				{
					throw new Exception($"Syntax error in Replace.txt line {lineCount}: Failed to split line on '->'. Correct syntax is [OriginalName] -> [NewName]");
				}
				var originalName = splitLine[0];
				var newName = splitLine[1];
				var res = data.ByName(originalName);
				if (res == null)
				{
					throw new Exception($"Error in Replace.txt line {lineCount}: Couldn't find asset with name {originalName} to replace. Are you sure you are using the right base file?");
				}
				var newRes = data.ByName(newName);
				if (newRes != null)
				{
					throw new Exception($"Error in Replace.txt line {lineCount}: Asset with name {newName} already exists but attempted to rename {originalName} to {newName}");
				}
				res.Name = new UndertaleString(newName);
				data.Strings.Add(res.Name);
			}
		}
	}
}
