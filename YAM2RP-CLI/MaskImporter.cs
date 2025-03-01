using UndertaleModLib;
using UndertaleModLib.Util;

namespace YAM2RP;

public class MaskImporter
{
	// TODO: Clean up, this is really some wack code
	public static void ImportMasks(UndertaleData data, string maskPath)
	{
		var dirFiles = Directory.GetFiles(maskPath, "*.png", SearchOption.AllDirectories);
		foreach (var file in dirFiles)
		{
			var fileNameWithExtension = Path.GetFileName(file);
			if (!fileNameWithExtension.EndsWith(".png"))
			{
				continue;
			}
			var stripped = Path.GetFileNameWithoutExtension(file);
			var lastUnderscore = stripped.LastIndexOf('_');
			if (lastUnderscore == -1)
			{
				throw new Exception($"Failed to get sprite name of {fileNameWithExtension}");
			}
			var spriteName = stripped.Substring(0, lastUnderscore);
			var sprite = data.Sprites.ByName(spriteName);
			if (sprite == null)
			{
				throw new Exception($"{fileNameWithExtension} could not be imported as the sprite does not exist");
			}
			var (width, height) = TextureWorker.GetImageSizeFromFile(fileNameWithExtension);
			if (sprite.Width != width || sprite.Height != height)
			{
				throw new Exception($"{fileNameWithExtension} is not the proper size");
			}

			if (!int.TryParse(stripped.AsSpan(lastUnderscore + 1), out var frame))
			{
				throw new Exception($"The index of {fileNameWithExtension} could not be determined");
			}
			var prevframe = 0;
			if (frame != 0)
			{
				prevframe = frame - 1;
			}
			if (frame < 0)
			{
				throw new Exception($"{spriteName} is using an invalid numbering scheme");
			}
			var prevFrameName = $"{spriteName}_{prevframe}.png";
			var previousFrameFiles = Directory.GetFiles(maskPath, prevFrameName, SearchOption.AllDirectories);
			if (previousFrameFiles.Length < 1)
			{
				throw new Exception($"{spriteName} is missing one or more indexes. The detected missing index is: {prevFrameName}");
			}
		}

		foreach (var file in dirFiles)
		{
			var fileNameWithExtension = Path.GetFileName(file);
			if (!fileNameWithExtension.EndsWith(".png"))
			{
				continue;
			}
			var stripped = Path.GetFileNameWithoutExtension(file);
			var lastUnderscore = stripped.LastIndexOf('_');
			var spriteName = stripped.Substring(0, lastUnderscore);
			var frame = int.Parse(stripped.AsSpan(lastUnderscore + 1));
			var sprite = data.Sprites.ByName(spriteName);
			var collisionMaskCount = sprite.CollisionMasks.Count;
			while (collisionMaskCount <= frame)
			{
				sprite.CollisionMasks.Add(sprite.NewMaskEntry());
				collisionMaskCount++;
			}
			try
			{
				sprite.CollisionMasks[frame].Data = TextureWorker.ReadMaskData(file, (int)sprite.Width, (int)sprite.Height);
			}
			catch
			{
				throw new Exception($"{fileNameWithExtension} has an error preventing import");
			}
		}
	}
}
