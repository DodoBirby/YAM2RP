using ImageMagick;
using UndertaleModLib;
using UndertaleModLib.Models;
using UndertaleModLib.Util;

namespace YAM2RP;

public static class HelperExtensions
{
	public static int FindIndex<T>(this IList<T> list, Predicate<T> predicate)
	{
		for (int i = 0; i < list.Count; i++)
		{
			if (predicate(list[i]))
			{
				return i;
			}
		}
		return -1;
	}

	public static T? NameLookupIfNotNull<T>(this IList<T> list, string? name) where T : UndertaleNamedResource
	{
		if (name == null)
		{
			return default;
		}
		return list.ByName(name);
	}

	public static T? ItemOrNull<T>(this IList<T> list, int index)
	{
		if (index >= list.Count)
		{
			return default;
		}
		return list[index];
	}

	public static int GetHeight(this Array array)
	{
		return array.GetLength(0);
	}

	public static int GetWidth(this Array array)
	{
		return array.GetLength(1);
	}

	public static void ReplaceTextureYAM2RP(this UndertaleTexturePageItem texPageItem, MagickImage image)
	{
		using var resizedImage = TextureWorker.ResizeImage(image, texPageItem.SourceWidth, texPageItem.SourceHeight);
		var embImage = TextureCache.GetEmbeddedTexture(texPageItem.TexturePage);
		embImage.Composite(resizedImage, texPageItem.SourceX, texPageItem.SourceY, CompositeOperator.Copy);
		texPageItem.TexturePage.TextureData.Image = GMImage.FromMagickImage(embImage).ConvertToFormat(texPageItem.TexturePage.TextureData.Image.Format);
	}

	public static IEnumerable<(int, T)> Enumerate<T>(this IEnumerable<T> enumerable)
	{
		var i = 0;
		foreach (var item in enumerable)
		{
			yield return (i++, item);
		}
	}
}
