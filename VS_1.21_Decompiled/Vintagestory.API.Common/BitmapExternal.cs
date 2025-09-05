using System;
using System.IO;
using SkiaSharp;
using Vintagestory.API.Client;
using Vintagestory.API.Util;

namespace Vintagestory.API.Common;

public class BitmapExternal : BitmapRef
{
	public SKBitmap bmp;

	public override int Height => bmp.Height;

	public override int Width => bmp.Width;

	public override int[] Pixels => Array.ConvertAll(bmp.Pixels, (SKColor p) => (int)(uint)p);

	public nint PixelsPtrAndLock => bmp.GetPixels();

	[Obsolete("This requires to manually set the underlying SKBitmap, prefer other overloads.")]
	public BitmapExternal()
	{
	}

	public BitmapExternal(SKBitmap bmp)
	{
		this.bmp = bmp;
	}

	public BitmapExternal(int width, int height)
	{
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0014: Expected O, but got Unknown
		bmp = new SKBitmap(width, height, false);
	}

	public BitmapExternal(MemoryStream ms, ILogger logger, AssetLocation? loc = null)
	{
		//IL_005c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0066: Expected O, but got Unknown
		//IL_006e: Unknown result type (might be due to invalid IL or missing references)
		try
		{
			bmp = Decode(ms.ToArray());
		}
		catch (Exception e)
		{
			if (loc != null)
			{
				logger.Error("Failed loading bitmap from png file {0}. Will default to an empty 1x1 bitmap.", loc);
				logger.Error(e);
			}
			else
			{
				logger.Error("Failed loading bitmap. Will default to an empty 1x1 bitmap.");
				logger.Error(e);
			}
			bmp = new SKBitmap(1, 1, false);
			bmp.SetPixel(0, 0, SKColors.Orange);
		}
	}

	public BitmapExternal(string filePath, ILogger? logger = null)
	{
		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		//IL_0042: Expected O, but got Unknown
		//IL_004a: Unknown result type (might be due to invalid IL or missing references)
		try
		{
			bmp = Decode(File.ReadAllBytes(filePath));
		}
		catch (Exception e)
		{
			if (logger != null)
			{
				logger.Error("Failed loading bitmap from data. Will default to an empty 1x1 bitmap.");
				logger.Error(e);
			}
			bmp = new SKBitmap(1, 1, false);
			bmp.SetPixel(0, 0, SKColors.Orange);
		}
	}

	public BitmapExternal(Stream stream, ILogger? logger = null)
	{
		//IL_004c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0056: Expected O, but got Unknown
		//IL_005e: Unknown result type (might be due to invalid IL or missing references)
		try
		{
			byte[] array = new byte[stream.Length];
			stream.ReadExactly(array);
			bmp = Decode(array);
		}
		catch (Exception e)
		{
			if (logger != null)
			{
				logger.Error("Failed loading bitmap from data. Will default to an empty 1x1 bitmap.");
				logger.Error(e);
			}
			bmp = new SKBitmap(1, 1, false);
			bmp.SetPixel(0, 0, SKColors.Orange);
		}
	}

	public BitmapExternal(byte[] data, int dataLength, ILogger logger)
	{
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0049: Expected O, but got Unknown
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		try
		{
			bmp = Decode(data.AsSpan().Slice(0, dataLength));
		}
		catch (Exception e)
		{
			logger.Error("Failed loading bitmap from data. Will default to an empty 1x1 bitmap.");
			logger.Error(e);
			bmp = new SKBitmap(1, 1, false);
			bmp.SetPixel(0, 0, SKColors.Orange);
		}
	}

	public unsafe static SKBitmap Decode(ReadOnlySpan<byte> buffer)
	{
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		//IL_0036: Unknown result type (might be due to invalid IL or missing references)
		fixed (byte* ptr = buffer)
		{
			SKData val = SKData.Create((IntPtr)(nint)ptr, buffer.Length);
			try
			{
				SKCodec val2 = SKCodec.Create(val);
				try
				{
					SKImageInfo info = val2.Info;
					((SKImageInfo)(ref info)).AlphaType = (SKAlphaType)3;
					((SKImageInfo)(ref info)).ColorType = (SKColorType)6;
					return SKBitmap.Decode(val2, info);
				}
				finally
				{
					((IDisposable)val2)?.Dispose();
				}
			}
			finally
			{
				((IDisposable)val)?.Dispose();
			}
		}
	}

	public override void Dispose()
	{
		((SKNativeObject)bmp).Dispose();
	}

	public override void Save(string filename)
	{
		bmp.Save(filename);
	}

	public override SKColor GetPixel(int x, int y)
	{
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		return bmp.GetPixel(x, y);
	}

	public override SKColor GetPixelRel(float x, float y)
	{
		//IL_004a: Unknown result type (might be due to invalid IL or missing references)
		return bmp.GetPixel((int)Math.Min(bmp.Width - 1, x * (float)bmp.Width), (int)Math.Min(bmp.Height - 1, y * (float)bmp.Height));
	}

	public unsafe override void MulAlpha(int alpha = 255)
	{
		int num = Width * Height;
		float num2 = (float)alpha / 255f;
		byte* ptr = (byte*)((IntPtr)(nint)bmp.GetPixels()).ToPointer();
		for (int i = 0; i < num; i++)
		{
			int num3 = ptr[3];
			*ptr = (byte)((float)(int)(*ptr) * num2);
			ptr[1] = (byte)((float)(int)ptr[1] * num2);
			ptr[2] = (byte)((float)(int)ptr[2] * num2);
			ptr[3] = (byte)((float)num3 * num2);
			ptr += 4;
		}
	}

	public override int[] GetPixelsTransformed(int rot = 0, int mulAlpha = 255)
	{
		int[] array = new int[Width * Height];
		int width = bmp.Width;
		int height = bmp.Height;
		FastBitmap fastBitmap = new FastBitmap();
		fastBitmap.bmp = bmp;
		int stride = fastBitmap.Stride;
		switch (rot)
		{
		case 0:
		{
			for (int num4 = 0; num4 < height; num4++)
			{
				fastBitmap.GetPixelRow(width, num4 * stride, array, num4 * width);
			}
			break;
		}
		case 90:
		{
			for (int k = 0; k < width; k++)
			{
				int num2 = k * width;
				for (int l = 0; l < height; l++)
				{
					array[l + num2] = fastBitmap.GetPixel(width - k - 1, l * stride);
				}
			}
			break;
		}
		case 180:
		{
			for (int m = 0; m < height; m++)
			{
				int num3 = m * width;
				int y = (height - m - 1) * stride;
				for (int n = 0; n < width; n++)
				{
					array[n + num3] = fastBitmap.GetPixel(width - n - 1, y);
				}
			}
			break;
		}
		case 270:
		{
			for (int i = 0; i < width; i++)
			{
				int num = i * width;
				for (int j = 0; j < height; j++)
				{
					array[j + num] = fastBitmap.GetPixel(i, (height - j - 1) * stride);
				}
			}
			break;
		}
		}
		if (mulAlpha != 255)
		{
			float num5 = (float)mulAlpha / 255f;
			int num6 = 16777215;
			for (int num7 = 0; num7 < array.Length; num7++)
			{
				int num8 = array[num7];
				uint num9 = (uint)num8 >> 24;
				num8 &= num6;
				array[num7] = num8 | ((int)((float)num9 * num5) << 24);
			}
		}
		return array;
	}
}
