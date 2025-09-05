using System;
using Cairo;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class ActivityCellEntry : GuiElement, IGuiElementCell, IDisposable
{
	public LoadedTexture hoverTexture;

	private double unScaledCellHeight = 35.0;

	private GuiElementRichtext nameTextElem;

	private GuiElementRichtext detailTextElem;

	private bool composed;

	public bool Selected;

	private Action<int> onClick;

	public bool Visible => true;

	ElementBounds IGuiElementCell.Bounds => Bounds;

	public ActivityCellEntry(ICoreClientAPI capi, ElementBounds bounds, string name, string detail, Action<int> onClick, float leftColWidth = 200f, float rightColWidth = 300f)
		: base(capi, bounds)
	{
		this.onClick = onClick;
		CairoFont cairoFont = CairoFont.WhiteDetailText();
		double fixedY = (unScaledCellHeight - cairoFont.UnscaledFontsize) / 2.0;
		ElementBounds elementBounds = ElementBounds.Fixed(0.0, fixedY, leftColWidth, 25.0).WithParent(Bounds);
		ElementBounds bounds2 = ElementBounds.Fixed(0.0, fixedY, rightColWidth, 25.0).WithParent(Bounds).FixedRightOf(elementBounds, 10.0);
		nameTextElem = new GuiElementRichtext(capi, VtmlUtil.Richtextify(capi, name, cairoFont), elementBounds);
		detailTextElem = new GuiElementRichtext(capi, VtmlUtil.Richtextify(capi, detail, cairoFont), bounds2);
		hoverTexture = new LoadedTexture(capi);
	}

	public void Recompose()
	{
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_0028: Expected O, but got Unknown
		composed = true;
		nameTextElem.Compose();
		detailTextElem.Compose();
		ImageSurface val = new ImageSurface((Format)0, 2, 2);
		Context obj = genContext(val);
		obj.NewPath();
		obj.LineTo(0.0, 0.0);
		obj.LineTo(2.0, 0.0);
		obj.LineTo(2.0, 2.0);
		obj.LineTo(0.0, 2.0);
		obj.ClosePath();
		obj.SetSourceRGBA(0.0, 0.0, 0.0, 0.15);
		obj.Fill();
		generateTexture(val, ref hoverTexture);
		obj.Dispose();
		((Surface)val).Dispose();
	}

	public void OnRenderInteractiveElements(ICoreClientAPI api, float deltaTime)
	{
		if (!composed)
		{
			Recompose();
		}
		nameTextElem.RenderInteractiveElements(deltaTime);
		detailTextElem.RenderInteractiveElements(deltaTime);
		int mouseX = api.Input.MouseX;
		int mouseY = api.Input.MouseY;
		Vec2d vec2d = Bounds.PositionInside(mouseX, mouseY);
		if (Selected || (vec2d != null && IsPositionInside(api.Input.MouseX, api.Input.MouseY)))
		{
			api.Render.Render2DTexturePremultipliedAlpha(hoverTexture.TextureId, Bounds.absX, Bounds.absY, Bounds.OuterWidth, Bounds.OuterHeight);
			if (Selected)
			{
				api.Render.Render2DTexturePremultipliedAlpha(hoverTexture.TextureId, Bounds.absX, Bounds.absY, Bounds.OuterWidth, Bounds.OuterHeight);
			}
		}
	}

	public void UpdateCellHeight()
	{
		Bounds.CalcWorldBounds();
		nameTextElem.BeforeCalcBounds();
		detailTextElem.BeforeCalcBounds();
		Bounds.fixedHeight = unScaledCellHeight;
	}

	public void OnMouseUpOnElement(MouseEvent args, int elementIndex)
	{
		_ = api.Input.MouseX;
		_ = api.Input.MouseY;
		if (!args.Handled)
		{
			onClick?.Invoke(elementIndex);
		}
	}

	public override void Dispose()
	{
		nameTextElem.Dispose();
		detailTextElem.Dispose();
		hoverTexture?.Dispose();
	}

	public void OnMouseDownOnElement(MouseEvent args, int elementIndex)
	{
	}

	public void OnMouseMoveOnElement(MouseEvent args, int elementIndex)
	{
	}
}
