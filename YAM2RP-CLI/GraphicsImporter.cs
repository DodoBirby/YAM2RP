using System.Text.RegularExpressions;
using UndertaleModLib;
using ImageMagick;
using UndertaleModLib.Models;
using UndertaleModLib.Util;

namespace YAM2RP;

public partial class GraphicsImporter
{
	static readonly Regex sprFrameRegex = SpriteFrameRegex();

	static List<TextureInfo> ProcessImageFiles(UndertaleData data, IEnumerable<string> files)
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
			var texInfo = new TextureInfo(spriteName, image, frameNumber, type);
			newTextures.Add(texInfo);
		}
		return newTextures;
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
		Console.WriteLine($"Packing {newTextures.Count} textures");
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

	static UndertaleSprite AddNewSprite(UndertaleData data, TextureInfo texInfo)
	{
		var newSprite = new UndertaleSprite()
		{
			Name = data.Strings.MakeString(texInfo.Name),
			Width = (uint)texInfo.Image.Width,
			Height = (uint)texInfo.Image.Height,
			MarginLeft = 0,
			MarginRight = texInfo.Image.Width - 1,
			MarginTop = 0,
			MarginBottom = texInfo.Image.Height - 1,
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
			existingSprite ??= AddNewSprite(data, texInfo);
			while (texInfo.Frame >= existingSprite.Textures.Count)
			{
				existingSprite.Textures.Add(texEntry);
			}
			existingSprite.Textures[texInfo.Frame] = texEntry;
			return;
		}
		throw new Exception("Unknown Graphics type");
	}

	static List<MagickImage> CreateAtlasImages(List<Atlas> atlases)
	{
		var images = new List<MagickImage>();
		foreach (var atlas in atlases)
		{
			var image = new MagickImage(new MagickColor(0, 0, 0, 0), 2048, 2048);
			images.Add(image);
			foreach (var node in atlas.Nodes)
			{
				image.Composite(node.TextureInfo.Image, node.X, node.Y, CompositeOperator.Copy);
			}
		}
		return images;
	}

	public static void ImportGraphics(UndertaleData data, string graphicsPath)
	{
		var newTextures = ProcessImageFiles(data, Directory.EnumerateFiles(graphicsPath, "*.png", SearchOption.AllDirectories));
		var atlases = PackTextures(newTextures);
		Console.WriteLine("Finished packing, beginning import");
		var lastTexturePage = data.EmbeddedTextures.Count - 1;
		var lastTexPageItem = data.TexturePageItems.Count - 1;
		var atlasImages = CreateAtlasImages(atlases);
		foreach ((var i, var atlas) in atlases.Enumerate())
		{
			var embTexture = new UndertaleEmbeddedTexture()
			{
				Name = data.Strings.MakeString($"Texture {++lastTexturePage}"),
				TextureData = new UndertaleEmbeddedTexture.TexData()
				{
					Image = GMImage.FromMagickImage(atlasImages[i])
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
			embTexture.TextureData.Image = embTexture.TextureData.Image.ConvertToPng();
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
