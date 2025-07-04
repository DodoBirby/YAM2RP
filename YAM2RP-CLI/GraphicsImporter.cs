﻿using System.Text.RegularExpressions;
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

	// This is false for out of bounds because an out of bounds point is not occupied by another image
	static bool IsOccupied(Atlas atlas, long x, long y)
	{
		if (x < 0 || x >= atlas.Mask.GetWidth() || y < 0 || y >= atlas.Mask.GetHeight())
		{
			return false;
		}
		return atlas.Mask[y, x];
	}

	static bool CanFit(Atlas atlas, TextureInfo texture, int imageX, int imageY, int margin)
	{
		var imageHeightWithMargin = texture.Image.Height + margin;
		var imageWidthWithMargin = texture.Image.Width + margin;
		var x = imageX - margin;
		var y = imageY - margin;
		if (IsOccupied(atlas, x, y)
			|| IsOccupied(atlas, x, y + imageHeightWithMargin - 1)
			|| IsOccupied(atlas, x + imageWidthWithMargin - 1, y)
			|| IsOccupied(atlas, x + imageWidthWithMargin - 1, y + imageHeightWithMargin - 1))
		{
			return false;
		}
		for (int dy = 0; dy < imageHeightWithMargin + margin; dy++)
		{
			for (int dx = 0; dx < imageWidthWithMargin + margin; dx++)
			{
				if (IsOccupied(atlas, x + dx, y + dy))
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
				if (CanFit(atlas, texture, x, y, 4))
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
		if (texInfo.Type == GraphicsTypes.Background)
		{
			var existingBackground = data.Backgrounds.ByName(texInfo.Name);
			if (existingBackground == null)
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
			existingBackground.Texture = texPageItem;
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

	static List<MagickImage> CreateAtlasImages(List<Atlas> atlases, uint extrudeAmount)
	{
		var images = new List<MagickImage>();
		var distortSettings = new DistortSettings(DistortMethod.ScaleRotateTranslate)
		{
			Viewport = new MagickGeometry()
		};
		foreach (var atlas in atlases)
		{
			var image = new MagickImage(new MagickColor(0, 0, 0, 0), 2048, 2048);
			images.Add(image);
			foreach (var node in atlas.Nodes)
			{
				using var newImage = node.TextureInfo.Image.Clone();
				distortSettings.Viewport.X = -(int)extrudeAmount;
				distortSettings.Viewport.Y = -(int)extrudeAmount;
				distortSettings.Viewport.Width = newImage.Width + extrudeAmount * 2;
				distortSettings.Viewport.Height = newImage.Height + extrudeAmount * 2;
				newImage.Distort(distortSettings, 0);
				image.Composite(newImage, (int)(node.X - extrudeAmount), (int)(node.Y - extrudeAmount), CompositeOperator.Copy);
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
		var atlasImages = CreateAtlasImages(atlases, 1);
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
