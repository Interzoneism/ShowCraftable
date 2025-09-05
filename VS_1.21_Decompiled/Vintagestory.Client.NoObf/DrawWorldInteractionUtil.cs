using System.Collections.Generic;
using Cairo;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;

namespace Vintagestory.Client.NoObf;

public class DrawWorldInteractionUtil
{
	private ICoreClientAPI capi;

	private GuiDialog.DlgComposers Composers;

	public double ActualWidth;

	private string composerKeyCode;

	public double UnscaledLineHeight = 30.0;

	public float FontSize = 20f;

	private GuiComposer composer;

	public Vec4f Color = ColorUtil.WhiteArgbVec;

	public GuiComposer Composer => Composers[composerKeyCode];

	public DrawWorldInteractionUtil(ICoreClientAPI capi, GuiDialog.DlgComposers composers, string composerSuffixCode)
	{
		this.capi = capi;
		Composers = composers;
		composerKeyCode = "worldInteractionHelp" + composerSuffixCode;
	}

	public void ComposeBlockWorldInteractionHelp(WorldInteraction[] wis)
	{
		if (wis == null || wis.Length == 0)
		{
			Composers.Remove(composerKeyCode);
			return;
		}
		capi.World.FrameProfiler.Mark("blockinteractionhelp-recomp-1");
		ElementBounds elementBounds = ElementStdBounds.AutosizedMainDialog.WithAlignment(EnumDialogArea.CenterMiddle);
		if (composer == null)
		{
			composer = capi.Gui.CreateCompo(composerKeyCode, elementBounds);
		}
		else
		{
			composer.Clear(elementBounds);
		}
		Composers[composerKeyCode] = composer;
		double lineHeight = GuiElement.scaled(UnscaledLineHeight);
		int num = 0;
		foreach (WorldInteraction wi in wis)
		{
			ItemStack[] stacks = wi.Itemstacks;
			if (stacks != null && wi.GetMatchingStacks != null)
			{
				stacks = wi.GetMatchingStacks(wi, capi.World.Player.CurrentBlockSelection, capi.World.Player.CurrentEntitySelection);
				if (stacks == null || stacks.Length == 0)
				{
					continue;
				}
			}
			if (stacks != null || wi.ShouldApply == null || wi.ShouldApply(wi, capi.World.Player.CurrentBlockSelection, capi.World.Player.CurrentEntitySelection))
			{
				double fixedY = (double)num * (UnscaledLineHeight + 8.0);
				ElementBounds elementBounds2 = ElementBounds.Fixed(0.0, fixedY, 600.0, 80.0);
				composer.AddIf(stacks != null && stacks.Length != 0).AddCustomRender(elementBounds2.FlatCopy(), delegate(float dt, ElementBounds bounds)
				{
					long num2 = capi.World.ElapsedMilliseconds / 1000 % stacks.Length;
					float size = (float)lineHeight * 0.8f;
					capi.Render.RenderItemstackToGui(new DummySlot(stacks[num2]), bounds.renderX + lineHeight / 2.0 + 1.0, bounds.renderY + lineHeight / 2.0, 100.0, size, ColorUtil.ColorFromRgba(Color));
				}).EndIf()
					.AddStaticCustomDraw(elementBounds2, delegate(Context ctx, ImageSurface surface, ElementBounds bounds)
					{
						drawHelp(ctx, surface, bounds, stacks, lineHeight, wi);
					});
				num++;
			}
		}
		capi.World.FrameProfiler.Mark("blockinteractionhelp-recomp-2");
		if (num == 0)
		{
			Composers.Remove(composerKeyCode);
			return;
		}
		composer.Compose();
		capi.World.FrameProfiler.Mark("blockinteractionhelp-recomp-3");
	}

