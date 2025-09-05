using System;
using System.Collections.Generic;
using Cairo;
using Vintagestory.API.MathTools;

namespace Vintagestory.API.Client;

public class GuiElementContainer : GuiElement
{
	public List<GuiElement> Elements = new List<GuiElement>();

	public int unscaledCellSpacing = 10;

	public int UnscaledCellVerPadding = 4;

	public int UnscaledCellHorPadding = 7;

	public bool Tabbable;

	private bool renderFocusHighlight;

	private LoadedTexture listTexture;

	private ElementBounds insideBounds;

	protected int currentFocusableElementKey;

	private bool tabPressed;

	private bool shiftTabPressed;

	public override bool Focusable => Tabbable;

	public GuiElement CurrentTabIndexElement
	{
		get
		{
			foreach (GuiElement element in Elements)
			{
				if (element.Focusable && element.HasFocus)
				{
					return element;
				}
			}
			return null;
		}
	}

	public GuiElement FirstTabbableElement
	{
		get
		{
			foreach (GuiElement element in Elements)
			{
				if (element.Focusable)
				{
					return element;
				}
			}
			return null;
		}
	}

	public int MaxTabIndex
	{
		get
		{
			int num = -1;
			foreach (GuiElement element in Elements)
			{
				if (element.Focusable)
				{
					num = Math.Max(num, element.TabIndex);
				}
			}
			return num;
		}
	}

	public GuiElementContainer(ICoreClientAPI capi, ElementBounds bounds)
		: base(capi, bounds)
	{
		listTexture = new LoadedTexture(capi);
		bounds.IsDrawingSurface = true;
	}

	public override void BeforeCalcBounds()
	{
		base.BeforeCalcBounds();
		insideBounds = new ElementBounds().WithFixedPadding(unscaledCellSpacing).WithEmptyParent();
		insideBounds.CalcWorldBounds();
		CalcTotalHeight();
	}

	internal void ReloadCells()
	{
		CalcTotalHeight();
		ComposeList();
	}

	public void CalcTotalHeight()
	{
		double num = 0.0;
		foreach (GuiElement element in Elements)
		{
			element.BeforeCalcBounds();
			num = Math.Max(num, element.Bounds.fixedY + element.Bounds.fixedHeight);
		}
		Bounds.fixedHeight = num + (double)unscaledCellSpacing;
	}

	public override void ComposeElements(Context ctx, ImageSurface surface)
	{
		insideBounds = new ElementBounds().WithFixedPadding(unscaledCellSpacing).WithEmptyParent();
		insideBounds.CalcWorldBounds();
		Bounds.CalcWorldBounds();
		ComposeList();
	}

	private void ComposeList()
	{
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Expected O, but got Unknown
		ImageSurface val = new ImageSurface((Format)0, (int)Bounds.OuterWidth, (int)Bounds.OuterHeight);
		Context val2 = genContext(val);
		CalcTotalHeight();
		Bounds.CalcWorldBounds();
		foreach (GuiElement element in Elements)
		{
			element.ComposeElements(val2, val);
		}
		generateTexture(val, ref listTexture);
		val2.Dispose();
		((Surface)val).Dispose();
	}

	public bool FocusElement(int tabIndex)
	{
		GuiElement guiElement = null;
		foreach (GuiElement element in Elements)
		{
			if (element.Focusable && element.TabIndex == tabIndex)
			{
				guiElement = element;
				break;
			}
		}
		if (guiElement != null)
		{
			UnfocusOwnElementsExcept(guiElement);
			guiElement.OnFocusGained();
			return true;
		}
		return false;
	}

	public void UnfocusOwnElements()
	{
		UnfocusOwnElementsExcept(null);
	}

	public void UnfocusOwnElementsExcept(GuiElement elem)
	{
		foreach (GuiElement element in Elements)
		{
			if (element != elem && element.Focusable && element.HasFocus)
			{
				element.OnFocusLost();
			}
		}
	}

	public void Clear()
	{
		Elements.Clear();
		Bounds.ChildBounds.Clear();
		currentFocusableElementKey = 0;
	}

	public void Add(GuiElement elem, int afterPosition = -1)
	{
		if (afterPosition == -1)
		{
			Elements.Add(elem);
		}
		else
		{
			Elements.Insert(afterPosition, elem);
		}
		if (elem.Focusable)
		{
			elem.TabIndex = currentFocusableElementKey++;
		}
		else
		{
			elem.TabIndex = -1;
		}
		elem.InsideClipBounds = InsideClipBounds;
		Bounds.WithChild(elem.Bounds);
	}

	public void RemoveCell(int position)
	{
		Elements.RemoveAt(position);
	}

	public override void OnMouseUp(ICoreClientAPI api, MouseEvent args)
	{
		foreach (GuiElement element in Elements)
		{
			element.OnMouseUp(api, args);
		}
		if (!args.Handled)
		{
			base.OnMouseUp(api, args);
		}
	}

