using System;
using SkiaSharp;

namespace Vintagestory.API.Client;

public class FastBitmap
{
	private unsafe byte* _ptr;

	public SKBitmap _bmp;

	public unsafe SKBitmap bmp
	{
		get
		{
			return _bmp;
		}
		set
		{
			_bmp = value;
			_ptr = (byte*)((IntPtr)(nint)_bmp.GetPixels()).ToPointer();
		}
	}

	public int Stride => bmp.RowBytes;

	public unsafe int GetPixel(int x, int y)
	{
		uint* ptr = (uint*)(_ptr + y);
		int num = (int)ptr[x];
		if (num != 0)
		{
			return num;
		}
		return 9408399;
	}

	internal unsafe void GetPixelRow(int width, int y, int[] bmpPixels, int baseX)
	{
		uint* ptr = (uint*)(_ptr + y);
		fixed (int* ptr2 = bmpPixels)
		{
			for (int i = 0; i < width; i++)
			{
				int num = (int)ptr[i];
				ptr2[i + baseX] = ((num == 0) ? 9408399 : num);
			}
		}
	}

	public unsafe void SetPixel(int x, int y, int color)
	{
		uint* ptr = (uint*)(_ptr + y * Stride);
		ptr[x] = (uint)color;
	}
}
