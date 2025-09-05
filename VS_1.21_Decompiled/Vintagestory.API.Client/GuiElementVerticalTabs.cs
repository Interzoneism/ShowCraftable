using System;
using Cairo;
using Vintagestory.API.MathTools;

namespace Vintagestory.API.Client;

public class GuiElementVerticalTabs : GuiElementTextBase
{
	protected Action<int, GuiTab> handler;

	protected GuiTab[] tabs;

	protected LoadedTexture baseTexture;

	protected LoadedTexture[] hoverTextures;

	protected int[] tabWidths;

	protected CairoFont selectedFont;

	protected double unscaledTabSpacing = 5.0;

	protected double unscaledTabHeight = 25.0;

	protected double unscaledTabPadding = 3.0;

	protected double tabHeight;

	protected double textOffsetY;

	public int ActiveElement;

	public bool Right;

	public bool ToggleTabs;

	public override bool Focusable => enabled;

	public GuiElementVerticalTabs(ICoreClientAPI capi, GuiTab[] tabs, CairoFont font, CairoFont selectedFont, ElementBounds bounds, Action<int, GuiTab> onTabClicked)
		: base(capi, "", font, bounds)
	{
		this.selectedFont = selectedFont;
		this.tabs = tabs;
		handler = onTabClicked;
		hoverTextures = new LoadedTexture[tabs.Length];
		for (int i = 0; i < tabs.Length; i++)
		{
			hoverTextures[i] = new LoadedTexture(capi);
		}
		baseTexture = new LoadedTexture(capi);
		tabWidths = new int[tabs.Length];
		if (tabs.Length != 0)
		{
			tabs[0].Active = true;
		}
	}

	public override void ComposeTextElements(Context ctxStatic, ImageSurface surfaceStatic)
	{
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_002e: Expected O, but got Unknown
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0035: Expected O, but got Unknown
		//IL_00b2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ef: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f4: Unknown result type (might be due to invalid IL or missing references)
		Bounds.CalcWorldBounds();
		ImageSurface val = new ImageSurface((Format)0, (int)Bounds.InnerWidth + 1, (int)Bounds.InnerHeight + 1);
		Context val2 = new Context((Surface)(object)val);
		double num = GuiElement.scaled(1.0);
		double num2 = GuiElement.scaled(unscaledTabSpacing);
		double num3 = GuiElement.scaled(unscaledTabPadding);
		tabHeight = GuiElement.scaled(unscaledTabHeight);
		double num4 = 0.0;
		Font.Color[3] = 0.85;
		Font.SetupContext(val2);
		double num5 = tabHeight + 1.0;
		FontExtents fontExtents = Font.GetFontExtents();
		textOffsetY = (num5 - ((FontExtents)(ref fontExtents)).Height) / 2.0;
		double num6 = 0.0;
		for (int i = 0; i < tabs.Length; i++)
		{
			TextExtents val3 = val2.TextExtents(tabs[i].Name);
			num6 = Math.Max((int)(((TextExtents)(ref val3)).Width + 1.0 + 2.0 * num3), num6);
		}
		for (int j = 0; j < tabs.Length; j++)
		{
			tabWidths[j] = (int)num6 + 1;
			double num7;
			if (Right)
			{
				num7 = 1.0;
				num4 += tabs[j].PaddingTop;
				val2.NewPath();
				val2.MoveTo(num7, num4 + tabHeight);
				val2.LineTo(num7, num4);
				val2.LineTo(num7 + (double)tabWidths[j] + num, num4);
				val2.ArcNegative(num7 + (double)tabWidths[j], num4 + num, num, 4.71238899230957, 3.1415927410125732);
				val2.ArcNegative(num7 + (double)tabWidths[j], num4 - num + tabHeight, num, 3.1415927410125732, 1.5707963705062866);
			}
			else
			{
				num7 = (int)Bounds.InnerWidth + 1;
				num4 += tabs[j].PaddingTop;
				val2.NewPath();
				val2.MoveTo(num7, num4 + tabHeight);
				val2.LineTo(num7, num4);
				val2.LineTo(num7 - (double)tabWidths[j] + num, num4);
				val2.ArcNegative(num7 - (double)tabWidths[j], num4 + num, num, 4.71238899230957, 3.1415927410125732);
				val2.ArcNegative(num7 - (double)tabWidths[j], num4 - num + tabHeight, num, 3.1415927410125732, 1.5707963705062866);
			}
			val2.ClosePath();
			double[] dialogDefaultBgColor = GuiStyle.DialogDefaultBgColor;
			val2.SetSourceRGBA(dialogDefaultBgColor[0], dialogDefaultBgColor[1], dialogDefaultBgColor[2], dialogDefaultBgColor[3]);
			val2.FillPreserve();
			ShadePath(val2);
			Font.SetupContext(val2);
			DrawTextLineAt(val2, tabs[j].Name, num7 - (double)((!Right) ? tabWidths[j] : 0) + num3, num4 + textOffsetY);
			num4 += tabHeight + num2;
		}
		Font.Color[3] = 1.0;
		ComposeOverlays();
		generateTexture(val, ref baseTexture);
		val2.Dispose();
		((Surface)val).Dispose();
	}

