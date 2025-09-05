using System;
using Cairo;
using Vintagestory.API.MathTools;

namespace Vintagestory.API.Client;

public class GuiElementHorizontalTabs : GuiElementTextBase
{
	private Action<int> handler;

	internal GuiTab[] tabs;

	private LoadedTexture baseTexture;

	private LoadedTexture[] hoverTextures;

	private LoadedTexture[] notifyTextures;

	private LoadedTexture[] arrowTextures;

	private int[] tabWidths;

	private double[] tabOffsets;

	private CairoFont selectedFont;

	private double totalWidth;

	private double currentScrollOffset;

	public int activeElement;

	public double unscaledTabSpacing = 5.0;

	public double unscaledTabPadding = 4.0;

	public bool AlarmTabs;

	private float fontHeight;

	private CairoFont notifyFont;

	private double maxScrollOffset => Math.Max(0.0, totalWidth - Bounds.InnerWidth + GuiElement.scaled(unscaledTabSpacing));

	private bool displayLeftArrow => currentScrollOffset > GuiElement.scaled(unscaledTabSpacing);

	private bool displayRightArrow => currentScrollOffset < maxScrollOffset - GuiElement.scaled(unscaledTabSpacing);

	public bool[] TabHasAlarm { get; set; }

	public override bool Focusable => enabled;

	public GuiElementHorizontalTabs(ICoreClientAPI capi, GuiTab[] tabs, CairoFont font, CairoFont selectedFont, ElementBounds bounds, Action<int> onTabClicked)
		: base(capi, "", font, bounds)
	{
		this.selectedFont = selectedFont;
		this.tabs = tabs;
		TabHasAlarm = new bool[tabs.Length];
		handler = onTabClicked;
		hoverTextures = new LoadedTexture[tabs.Length];
		for (int i = 0; i < tabs.Length; i++)
		{
			hoverTextures[i] = new LoadedTexture(capi);
		}
		arrowTextures = new LoadedTexture[2];
		arrowTextures[0] = new LoadedTexture(capi);
		arrowTextures[1] = new LoadedTexture(capi);
		tabWidths = new int[tabs.Length];
		tabOffsets = new double[tabs.Length];
		baseTexture = new LoadedTexture(capi);
	}

	[Obsolete("Use TabHasAlarm[] property instead. Used by the chat window to mark a tab/chat as unread")]
	public void SetAlarmTab(int tabIndex)
	{
	}

	public void WithAlarmTabs(CairoFont notifyFont)
	{
		this.notifyFont = notifyFont;
		notifyTextures = new LoadedTexture[tabs.Length];
		for (int i = 0; i < tabs.Length; i++)
		{
			notifyTextures[i] = new LoadedTexture(api);
		}
		AlarmTabs = true;
		ComposeOverlays(isNotifyTabs: true);
	}

	public override void ComposeTextElements(Context ctxStatic, ImageSurface surfaceStatic)
	{
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0023: Expected O, but got Unknown
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		//IL_002a: Expected O, but got Unknown
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0042: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a9: Unknown result type (might be due to invalid IL or missing references)
		//IL_011a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0120: Expected O, but got Unknown
		//IL_0121: Unknown result type (might be due to invalid IL or missing references)
		//IL_0127: Expected O, but got Unknown
		ImageSurface val = new ImageSurface((Format)0, (int)Bounds.InnerWidth + 1, (int)Bounds.InnerHeight + 1);
		Context val2 = new Context((Surface)(object)val);
		Font.SetupContext(val2);
		FontExtents fontExtents = Font.GetFontExtents();
		fontHeight = (float)((FontExtents)(ref fontExtents)).Height;
		double num = GuiElement.scaled(1.0);
		double num2 = GuiElement.scaled(unscaledTabSpacing);
		double num3 = GuiElement.scaled(unscaledTabPadding);
		totalWidth = 0.0;
		for (int i = 0; i < tabs.Length; i++)
		{
			int[] array = tabWidths;
			int num4 = i;
			TextExtents val3 = val2.TextExtents(tabs[i].Name);
			array[num4] = (int)(((TextExtents)(ref val3)).Width + 2.0 * num3 + 1.0);
			totalWidth += num2 + (double)tabWidths[i];
		}
		val2.Dispose();
		((Surface)val).Dispose();
		val = new ImageSurface((Format)0, (int)totalWidth + 1, (int)Bounds.InnerHeight + 1);
		val2 = new Context((Surface)(object)val);
		double num5 = num2;
		Font.Color[3] = 0.5;
		for (int j = 0; j < tabs.Length; j++)
		{
			val2.NewPath();
			val2.MoveTo(num5, Bounds.InnerHeight);
			val2.LineTo(num5, num);
			val2.Arc(num5 + num, num, num, 3.1415927410125732, 4.71238899230957);
			val2.Arc(num5 + (double)tabWidths[j] - num, num, num, -1.5707963705062866, 0.0);
			val2.LineTo(num5 + (double)tabWidths[j], Bounds.InnerHeight);
			val2.ClosePath();
			double[] dialogDefaultBgColor = GuiStyle.DialogDefaultBgColor;
			val2.SetSourceRGBA(dialogDefaultBgColor[0], dialogDefaultBgColor[1], dialogDefaultBgColor[2], dialogDefaultBgColor[3] * 0.75);
			val2.FillPreserve();
			ShadePath(val2);
			if (AlarmTabs)
			{
				notifyFont.SetupContext(val2);
			}
			else
			{
				Font.SetupContext(val2);
			}
			DrawTextLineAt(val2, tabs[j].Name, num5 + num3, ((float)val.Height - fontHeight) / 2f);
			num5 += (double)tabWidths[j] + num2;
			tabOffsets[j] = num5;
		}
		Font.Color[3] = 1.0;
		ComposeOverlays();
		ComposeArrows();
		generateTexture(val, ref baseTexture);
		val2.Dispose();
		((Surface)val).Dispose();
	}

