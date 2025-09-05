using System;
using System.Collections.Generic;
using Cairo;
using SkiaSharp;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;

namespace Vintagestory.API.Client;

public abstract class GuiElement : IDisposable
{
	public static AssetLocation dirtTextureName = new AssetLocation("gui/backgrounds/soil.png");

	public static AssetLocation noisyMetalTextureName = new AssetLocation("gui/backgrounds/noisymetal.png");

	public static AssetLocation woodTextureName = new AssetLocation("gui/backgrounds/oak.png");

	public static AssetLocation stoneTextureName = new AssetLocation("gui/backgrounds/stone.png");

	public static AssetLocation waterTextureName = new AssetLocation("gui/backgrounds/water.png");

	public static AssetLocation paperTextureName = new AssetLocation("gui/backgrounds/signpaper.png");

	internal static Dictionary<AssetLocation, KeyValuePair<SurfacePattern, ImageSurface>> cachedPatterns = new Dictionary<AssetLocation, KeyValuePair<SurfacePattern, ImageSurface>>();

	internal string lastShownText = "";

	internal ImageSurface metalNail;

	public ElementBounds Bounds;

	public int TabIndex;

	protected bool hasFocus;

	protected ICoreClientAPI api;

	public virtual ElementBounds InsideClipBounds { get; set; }

	public bool RenderAsPremultipliedAlpha { get; set; } = true;

	public bool HasFocus => hasFocus;

	public virtual double DrawOrder => 0.0;

	public virtual bool Focusable => false;

	public virtual double Scale { get; set; } = 1.0;

	public virtual string MouseOverCursor { get; protected set; }

	public virtual void OnFocusGained()
	{
		hasFocus = true;
	}

	public virtual void OnFocusLost()
	{
		hasFocus = false;
	}

	public GuiElement(ICoreClientAPI capi, ElementBounds bounds)
	{
		api = capi;
		Bounds = bounds;
	}

	public virtual void ComposeElements(Context ctxStatic, ImageSurface surface)
	{
	}

	public virtual void RenderInteractiveElements(float deltaTime)
	{
	}

	public virtual void PostRenderInteractiveElements(float deltaTime)
	{
	}

	public void RenderFocusOverlay(float deltaTime)
	{
		ElementBounds elementBounds = Bounds;
		if (InsideClipBounds != null)
		{
			elementBounds = InsideClipBounds;
		}
		api.Render.RenderRectangle((int)elementBounds.renderX, (int)elementBounds.renderY, 800f, (int)elementBounds.OuterWidth, (int)elementBounds.OuterHeight, 1627389951);
	}

	protected void generateTexture(ImageSurface surface, ref int textureId, bool linearMag = true)
	{
		GenerateTexture(api, surface, ref textureId, linearMag);
	}

	public static void GenerateTexture(ICoreClientAPI api, ImageSurface surface, ref int textureId, bool linearMag = true)
	{
		int num = textureId;
		textureId = api.Gui.LoadCairoTexture(surface, linearMag);
		if (num > 0)
		{
			api.Render.GLDeleteTexture(num);
		}
	}

	protected void generateTexture(ImageSurface surface, ref LoadedTexture intoTexture, bool linearMag = true)
	{
		api.Gui.LoadOrUpdateCairoTexture(surface, linearMag, ref intoTexture);
	}

	public static double scaled(double value)
	{
		return value * (double)RuntimeEnv.GUIScale;
	}

	public static int scaledi(double value)
	{
		return (int)(value * (double)RuntimeEnv.GUIScale);
	}

	protected Context genContext(ImageSurface surface)
	{
		return GenContext(surface);
	}

