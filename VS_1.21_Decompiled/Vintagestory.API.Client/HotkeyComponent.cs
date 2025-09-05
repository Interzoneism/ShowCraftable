using System.Collections.Generic;
using Cairo;
using Vintagestory.API.Config;

namespace Vintagestory.API.Client;

public class HotkeyComponent : RichTextComponent
{
	private LoadedTexture hotkeyTexture;

	private HotKey hotkey;

	public HotkeyComponent(ICoreClientAPI api, string hotkeycode, CairoFont font)
		: base(api, hotkeycode, font)
	{
		PaddingLeft = 0.0;
		PaddingRight = 1.0;
		if (api.Input.HotKeys.TryGetValue(hotkeycode.ToLowerInvariant(), out hotkey))
		{
			DisplayText = hotkey.CurrentMapping.ToString();
		}
		init();
		hotkeyTexture = new LoadedTexture(api);
		Font = Font.Clone().WithFontSize((float)Font.UnscaledFontsize * 0.9f);
	}

	public override void ComposeElements(Context ctx, ImageSurface surfaceUnused)
	{
	}

	public void GenHotkeyTexture()
	{
		//IL_00cd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d2: Unknown result type (might be due to invalid IL or missing references)
		//IL_010c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0111: Unknown result type (might be due to invalid IL or missing references)
		//IL_016d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0172: Unknown result type (might be due to invalid IL or missing references)
		//IL_018b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0192: Expected O, but got Unknown
		//IL_0194: Unknown result type (might be due to invalid IL or missing references)
		//IL_019b: Expected O, but got Unknown
		List<string> list = new List<string>();
		if (hotkey != null)
		{
			if (hotkey.CurrentMapping.Ctrl)
			{
				list.Add("Ctrl");
			}
			if (hotkey.CurrentMapping.Alt)
			{
				list.Add("Alt");
			}
			if (hotkey.CurrentMapping.Shift)
			{
				list.Add("Shift");
			}
			list.Add(hotkey.CurrentMapping.PrimaryAsString());
			if (hotkey.CurrentMapping.SecondKeyCode.HasValue)
			{
				list.Add(hotkey.CurrentMapping.SecondaryAsString());
			}
		}
		else
		{
			list.Add("?");
		}
		double num = 0.0;
		TextExtents textExtents = Font.GetTextExtents("+");
		double width = ((TextExtents)(ref textExtents)).Width;
		double num2 = 3.0;
		double num3 = 4.0;
		foreach (string item in list)
		{
			textExtents = Font.GetTextExtents(item);
			double num4 = ((TextExtents)(ref textExtents)).Width + GuiElement.scaled(num2 + 2.0 * num3);
			if (num > 0.0)
			{
				num4 += GuiElement.scaled(num2) + width;
			}
			num += num4;
		}
		FontExtents fontExtents = Font.GetFontExtents();
		double height = ((FontExtents)(ref fontExtents)).Height;
		int num5 = (int)height;
		ImageSurface val = new ImageSurface((Format)0, (int)num + 3, num5 + 5);
		Context val2 = new Context((Surface)(object)val);
		Font.SetupContext(val2);
		double x = 0.0;
		double y = 0.0;
		foreach (string item2 in list)
		{
			x = DrawHotkey(api, item2, x, y, val2, Font, num5, height, width, num2, num3, Font.Color);
		}
		api.Gui.LoadOrUpdateCairoTexture(val, linearMag: true, ref hotkeyTexture);
		val2.Dispose();
		((Surface)val).Dispose();
	}

	public override void RenderInteractiveElements(float deltaTime, double renderX, double renderY, double renderZ)
	{
		base.RenderInteractiveElements(deltaTime, renderX, renderY, renderZ);
		TextLine obj = Lines[Lines.Length - 1];
		double fontOrientOffsetX = GetFontOrientOffsetX();
		LineRectangled bounds = obj.Bounds;
		api.Render.Render2DTexture(hotkeyTexture.TextureId, (float)(renderX + fontOrientOffsetX + bounds.X), (int)(renderY + bounds.Y), hotkeyTexture.Width, hotkeyTexture.Height, (float)renderZ + 50f);
	}

	public override EnumCalcBoundsResult CalcBounds(TextFlowPath[] flowPath, double currentLineHeight, double offsetX, double lineY, out double nextOffsetX)
	{
		GenHotkeyTexture();
		linebreak = EnumLinebreakBehavior.None;
		EnumCalcBoundsResult result = base.CalcBounds(flowPath, currentLineHeight, offsetX, lineY, out nextOffsetX);
		BoundsPerLine[0].Width += RuntimeEnv.GUIScale * 4f;
		nextOffsetX += RuntimeEnv.GUIScale * 4f;
		return result;
	}

	public override void Dispose()
	{
		base.Dispose();
		hotkeyTexture?.Dispose();
	}

	public static double DrawHotkey(ICoreClientAPI capi, string keycode, double x, double y, Context ctx, CairoFont font, double lineheight, double textHeight, double pluswdith, double symbolspacing, double leftRightPadding, double[] color)
	{
		//IL_0060: Unknown result type (might be due to invalid IL or missing references)
		//IL_0065: Unknown result type (might be due to invalid IL or missing references)
		if (x > 0.0)
		{
			capi.Gui.Text.DrawTextLine(ctx, font, "+", x + symbolspacing, y + (lineheight - textHeight) / 2.0 + GuiElement.scaled(2.0));
			x += pluswdith + 2.0 * symbolspacing;
		}
		TextExtents textExtents = font.GetTextExtents(keycode);
		double width = ((TextExtents)(ref textExtents)).Width;
		GuiElement.RoundRectangle(ctx, x + 1.0, y + 1.0, (int)(width + GuiElement.scaled(leftRightPadding * 2.0)), lineheight, 3.5);
		ctx.SetSourceRGBA(color);
		ctx.LineWidth = 1.5;
		ctx.StrokePreserve();
		ctx.SetSourceRGBA(new double[4]
		{
			color[0],
			color[1],
			color[2],
			color[3] * 0.5
		});
		ctx.Fill();
		ctx.SetSourceRGBA(new double[4] { 1.0, 1.0, 1.0, 1.0 });
		int num = (int)(x + 1.0 + GuiElement.scaled(leftRightPadding));
		int num2 = (int)(y + (lineheight - textHeight) / 2.0 + GuiElement.scaled(1.0));
		capi.Gui.Text.DrawTextLine(ctx, font, keycode, num, num2);
		return (int)(x + symbolspacing + width + GuiElement.scaled(leftRightPadding * 2.0));
	}
}
