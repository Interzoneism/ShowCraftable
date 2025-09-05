using System;
using Cairo;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.Common;

namespace Vintagestory.Client.NoObf;

public class GuiElementModCell : GuiElementTextBase, IGuiElementCell, IDisposable
{
	public static double unscaledRightBoxWidth = 40.0;

	public ModCellEntry cell;

	private double titleTextheight;

	private bool showModifyIcons = true;

	public bool On;

	internal int leftHighlightTextureId;

	internal int rightHighlightTextureId;

	internal int switchOnTextureId;

	internal double unscaledSwitchPadding = 4.0;

	internal double unscaledSwitchSize = 25.0;

	private LoadedTexture modcellTexture;

	private LoadedTexture warningTextTexture;

	private IAsset warningIcon;

	private ICoreClientAPI capi;

	public Action<int> OnMouseDownOnCellLeft;

	public Action<int> OnMouseDownOnCellRight;

	ElementBounds IGuiElementCell.Bounds => Bounds;

	public GuiElementModCell(ICoreClientAPI capi, ModCellEntry cell, ElementBounds bounds, IAsset warningIcon)
		: base(capi, "", null, bounds)
	{
		this.cell = cell;
		if (cell.TitleFont == null)
		{
			cell.TitleFont = CairoFont.WhiteSmallishText();
		}
		if (cell.DetailTextFont == null)
		{
			cell.DetailTextFont = CairoFont.WhiteSmallText();
			cell.DetailTextFont.Color[3] *= 0.6;
		}
		modcellTexture = new LoadedTexture(capi);
		if (cell.Mod.Info?.Dependencies != null)
		{
			foreach (ModDependency dependency in cell.Mod.Info.Dependencies)
			{
				if (dependency.Version.Length != 0 && !(dependency.Version == "*") && cell.Mod.Enabled && (dependency.ModID == "game" || dependency.ModID == "creative" || dependency.ModID == "survival") && !GameVersion.IsCompatibleApiVersion(dependency.Version))
				{
					this.warningIcon = warningIcon;
					warningTextTexture = capi.Gui.TextTexture.GenTextTexture(Lang.Get("mod-versionmismatch", dependency.Version, "1.21.0"), CairoFont.WhiteDetailText(), new TextBackground
					{
						FillColor = GuiStyle.DialogLightBgColor,
						Padding = 3,
						Radius = GuiStyle.ElementBGRadius
					});
				}
			}
		}
		this.capi = capi;
	}