	private void ComposeOverlays()
	{
		//IL_0036: Unknown result type (might be due to invalid IL or missing references)
		//IL_003d: Expected O, but got Unknown
		double num = GuiElement.scaled(1.0);
		double num2 = GuiElement.scaled(unscaledTabPadding);
		for (int i = 0; i < tabs.Length; i++)
		{
			ImageSurface val = new ImageSurface((Format)0, tabWidths[i] + 1, (int)tabHeight + 1);
			Context val2 = genContext(val);
			double num3 = tabWidths[i] + 1;
			val2.SetSourceRGBA(1.0, 1.0, 1.0, 0.0);
			val2.Paint();
			val2.NewPath();
			val2.MoveTo(num3, tabHeight + 1.0);
			val2.LineTo(num3, 0.0);
			val2.LineTo(num, 0.0);
			val2.ArcNegative(0.0, num, num, 4.71238899230957, 3.1415927410125732);
			val2.ArcNegative(0.0, tabHeight - num, num, 3.1415927410125732, 1.5707963705062866);
			val2.ClosePath();
			double[] dialogDefaultBgColor = GuiStyle.DialogDefaultBgColor;
			val2.SetSourceRGBA(dialogDefaultBgColor[0], dialogDefaultBgColor[1], dialogDefaultBgColor[2], dialogDefaultBgColor[3]);
			val2.Fill();
			val2.NewPath();
			if (Right)
			{
				val2.LineTo(1.0, 1.0);
				val2.LineTo(num3, 1.0);
				val2.LineTo(num3, 1.0 + tabHeight - 1.0);
				val2.LineTo(1.0, tabHeight - 1.0);
			}
			else
			{
				val2.LineTo(1.0 + num3, 1.0);
				val2.LineTo(1.0, 1.0);
				val2.LineTo(1.0, tabHeight - 1.0);
				val2.LineTo(1.0 + num3, 1.0 + tabHeight - 1.0);
			}
			float num4 = 2f;
			val2.SetSourceRGBA(GuiStyle.DialogLightBgColor[0] * 1.6, GuiStyle.DialogStrongBgColor[1] * 1.6, GuiStyle.DialogStrongBgColor[2] * 1.6, 1.0);
			val2.LineWidth = (double)num4 * 1.75;
			val2.StrokePreserve();
			SurfaceTransformBlur.BlurPartial(val, 8.0, 16);
			val2.SetSourceRGBA(new double[4]
			{
				0.17647058823529413,
				7.0 / 51.0,
				11.0 / 85.0,
				1.0
			});
			val2.LineWidth = num4;
			val2.Stroke();
			selectedFont.SetupContext(val2);
			DrawTextLineAt(val2, tabs[i].Name, num2 + 2.0, textOffsetY);
			generateTexture(val, ref hoverTextures[i]);
			val2.Dispose();
			((Surface)val).Dispose();
		}
	}

