using System.Text.RegularExpressions;
using UndertaleModLib;
using ImageMagick;
using UndertaleModLib.Models;

namespace YAM2RP;

public partial class GraphicsImporter
{
	static readonly Regex sprFrameRegex = SpriteFrameRegex();

	static List<TextureInfo> ProcessImageFilesAndReplaceExistingTextures(UndertaleData data, IEnumerable<string> files)
	{
		var newTextures = new List<TextureInfo>();
		foreach (var file in files)
		{
			var fileName = Path.GetFileNameWithoutExtension(file);
			var matches = sprFrameRegex.Match(fileName);
			if (!matches.Success)
			{
				throw new Exception($"{file} doesn't follow the correct graphics naming convention");
			}
			var spriteName = matches.Groups[1].Value;
			var frameNumber = int.TryParse(matches.Groups[2].Value, out var result) ? result : 0;
			var type = GetGraphicsType(file);
			var image = FileToImage(file);
			if (AddToExisting(data, spriteName, image, frameNumber, type))
			{
				continue;
			}
			var texInfo = new TextureInfo(spriteName, image, frameNumber, type);
			newTextures.Add(texInfo);
		}
		return newTextures;
	}

	static bool AddToExisting(UndertaleData data, string spriteName, MagickImage image, int frameNumber, GraphicsTypes type)
	{
		if (type == GraphicsTypes.Background)
		{
			var existingBackground = data.Backgrounds.ByName(spriteName);
			if (existingBackground == null)
			{
				return false;
			}
			if (image.Height != existingBackground.Texture.SourceHeight || image.Width != existingBackground.Texture.SourceWidth)
			{
				Console.WriteLine($"WARNING: {spriteName} has size {existingBackground.Texture.SourceWidth}x{existingBackground.Texture.SourceHeight} in the base file but is being replaced by an " +
					$"image of size {image.Width}x{image.Height}, the new image will be automatically resized");
			}
			existingBackground.Texture.ReplaceTexture(image);
			return true;
		}
		if (type == GraphicsTypes.Sprite)
		{
			var existingSprite = data.Sprites.ByName(spriteName);
			if (existingSprite == null)
			{
				return false;
			}
			var existingTexture = existingSprite.Textures.ItemOrNull(frameNumber);
			if (existingTexture == null)
			{
				return false;
			}
			if (image.Height != existingTexture.Texture.SourceHeight || image.Width != existingTexture.Texture.SourceWidth)
			{
				Console.WriteLine($"WARNING: {spriteName} has size {existingTexture.Texture.SourceWidth}x{existingTexture.Texture.SourceHeight} in the base file but is being replaced by an " +
					$"image of size {image.Width}x{image.Height}, the new image will be automatically resized");
			}
			existingTexture.Texture.ReplaceTexture(image);
			return true;
		}
		throw new Exception("Unknown Graphics type");
	}

	static GraphicsTypes GetGraphicsType(string file)
	{
		var directory = Path.GetDirectoryName(file);
		var directoryName = Path.GetFileName(directory);
		if (string.Equals(directoryName, "backgrounds", StringComparison.OrdinalIgnoreCase)
			|| string.Equals(directoryName, "background", StringComparison.OrdinalIgnoreCase))
		{
			return GraphicsTypes.Background;
		}
		else
		{
			return GraphicsTypes.Sprite;
		}
	}

	static MagickImage FileToImage(string file)
	{
		using var stream = File.OpenRead(file);
		return new MagickImage(file);
	}

	static bool CanFit(Atlas atlas, TextureInfo texture, int x, int y)
	{
		if (atlas.Mask[y, x]
			|| atlas.Mask[y, x + texture.Image.Width - 1]
			|| atlas.Mask[y + texture.Image.Height - 1, x]
			|| atlas.Mask[y + texture.Image.Height - 1, x + texture.Image.Width - 1])
		{
			return false;
		}
		for (int dy = 0; dy < texture.Image.Height; dy++)
		{
			for (int dx = 0; dx < texture.Image.Width; dx++)
			{
				if (atlas.Mask[y + dy, x + dx])
				{
					return false;
				}
			}
		}
		return true;
	}

	static void SetMask(Atlas atlas, TextureInfo texture, int x, int y)
	{
		for (int dy = 0; dy < texture.Image.Height; dy++)
		{
			for (int dx = 0; dx < texture.Image.Width; dx++)
			{
				atlas.Mask[y + dy, x + dx] = true;
			}
		}
	}

	static bool AddNode(Atlas atlas, TextureInfo texture)
	{
		for (int y = 0; y < atlas.Mask.GetHeight() - texture.Image.Height + 1; y++)
		{
			for (int x = 0; x < atlas.Mask.GetWidth() - texture.Image.Width + 1; x++)
			{
				if (CanFit(atlas, texture, x, y))
				{
					SetMask(atlas, texture, x, y);
					var node = new Node(x, y, texture);
					atlas.Nodes.Add(node);
					return true;
				}
			}
		}
		return false;
	}

