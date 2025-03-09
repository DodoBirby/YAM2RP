using UndertaleModLib;

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
}