	private void ComposeOverlays(bool isNotifyTabs = false)
	{
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		//IL_004b: Expected O, but got Unknown
		double num = GuiElement.scaled(1.0);
		GuiElement.scaled(unscaledTabSpacing);
		double posX = GuiElement.scaled(unscaledTabPadding);
		for (int i = 0; i < tabs.Length; i++)
		{
			ImageSurface val = new ImageSurface((Format)0, tabWidths[i], (int)Bounds.InnerHeight + 1);
			Context val2 = genContext(val);
			double num2 = Math.PI / 180.0;
			val2.SetSourceRGBA(1.0, 1.0, 1.0, 0.0);
			val2.Paint();
			val2.NewPath();
			val2.MoveTo(0.0, Bounds.InnerHeight + 1.0);
			val2.LineTo(0.0, num);
			val2.Arc(num, num, num, 180.0 * num2, 270.0 * num2);
			val2.Arc((double)tabWidths[i] - num, num, num, -90.0 * num2, 0.0 * num2);
			val2.LineTo((double)tabWidths[i], (double)val.Height);
			val2.ClosePath();
			double[] dialogDefaultBgColor = GuiStyle.DialogDefaultBgColor;
			val2.SetSourceRGBA(dialogDefaultBgColor[0], dialogDefaultBgColor[1], dialogDefaultBgColor[2], dialogDefaultBgColor[3] * 0.75);
			val2.FillPreserve();
			val2.SetSourceRGBA(dialogDefaultBgColor[0] * 1.6, dialogDefaultBgColor[1] * 1.6, dialogDefaultBgColor[2] * 1.6, 1.0);
			val2.LineWidth = 3.5;
			val2.StrokePreserve();
			SurfaceTransformBlur.BlurPartial(val, 5.2, 10);
			val2.SetSourceRGBA(dialogDefaultBgColor[0], dialogDefaultBgColor[1], dialogDefaultBgColor[2], dialogDefaultBgColor[3] * 0.75);
			val2.LineWidth = 1.0;
			val2.StrokePreserve();
			val2.NewPath();
			val2.MoveTo(0.0, Bounds.InnerHeight);
			val2.LineTo(0.0, num);
			val2.Arc(num, num, num, 180.0 * num2, 270.0 * num2);
			val2.Arc((double)tabWidths[i] - num, num, num, -90.0 * num2, 0.0 * num2);
			val2.LineTo((double)tabWidths[i], Bounds.InnerHeight);
			ShadePath(val2);
			if (isNotifyTabs)
			{
				notifyFont.SetupContext(val2);
			}
			else
			{
				selectedFont.SetupContext(val2);
			}
			val2.Operator = (Operator)0;
			val2.Rectangle(0.0, (double)(val.Height - 1), (double)val.Width, 1.0);
			val2.Fill();
			val2.Operator = (Operator)2;
			DrawTextLineAt(val2, tabs[i].Name, posX, ((float)val.Height - fontHeight) / 2f);
			if (isNotifyTabs)
			{
				generateTexture(val, ref notifyTextures[i]);
			}
			else
			{
				generateTexture(val, ref hoverTextures[i]);
			}
			val2.Dispose();
			((Surface)val).Dispose();
		}
	}