	public static Context GenContext(ImageSurface surface)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		//IL_0036: Unknown result type (might be due to invalid IL or missing references)
		//IL_003e: Expected O, but got Unknown
		Context val = new Context((Surface)(object)surface);
		val.SetSourceRGBA(0.0, 0.0, 0.0, 0.0);
		val.Paint();
		val.Antialias = (Antialias)6;
		return val;
	}

	[Obsolete("Use getPattern(BitmapExternal bitmap) for easier update to .NET7.0")]
	public static SurfacePattern getPattern(SKBitmap bitmap)
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0013: Expected O, but got Unknown
		return new SurfacePattern(getImageSurfaceFromAsset(bitmap))
		{
			Extend = (Extend)1
		};
	}

	[Obsolete("Use getPattern(BitmapExternal bitmap) for easier update to .NET7.0")]
	public static SurfacePattern getPattern(BitmapExternal bitmap)
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0013: Expected O, but got Unknown
		return new SurfacePattern(getImageSurfaceFromAsset(bitmap))
		{
			Extend = (Extend)1
		};
	}

	[Obsolete("Use getImageSurfaceFromAsset(BitmapExternal bitmap) for easier update to .NET7.0")]
	public unsafe static ImageSurface getImageSurfaceFromAsset(SKBitmap bitmap)
	{
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0013: Expected O, but got Unknown
		ImageSurface val = new ImageSurface((Format)0, bitmap.Width, bitmap.Height);
		uint* ptr = (uint*)((IntPtr)(nint)val.DataPtr).ToPointer();
		uint* ptr2 = (uint*)((IntPtr)(nint)bitmap.GetPixels()).ToPointer();
		int num = bitmap.Width * bitmap.Height;
		for (int i = 0; i < num; i++)
		{
			ptr[i] = ptr2[i];
		}
		((Surface)val).MarkDirty();
		return val;
	}

	public unsafe static ImageSurface getImageSurfaceFromAsset(BitmapExternal bitmap)
	{
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0013: Expected O, but got Unknown
		ImageSurface val = new ImageSurface((Format)0, bitmap.Width, bitmap.Height);
		uint* ptr = (uint*)((IntPtr)(nint)val.DataPtr).ToPointer();
		uint* ptr2 = (uint*)((IntPtr)bitmap.PixelsPtrAndLock).ToPointer();
		int num = bitmap.Width * bitmap.Height;
		for (int i = 0; i < num; i++)
		{
			ptr[i] = ptr2[i];
		}
		((Surface)val).MarkDirty();
		return val;
	}

	[Obsolete("Use getImageSurfaceFromAsset(BitmapExternal bitmap, int width, int height) for easier update to .NET7.0")]
	public static ImageSurface getImageSurfaceFromAsset(SKBitmap bitmap, int width, int height)
	{
		//IL_0003: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		//IL_0013: Expected O, but got Unknown
		//IL_0014: Expected O, but got Unknown
		ImageSurface val = new ImageSurface((Format)0, width, height);
		SurfaceDrawImage.Image(val, bitmap, 0, 0, width, height);
		return val;
	}

	public static ImageSurface getImageSurfaceFromAsset(BitmapExternal bitmap, int width, int height)
	{
		//IL_0003: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		//IL_0013: Expected O, but got Unknown
		//IL_0014: Expected O, but got Unknown
		ImageSurface val = new ImageSurface((Format)0, width, height);
		SurfaceDrawImage.Image(val, bitmap, 0, 0, width, height);
		return val;
	}

	public virtual void BeforeCalcBounds()
	{
	}

	public static SurfacePattern getPattern(ICoreClientAPI capi, AssetLocation textureLoc, bool doCache = true, int mulAlpha = 255, float scale = 1f)
	{
		//IL_007a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0080: Expected O, but got Unknown
		//IL_00a3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a9: Expected O, but got Unknown
		AssetLocation key = textureLoc.Clone().WithPathPrefix(scale + "-").WithPathPrefix(mulAlpha + "@");
		if (cachedPatterns.ContainsKey(key) && ((Pattern)cachedPatterns[key].Key).HandleValid)
		{
			return cachedPatterns[key].Key;
		}
		ImageSurface imageSurfaceFromAsset = getImageSurfaceFromAsset(capi, textureLoc, mulAlpha);
		SurfacePattern val = new SurfacePattern(imageSurfaceFromAsset);
		((Pattern)val).Extend = (Extend)1;
		val.Filter = (Filter)3;
		if (doCache)
		{
			cachedPatterns[key] = new KeyValuePair<SurfacePattern, ImageSurface>(val, imageSurfaceFromAsset);
		}
		Matrix val2 = new Matrix();
		val2.Scale((double)(scale / RuntimeEnv.GUIScale), (double)(scale / RuntimeEnv.GUIScale));
		((Pattern)val).Matrix = val2;
		return val;
	}

	public unsafe static ImageSurface getImageSurfaceFromAsset(ICoreClientAPI capi, AssetLocation textureLoc, int mulAlpha = 255)
	{
		//IL_004a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0050: Expected O, but got Unknown
		byte[] data = capi.Assets.Get(textureLoc.Clone().WithPathPrefixOnce("textures/")).Data;
		BitmapExternal bitmapExternal = capi.Render.BitmapCreateFromPng(data);
		if (mulAlpha != 255)
		{
			bitmapExternal.MulAlpha(mulAlpha);
		}
		ImageSurface val = new ImageSurface((Format)0, bitmapExternal.Width, bitmapExternal.Height);
		uint* ptr = (uint*)((IntPtr)(nint)val.DataPtr).ToPointer();
		uint* ptr2 = (uint*)((IntPtr)bitmapExternal.PixelsPtrAndLock).ToPointer();
		int num = bitmapExternal.Width * bitmapExternal.Height;
		for (int i = 0; i < num; i++)
		{
			ptr[i] = ptr2[i];
		}
		((Surface)val).MarkDirty();
		bitmapExternal.Dispose();
		return val;
	}

	public static SurfacePattern fillWithPattern(ICoreClientAPI capi, Context ctx, AssetLocation textureLoc, bool nearestScalingFiler = false, bool preserve = false, int mulAlpha = 255, float scale = 1f)
	{
		SurfacePattern pattern = getPattern(capi, textureLoc, doCache: true, mulAlpha, scale);
		if (nearestScalingFiler)
		{
			pattern.Filter = (Filter)3;
		}
		ctx.SetSource((Pattern)(object)pattern);
		if (preserve)
		{
			ctx.FillPreserve();
		}
		else
		{
			ctx.Fill();
		}
		return pattern;
	}

	public static void DiscardPattern(AssetLocation textureLoc)
	{
		if (cachedPatterns.ContainsKey(textureLoc))
		{
			KeyValuePair<SurfacePattern, ImageSurface> keyValuePair = cachedPatterns[textureLoc];
			((Pattern)keyValuePair.Key).Dispose();
			((Surface)keyValuePair.Value).Dispose();
			cachedPatterns.Remove(textureLoc);
		}
	}

	internal SurfacePattern paintWithPattern(ICoreClientAPI capi, Context ctx, AssetLocation textureLoc)
	{
		SurfacePattern pattern = getPattern(capi, textureLoc);
		ctx.SetSource((Pattern)(object)pattern);
		ctx.Paint();
		return pattern;
	}

	protected void Lamp(Context ctx, double x, double y, float[] color)
	{
		ctx.SetSourceRGBA((double)color[0], (double)color[1], (double)color[2], 1.0);
		RoundRectangle(ctx, x, y, scaled(10.0), scaled(10.0), GuiStyle.ElementBGRadius);
		ctx.Fill();
		EmbossRoundRectangleElement(ctx, x, y, scaled(10.0), scaled(10.0));
	}

	public static void Rectangle(Context ctx, ElementBounds bounds)
	{
		ctx.NewPath();
		ctx.LineTo(bounds.drawX, bounds.drawY);
		ctx.LineTo(bounds.drawX + bounds.OuterWidth, bounds.drawY);
		ctx.LineTo(bounds.drawX + bounds.OuterWidth, bounds.drawY + bounds.OuterHeight);
		ctx.LineTo(bounds.drawX, bounds.drawY + bounds.OuterHeight);
		ctx.ClosePath();
	}

	public static void Rectangle(Context ctx, double x, double y, double width, double height)
	{
		ctx.NewPath();
		ctx.LineTo(x, y);
		ctx.LineTo(x + width, y);
		ctx.LineTo(x + width, y + height);
		ctx.LineTo(x, y + height);
		ctx.ClosePath();
	}

	public void DialogRoundRectangle(Context ctx, ElementBounds bounds)
	{
		RoundRectangle(ctx, bounds.bgDrawX, bounds.bgDrawY, bounds.OuterWidth, bounds.OuterHeight, GuiStyle.DialogBGRadius);
	}

	public void ElementRoundRectangle(Context ctx, ElementBounds bounds, bool isBackground = false, double radius = -1.0)
	{
		if (radius == -1.0)
		{
			radius = GuiStyle.ElementBGRadius;
		}
		if (isBackground)
		{
			RoundRectangle(ctx, bounds.bgDrawX, bounds.bgDrawY, bounds.OuterWidth, bounds.OuterHeight, radius);
		}
		else
		{
			RoundRectangle(ctx, bounds.drawX, bounds.drawY, bounds.InnerWidth, bounds.InnerHeight, radius);
		}
	}

	public static void RoundRectangle(Context ctx, double x, double y, double width, double height, double radius)
	{
		double num = Math.PI / 180.0;
		ctx.Antialias = (Antialias)6;
		ctx.NewPath();
		ctx.Arc(x + width - radius, y + radius, radius, -90.0 * num, 0.0 * num);
		ctx.Arc(x + width - radius, y + height - radius, radius, 0.0 * num, 90.0 * num);
		ctx.Arc(x + radius, y + height - radius, radius, 90.0 * num, 180.0 * num);
		ctx.Arc(x + radius, y + radius, radius, 180.0 * num, 270.0 * num);
		ctx.ClosePath();
	}

	public void ShadePath(Context ctx, double thickness = 2.0)
	{
		ctx.Operator = (Operator)5;
		ctx.SetSourceRGBA(GuiStyle.DialogBorderColor);
		ctx.LineWidth = thickness;
		ctx.Stroke();
		ctx.Operator = (Operator)2;
	}

	public void EmbossRoundRectangleDialog(Context ctx, double x, double y, double width, double height, bool inverse = false)
	{
		EmbossRoundRectangle(ctx, x, y, width, height, GuiStyle.DialogBGRadius, 4, 0.5f, 0.5f, inverse, 0.25f);
	}

	public void EmbossRoundRectangleElement(Context ctx, double x, double y, double width, double height, bool inverse = false, int depth = 2, int radius = -1)
	{
		EmbossRoundRectangle(ctx, x, y, width, height, (radius == -1) ? GuiStyle.ElementBGRadius : ((double)radius), depth, 0.7f, 0.8f, inverse, 0.25f);
	}

	public void EmbossRoundRectangleElement(Context ctx, ElementBounds bounds, bool inverse = false, int depth = 2, int radius = -1)
	{
		EmbossRoundRectangle(ctx, bounds.drawX, bounds.drawY, bounds.InnerWidth, bounds.InnerHeight, radius, depth, 0.7f, 0.8f, inverse, 0.25f);
	}

	protected void EmbossRoundRectangle(Context ctx, double x, double y, double width, double height, double radius, int depth = 3, float intensity = 0.4f, float lightDarkBalance = 1f, bool inverse = false, float alphaOffset = 0f)
	{
		double num = Math.PI / 180.0;
		int num2 = depth;
		int num3 = 0;
		ctx.Antialias = (Antialias)6;
		int num4 = 255;
		int num5 = 0;
		if (inverse)
		{
			num4 = 0;
			num5 = 255;
			lightDarkBalance = 2f - lightDarkBalance;
		}
		while (num2-- > 0)
		{
			ctx.NewPath();
			ctx.Arc(x + radius, y + height - radius, radius, 135.0 * num, 180.0 * num);
			ctx.Arc(x + radius, y + radius, radius, 180.0 * num, 270.0 * num);
			ctx.Arc(x + width - radius, y + radius, radius, -90.0 * num, -45.0 * num);
			float num6 = intensity * (float)(depth - num3) / (float)depth;
			double num7 = Math.Min(1f, lightDarkBalance * num6) - alphaOffset;
			ctx.SetSourceRGBA((double)num4, (double)num4, (double)num4, num7);
			ctx.LineWidth = 1.0;
			ctx.Stroke();
			ctx.NewPath();
			ctx.Arc(x + width - radius, y + radius, radius, -45.0 * num, 0.0 * num);
			ctx.Arc(x + width - radius, y + height - radius, radius, 0.0 * num, 90.0 * num);
			ctx.Arc(x + radius, y + height - radius, radius, 90.0 * num, 135.0 * num);
			num7 = Math.Min(1f, (2f - lightDarkBalance) * num6) - alphaOffset;
			ctx.SetSourceRGBA((double)num5, (double)num5, (double)num5, num7);
			ctx.LineWidth = 1.0;
			ctx.Stroke();
			num3++;
			x += 1.0;
			y += 1.0;
			width -= 2.0;
			height -= 2.0;
		}
	}

	public virtual void RenderBoundsDebug()
	{
		api.Render.RenderRectangle((int)Bounds.renderX, (int)Bounds.renderY, 500f, (int)Bounds.OuterWidth, (int)Bounds.OuterHeight, OutlineColor());
	}

	public virtual void OnMouseDown(ICoreClientAPI api, MouseEvent mouse)
	{
		if (IsPositionInside(mouse.X, mouse.Y))
		{
			OnMouseDownOnElement(api, mouse);
		}
	}

	public virtual void OnMouseDownOnElement(ICoreClientAPI api, MouseEvent args)
	{
		args.Handled = true;
	}

	public virtual void OnMouseUpOnElement(ICoreClientAPI api, MouseEvent args)
	{
	}

	public virtual void OnMouseUp(ICoreClientAPI api, MouseEvent args)
	{
		if (IsPositionInside(args.X, args.Y))
		{
			OnMouseUpOnElement(api, args);
		}
	}

	public virtual bool OnMouseEnterSlot(ICoreClientAPI api, ItemSlot slot)
	{
		return false;
	}

	public virtual bool OnMouseLeaveSlot(ICoreClientAPI api, ItemSlot slot)
	{
		return false;
	}

	public virtual void OnMouseMove(ICoreClientAPI api, MouseEvent args)
	{
	}

	public virtual void OnMouseWheel(ICoreClientAPI api, MouseWheelEventArgs args)
	{
	}

	public virtual void OnKeyDown(ICoreClientAPI api, KeyEvent args)
	{
	}

	public virtual void OnKeyUp(ICoreClientAPI api, KeyEvent args)
	{
	}

	public virtual void OnKeyPress(ICoreClientAPI api, KeyEvent args)
	{
	}

	public virtual bool IsPositionInside(int posX, int posY)
	{
		if (Bounds.PointInside(posX, posY))
		{
			if (InsideClipBounds != null)
			{
				return InsideClipBounds.PointInside(posX, posY);
			}
			return true;
		}
		return false;
	}

	public virtual int OutlineColor()
	{
		return -2130706433;
	}

	protected void Render2DTexture(int textureid, float posX, float posY, float width, float height, float z = 50f, Vec4f color = null)
	{
		if (RenderAsPremultipliedAlpha)
		{
			api.Render.Render2DTexturePremultipliedAlpha(textureid, posX, posY, width, height, z, color);
		}
		else
		{
			api.Render.Render2DTexture(textureid, posX, posY, width, height, z, color);
		}
	}

	protected void Render2DTexture(int textureid, double posX, double posY, double width, double height, float z = 50f, Vec4f color = null)
	{
		if (RenderAsPremultipliedAlpha)
		{
			api.Render.Render2DTexturePremultipliedAlpha(textureid, posX, posY, width, height, z, color);
		}
		else
		{
			api.Render.Render2DTexture(textureid, (float)posX, (float)posY, (float)width, (float)height, z, color);
		}
	}

	protected void Render2DTexture(int textureid, ElementBounds bounds, float z = 50f, Vec4f color = null)
	{
		if (RenderAsPremultipliedAlpha)
		{
			api.Render.Render2DTexturePremultipliedAlpha(textureid, bounds, z, color);
		}
		else
		{
			api.Render.Render2DTexture(textureid, bounds, z, color);
		}
	}

	public virtual void Dispose()
	{
	}
}
