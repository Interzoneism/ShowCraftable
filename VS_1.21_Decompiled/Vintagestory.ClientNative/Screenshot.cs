using System;
using System.IO;
using CompactExifLib;
using OpenTK.Graphics.OpenGL;
using OpenTK.Windowing.Desktop;
using SkiaSharp;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.Client.NoObf;
using Vintagestory.Common;

namespace Vintagestory.ClientNative;

public class Screenshot
{
	public GameWindow d_GameWindow;

	public string SaveScreenshot(ClientPlatformAbstract platform, Size2i size, string path = null, string filename = null, bool withAlpha = false, bool flip = true, string metadataStr = null)
	{
		string text = $"{DateTime.Now:yyyy-MM-dd_HH-mm-ss}";
		if (path == null)
		{
			if (!Directory.Exists(GamePaths.Screenshots))
			{
				Directory.CreateDirectory(GamePaths.Screenshots);
			}
			path = GamePaths.Screenshots;
		}
		if (filename == null)
		{
			filename = Path.Combine(path, text + ".png");
		}
		if (!GameDatabase.HaveWriteAccessFolder(path))
		{
			throw new Exception("No write access to " + path);
		}
		SKBitmap val = GrabScreenshot(size, ClientSettings.ScaleScreenshot, flip, withAlpha);
		try
		{
			val.Save(filename);
		}
		finally
		{
			((IDisposable)val)?.Dispose();
		}
		if (metadataStr != null)
		{
			ExifData exifData = new ExifData(filename);
			exifData.SetTagValue(ExifTag.Make, metadataStr, StrCoding.UsAscii);
			exifData.SetTagValue(ExifTag.ImageDescription, metadataStr, StrCoding.Utf8);
			exifData.Save(filename);
		}
		return Path.GetFileName(filename);
	}

	public SKBitmap GrabScreenshot(Size2i size, bool scaleScreenshot, bool flip, bool withAlpha)
	{
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0020: Expected O, but got Unknown
		//IL_004d: Unknown result type (might be due to invalid IL or missing references)
		//IL_005d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0067: Unknown result type (might be due to invalid IL or missing references)
		//IL_006c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0071: Unknown result type (might be due to invalid IL or missing references)
		//IL_008e: Unknown result type (might be due to invalid IL or missing references)
		//IL_009b: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a1: Expected O, but got Unknown
		//IL_00a2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a8: Expected O, but got Unknown
		SKBitmap val = new SKBitmap(new SKImageInfo(size.Width, size.Height, (SKColorType)6, (SKAlphaType)((!withAlpha) ? 1 : 3)));
		GL.ReadPixels(0, 0, size.Width, size.Height, (PixelFormat)32993, (PixelType)5121, val.GetPixels());
		if (scaleScreenshot)
		{
			val = val.Resize(new SKImageInfo(((NativeWindow)d_GameWindow).ClientSize.X, ((NativeWindow)d_GameWindow).ClientSize.Y), new SKSamplingOptions(SKCubicResampler.Mitchell));
		}
		if (!flip)
		{
			return val;
		}
		SKBitmap val2 = new SKBitmap(val.Width, val.Height, val.ColorType, (SKAlphaType)((!withAlpha) ? 1 : 3));
		SKCanvas val3 = new SKCanvas(val2);
		try
		{
			val3.Translate((float)val.Width, (float)val.Height);
			val3.RotateDegrees(180f);
			val3.Scale(-1f, 1f, (float)val.Width / 2f, 0f);
			val3.DrawBitmap(val, 0f, 0f, (SKPaint)null);
			return val2;
		}
		finally
		{
			((IDisposable)val3)?.Dispose();
		}
	}
}