	public void drawHelp(Context ctx, ImageSurface surface, ElementBounds currentBounds, ItemStack[] stacks, double lineheight, WorldInteraction wi)
	{
		//IL_00bb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00dc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e1: Unknown result type (might be due to invalid IL or missing references)
		//IL_0434: Unknown result type (might be due to invalid IL or missing references)
		//IL_0439: Unknown result type (might be due to invalid IL or missing references)
		capi.World.FrameProfiler.Mark("blockinteractionhelp-recomp-2.1");
		double num = 0.0;
		double drawY = currentBounds.drawY;
		double[] array = (double[])GuiStyle.DialogDefaultTextColor.Clone();
		array[0] = (array[0] + 1.0) / 2.0;
		array[1] = (array[1] + 1.0) / 2.0;
		array[2] = (array[2] + 1.0) / 2.0;
		CairoFont cairoFont = CairoFont.WhiteMediumText().WithColor(array).WithFontSize(FontSize)
			.WithStroke(GuiStyle.DarkBrownColor, 2.0);
		cairoFont.SetupContext(ctx);
		FontExtents fontExtents = cairoFont.GetFontExtents();
		double height = ((FontExtents)(ref fontExtents)).Height;
		double num2 = 5.0;
		TextExtents textExtents = cairoFont.GetTextExtents("+");
		double width = ((TextExtents)(ref textExtents)).Width;
		if ((stacks != null && stacks.Length != 0) || wi.RequireFreeHand)
		{
			GuiElement.RoundRectangle(ctx, num, drawY + 1.0, lineheight, lineheight, 3.5);
			ctx.SetSourceRGBA(array);
			ctx.LineWidth = 1.5;
			ctx.StrokePreserve();
			ctx.SetSourceRGBA(new double[4] { 1.0, 1.0, 1.0, 0.5 });
			ctx.Fill();
			ctx.SetSourceRGBA(new double[4] { 1.0, 1.0, 1.0, 1.0 });
			num += lineheight + num2 + 1.0;
		}
		List<HotKey> list = new List<HotKey>();
		if (wi.HotKeyCodes != null)
		{
			string[] hotKeyCodes = wi.HotKeyCodes;
			foreach (string toggleKeyCombinationCode in hotKeyCodes)
			{
				HotKey hotKeyByCode = capi.Input.GetHotKeyByCode(toggleKeyCombinationCode);
				if (hotKeyByCode != null)
				{
					list.Add(hotKeyByCode);
				}
			}
		}
		else
		{
			HotKey hotKeyByCode2 = capi.Input.GetHotKeyByCode(wi.HotKeyCode);
			if (hotKeyByCode2 != null)
			{
				list.Add(hotKeyByCode2);
			}
		}
		foreach (HotKey item in list)
		{
			if (!(item.Code != "ctrl") || item.CurrentMapping.Ctrl)
			{
				num = DrawHotkey(item, num, drawY, ctx, cairoFont, lineheight, height, width, num2, array);
			}
		}
		foreach (HotKey item2 in list)
		{
			if (!(item2.Code != "shift") || item2.CurrentMapping.Shift)
			{
				num = DrawHotkey(item2, num, drawY, ctx, cairoFont, lineheight, height, width, num2, array);
			}
		}
		foreach (HotKey item3 in list)
		{
			if (!(item3.Code == "shift") && !(item3.Code == "ctrl") && !item3.CurrentMapping.Shift && !item3.CurrentMapping.Ctrl)
			{
				num = DrawHotkey(item3, num, drawY, ctx, cairoFont, lineheight, height, width, num2, array);
			}
		}
		if (wi.MouseButton == EnumMouseButton.Left)
		{
			HotKey hotKeyByCode3 = capi.Input.GetHotKeyByCode("primarymouse");
			num = DrawHotkey(hotKeyByCode3, num, drawY, ctx, cairoFont, lineheight, height, width, num2, array);
		}
		if (wi.MouseButton == EnumMouseButton.Right)
		{
			HotKey hotKeyByCode4 = capi.Input.GetHotKeyByCode("secondarymouse");
			num = DrawHotkey(hotKeyByCode4, num, drawY, ctx, cairoFont, lineheight, height, width, num2, array);
		}
		capi.World.FrameProfiler.Mark("blockinteractionhelp-recomp-2.2");
		string text = ": " + Lang.Get(wi.ActionLangCode);
		capi.Gui.Text.DrawTextLine(ctx, cairoFont, text, num - 4.0, drawY + (lineheight - height) / 2.0 + 2.0);
		double num3 = num;
		textExtents = cairoFont.GetTextExtents(text);
		ActualWidth = num3 + ((TextExtents)(ref textExtents)).Width;
		capi.World.FrameProfiler.Mark("blockinteractionhelp-recomp-2.3");
	}

	private double DrawHotkey(HotKey hk, double x, double y, Context ctx, CairoFont font, double lineheight, double textHeight, double pluswdith, double symbolspacing, double[] color)
	{
		KeyCombination currentMapping = hk.CurrentMapping;
		if (currentMapping.IsMouseButton(currentMapping.KeyCode))
		{
			return DrawMouseButton(currentMapping.KeyCode - 240, x, y, ctx, font, lineheight, textHeight, pluswdith, symbolspacing, color);
		}
		if (currentMapping.Ctrl)
		{
			x = HotkeyComponent.DrawHotkey(capi, GlKeyNames.ToString(GlKeys.LControl), x, y, ctx, font, lineheight, textHeight, pluswdith, symbolspacing, 10.0, color);
		}
		if (currentMapping.Shift)
		{
			x = HotkeyComponent.DrawHotkey(capi, GlKeyNames.ToString(GlKeys.LShift), x, y, ctx, font, lineheight, textHeight, pluswdith, symbolspacing, 10.0, color);
		}
		x = HotkeyComponent.DrawHotkey(capi, currentMapping.PrimaryAsString(), x, y, ctx, font, lineheight, textHeight, pluswdith, symbolspacing, 10.0, color);
		return x;
	}

	private double DrawMouseButton(int button, double x, double y, Context ctx, CairoFont font, double lineheight, double textHeight, double pluswdith, double symbolspacing, double[] color)
	{
		object obj;
		switch (button)
		{
		case 0:
		case 2:
			if (x > 0.0)
			{
				capi.Gui.Text.DrawTextLine(ctx, font, "+", (double)(int)x + symbolspacing, y + (double)(int)((lineheight - textHeight) / 2.0) + 2.0);
				x += pluswdith + 2.0 * symbolspacing;
			}
			capi.Gui.Icons.DrawIcon(ctx, (button == 0) ? "leftmousebutton" : "rightmousebutton", x, y + 1.0, lineheight, lineheight, color);
			return x + lineheight + symbolspacing + 1.0;
		default:
			obj = "b" + button;
			break;
		case 1:
			obj = "mb";
			break;
		}
		string keycode = (string)obj;
		return HotkeyComponent.DrawHotkey(capi, keycode, x, y, ctx, font, lineheight, textHeight, pluswdith, symbolspacing, 8.0, color);
	}

	public void Dispose()
	{
		composer?.Dispose();
	}
}