	private void ComposeArrows()
	{
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0035: Expected O, but got Unknown
		//IL_0216: Unknown result type (might be due to invalid IL or missing references)
		//IL_021c: Expected O, but got Unknown
		ImageSurface val = new ImageSurface((Format)0, (int)((Bounds.InnerHeight - 2.0) / 2.0), (int)Bounds.InnerHeight - 2);
		Context val2 = genContext(val);
		val2.SetSourceRGBA(ColorUtil.Hex2Doubles("#a88b6c", 1.0));
		GuiElement.RoundRectangle(val2, 0.0, 0.0, val.Width, val.Height, 1.0);
		val2.Fill();
		EmbossRoundRectangleElement(val2, 0.0, 0.0, val.Width, val.Height, inverse: false, 2, 1);
		val2.NewPath();
		val2.LineTo(GuiElement.scaled(1.0) * Scale + 1.0, (double)val.Height - GuiElement.scaled(9.5) * Scale);
		val2.LineTo(((double)val.Width - GuiElement.scaled(2.0) - 1.0) * Scale, (double)val.Height - GuiElement.scaled(14.25) * Scale);
		val2.LineTo(((double)val.Width - GuiElement.scaled(2.0) - 1.0) * Scale, (double)val.Height - GuiElement.scaled(4.75) * Scale);
		val2.ClosePath();
		val2.SetSourceRGBA(1.0, 1.0, 1.0, 1.0);
		val2.Fill();
		generateTexture(val, ref arrowTextures[0]);
		((Surface)val).Dispose();
		val2.Dispose();
		val = new ImageSurface((Format)0, (int)((Bounds.InnerHeight - 2.0) / 2.0), (int)Bounds.InnerHeight - 2);
		val2 = genContext(val);
		val2.SetSourceRGBA(ColorUtil.Hex2Doubles("#a88b6c", 1.0));
		GuiElement.RoundRectangle(val2, 0.0, 0.0, val.Width, val.Height, 1.0);
		val2.Fill();
		EmbossRoundRectangleElement(val2, 0.0, 0.0, val.Width, val.Height, inverse: false, 2, 1);
		val2.NewPath();
		val2.LineTo(((double)val.Width - GuiElement.scaled(2.0) - 1.0) * Scale, (double)val.Height - GuiElement.scaled(9.5) * Scale);
		val2.LineTo(GuiElement.scaled(1.0) * Scale + 1.0, (double)val.Height - GuiElement.scaled(14.25) * Scale);
		val2.LineTo(GuiElement.scaled(1.0) * Scale + 1.0, (double)val.Height - GuiElement.scaled(4.75) * Scale);
		val2.ClosePath();
		val2.SetSourceRGBA(1.0, 1.0, 1.0, 1.0);
		val2.Fill();
		generateTexture(val, ref arrowTextures[1]);
		val2.Dispose();
		((Surface)val).Dispose();
	}

	public override void RenderInteractiveElements(float deltaTime)
	{
		api.Render.PushScissor(Bounds, stacking: true);
		api.Render.Render2DTexture(baseTexture.TextureId, (int)(Bounds.renderX - currentScrollOffset), (int)Bounds.renderY, (int)totalWidth + 1, (int)Bounds.InnerHeight + 1);
		api.Render.PopScissor();
		double num = GuiElement.scaled(unscaledTabSpacing);
		int num2 = api.Input.MouseX - (int)Bounds.absX;
		int num3 = api.Input.MouseY - (int)Bounds.absY;
		double num4 = num;
		for (int i = 0; i < tabs.Length; i++)
		{
			if ((i == activeElement || ((double)num2 > num4 - currentScrollOffset && (double)num2 < num4 + (double)tabWidths[i] - currentScrollOffset && num3 > 0 && (double)num3 < Bounds.InnerHeight && num2 > 0 && (double)num2 < Bounds.InnerWidth)) && (i == activeElement || ((!displayLeftArrow || num2 <= 0 || num2 >= arrowTextures[0].Width) && (!displayRightArrow || !((double)num2 > Bounds.InnerWidth - (double)arrowTextures[1].Width) || !((double)num2 < Bounds.InnerWidth)))))
			{
				api.Render.PushScissor(Bounds, stacking: true);
				api.Render.Render2DTexturePremultipliedAlpha(hoverTextures[i].TextureId, (int)((double)(int)(Bounds.renderX - currentScrollOffset) + num4), (int)Bounds.renderY, tabWidths[i], (int)Bounds.InnerHeight + 1);
				api.Render.PopScissor();
			}
			if (TabHasAlarm[i])
			{
				api.Render.PushScissor(Bounds, stacking: true);
				api.Render.Render2DTexturePremultipliedAlpha(notifyTextures[i].TextureId, (int)((double)(int)(Bounds.renderX - currentScrollOffset) + num4), (int)Bounds.renderY, tabWidths[i], (int)Bounds.InnerHeight + 1);
				api.Render.PopScissor();
			}
			num4 += (double)tabWidths[i] + num;
		}
		if (displayLeftArrow)
		{
			api.Render.Render2DTexturePremultipliedAlpha(arrowTextures[0].TextureId, (int)Bounds.renderX, (int)Bounds.renderY + 1, (int)((Bounds.InnerHeight - 2.0) / 2.0), (int)Bounds.InnerHeight - 2);
		}
		if (displayRightArrow)
		{
			api.Render.Render2DTexturePremultipliedAlpha(arrowTextures[1].TextureId, (double)(int)Bounds.renderX + Bounds.InnerWidth - (double)arrowTextures[1].Width, (int)Bounds.renderY + 1, (int)((Bounds.InnerHeight - 2.0) / 2.0), (int)Bounds.InnerHeight - 2);
		}
	}

