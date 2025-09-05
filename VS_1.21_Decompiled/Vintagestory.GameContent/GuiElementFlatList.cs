using System;
using System.Collections.Generic;
using System.Linq;
using Cairo;
using Vintagestory.API.Client;

namespace Vintagestory.GameContent;

public class GuiElementFlatList : GuiElement
{
	public List<IFlatListItem> Elements = new List<IFlatListItem>();

	public int unscaledCellSpacing = 5;

	public int unscaledCellHeight = 40;

	public int unscalledYPad = 8;

	public Action<int> onLeftClick;

	private LoadedTexture hoverOverlayTexture;

	public ElementBounds insideBounds;

	private bool wasMouseDownOnElement;

	public GuiElementFlatList(ICoreClientAPI capi, ElementBounds bounds, Action<int> onLeftClick, List<IFlatListItem> elements = null)
		: base(capi, bounds)
	{
		hoverOverlayTexture = new LoadedTexture(capi);
		insideBounds = new ElementBounds().WithFixedPadding(unscaledCellSpacing).WithEmptyParent();
		insideBounds.CalcWorldBounds();
		this.onLeftClick = onLeftClick;
		if (elements != null)
		{
			Elements = elements;
		}
		CalcTotalHeight();
	}

	public void CalcTotalHeight()
	{
		double num = Elements.Where((IFlatListItem e) => e.Visible).Count() * (unscaledCellHeight + unscaledCellSpacing);
		insideBounds.fixedHeight = num + (double)unscaledCellSpacing;
	}

	public override void ComposeElements(Context ctxStatic, ImageSurface surfaceStatic)
	{
		//IL_0052: Unknown result type (might be due to invalid IL or missing references)
		//IL_0058: Expected O, but got Unknown
		//IL_0059: Unknown result type (might be due to invalid IL or missing references)
		//IL_005e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0088: Unknown result type (might be due to invalid IL or missing references)
		insideBounds = new ElementBounds().WithFixedPadding(unscaledCellSpacing).WithEmptyParent();
		insideBounds.CalcWorldBounds();
		CalcTotalHeight();
		Bounds.CalcWorldBounds();
		ImageSurface val = new ImageSurface((Format)0, (int)Bounds.InnerWidth, (int)GuiElement.scaled(unscaledCellHeight));
		Context val2 = new Context((Surface)(object)val);
		val2.SetSourceRGBA(1.0, 1.0, 1.0, 0.5);
		val2.Paint();
		generateTexture(val, ref hoverOverlayTexture);
		val2.Dispose();
		((Surface)val).Dispose();
	}

	public override void OnMouseDownOnElement(ICoreClientAPI api, MouseEvent args)
	{
		if (Bounds.ParentBounds.PointInside(args.X, args.Y))
		{
			base.OnMouseDownOnElement(api, args);
			wasMouseDownOnElement = true;
		}
	}

	public override void OnMouseUpOnElement(ICoreClientAPI api, MouseEvent args)
	{
		if (!Bounds.ParentBounds.PointInside(args.X, args.Y) || !wasMouseDownOnElement)
		{
			return;
		}
		wasMouseDownOnElement = false;
		int num = 0;
		int mouseX = api.Input.MouseX;
		int mouseY = api.Input.MouseY;
		double num2 = insideBounds.absY;
		foreach (IFlatListItem element in Elements)
		{
			if (!element.Visible)
			{
				num++;
				continue;
			}
			float num3 = (float)(5.0 + Bounds.absY + num2);
			double num4 = GuiElement.scaled(unscalledYPad);
			if ((double)mouseX > Bounds.absX && (double)mouseX <= Bounds.absX + Bounds.InnerWidth && (double)mouseY >= (double)num3 - num4 && (double)mouseY <= (double)num3 + GuiElement.scaled(unscaledCellHeight) - num4)
			{
				api.Gui.PlaySound("menubutton_press");
				onLeftClick?.Invoke(num);
				args.Handled = true;
				break;
			}
			num2 += GuiElement.scaled(unscaledCellHeight + unscaledCellSpacing);
			num++;
		}
	}

	public override void OnMouseMove(ICoreClientAPI api, MouseEvent args)
	{
		if (!Bounds.ParentBounds.PointInside(args.X, args.Y))
		{
			return;
		}
		EachVisibleElem(delegate(IFlatListItem elem)
		{
			if (elem is IFlatListItemInteractable flatListItemInteractable)
			{
				flatListItemInteractable.OnMouseMove(api, args);
			}
		});
	}

	public override void OnMouseDown(ICoreClientAPI api, MouseEvent args)
	{
		if (!Bounds.ParentBounds.PointInside(args.X, args.Y))
		{
			return;
		}
		EachVisibleElem(delegate(IFlatListItem elem)
		{
			if (elem is IFlatListItemInteractable flatListItemInteractable)
			{
				flatListItemInteractable.OnMouseDown(api, args);
			}
		});
		if (!args.Handled)
		{
			base.OnMouseDown(api, args);
		}
	}

	public override void OnMouseUp(ICoreClientAPI api, MouseEvent args)
	{
		if (!Bounds.ParentBounds.PointInside(args.X, args.Y))
		{
			return;
		}
		EachVisibleElem(delegate(IFlatListItem elem)
		{
			if (elem is IFlatListItemInteractable flatListItemInteractable)
			{
				flatListItemInteractable.OnMouseUp(api, args);
			}
		});
		if (!args.Handled)
		{
			base.OnMouseUp(api, args);
		}
	}

	protected void EachVisibleElem(Action<IFlatListItem> onElem)
	{
		foreach (IFlatListItem element in Elements)
		{
			if (element.Visible)
			{
				onElem(element);
			}
		}
	}

	public override void RenderInteractiveElements(float deltaTime)
	{
		int mouseX = api.Input.MouseX;
		int mouseY = api.Input.MouseY;
		bool flag = Bounds.ParentBounds.PointInside(mouseX, mouseY);
		double num = insideBounds.absY;
		double num2 = GuiElement.scaled(unscalledYPad);
		double num3 = GuiElement.scaled(unscaledCellHeight);
		foreach (IFlatListItem element in Elements)
		{
			if (element.Visible)
			{
				float num4 = (float)(5.0 + Bounds.absY + num);
				if (flag && (double)mouseX > Bounds.absX && (double)mouseX <= Bounds.absX + Bounds.InnerWidth && (double)mouseY >= (double)num4 - num2 && (double)mouseY <= (double)num4 + num3 - num2)
				{
					api.Render.Render2DLoadedTexture(hoverOverlayTexture, (float)Bounds.absX, num4 - (float)num2);
				}
				if (num > -50.0 && num < Bounds.OuterHeight + 50.0)
				{
					element.RenderListEntryTo(api, deltaTime, Bounds.absX, num4, Bounds.InnerWidth, num3);
				}
				num += GuiElement.scaled(unscaledCellHeight + unscaledCellSpacing);
			}
		}
	}

	public override void Dispose()
	{
		base.Dispose();
		hoverOverlayTexture.Dispose();
		foreach (IFlatListItem element in Elements)
		{
			element.Dispose();
		}
	}
}