	static List<Atlas> PackTextures(List<TextureInfo> newTextures)
	{
		var atlases = new List<Atlas>();
		var atlas = new Atlas(2048, 2048);
		foreach (var texture in newTextures.OrderByDescending(x => x.Image.Width * x.Image.Height))
		{
			if (!AddNode(atlas, texture))
			{
				atlases.Add(atlas);
				atlas = new Atlas(2048, 2048);
				AddNode(atlas, texture);
			}
		}
		atlases.Add(atlas);
		return atlases;
	}

	static UndertaleSprite AddNewSprite(UndertaleData data, UndertaleTexturePageItem texPageItem, TextureInfo texInfo)
	{
		var newSprite = new UndertaleSprite()
		{
			Name = data.Strings.MakeString(texInfo.Name),
			Width = texInfo.Image.Width,
			Height = texInfo.Image.Height,
			MarginLeft = 0,
			MarginRight = (int)texInfo.Image.Width - 1,
			MarginTop = 0,
			MarginBottom = (int)texInfo.Image.Height - 1,
			OriginX = 0,
			OriginY = 0
		};
		newSprite.CollisionMasks.Add(CreateFilledMaskEntry(newSprite));
		data.Sprites.Add(newSprite);
		return newSprite;
	}

	static UndertaleSprite.MaskEntry CreateFilledMaskEntry(UndertaleSprite sprite)
	{
		var maskEntry = sprite.NewMaskEntry();
		for (int i = 0; i < maskEntry.Data.Length; i++)
		{
			maskEntry.Data[i] = byte.MaxValue;
		}
		return maskEntry;
	}

	static void AddGraphicToDataAndTexturePageItem(UndertaleData data, UndertaleTexturePageItem texPageItem, TextureInfo texInfo)
	{
		texPageItem.ReplaceTexture(texInfo.Image);
		if (texInfo.Type == GraphicsTypes.Background)
		{
			var newBackground = new UndertaleBackground()
			{
				Name = data.Strings.MakeString(texInfo.Name),
				Texture = texPageItem,
				Transparent = false,
				Preload = false
			};
			data.Backgrounds.Add(newBackground);
			return;
		}
		if (texInfo.Type == GraphicsTypes.Sprite)
		{
			var texEntry = new UndertaleSprite.TextureEntry()
			{
				Texture = texPageItem
			};
			var existingSprite = data.Sprites.ByName(texInfo.Name);
			if (existingSprite == null)
			{
				existingSprite = AddNewSprite(data, texPageItem, texInfo);
			}
			while (texInfo.Frame >= existingSprite.Textures.Count)
			{
				existingSprite.Textures.Add(texEntry);
			}
			existingSprite.Textures[texInfo.Frame] = texEntry;
			return;
		}
		throw new Exception("Unknown Graphics type");
	}

	public static void ImportGraphics(UndertaleData data, string graphicsPath)
	{
		var newTextures = ProcessImageFilesAndReplaceExistingTextures(data, Directory.EnumerateFiles(graphicsPath, "*.png", SearchOption.AllDirectories));
		var atlases = PackTextures(newTextures);
		var lastTexturePage = data.EmbeddedTextures.Count - 1;
		var lastTexPageItem = data.TexturePageItems.Count - 1;
		foreach (var atlas in atlases)
		{
			var embTexture = new UndertaleEmbeddedTexture()
			{
				Name = data.Strings.MakeString($"Texture {++lastTexturePage}"),
				TextureData = new UndertaleEmbeddedTexture.TexData()
				{
					Image = new UndertaleModLib.Util.GMImage(2048, 2048)
				}
			};
			data.EmbeddedTextures.Add(embTexture);
			foreach (var node in atlas.Nodes)
			{
				var texPageItem = new UndertaleTexturePageItem()
				{
					Name = data.Strings.MakeString($"PageItem {++lastTexPageItem}"),
					SourceX = (ushort)node.X,
					SourceY = (ushort)node.Y,
					SourceWidth = (ushort)node.TextureInfo.Image.Width,
					SourceHeight = (ushort)node.TextureInfo.Image.Height,
					TargetX = 0,
					TargetY = 0,
					TargetWidth = (ushort)node.TextureInfo.Image.Width,
					TargetHeight = (ushort)node.TextureInfo.Image.Height,
					BoundingHeight = (ushort)node.TextureInfo.Image.Height,
					BoundingWidth = (ushort)node.TextureInfo.Image.Width,
					TexturePage = embTexture
				};
				data.TexturePageItems.Add(texPageItem);
				AddGraphicToDataAndTexturePageItem(data, texPageItem, node.TextureInfo);
			}
		}
	}

	[GeneratedRegex("^(.+?)(?:_(\\d+))?$")]
	private static partial Regex SpriteFrameRegex();
}

public enum GraphicsTypes
{
	Background,
	Sprite
}

public class TextureInfo(string name, MagickImage image, int frame, GraphicsTypes type)
{
	public string Name { get; init; } = name;
	public MagickImage Image { get; init; } = image;
	public int Frame { get; init; } = frame;
	public GraphicsTypes Type { get; init; } = type;
}

public class Atlas(int width, int height)
{
	public List<Node> Nodes { get; init; } = [];
	public bool[,] Mask { get; init; } = new bool[height, width];
}

public class Node(int x, int y, TextureInfo textureInfo)
{
	public int X { get; init; } = x;
	public int Y { get; init; } = y;
	public TextureInfo TextureInfo { get; init; } = textureInfo;
}