	public override void OnKeyDown(ICoreClientAPI api, KeyEvent args)
	{
		if (base.HasFocus)
		{
			if (args.KeyCode == 48)
			{
				args.Handled = true;
				SetValue((activeElement + 1) % tabs.Length);
			}
			if (args.KeyCode == 47)
			{
				SetValue(GameMath.Mod(activeElement - 1, tabs.Length));
				args.Handled = true;
			}
		}
	}

	public override void OnMouseWheel(ICoreClientAPI api, MouseWheelEventArgs args)
	{
		if (enabled && Bounds.PointInside(api.Input.MouseX, api.Input.MouseY))
		{
			args.SetHandled();
			double num = (double)args.deltaPrecise * GuiElement.scaled(10.0);
			if ((!(currentScrollOffset <= 0.0) || !(num < 0.0)) && (!(currentScrollOffset >= maxScrollOffset) || !(num > 0.0)))
			{
				currentScrollOffset = Math.Clamp(currentScrollOffset + num, 0.0, maxScrollOffset);
			}
		}
	}

	public override void OnMouseDownOnElement(ICoreClientAPI api, MouseEvent args)
	{
		base.OnMouseDownOnElement(api, args);
		double num = GuiElement.scaled(unscaledTabSpacing);
		double num2 = num;
		int num3 = api.Input.MouseX - (int)Bounds.absX;
		int num4 = api.Input.MouseY - (int)Bounds.absY;
		if (displayLeftArrow && num3 > 0 && num3 < arrowTextures[0].Width)
		{
			currentScrollOffset -= GuiElement.scaled(5.0);
			return;
		}
		if (displayRightArrow && (double)num3 > Bounds.InnerWidth - (double)arrowTextures[1].Width && (double)num3 < Bounds.InnerWidth)
		{
			currentScrollOffset += GuiElement.scaled(5.0);
			return;
		}
		for (int i = 0; i < tabs.Length; i++)
		{
			if ((double)num3 > num2 - currentScrollOffset && (double)num3 < num2 + (double)tabWidths[i] - currentScrollOffset && num4 > 0 && (double)num4 < Bounds.InnerHeight)
			{
				SetValue(i);
				break;
			}
			num2 += (double)tabWidths[i] + num;
		}
	}

	public void SetValue(int selectedIndex, bool callhandler = true)
	{
		if (callhandler)
		{
			handler(tabs[selectedIndex].DataInt);
			api.Gui.PlaySound("menubutton_wood");
		}
		activeElement = selectedIndex;
		double num = tabOffsets[activeElement] - Bounds.InnerWidth + (double)arrowTextures[1].Width;
		if (currentScrollOffset < num)
		{
			currentScrollOffset = Math.Clamp(num, 0.0, maxScrollOffset);
		}
		num = tabOffsets[activeElement] - (double)tabWidths[activeElement] - GuiElement.scaled(unscaledTabSpacing) - (double)arrowTextures[0].Width;
		if (currentScrollOffset > num)
		{
			currentScrollOffset = Math.Clamp(num, 0.0, maxScrollOffset);
		}
	}

	public override void Dispose()
	{
		base.Dispose();
		baseTexture?.Dispose();
		LoadedTexture[] array = arrowTextures;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].Dispose();
		}
		for (int j = 0; j < hoverTextures.Length; j++)
		{
			hoverTextures[j].Dispose();
			if (notifyTextures != null)
			{
				notifyTextures[j].Dispose();
			}
		}
	}
}
