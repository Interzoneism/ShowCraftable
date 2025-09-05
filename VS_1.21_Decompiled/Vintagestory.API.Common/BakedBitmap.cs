using SkiaSharp;

namespace Vintagestory.API.Common;

public class BakedBitmap : IBitmap
{
	public int[] TexturePixels;

	public int Width;

	public int Height;

	public int[] Pixels => TexturePixels;

	int IBitmap.Width => Width;

	int IBitmap.Height => Height;

	public SKColor GetPixel(int x, int y)
	{
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		return new SKColor((uint)TexturePixels[Width * y + x]);
	}

	public int GetPixelArgb(int x, int y)
	{
		return TexturePixels[Width * y + x];
	}

	public SKColor GetPixelRel(float x, float y)
	{
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		return new SKColor((uint)TexturePixels[Width * (int)(y * (float)Height) + (int)(x * (float)Width)]);
	}

	public int[] GetPixelsTransformed(int rot = 0, int alpha = 100)
	{
		int[] array = new int[Width * Height];
		switch (rot)
		{
		case 0:
		{
			for (int num = 0; num < Width; num++)
			{
				for (int num2 = 0; num2 < Height; num2++)
				{
					array[num + num2 * Width] = GetPixelArgb(num, num2);
				}
			}
			break;
		}
		case 90:
		{
			for (int k = 0; k < Width; k++)
			{
				for (int l = 0; l < Height; l++)
				{
					array[l + k * Width] = GetPixelArgb(Width - k - 1, l);
				}
			}
			break;
		}
		case 180:
		{
			for (int m = 0; m < Width; m++)
			{
				for (int n = 0; n < Height; n++)
				{
					array[m + n * Width] = GetPixelArgb(Width - m - 1, Height - n - 1);
				}
			}
			break;
		}
		case 270:
		{
			for (int i = 0; i < Width; i++)
			{
				for (int j = 0; j < Height; j++)
				{
					array[j + i * Width] = GetPixelArgb(i, Height - j - 1);
				}
			}
			break;
		}
		}
		if (alpha != 100)
		{
			float num3 = (float)alpha / 100f;
			for (int num4 = 0; num4 < array.Length; num4++)
			{
				int num5 = array[num4];
				int num6 = (num5 >> 24) & 0xFF;
				array[num4] = num5 | ((int)((float)num6 * num3) << 24);
			}
		}
		return array;
	}
}
