using ImageMagick;
using UndertaleModLib.Models;

namespace YAM2RP;

public static class TextureCache
{
	static readonly Dictionary<UndertaleEmbeddedTexture, MagickImage> embeddedDictionary = [];

	public static MagickImage GetEmbeddedTexture(UndertaleEmbeddedTexture texture)
	{
		if (embeddedDictionary.TryGetValue(texture, out var image))
		{
			return image;
		}
		var newImage = texture.TextureData.Image.GetMagickImage();
		embeddedDictionary.Add(texture, newImage);

		return newImage;
	}
}
