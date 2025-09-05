using System;
using System.Collections.Generic;
using Cairo;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;

namespace Vintagestory.API.Client;

public class GuiComposer : IDisposable
{
	public Action<bool> OnFocusChanged;

	public static int Outlines;

	internal IGuiComposerManager composerManager;

	internal Dictionary<string, GuiElement> staticElements = new Dictionary<string, GuiElement>();

	internal Dictionary<string, GuiElement> interactiveElements = new Dictionary<string, GuiElement>();

	protected List<GuiElement> interactiveElementsInDrawOrder = new List<GuiElement>();

	protected int currentElementKey;

	protected int currentFocusableElementKey;

	public string DialogName;

	protected LoadedTexture staticElementsTexture;

	protected ElementBounds bounds;

	protected Stack<ElementBounds> parentBoundsForNextElement = new Stack<ElementBounds>();

	protected Stack<bool> conditionalAdds = new Stack<bool>();

	protected ElementBounds lastAddedElementBounds;

	protected GuiElement lastAddedElement;

	public bool Composed;

	internal bool recomposeOnRender;

	internal bool onlyDynamicRender;

	internal ElementBounds InsideClipBounds;

	public ICoreClientAPI Api;

	public float zDepth = 50f;

	private bool premultipliedAlpha = true;

	public Vec4f Color;

	internal bool IsCached;

	public bool Tabbable = true;

	public bool Enabled = true;

	private bool renderFocusHighlight;

	public string MouseOverCursor;

	public ElementBounds LastAddedElementBounds => lastAddedElementBounds;

	public ElementBounds CurParentBounds => parentBoundsForNextElement.Peek();

	public int CurrentElementKey => currentElementKey;

	public GuiElement LastAddedElement => lastAddedElement;

	public GuiElement this[string key]
	{
		get
		{
			if (!interactiveElements.TryGetValue(key, out var value))
			{
				staticElements.TryGetValue(key, out value);
			}
			return value;
		}
	}

	public ElementBounds Bounds => bounds;

	public GuiElement CurrentTabIndexElement
	{
		get
		{
			foreach (GuiElement value in interactiveElements.Values)
			{
				if (value.Focusable && value.HasFocus)
				{
					return value;
				}
			}
			return null;
		}
	}

