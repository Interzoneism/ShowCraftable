using SkiaSharp;

namespace Vintagestory.API.MathTools;

public static class SkColorFix
{
	public static int ToInt(this SKColor skcolor)
	{
		return ((SKColor)(ref skcolor)).Blue | (((SKColor)(ref skcolor)).Green << 8) | (((SKColor)(ref skcolor)).Red << 16) | (((SKColor)(ref skcolor)).Alpha << 24);
	}
}
