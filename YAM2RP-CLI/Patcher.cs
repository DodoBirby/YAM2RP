using UndertaleModLib;
using UndertaleModLib.Models;

namespace YAM2RP;

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
		var displayNamePath = Path.Combine(yam2rpPath, "DisplayName.txt");

		var shouldSetDisplayName = File.Exists(displayNamePath);
		var shouldReplaceNames = File.Exists(nameReplacePath);
		var shouldImportGraphics = Directory.Exists(graphicsPath);
		var shouldImportMasks = Directory.Exists(maskPath);
		var shouldChangeSpriteOptions = File.Exists(spriteOptionsPath);
		var shouldImportCode = Directory.Exists(scriptPath);
		var shouldImportObjects = Directory.Exists(objectPath);
		var shouldImportRooms = Directory.Exists(roomPath);
		var shouldImportSounds = Directory.Exists(soundPath);

		if (shouldSetDisplayName)
		{
			Console.WriteLine("Setting display name");
			ImportDisplayName(data, File.ReadAllLines(displayNamePath));
			Console.WriteLine($"Set display name to {data.GeneralInfo.DisplayName.Content}");
		}

		if (shouldReplaceNames)
		{
			Console.WriteLine("Replacing names");
			ReplaceNames(data, File.ReadAllLines(nameReplacePath));
			Console.WriteLine("Finished replacing names");
		}

		if (shouldImportGraphics)
		{
			Console.WriteLine("Importing Graphics");
			GraphicsImporter.ImportGraphics(data, graphicsPath);
			Console.WriteLine("Imported Graphics");
		}

		if (shouldImportMasks)
		{
			Console.WriteLine("Importing collision masks");
			MaskImporter.ImportMasks(data, maskPath);
			Console.WriteLine("Finished importing collision masks");
		}

		if (shouldChangeSpriteOptions)
		{
			Console.WriteLine("Changing sprite options");
			ImportSpriteOptions(data, File.ReadAllLines(spriteOptionsPath));
			Console.WriteLine("Changed sprite options");
		}

		if (shouldImportSounds)
		{
			Console.WriteLine("Importing sounds");
			SoundImporter.ImportSounds(data, soundPath);
			Console.WriteLine("Imported sounds");
		}

		// Import order is important here
		// We go CodeNames + ScriptNames -> ObjectNames and Linking Object methods to code entries -> Room names -> Code bodies -> Object bodies -> Room bodies
		if (shouldImportCode)
		{
			Console.WriteLine("Importing code names");
			CodeImporter.ImportCodeNames(data, scriptPath);
		}

		if (shouldImportObjects)
		{
			Console.WriteLine("Importing object names");
			ObjectImporter.ImportObjectNamesAndMethods(data, objectPath);
			Console.WriteLine("Imported object names");
		}

		if (shouldImportRooms)
		{
			Console.WriteLine("Importing room names");
			RoomImporter.ImportRoomNames(data, roomPath);
			Console.WriteLine("Imported room names");
		}

		if (shouldImportCode)
		{
			Console.WriteLine("Importing code");
			CodeImporter.ImportCode(data, scriptPath);
		}
		
		if (shouldImportObjects)
		{
			Console.WriteLine("Importing object bodies");
			ObjectImporter.ImportObjectBodies(data);
			Console.WriteLine("Imported object bodies");
		}

		if (shouldImportRooms)
		{
			Console.WriteLine("Importing room bodies");
			RoomImporter.ImportRoomBodies(data);
			Console.WriteLine("Imported room bodies");
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

	static void ImportDisplayName(UndertaleData data, string[] lines)
	{
		foreach (var line in lines)
		{
			var trimmedLine = line.Trim();
			if (trimmedLine == "" || trimmedLine[0] == '#')
			{
				continue;
			}
			var displayName = trimmedLine;
			data.GeneralInfo.DisplayName = data.Strings.MakeString(displayName);
			break;
		}
	}

	// TODO: Use regex or something here to allow for keyword args, users should probably not have to type all 11 args just to change one thing
	static void ImportSpriteOptions(UndertaleData data, string[] lines)
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
			var splitLine = trimmedLine.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
			if (splitLine.Length != 11)
			{
				throw new Exception($"Syntax error in SpriteOptions.txt line {lineCount}: " +
					"Incorrect amount of arguments. Correct syntax is [SpriteName] [MarginLeft] [MarginRight] [MarginBottom] [MarginTop] [Transparent] [Smooth] [Preload] [SepMasks] [OriginX] [OriginY]");
			}
			var spriteName = splitLine[0];
			var sprite = data.Sprites.ByName(spriteName);
			if (sprite == null)
			{
				throw new Exception($"Error in SpriteOptions.txt line {lineCount} Could not find sprite with name {spriteName}");
			}
			if (!int.TryParse(splitLine[1], out var marginLeft))
			{
				throw new Exception($"Syntax error in SpriteOptions.txt line {lineCount}: Argument [MarginLeft] could not be parsed as an integer");
			}
			if (!int.TryParse(splitLine[2], out var marginRight))
			{
				throw new Exception($"Syntax error in SpriteOptions.txt line {lineCount}: Argument [MarginRight] could not be parsed as an integer");
			}
			if (!int.TryParse(splitLine[3], out var marginBottom))
			{
				throw new Exception($"Syntax error in SpriteOptions.txt line {lineCount}: Argument [MarginBottom] could not be parsed as an integer");
			}
			if (!int.TryParse(splitLine[4], out var marginTop))
			{
				throw new Exception($"Syntax error in SpriteOptions.txt line {lineCount}: Argument [MarginTop] could not be parsed as an integer");
			}
			if (!bool.TryParse(splitLine[5], out var transparent))
			{
				throw new Exception($"Syntax error in SpriteOptions.txt line {lineCount}: Argument [Transparent] could not be parsed as an integer");
			}
			if (!bool.TryParse(splitLine[6], out var smooth))
			{
				throw new Exception($"Syntax error in SpriteOptions.txt line {lineCount}: Argument [Smooth] could not be parsed as an integer");
			}
			if (!bool.TryParse(splitLine[7], out var preload))
			{
				throw new Exception($"Syntax error in SpriteOptions.txt line {lineCount}: Argument [Preload] could not be parsed as an integer");
			}
			if (!Enum.TryParse<UndertaleSprite.SepMaskType>(splitLine[8], true, out var result))
			{
				throw new Exception($"Syntax error in SpriteOptions.txt line {lineCount}: Argument [SepMasks] could not be parsed as a sepMaskType");
			}
			var sepMaskType = result;
			if (!int.TryParse(splitLine[9], out var originX))
			{
				throw new Exception($"Syntax error in SpriteOptions.txt line {lineCount}: Argument [OriginX] could not be parsed as an integer");
			}
			if (!int.TryParse(splitLine[10], out var originY))
			{
				throw new Exception($"Syntax error in SpriteOptions.txt line {lineCount}: Argument [OriginY] could not be parsed as an integer");
			}
			sprite.MarginLeft = marginLeft;
			sprite.MarginRight = marginRight;
			sprite.MarginTop = marginTop;
			sprite.MarginBottom = marginBottom;
			sprite.Transparent = transparent;
			sprite.Smooth = smooth;
			sprite.Preload = preload;
			sprite.SepMasks = sepMaskType;
			sprite.OriginX = originX;
			sprite.OriginY = originY;
		}
	}
}