	public GuiElement FirstTabbableElement
	{
		get
		{
			foreach (GuiElement value in interactiveElements.Values)
			{
				if (value.Focusable)
				{
					return value;
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
			foreach (GuiElement value in interactiveElements.Values)
			{
				if (value.Focusable)
				{
					num = Math.Max(num, value.TabIndex);
				}
			}
			return num;
		}
	}

	public event Action OnComposed;

	internal GuiComposer(ICoreClientAPI api, ElementBounds bounds, string dialogName)
	{
		staticElementsTexture = new LoadedTexture(api);
		DialogName = dialogName;
		this.bounds = bounds;
		Api = api;
		parentBoundsForNextElement.Push(bounds);
	}

	public static GuiComposer CreateEmpty(ICoreClientAPI api)
	{
		return new GuiComposer(api, ElementBounds.Empty, null).Compose();
	}

	public GuiComposer PremultipliedAlpha(bool enable)
	{
		premultipliedAlpha = enable;
		return this;
	}

	public GuiComposer AddIf(bool condition)
	{
		conditionalAdds.Push(condition);
		return this;
	}

	public GuiComposer EndIf()
	{
		if (conditionalAdds.Count > 0)
		{
			conditionalAdds.Pop();
		}
		return this;
	}

	public GuiComposer Execute(Action method)
	{
		if (conditionalAdds.Count > 0 && !conditionalAdds.Peek())
		{
			return this;
		}
		method();
		return this;
	}

	public GuiComposer BeginChildElements(ElementBounds bounds)
	{
		if (conditionalAdds.Count > 0 && !conditionalAdds.Peek())
		{
			return this;
		}
		parentBoundsForNextElement.Peek().WithChild(bounds);
		parentBoundsForNextElement.Push(bounds);
		string key = "element-" + ++currentElementKey;
		staticElements.Add(key, new GuiElementParent(Api, bounds));
		return this;
	}

	public GuiComposer BeginChildElements()
	{
		if (conditionalAdds.Count > 0 && !conditionalAdds.Peek())
		{
			return this;
		}
		parentBoundsForNextElement.Push(lastAddedElementBounds);
		return this;
	}

	public GuiComposer EndChildElements()
	{
		if (conditionalAdds.Count > 0 && !conditionalAdds.Peek())
		{
			return this;
		}
		if (parentBoundsForNextElement.Count > 1)
		{
			parentBoundsForNextElement.Pop();
		}
		return this;
	}

	public GuiComposer OnlyDynamic()
	{
		onlyDynamicRender = true;
		return this;
	}

	public void ReCompose()
	{
		Composed = false;
		Compose(focusFirstElement: false);
	}

	internal void UnFocusElements()
	{
		composerManager.UnfocusElements();
		OnFocusChanged?.Invoke(obj: false);
	}

	public bool FocusElement(int tabIndex)
	{
		GuiElement guiElement = null;
		foreach (GuiElement value in interactiveElements.Values)
		{
			if (value.Focusable && value.TabIndex == tabIndex)
			{
				guiElement = value;
				break;
			}
		}
		if (guiElement != null)
		{
			UnfocusOwnElementsExcept(guiElement);
			guiElement.OnFocusGained();
			OnFocusChanged?.Invoke(obj: true);
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
		foreach (GuiElement value in interactiveElements.Values)
		{
			if (value != elem && value.Focusable && value.HasFocus)
			{
				value.OnFocusLost();
				OnFocusChanged?.Invoke(obj: false);
			}
		}
	}

	public GuiComposer Compose(bool focusFirstElement = true)
	{
		//IL_0105: Unknown result type (might be due to invalid IL or missing references)
		//IL_010b: Expected O, but got Unknown
		//IL_010c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0112: Expected O, but got Unknown
		if (Composed)
		{
			if (focusFirstElement && MaxTabIndex >= 0)
			{
				FocusElement(0);
			}
			return this;
		}
		foreach (GuiElement value in staticElements.Values)
		{
			value.BeforeCalcBounds();
		}
		bounds.Initialized = false;
		try
		{
			bounds.CalcWorldBounds();
		}
		catch (Exception e)
		{
			Api.Logger.Error("Exception thrown when trying to calculate world bounds for gui composite " + DialogName + ":");
			Api.Logger.Error(e);
		}
		bounds.IsDrawingSurface = true;
		int num = (int)bounds.OuterWidth;
		int num2 = (int)bounds.OuterHeight;
		if (staticElementsTexture.TextureId != 0)
		{
			num = Math.Max(num, staticElementsTexture.Width);
			num2 = Math.Max(num2, staticElementsTexture.Height);
		}
		ImageSurface val = new ImageSurface((Format)0, num, num2);
		Context val2 = new Context((Surface)(object)val);
		val2.SetSourceRGBA(0.0, 0.0, 0.0, 0.0);
		val2.Paint();
		val2.Antialias = (Antialias)6;
		foreach (GuiElement value2 in staticElements.Values)
		{
			value2.ComposeElements(val2, val);
		}
		interactiveElementsInDrawOrder.Clear();
		foreach (GuiElement value3 in interactiveElements.Values)
		{
			int num3 = 0;
			foreach (GuiElement item in interactiveElementsInDrawOrder)
			{
				if (value3.DrawOrder >= item.DrawOrder)
				{
					num3++;
					continue;
				}
				break;
			}
			interactiveElementsInDrawOrder.Insert(num3, value3);
		}
		if (!premultipliedAlpha)
		{
			SurfaceTransformDemulAlpha.DemulAlpha(val);
		}
		Api.Gui.LoadOrUpdateCairoTexture(val, linearMag: true, ref staticElementsTexture);
		val2.Dispose();
		((Surface)val).Dispose();
		Composed = true;
		if (focusFirstElement && MaxTabIndex >= 0)
		{
			FocusElement(0);
		}
		this.OnComposed?.Invoke();
		return this;
	}

	public void OnMouseUp(MouseEvent mouse)
	{
		if (!Enabled)
		{
			return;
		}
		foreach (GuiElement value in interactiveElements.Values)
		{
			value.OnMouseUp(Api, mouse);
		}
	}

	public void OnMouseDown(MouseEvent mouseArgs)
	{
		if (!Enabled)
		{
			return;
		}
		bool flag = false;
		bool flag2 = false;
		renderFocusHighlight = false;
		foreach (GuiElement value in interactiveElements.Values)
		{
			if (!flag)
			{
				value.OnMouseDown(Api, mouseArgs);
				flag2 = mouseArgs.Handled;
			}
			if (!flag && flag2)
			{
				if (value.Focusable && !value.HasFocus)
				{
					value.OnFocusGained();
					if (value.HasFocus)
					{
						OnFocusChanged?.Invoke(obj: true);
					}
				}
			}
			else if (value.Focusable && value.HasFocus)
			{
				value.OnFocusLost();
			}
			flag = flag2;
		}
	}

	public void OnMouseMove(MouseEvent mouse)
	{
		if (!Enabled)
		{
			return;
		}
		foreach (GuiElement value in interactiveElements.Values)
		{
			value.OnMouseMove(Api, mouse);
			if (mouse.Handled)
			{
				break;
			}
		}
	}

	public bool OnMouseEnterSlot(ItemSlot slot)
	{
		if (!Enabled)
		{
			return false;
		}
		foreach (GuiElement value in interactiveElements.Values)
		{
			if (value.OnMouseEnterSlot(Api, slot))
			{
				return true;
			}
		}
		return false;
	}

	public bool OnMouseLeaveSlot(ItemSlot slot)
	{
		if (!Enabled)
		{
			return false;
		}
		foreach (GuiElement value in interactiveElements.Values)
		{
			if (value.OnMouseLeaveSlot(Api, slot))
			{
				return true;
			}
		}
		return false;
	}

	public void OnMouseWheel(MouseWheelEventArgs mouse)
	{
		if (!Enabled)
		{
			return;
		}
		foreach (KeyValuePair<string, GuiElement> interactiveElement in interactiveElements)
		{
			GuiElement value = interactiveElement.Value;
			if (value.IsPositionInside(Api.Input.MouseX, Api.Input.MouseY))
			{
				value.OnMouseWheel(Api, mouse);
			}
			if (mouse.IsHandled)
			{
				return;
			}
		}
		foreach (GuiElement value2 in interactiveElements.Values)
		{
			value2.OnMouseWheel(Api, mouse);
			if (mouse.IsHandled)
			{
				break;
			}
		}
	}

	public void OnKeyDown(KeyEvent args, bool haveFocus)
	{
		if (!Enabled)
		{
			return;
		}
		foreach (GuiElement value in interactiveElements.Values)
		{
			value.OnKeyDown(Api, args);
			if (args.Handled)
			{
				break;
			}
		}
		if (haveFocus && !args.Handled && args.KeyCode == 52 && Tabbable)
		{
			renderFocusHighlight = true;
			GuiElement currentTabIndexElement = CurrentTabIndexElement;
			if (currentTabIndexElement != null && MaxTabIndex > 0)
			{
				int num = ((!args.ShiftPressed) ? 1 : (-1));
				int tabIndex = GameMath.Mod(currentTabIndexElement.TabIndex + num, MaxTabIndex + 1);
				FocusElement(tabIndex);
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

	public void OnKeyUp(KeyEvent args)
	{
		if (!Enabled)
		{
			return;
		}
		foreach (GuiElement value in interactiveElements.Values)
		{
			value.OnKeyUp(Api, args);
			if (args.Handled)
			{
				break;
			}
		}
	}

	public void OnKeyPress(KeyEvent args)
	{
		if (!Enabled)
		{
			return;
		}
		foreach (GuiElement value in interactiveElements.Values)
		{
			value.OnKeyPress(Api, args);
			if (args.Handled)
			{
				break;
			}
		}
	}

	public void Clear(ElementBounds newBounds)
	{
		foreach (KeyValuePair<string, GuiElement> interactiveElement in interactiveElements)
		{
			interactiveElement.Value.Dispose();
		}
		foreach (KeyValuePair<string, GuiElement> staticElement in staticElements)
		{
			staticElement.Value.Dispose();
		}
		interactiveElements.Clear();
		interactiveElementsInDrawOrder.Clear();
		staticElements.Clear();
		conditionalAdds.Clear();
		parentBoundsForNextElement.Clear();
		bounds = newBounds;
		if (bounds.ParentBounds == null)
		{
			bounds.ParentBounds = Api.Gui.WindowBounds;
		}
		parentBoundsForNextElement.Push(bounds);
		lastAddedElementBounds = null;
		lastAddedElement = null;
		Composed = false;
	}

	public void PostRender(float deltaTime)
	{
		if (!Enabled || Api.Render.FrameWidth == 0 || Api.Render.FrameHeight == 0)
		{
			return;
		}
		if (bounds.ParentBounds.RequiresRecalculation)
		{
			Api.Logger.Notification("Window probably resized, recalculating dialog bounds and recomposing " + DialogName + "...");
			bounds.MarkDirtyRecursive();
			bounds.ParentBounds.CalcWorldBounds();
			if (bounds.ParentBounds.OuterWidth == 0.0 || bounds.ParentBounds.OuterHeight == 0.0)
			{
				return;
			}
			bounds.CalcWorldBounds();
			ReCompose();
		}
		foreach (GuiElement item in interactiveElementsInDrawOrder)
		{
			item.PostRenderInteractiveElements(deltaTime);
		}
	}

	public void Render(float deltaTime)
	{
		if (!Enabled)
		{
			return;
		}
		if (recomposeOnRender)
		{
			ReCompose();
			recomposeOnRender = false;
		}
		if (!onlyDynamicRender)
		{
			int num = Math.Max(bounds.OuterWidthInt, staticElementsTexture.Width);
			int num2 = Math.Max(bounds.OuterHeightInt, staticElementsTexture.Height);
			Api.Render.Render2DTexture(staticElementsTexture.TextureId, (int)bounds.renderX, (int)bounds.renderY, num, num2, zDepth, Color);
		}
		MouseOverCursor = null;
		foreach (GuiElement item in interactiveElementsInDrawOrder)
		{
			item.RenderInteractiveElements(deltaTime);
			if (item.IsPositionInside(Api.Input.MouseX, Api.Input.MouseY))
			{
				MouseOverCursor = item.MouseOverCursor;
			}
		}
		foreach (GuiElement item2 in interactiveElementsInDrawOrder)
		{
			if (item2.HasFocus && renderFocusHighlight)
			{
				item2.RenderFocusOverlay(deltaTime);
			}
		}
		if (Outlines == 1)
		{
			Api.Render.RenderRectangle((int)bounds.renderX, (int)bounds.renderY, 500f, (int)bounds.OuterWidth, (int)bounds.OuterHeight, -1);
			foreach (GuiElement value in staticElements.Values)
			{
				value.RenderBoundsDebug();
			}
		}
		if (Outlines != 2)
		{
			return;
		}
		foreach (GuiElement value2 in interactiveElements.Values)
		{
			value2.RenderBoundsDebug();
		}
	}

	internal static double scaled(double value)
	{
		return value * (double)RuntimeEnv.GUIScale;
	}

	public GuiComposer AddInteractiveElement(GuiElement element, string key = null)
	{
		if (conditionalAdds.Count > 0 && !conditionalAdds.Peek())
		{
			return this;
		}
		element.RenderAsPremultipliedAlpha = premultipliedAlpha;
		if (key == null)
		{
			int num = ++currentElementKey;
			key = "element-" + num;
		}
		interactiveElements.Add(key, element);
		staticElements.Add(key, element);
		if (element.Focusable)
		{
			element.TabIndex = currentFocusableElementKey++;
		}
		else
		{
			element.TabIndex = -1;
		}
		element.InsideClipBounds = InsideClipBounds;
		if (parentBoundsForNextElement.Peek() == element.Bounds)
		{
			throw new ArgumentException($"Fatal: Attempting to add a self referencing bounds->child bounds reference. This would cause a stack overflow. Make sure you don't re-use the same bounds for a parent and child element (key {key})");
		}
		parentBoundsForNextElement.Peek().WithChild(element.Bounds);
		lastAddedElementBounds = element.Bounds;
		lastAddedElement = element;
		return this;
	}

	public GuiComposer AddStaticElement(GuiElement element, string key = null)
	{
		if (conditionalAdds.Count > 0 && !conditionalAdds.Peek())
		{
			return this;
		}
		element.RenderAsPremultipliedAlpha = premultipliedAlpha;
		if (key == null)
		{
			int num = ++currentElementKey;
			key = "element-" + num;
		}
		staticElements.Add(key, element);
		parentBoundsForNextElement.Peek().WithChild(element.Bounds);
		lastAddedElementBounds = element.Bounds;
		lastAddedElement = element;
		element.InsideClipBounds = InsideClipBounds;
		return this;
	}

	public GuiElement GetElement(string key)
	{
		if (interactiveElements.ContainsKey(key))
		{
			return interactiveElements[key];
		}
		if (staticElements.ContainsKey(key))
		{
			return staticElements[key];
		}
		return null;
	}

	public void Dispose()
	{
		foreach (KeyValuePair<string, GuiElement> interactiveElement in interactiveElements)
		{
			interactiveElement.Value.Dispose();
		}
		foreach (KeyValuePair<string, GuiElement> staticElement in staticElements)
		{
			staticElement.Value.Dispose();
		}
		staticElementsTexture.Dispose();
		Composed = false;
		lastAddedElement = null;
	}
}