	public override void OnMouseDown(ICoreClientAPI api, MouseEvent args)
	{
		bool flag = false;
		bool flag2 = false;
		renderFocusHighlight = false;
		foreach (GuiElement element in Elements)
		{
			if (!flag)
			{
				element.OnMouseDown(api, args);
				flag2 = args.Handled;
			}
			if (!flag && flag2)
			{
				if (element.Focusable && !element.HasFocus)
				{
					element.OnFocusGained();
				}
			}
			else if (element.Focusable && element.HasFocus)
			{
				element.OnFocusLost();
			}
			flag = flag2;
		}
		if (!args.Handled)
		{
			base.OnMouseDown(api, args);
		}
	}

	public override void OnMouseMove(ICoreClientAPI api, MouseEvent args)
	{
		foreach (GuiElement element in Elements)
		{
			element.OnMouseMove(api, args);
			if (args.Handled)
			{
				break;
			}
		}
		if (!args.Handled)
		{
			base.OnMouseMove(api, args);
		}
	}

	public override void OnKeyDown(ICoreClientAPI api, KeyEvent args)
	{
		tabPressed = args.KeyCode == 52;
		shiftTabPressed = tabPressed && args.ShiftPressed;
		if (!base.HasFocus)
		{
			return;
		}
		base.OnKeyDown(api, args);
		foreach (GuiElement element in Elements)
		{
			element.OnKeyDown(api, args);
			if (args.Handled)
			{
				break;
			}
		}
		if (!args.Handled && args.KeyCode == 52 && Tabbable)
		{
			renderFocusHighlight = true;
			GuiElement currentTabIndexElement = CurrentTabIndexElement;
			if (currentTabIndexElement != null && MaxTabIndex > 0)
			{
				int num = ((!args.ShiftPressed) ? 1 : (-1));
				int num2 = currentTabIndexElement.TabIndex + num;
				if (num2 < 0 || num2 > MaxTabIndex || args.CtrlPressed)
				{
					return;
				}
				FocusElement(num2);
				args.Handled = true;
			}
			else if (MaxTabIndex > 0)
			{
				FocusElement(args.ShiftPressed ? GameMath.Mod(-1, MaxTabIndex + 1) : 0);
				args.Handled = true;
			}
		}
		if (!args.Handled && (args.KeyCode == 49 || args.KeyCode == 82) && CurrentTabIndexElement is GuiElementEditableTextBase)
		{
			UnfocusOwnElementsExcept(null);
		}
	}

	public override void OnKeyUp(ICoreClientAPI api, KeyEvent args)
	{
		tabPressed = false;
		shiftTabPressed = false;
		if (!base.HasFocus)
		{
			return;
		}
		base.OnKeyUp(api, args);
		foreach (GuiElement element in Elements)
		{
			element.OnKeyUp(api, args);
			if (args.Handled)
			{
				break;
			}
		}
	}

	public override void OnKeyPress(ICoreClientAPI api, KeyEvent args)
	{
		if (!base.HasFocus)
		{
			return;
		}
		base.OnKeyPress(api, args);
		foreach (GuiElement element in Elements)
		{
			element.OnKeyPress(api, args);
			if (args.Handled)
			{
				break;
			}
		}
	}

	public override void OnMouseWheel(ICoreClientAPI api, MouseWheelEventArgs args)
	{
		if (!Bounds.ParentBounds.PointInside(api.Input.MouseX, api.Input.MouseY))
		{
			return;
		}
		foreach (GuiElement element in Elements)
		{
			if (element.IsPositionInside(api.Input.MouseX, api.Input.MouseY))
			{
				element.OnMouseWheel(api, args);
			}
			if (args.IsHandled)
			{
				return;
			}
		}
		foreach (GuiElement element2 in Elements)
		{
			element2.OnMouseWheel(api, args);
			if (args.IsHandled)
			{
				break;
			}
		}
	}

	public override void OnFocusGained()
	{
		base.OnFocusGained();
		if (CurrentTabIndexElement == null)
		{
			renderFocusHighlight = tabPressed;
			if (shiftTabPressed)
			{
				FocusElement(MaxTabIndex);
			}
			else
			{
				FocusElement(FirstTabbableElement.TabIndex);
			}
		}
	}

	public override void OnFocusLost()
	{
		base.OnFocusLost();
		renderFocusHighlight = false;
		UnfocusOwnElements();
	}

	public override void RenderInteractiveElements(float deltaTime)
	{
		api.Render.Render2DTexturePremultipliedAlpha(listTexture.TextureId, Bounds);
		MouseOverCursor = null;
		foreach (GuiElement element in Elements)
		{
			element.RenderInteractiveElements(deltaTime);
			if (element.IsPositionInside(api.Input.MouseX, api.Input.MouseY))
			{
				MouseOverCursor = element.MouseOverCursor;
			}
		}
		foreach (GuiElement element2 in Elements)
		{
			if (element2.HasFocus && renderFocusHighlight)
			{
				if (InsideClipBounds != null)
				{
					ElementBounds insideClipBounds = element2.InsideClipBounds;
					element2.InsideClipBounds = null;
					element2.RenderFocusOverlay(deltaTime);
					element2.InsideClipBounds = insideClipBounds;
				}
				else
				{
					element2.RenderFocusOverlay(deltaTime);
				}
			}
		}
	}

	public override void Dispose()
	{
		base.Dispose();
		listTexture.Dispose();
		foreach (GuiElement element in Elements)
		{
			element.Dispose();
		}
	}
}