	public override void RenderInteractiveElements(float deltaTime)
	{
		api.Render.Render2DTexture(baseTexture.TextureId, (int)Bounds.renderX, (int)Bounds.renderY, (int)Bounds.InnerWidth + 1, (int)Bounds.InnerHeight + 1);
		double num = GuiElement.scaled(unscaledTabSpacing);
		int num2 = api.Input.MouseX - (int)Bounds.absX;
		int num3 = api.Input.MouseY - (int)Bounds.absY;
		double num4 = (int)Bounds.InnerWidth;
		double num5 = 0.0;
		for (int i = 0; i < tabs.Length; i++)
		{
			GuiTab guiTab = tabs[i];
			num5 += guiTab.PaddingTop;
			if (Right)
			{
				if (guiTab.Active || (num2 >= 0 && (double)num2 < num4 && (double)num3 > num5 && (double)num3 < num5 + tabHeight))
				{
					api.Render.Render2DTexturePremultipliedAlpha(hoverTextures[i].TextureId, (int)Bounds.renderX, (int)(Bounds.renderY + num5), tabWidths[i] + 1, (int)tabHeight + 1);
				}
			}
			else if (guiTab.Active || ((double)num2 > num4 - (double)tabWidths[i] - 3.0 && (double)num2 < num4 && (double)num3 > num5 && (double)num3 < num5 + tabHeight))
			{
				api.Render.Render2DTexturePremultipliedAlpha(hoverTextures[i].TextureId, (int)(Bounds.renderX + num4 - (double)tabWidths[i] - 1.0), (int)(Bounds.renderY + num5), tabWidths[i] + 1, (int)tabHeight + 1);
			}
			num5 += tabHeight + num;
		}
	}

	public override void OnKeyDown(ICoreClientAPI api, KeyEvent args)
	{
		if (base.HasFocus)
		{
			if (args.KeyCode == 46)
			{
				args.Handled = true;
				SetValue((ActiveElement + 1) % tabs.Length);
			}
			if (args.KeyCode == 45)
			{
				SetValue(GameMath.Mod(ActiveElement - 1, tabs.Length));
				args.Handled = true;
			}
		}
	}

	public override void OnMouseDownOnElement(ICoreClientAPI api, MouseEvent args)
	{
		double num = GuiElement.scaled(unscaledTabSpacing);
		double num2 = Bounds.InnerWidth + 1.0;
		double num3 = 0.0;
		int num4 = api.Input.MouseX - (int)Bounds.absX;
		int num5 = api.Input.MouseY - (int)Bounds.absY;
		for (int i = 0; i < tabs.Length; i++)
		{
			num3 += tabs[i].PaddingTop;
			bool num6 = (double)num4 > num2 - (double)tabWidths[i] - 3.0 && (double)num4 < num2;
			bool flag = (double)num5 > num3 && (double)num5 < num3 + tabHeight + num;
			if (num6 && flag)
			{
				SetValue(i);
				args.Handled = true;
				break;
			}
			num3 += tabHeight + num;
		}
	}

	public void SetValue(int index)
	{
		api.Gui.PlaySound("menubutton_wood");
		if (!ToggleTabs)
		{
			GuiTab[] array = tabs;
			for (int i = 0; i < array.Length; i++)
			{
				array[i].Active = false;
			}
		}
		tabs[index].Active = !tabs[index].Active;
		handler(index, tabs[index]);
		ActiveElement = index;
	}

	public void SetValue(int index, bool triggerHandler)
	{
		if (!ToggleTabs)
		{
			GuiTab[] array = tabs;
			for (int i = 0; i < array.Length; i++)
			{
				array[i].Active = false;
			}
		}
		tabs[index].Active = !tabs[index].Active;
		if (triggerHandler)
		{
			handler(index, tabs[index]);
			api.Gui.PlaySound("menubutton_wood");
		}
		ActiveElement = index;
	}

	public override void Dispose()
	{
		base.Dispose();
		for (int i = 0; i < hoverTextures.Length; i++)
		{
			hoverTextures[i].Dispose();
		}
		baseTexture.Dispose();
	}
}
