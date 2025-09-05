namespace Vintagestory.API.Common;

public static class AssetLocationExtensions
{
	public static string ToNonNullString(this AssetLocation loc)
	{
		if (!(loc == null))
		{
			return loc.ToShortString();
		}
		return "";
	}
}