	private void Compose()
	{
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_003d: Expected O, but got Unknown
		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0044: Expected O, but got Unknown
		//IL_0250: Unknown result type (might be due to invalid IL or missing references)
		//IL_0255: Unknown result type (might be due to invalid IL or missing references)
		ComposeHover(left: true, ref leftHighlightTextureId);
		ComposeHover(left: false, ref rightHighlightTextureId);
		genOnTexture();
		ImageSurface val = new ImageSurface((Format)0, Bounds.OuterWidthInt, Bounds.OuterHeightInt);
		Context val2 = new Context((Surface)(object)val);
		double num = GuiElement.scaled(unscaledRightBoxWidth);
		Bounds.CalcWorldBounds();
		ModContainer mod = cell.Mod;
		bool num2 = mod?.Info != null && (mod == null || !mod.Error.HasValue);
		if (cell.DrawAsButton)
		{
			GuiElement.RoundRectangle(val2, 0.0, 0.0, Bounds.OuterWidth, Bounds.OuterHeight, 0.0);
			val2.SetSourceRGB(GuiStyle.DialogDefaultBgColor[0], GuiStyle.DialogDefaultBgColor[1], GuiStyle.DialogDefaultBgColor[2]);
			val2.Fill();
		}
		double num3 = 0.0;
		if (mod.Icon != null)
		{
			int num4 = (int)(Bounds.InnerHeight - Bounds.absPaddingY * 2.0 - 10.0);
			num3 = num4 + 15;
			val.Image(mod.Icon, (int)Bounds.absPaddingX + 5, (int)Bounds.absPaddingY + 5, num4, num4);
		}
		Font = cell.TitleFont;
		titleTextheight = textUtil.AutobreakAndDrawMultilineTextAt(val2, Font, cell.Title, Bounds.absPaddingX + num3, Bounds.absPaddingY, Bounds.InnerWidth - num3);
		Font = cell.DetailTextFont;
		textUtil.AutobreakAndDrawMultilineTextAt(val2, Font, cell.DetailText, Bounds.absPaddingX + num3, Bounds.absPaddingY + titleTextheight + Bounds.absPaddingY, Bounds.InnerWidth - num3);
		if (cell.RightTopText != null)
		{
			TextExtents textExtents = Font.GetTextExtents(cell.RightTopText);
			textUtil.AutobreakAndDrawMultilineTextAt(val2, Font, cell.RightTopText, Bounds.absPaddingX + Bounds.InnerWidth - ((TextExtents)(ref textExtents)).Width - num - GuiElement.scaled(10.0), Bounds.absPaddingY + GuiElement.scaled(cell.RightTopOffY), ((TextExtents)(ref textExtents)).Width + 1.0, EnumTextOrientation.Right);
		}
		if (cell.DrawAsButton)
		{
			EmbossRoundRectangleElement(val2, 0.0, 0.0, Bounds.OuterWidth, Bounds.OuterHeight, inverse: false, (int)GuiElement.scaled(4.0), 0);
		}
		if (!num2)
		{
			val2.SetSourceRGBA(0.0, 0.0, 0.0, 0.5);
			GuiElement.RoundRectangle(val2, 0.0, 0.0, Bounds.OuterWidth, Bounds.OuterHeight, 1.0);
			val2.Fill();
		}
		double num5 = GuiElement.scaled(unscaledSwitchSize);
		double num6 = GuiElement.scaled(unscaledSwitchPadding);
		double num7 = Bounds.absPaddingX + Bounds.InnerWidth - GuiElement.scaled(0.0) - num5 - num6;
		double num8 = Bounds.absPaddingY + Bounds.absPaddingY;
		if (showModifyIcons)
		{
			val2.SetSourceRGBA(0.0, 0.0, 0.0, 0.2);
			GuiElement.RoundRectangle(val2, num7, num8, num5, num5, 3.0);
			val2.Fill();
			EmbossRoundRectangleElement(val2, num7, num8, num5, num5, inverse: true, (int)GuiElement.scaled(2.0), 2);
		}
		if (warningIcon != null)
		{
			capi.Gui.DrawSvg(warningIcon, val, (int)(num7 - GuiElement.scaled(3.0)), (int)(num8 + GuiElement.scaled(35.0)), (int)GuiElement.scaled(30.0), (int)GuiElement.scaled(30.0), (int?)ColorUtil.ColorFromRgba(255, 209, 74, 255));
			capi.Gui.DrawSvg(capi.Assets.Get("textures/icons/excla.svg"), val, (int)(num7 - GuiElement.scaled(3.0)), (int)(num8 + GuiElement.scaled(35.0)), (int)GuiElement.scaled(30.0), (int)GuiElement.scaled(30.0), (int?)(-16777216));
		}
		generateTexture(val, ref modcellTexture);
		val2.Dispose();
		((Surface)val).Dispose();
	}

	private void genOnTexture()
	{
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_0028: Expected O, but got Unknown
		double num = GuiElement.scaled(unscaledSwitchSize - 2.0 * unscaledSwitchPadding);
		ImageSurface val = new ImageSurface((Format)0, (int)num, (int)num);
		Context val2 = genContext(val);
		GuiElement.RoundRectangle(val2, 0.0, 0.0, num, num, 2.0);
		GuiElement.fillWithPattern(api, val2, GuiElement.waterTextureName);
		generateTexture(val, ref switchOnTextureId);
		val2.Dispose();
		((Surface)val).Dispose();
	}

	private void ComposeHover(bool left, ref int textureId)
	{
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Expected O, but got Unknown
		ImageSurface val = new ImageSurface((Format)0, (int)Bounds.OuterWidth, (int)Bounds.OuterHeight);
		Context val2 = genContext(val);
		double num = GuiElement.scaled(unscaledRightBoxWidth);
		if (left)
		{
			val2.NewPath();
			val2.LineTo(0.0, 0.0);
			val2.LineTo(Bounds.InnerWidth - num, 0.0);
			val2.LineTo(Bounds.InnerWidth - num, Bounds.OuterHeight);
			val2.LineTo(0.0, Bounds.OuterHeight);
			val2.ClosePath();
		}
		else
		{
			val2.NewPath();
			val2.LineTo(Bounds.InnerWidth - num, 0.0);
			val2.LineTo(Bounds.OuterWidth, 0.0);
			val2.LineTo(Bounds.OuterWidth, Bounds.OuterHeight);
			val2.LineTo(Bounds.InnerWidth - num, Bounds.OuterHeight);
			val2.ClosePath();
		}
		val2.SetSourceRGBA(0.0, 0.0, 0.0, 0.15);
		val2.Fill();
		generateTexture(val, ref textureId);
		val2.Dispose();
		((Surface)val).Dispose();
	}

