using System;
using System.IO;
using OpenTK.Graphics.OpenGL;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Common.Input;
using OpenTK.Windowing.Desktop;
using SkiaSharp;
using Vintagestory.API.Config;

namespace Vintagestory.Client.NoObf;

public class GameWindowNative : GameWindow
{
	public GameWindowNative(GameWindowSettings gameWindowSettings, NativeWindowSettings nativeWindowSettings)
		: base(gameWindowSettings, nativeWindowSettings)
	{
		//IL_00ac: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ca: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d0: Expected O, but got Unknown
		//IL_00d0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00da: Expected O, but got Unknown
		GL.ClearColor(0f, 0f, 0f, 1f);
		GL.Clear((ClearBufferMask)16384);
		((IGraphicsContext)((NativeWindow)this).Context).SwapBuffers();
		if (RuntimeEnv.OS == OS.Mac || RuntimeEnv.IsWaylandSession)
		{
			return;
		}
		try
		{
			SKCodec val = SKCodec.Create(Path.Combine(GamePaths.AssetsPath, "gameicon.ico"));
			byte[] pixels = val.Pixels;
			byte[] array = new byte[pixels.Length];
			for (int i = 0; i < pixels.Length; i += 4)
			{
				array[i] = pixels[i + 2];
				array[i + 1] = pixels[i + 1];
				array[i + 2] = pixels[i];
				array[i + 3] = pixels[i + 3];
			}
			Image[] array2 = new Image[1];
			SKImageInfo info = val.Info;
			int height = ((SKImageInfo)(ref info)).Height;
			info = val.Info;
			array2[0] = new Image(height, ((SKImageInfo)(ref info)).Width, array);
			((NativeWindow)this).Icon = new WindowIcon((Image[])(object)array2);
		}
		catch (Exception)
		{
		}
	}
}
