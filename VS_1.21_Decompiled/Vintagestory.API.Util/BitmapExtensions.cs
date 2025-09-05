using System;
using System.IO;
using SkiaSharp;

namespace Vintagestory.API.Util;

public static class BitmapExtensions
{
	public unsafe static void SetPixels(this SKBitmap bmp, int[] pixels)
	{
		if (bmp.Width * bmp.Height != pixels.Length)
		{
			throw new ArgumentException("Pixel array must be width*height length");
		}
		fixed (int* pixels2 = pixels)
		{
			bmp.SetPixels((IntPtr)(nint)pixels2);
		}
	}

	public static void Save(this SKBitmap bmp, string filename)
	{
		using Stream stream = File.OpenWrite(filename);
		bmp.Encode((SKEncodedImageFormat)4, 100).SaveTo(stream);
	}
}