	public void UpdateCellHeight()
	{
		Bounds.CalcWorldBounds();
		double num = Bounds.absPaddingY / (double)RuntimeEnv.GUIScale;
		double num2 = Bounds.InnerWidth;
		ModContainer mod = cell.Mod;
		if (mod?.Info != null && mod.Icon != null)
		{
			int num3 = (int)(Bounds.InnerHeight - Bounds.absPaddingY * 2.0 - 10.0);
			num2 -= (double)(num3 + 10);
		}
		Font = cell.TitleFont;
		base.Text = cell.Title;
		titleTextheight = textUtil.GetMultilineTextHeight(Font, cell.Title, num2) / (double)RuntimeEnv.GUIScale;
		Font = cell.DetailTextFont;
		base.Text = cell.DetailText;
		double num4 = textUtil.GetMultilineTextHeight(Font, cell.DetailText, num2) / (double)RuntimeEnv.GUIScale;
		Bounds.fixedHeight = num + titleTextheight + num + num4 + num;
		if (showModifyIcons && Bounds.fixedHeight < 73.0)
		{
			Bounds.fixedHeight = 73.0;
		}
	}

	public void OnRenderInteractiveElements(ICoreClientAPI api, float deltaTime)
	{
		if (modcellTexture.TextureId == 0)
		{
			Compose();
		}
		api.Render.Render2DTexturePremultipliedAlpha(modcellTexture.TextureId, (int)Bounds.absX, (int)Bounds.absY, Bounds.OuterWidthInt, Bounds.OuterHeightInt);
		int mouseX = api.Input.MouseX;
		int mouseY = api.Input.MouseY;
		Vec2d vec2d = Bounds.PositionInside(mouseX, mouseY);
		if (cell.Mod?.Info != null && vec2d != null)
		{
			if (vec2d.X > Bounds.InnerWidth - GuiElement.scaled(GuiElementMainMenuCell.unscaledRightBoxWidth))
			{
				api.Render.Render2DTexturePremultipliedAlpha(rightHighlightTextureId, (int)Bounds.absX, (int)Bounds.absY, Bounds.OuterWidth, Bounds.OuterHeight);
			}
			else
			{
				api.Render.Render2DTexturePremultipliedAlpha(leftHighlightTextureId, (int)Bounds.absX, (int)Bounds.absY, Bounds.OuterWidth, Bounds.OuterHeight);
			}
		}
		if (On)
		{
			double num = GuiElement.scaled(unscaledSwitchSize - 2.0 * unscaledSwitchPadding);
			double num2 = GuiElement.scaled(unscaledSwitchPadding);
			double posX = Bounds.renderX + Bounds.InnerWidth - num + num2 - GuiElement.scaled(5.0);
			double posY = Bounds.renderY + GuiElement.scaled(8.0) + num2;
			api.Render.Render2DTexturePremultipliedAlpha(switchOnTextureId, posX, posY, (int)num, (int)num);
		}
		else
		{
			api.Render.Render2DTexturePremultipliedAlpha(rightHighlightTextureId, (int)Bounds.renderX, (int)Bounds.renderY, Bounds.OuterWidth, Bounds.OuterHeight);
			api.Render.Render2DTexturePremultipliedAlpha(leftHighlightTextureId, (int)Bounds.renderX, (int)Bounds.renderY, Bounds.OuterWidth, Bounds.OuterHeight);
		}
		if (warningTextTexture != null && IsPositionInside(api.Input.MouseX, api.Input.MouseY))
		{
			api.Render.GlScissorFlag(enable: false);
			api.Render.Render2DTexturePremultipliedAlpha(warningTextTexture.TextureId, mouseX + 25, mouseY + 10, warningTextTexture.Width, warningTextTexture.Height, 500f);
			api.Render.GlScissorFlag(enable: true);
		}
	}

	public override void Dispose()
	{
		base.Dispose();
		modcellTexture?.Dispose();
		warningTextTexture?.Dispose();
		api.Render.GLDeleteTexture(leftHighlightTextureId);
		api.Render.GLDeleteTexture(rightHighlightTextureId);
		api.Render.GLDeleteTexture(switchOnTextureId);
	}

	public void OnMouseUpOnElement(MouseEvent args, int elementIndex)
	{
		int mouseX = api.Input.MouseX;
		int mouseY = api.Input.MouseY;
		Vec2d vec2d = Bounds.PositionInside(mouseX, mouseY);
		api.Gui.PlaySound("menubutton_press");
		if (vec2d.X > Bounds.InnerWidth - GuiElement.scaled(GuiElementMainMenuCell.unscaledRightBoxWidth))
		{
			OnMouseDownOnCellRight?.Invoke(elementIndex);
			args.Handled = true;
		}
		else
		{
			OnMouseDownOnCellLeft?.Invoke(elementIndex);
			args.Handled = true;
		}
	}

	public void OnMouseMoveOnElement(MouseEvent args, int elementIndex)
	{
	}

	public void OnMouseDownOnElement(MouseEvent args, int elementIndex)
	{
	}
}
