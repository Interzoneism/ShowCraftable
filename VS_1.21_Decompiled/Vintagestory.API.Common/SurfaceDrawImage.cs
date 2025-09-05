using Cairo;

namespace Vintagestory.API.Common;

public static class SurfaceDrawImage
{
	public static void Image(this ImageSurface surface, BitmapRef bmp, int xPos, int yPos, int width, int height)
	{
		SurfaceDrawImage.Image(surface, ((BitmapExternal)bmp).bmp, xPos, yPos, width, height);
	}
}
