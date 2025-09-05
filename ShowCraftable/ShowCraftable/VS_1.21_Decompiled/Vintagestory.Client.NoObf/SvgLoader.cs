using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using Cairo;
using NanoSvg;
using OpenTK.Graphics.OpenGL;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Vintagestory.Client.NoObf;

public class SvgLoader
{
	private readonly ICoreClientAPI capi;

	private nint rasterizer;

	public SvgLoader(ICoreClientAPI _capi)
	{
		capi = _capi;
		rasterizer = SvgNativeMethods.nsvgCreateRasterizer();
		if (rasterizer == IntPtr.Zero)
		{
			throw new Win32Exception(Marshal.GetLastWin32Error());
		}
	}

	public unsafe void DrawSvg(IAsset svgAsset, ImageSurface intoSurface, int posx, int posy, int width = 0, int height = 0, int? color = 0)
	{
		byte[] array = rasterizeSvg(svgAsset, width, height, width, height, color);
		int num = intoSurface.Width * intoSurface.Height;
		nint dataPtr = intoSurface.DataPtr;
		fixed (byte* ptr = array)
		{
			int* ptr2 = (int*)ptr;
			int* ptr3 = (int*)((IntPtr)dataPtr).ToPointer();
			for (int i = 0; i < width; i++)
			{
				for (int j = 0; j < height; j++)
				{
					int rgb = ptr2[j * width + i];
					int num2 = (posy + j) * intoSurface.Width + posx + i;
					if (num2 >= 0 && num2 < num)
					{
						int rgb2 = ptr3[num2];
						ptr3[num2] = ColorUtil.ColorOver(rgb, rgb2);
					}
				}
			}
		}
		((Surface)intoSurface).MarkDirty();
	}

	public unsafe void DrawSvg(IAsset svgAsset, ImageSurface intoSurface, Matrix matrix, int posx, int posy, int width = 0, int height = 0, int? color = 0)
	{
		byte[] array = rasterizeSvg(svgAsset, width, height, width, height, color);
		int num = intoSurface.Width * intoSurface.Height;
		nint dataPtr = intoSurface.DataPtr;
		fixed (byte* ptr = array)
		{
			int* ptr2 = (int*)ptr;
			int* ptr3 = (int*)((IntPtr)dataPtr).ToPointer();
			for (int i = 0; i < width; i++)
			{
				for (int j = 0; j < height; j++)
				{
					int rgb = ptr2[j * width + i];
					double num2 = posx + i;
					double num3 = posy + j;
					matrix.TransformPoint(ref num2, ref num3);
					int num4 = (int)num2;
					int num5 = (int)num3 * intoSurface.Width + num4;
					if (num5 >= 0 && num5 < num)
					{
						int rgb2 = ptr3[num5];
						ptr3[num5] = ColorUtil.ColorOver(rgb, rgb2);
					}
				}
			}
		}
		((Surface)intoSurface).MarkDirty();
	}

	public unsafe LoadedTexture LoadSvg(IAsset svgAsset, int textureWidth, int textureHeight, int width = 0, int height = 0, int? color = 0)
	{
		int num = GL.GenTexture();
		GL.BindTexture((TextureTarget)3553, num);
		GL.TexParameter((TextureTarget)3553, (TextureParameterName)10241, 9729);
		GL.TexParameter((TextureTarget)3553, (TextureParameterName)10240, 9729);
		fixed (byte* ptr = rasterizeSvg(svgAsset, textureWidth, textureHeight, width, height, color))
		{
			GL.TexImage2D((TextureTarget)3553, 0, (PixelInternalFormat)32856, textureWidth, textureHeight, 0, (PixelFormat)6408, (PixelType)5121, (IntPtr)(nint)ptr);
		}
		return new LoadedTexture(capi, num, width, height);
	}

	public unsafe byte[] rasterizeSvg(IAsset svgAsset, int textureWidth, int textureHeight, int width = 0, int height = 0, int? color = 0)
	{
		//IL_009c: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cf: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ab: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b7: Unknown result type (might be due to invalid IL or missing references)
		//IL_0107: Unknown result type (might be due to invalid IL or missing references)
		//IL_0114: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ed: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f6: Unknown result type (might be due to invalid IL or missing references)
		//IL_012d: Unknown result type (might be due to invalid IL or missing references)
		//IL_013d: Unknown result type (might be due to invalid IL or missing references)
		float num = 1f;
		float num2 = 96f;
		byte[] array = ((!color.HasValue) ? null : ColorUtil.ToRGBABytes(color.Value));
		int num3 = 0;
		int num4 = 0;
		if (rasterizer == IntPtr.Zero)
		{
			throw new ObjectDisposedException("SvgLoader is already disposed!");
		}
		if (svgAsset.Data == null)
		{
			throw new ArgumentNullException("Asset Data is null. Is the asset loaded?");
		}
		nint num5 = SvgNativeMethods.nsvgParse(svgAsset.ToText(), "px", num2);
		if (num5 == IntPtr.Zero)
		{
			throw new Win32Exception(Marshal.GetLastWin32Error());
		}
		if (SvgNativeMethods.nsvgImageGetSize((IntPtr)num5) == (IntPtr)IntPtr.Zero)
		{
			throw new Win32Exception(Marshal.GetLastWin32Error());
		}
		NsvgSize val = Marshal.PtrToStructure<NsvgSize>(SvgNativeMethods.nsvgImageGetSize((IntPtr)num5));
		if (width == 0 && height == 0)
		{
			width = (int)(val.width * num);
			height = (int)(val.height * num);
		}
		else if (width == 0)
		{
			num = (float)height / val.height;
			width = (int)(val.width * num);
		}
		else if (height == 0)
		{
			num = (float)width / val.width;
			height = (int)(val.height * num);
		}
		else
		{
			float num6 = (float)width / val.width;
			float num7 = (float)height / val.height;
			num = ((num6 < num7) ? num6 : num7);
			num3 = (int)((float)textureWidth - val.width * num) / 2;
			num4 = (int)((float)textureHeight - val.height * num) / 2;
		}
		byte[] array2 = new byte[textureWidth * textureHeight * 4];
		fixed (byte* ptr = array2)
		{
			SvgNativeMethods.nsvgRasterize((IntPtr)rasterizer, (IntPtr)num5, (float)num3, (float)num4, num, (IntPtr)(nint)ptr, textureWidth, textureHeight, textureWidth * 4);
			if (array != null)
			{
				for (int i = 0; i < array2.Length - 1; i += 4)
				{
					float num8 = (float)(int)array2[i + 3] / 255f;
					array2[i] = (byte)(num8 * (float)(int)array[0]);
					array2[i + 1] = (byte)(num8 * (float)(int)array[1]);
					array2[i + 2] = (byte)(num8 * (float)(int)array[2]);
					array2[i + 3] = (byte)(num8 * (float)(int)array[3]);
				}
			}
		}
		SvgNativeMethods.nsvgDelete((IntPtr)num5);
		return array2;
	}

	~SvgLoader()
	{
		if (rasterizer != IntPtr.Zero)
		{
			SvgNativeMethods.nsvgDeleteRasterizer((IntPtr)rasterizer);
			rasterizer = IntPtr.Zero;
		}
	}
}
