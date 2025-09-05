using System;
using System.Collections.Generic;
using Cairo;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;

namespace Vintagestory.API.Client;

public class GuiElementListMenu : GuiElementTextBase
{
	public int MaxHeight = 350;

	protected double expandedBoxWidth;

	protected double expandedBoxHeight;

	protected double unscaledLineHeight = 30.0;

	private GuiElementSwitch[] switches = Array.Empty<GuiElementSwitch>();

	protected SelectionChangedDelegate onSelectionChanged;

	protected LoadedTexture hoverTexture;

	protected LoadedTexture dropDownTexture;

	protected LoadedTexture scrollbarTexture;

	protected bool expanded;

	protected bool multiSelect;

	protected double scrollOffY;

	protected GuiElementCompactScrollbar scrollbar;

	protected GuiElementRichtext[] richtTextElem;

	protected ElementBounds visibleBounds;

	public string[] Values { get; set; }

	public string[] Names { get; set; }

	public int SelectedIndex
	{
		get
		{
			if (SelectedIndices != null && SelectedIndices.Length != 0)
			{
				return SelectedIndices[0];
			}
			return 0;
		}
		set
		{
			if (value < 0)
			{
				SelectedIndices = Array.Empty<int>();
				return;
			}
			if (SelectedIndices != null && SelectedIndices.Length != 0)
			{
				SelectedIndices[0] = value;
				return;
			}
			SelectedIndices = new int[1] { value };
		}
	}

	public int HoveredIndex { get; set; }

	public int[] SelectedIndices { get; set; }

	public bool IsOpened => expanded;

	public override double DrawOrder => 0.5;

	public override bool Focusable => enabled;

	public GuiElementListMenu(ICoreClientAPI capi, string[] values, string[] names, int selectedIndex, SelectionChangedDelegate onSelectionChanged, ElementBounds bounds, CairoFont font, bool multiSelect)
		: base(capi, "", font, bounds)
	{
		if (values.Length != names.Length)
		{
			throw new ArgumentException("Values and Names arrays must be of the same length!");
		}
		hoverTexture = new LoadedTexture(capi);
		dropDownTexture = new LoadedTexture(capi);
		scrollbarTexture = new LoadedTexture(capi);
		Values = values;
		Names = names;
		SelectedIndex = selectedIndex;
		this.multiSelect = multiSelect;
		this.onSelectionChanged = onSelectionChanged;
		HoveredIndex = selectedIndex;
		ElementBounds bounds2 = ElementBounds.Fixed(0.0, 0.0, 0.0, 0.0).WithEmptyParent();
		scrollbar = new GuiElementCompactScrollbar(api, OnNewScrollbarValue, bounds2);
		scrollbar.zOffset = 300f;
		richtTextElem = new GuiElementRichtext[values.Length];
		for (int i = 0; i < values.Length; i++)
		{
			ElementBounds bounds3 = ElementBounds.Fixed(0.0, 0.0, 700.0, 100.0).WithEmptyParent();
			richtTextElem[i] = new GuiElementRichtext(capi, Array.Empty<RichTextComponentBase>(), bounds3);
		}
	}

	private void OnNewScrollbarValue(float offY)
	{
		scrollOffY = (int)((double)offY / (30.0 * Scale) * 30.0 * Scale);
	}

	public override void ComposeElements(Context ctx, ImageSurface surface)
	{
		ComposeDynamicElements();
	}

	public void ComposeDynamicElements()
	{
		//IL_01b5: Unknown result type (might be due to invalid IL or missing references)
		//IL_01bb: Expected O, but got Unknown
		//IL_02d6: Unknown result type (might be due to invalid IL or missing references)
		//IL_02db: Unknown result type (might be due to invalid IL or missing references)
		//IL_03a5: Unknown result type (might be due to invalid IL or missing references)
		//IL_03aa: Unknown result type (might be due to invalid IL or missing references)
		//IL_05ab: Unknown result type (might be due to invalid IL or missing references)
		//IL_05b1: Expected O, but got Unknown
		//IL_0614: Unknown result type (might be due to invalid IL or missing references)
		//IL_061a: Expected O, but got Unknown
		Bounds.CalcWorldBounds();
		if (multiSelect)
		{
			if (switches != null)
			{
				GuiElementSwitch[] array = switches;
				for (int i = 0; i < array.Length; i++)
				{
					array[i].Dispose();
				}
			}
			switches = new GuiElementSwitch[Names.Length];
		}
		for (int j = 0; j < richtTextElem.Length; j++)
		{
			richtTextElem[j].Dispose();
		}
		richtTextElem = new GuiElementRichtext[Values.Length];
		for (int k = 0; k < Values.Length; k++)
		{
			ElementBounds bounds = ElementBounds.Fixed(0.0, 0.0, 700.0, 100.0).WithEmptyParent();
			richtTextElem[k] = new GuiElementRichtext(api, Array.Empty<RichTextComponentBase>(), bounds);
		}
		double num = Scale * (double)RuntimeEnv.GUIScale;
		double num2 = unscaledLineHeight * num;
		expandedBoxWidth = Bounds.InnerWidth;
		expandedBoxHeight = (double)Values.Length * num2;
		double num3 = 10.0;
		for (int l = 0; l < Values.Length; l++)
		{
			GuiElementRichtext guiElementRichtext = richtTextElem[l];
			guiElementRichtext.SetNewTextWithoutRecompose(Names[l], Font);
			guiElementRichtext.BeforeCalcBounds();
			expandedBoxWidth = Math.Max(expandedBoxWidth, guiElementRichtext.MaxLineWidth + 5.0 * num + GuiElement.scaled(num3 + 5.0));
		}
		ImageSurface val = new ImageSurface((Format)0, (int)expandedBoxWidth, (int)expandedBoxHeight);
		Context val2 = genContext(val);
		visibleBounds = Bounds.FlatCopy();
		visibleBounds.fixedHeight = Math.Min(MaxHeight, expandedBoxHeight / (double)RuntimeEnv.GUIScale);
		visibleBounds.fixedWidth = expandedBoxWidth / (double)RuntimeEnv.GUIScale;
		visibleBounds.fixedY += Bounds.InnerHeight / (double)RuntimeEnv.GUIScale;
		visibleBounds.CalcWorldBounds();
		Font.SetupContext(val2);
		val2.SetSourceRGBA(GuiStyle.DialogStrongBgColor);
		GuiElement.RoundRectangle(val2, 0.0, 0.0, expandedBoxWidth, expandedBoxHeight, 1.0);
		val2.FillPreserve();
		val2.SetSourceRGBA(0.0, 0.0, 0.0, 0.5);
		val2.LineWidth = 2.0;
		val2.Stroke();
		FontExtents fontExtents = Font.GetFontExtents();
		double num4 = ((FontExtents)(ref fontExtents)).Height / (double)RuntimeEnv.GUIScale;
		double num5 = (unscaledLineHeight - num4) / 2.0;
		double num6 = (multiSelect ? (num4 + 10.0) : 0.0);
		double num7 = num4 * num;
		val2.SetSourceRGBA(GuiStyle.DialogDefaultTextColor);
		ElementBounds elementBounds = Bounds.FlatCopy();
		elementBounds.IsDrawingSurface = true;
		elementBounds.CalcWorldBounds();
		for (int m = 0; m < Values.Length; m++)
		{
			int num8 = m;
			double num9 = ((double)(int)num5 + (double)m * unscaledLineHeight) * num;
			double fixedX = num6 + 5.0 * num;
			TextExtents textExtents = Font.GetTextExtents(Names[m]);
			double num10 = (num7 - ((TextExtents)(ref textExtents)).Height) / 2.0;
			if (multiSelect)
			{
				double padding = 2.0;
				ElementBounds elementBounds2 = new ElementBounds
				{
					ParentBounds = elementBounds,
					fixedX = 4.0 * Scale,
					fixedY = (num9 + num10) / (double)RuntimeEnv.GUIScale,
					fixedWidth = num4 * Scale,
					fixedHeight = num4 * Scale,
					fixedPaddingX = 0.0,
					fixedPaddingY = 0.0
				};
				switches[m] = new GuiElementSwitch(api, delegate(bool on)
				{
					toggled(on, num8);
				}, elementBounds2, elementBounds2.fixedHeight, padding);
				switches[m].ComposeElements(val2, val);
				val2.SetSourceRGBA(GuiStyle.DialogDefaultTextColor);
			}
			GuiElementRichtext obj = richtTextElem[m];
			obj.Bounds.fixedX = fixedX;
			obj.Bounds.fixedY = (num9 + num10) / (double)RuntimeEnv.GUIScale;
			obj.BeforeCalcBounds();
			obj.Bounds.CalcWorldBounds();
			obj.ComposeFor(obj.Bounds, val2, val);
		}
		generateTexture(val, ref dropDownTexture);
		val2.Dispose();
		((Surface)val).Dispose();
		scrollbar.Bounds.WithFixedSize(num3, visibleBounds.fixedHeight - 3.0).WithFixedPosition(expandedBoxWidth / (double)RuntimeEnv.GUIScale - 10.0, 0.0).WithFixedPadding(0.0, 2.0);
		scrollbar.Bounds.WithEmptyParent();
		scrollbar.Bounds.CalcWorldBounds();
		val = new ImageSurface((Format)0, (int)expandedBoxWidth, (int)scrollbar.Bounds.OuterHeight);
		val2 = genContext(val);
		scrollbar.ComposeElements(val2, val);
		scrollbar.SetHeights((int)visibleBounds.InnerHeight, (int)expandedBoxHeight);
		generateTexture(val, ref scrollbarTexture);
		val2.Dispose();
		((Surface)val).Dispose();
		val = new ImageSurface((Format)0, (int)expandedBoxWidth, (int)(unscaledLineHeight * num));
		val2 = genContext(val);
		double[] dialogHighlightColor = GuiStyle.DialogHighlightColor;
		dialogHighlightColor[3] = 0.5;
		val2.SetSourceRGBA(dialogHighlightColor);
		GuiElement.RoundRectangle(val2, 0.0, 0.0, expandedBoxWidth, unscaledLineHeight * num, 0.0);
		val2.Fill();
		generateTexture(val, ref hoverTexture);
		val2.Dispose();
		((Surface)val).Dispose();
	}

	private void toggled(bool on, int num)
	{
		List<int> list = new List<int>();
		for (int i = 0; i < switches.Length; i++)
		{
			if (switches[i].On)
			{
				list.Add(i);
			}
		}
		SelectedIndices = list.ToArray();
	}

	public override bool IsPositionInside(int posX, int posY)
	{
		if (!IsOpened)
		{
			return false;
		}
		if ((double)posX >= Bounds.absX && (double)posX <= Bounds.absX + expandedBoxWidth && (double)posY >= Bounds.absY)
		{
			return (double)posY <= Bounds.absY + expandedBoxHeight;
		}
		return false;
	}

	public override void RenderInteractiveElements(float deltaTime)
	{
		if (!expanded)
		{
			return;
		}
		double num = Scale * (double)RuntimeEnv.GUIScale;
		api.Render.PushScissor(visibleBounds);
		api.Render.Render2DTexture(dropDownTexture.TextureId, (int)Bounds.renderX, (int)Bounds.renderY + (int)Bounds.InnerHeight - (int)scrollOffY, (int)expandedBoxWidth, (int)expandedBoxHeight, 310f);
		if (multiSelect)
		{
			api.Render.GlPushMatrix();
			api.Render.GlTranslate(0.0, Bounds.InnerHeight - (double)(int)scrollOffY, 350.0);
			for (int i = 0; i < switches.Length; i++)
			{
				switches[i].RenderInteractiveElements(deltaTime);
			}
			api.Render.GlPopMatrix();
		}
		if (HoveredIndex >= 0)
		{
			api.Render.Render2DTexturePremultipliedAlpha(hoverTexture.TextureId, (int)Bounds.renderX + 1, (int)(Bounds.renderY + Bounds.InnerHeight + unscaledLineHeight * num * (double)HoveredIndex - (double)(int)scrollOffY + 1.0), (double)(int)expandedBoxWidth - GuiElement.scaled(10.0), (double)(int)unscaledLineHeight * num - 2.0, 311f);
		}
		api.Render.PopScissor();
		if (api.Render.ScissorStack.Count > 0)
		{
			api.Render.GlScissorFlag(enable: false);
		}
		api.Render.GlPushMatrix();
		api.Render.GlTranslate(0f, 0f, 200f);
		api.Render.Render2DTexturePremultipliedAlpha(scrollbarTexture.TextureId, (int)visibleBounds.renderX, (int)visibleBounds.renderY, scrollbarTexture.Width, scrollbarTexture.Height, 316f);
		scrollbar.Bounds.WithParent(Bounds);
		scrollbar.Bounds.absFixedY = Bounds.InnerHeight;
		scrollbar.RenderInteractiveElements(deltaTime);
		api.Render.GlPopMatrix();
		if (api.Render.ScissorStack.Count > 0)
		{
			api.Render.GlScissorFlag(enable: true);
		}
	}

	public override void OnKeyDown(ICoreClientAPI api, KeyEvent args)
	{
		if (!hasFocus)
		{
			return;
		}
		if ((args.KeyCode == 49 || args.KeyCode == 82) && expanded)
		{
			expanded = false;
			SelectedIndex = HoveredIndex;
			onSelectionChanged?.Invoke(Values[SelectedIndex], selected: true);
			args.Handled = true;
		}
		else if (args.KeyCode == 45 || args.KeyCode == 46)
		{
			args.Handled = true;
			if (!expanded)
			{
				expanded = true;
				HoveredIndex = SelectedIndex;
			}
			else if (args.KeyCode == 45)
			{
				HoveredIndex = GameMath.Mod(HoveredIndex - 1, Values.Length);
			}
			else
			{
				HoveredIndex = GameMath.Mod(HoveredIndex + 1, Values.Length);
			}
		}
	}

	public override void OnMouseMove(ICoreClientAPI api, MouseEvent args)
	{
		if (!expanded)
		{
			return;
		}
		int mouseX = api.Input.MouseX;
		int mouseY = api.Input.MouseY;
		double num = Scale * (double)RuntimeEnv.GUIScale;
		if (!((double)mouseX >= Bounds.renderX) || !((double)mouseX <= Bounds.renderX + expandedBoxWidth))
		{
			return;
		}
		if (scrollbar.mouseDownOnScrollbarHandle && (scrollbar.mouseDownOnScrollbarHandle || Bounds.renderX + expandedBoxWidth - (double)args.X < GuiElement.scaled(10.0)))
		{
			scrollbar.OnMouseMove(api, args);
			return;
		}
		int num2 = (int)(((double)mouseY - Bounds.renderY - Bounds.InnerHeight + scrollOffY) / (unscaledLineHeight * num));
		if (num2 >= 0 && num2 < Values.Length)
		{
			HoveredIndex = num2;
			args.Handled = true;
		}
	}

	public override void OnMouseUp(ICoreClientAPI api, MouseEvent args)
	{
		base.OnMouseUp(api, args);
		if (expanded)
		{
			scrollbar.OnMouseUp(api, args);
		}
	}

	public void Open()
	{
		expanded = true;
	}

	internal void Close()
	{
		expanded = false;
	}

	public override void OnMouseDown(ICoreClientAPI api, MouseEvent args)
	{
		if (!expanded || !((double)args.X >= Bounds.renderX) || !((double)args.X <= Bounds.renderX + expandedBoxWidth))
		{
			return;
		}
		double num = Scale * (double)RuntimeEnv.GUIScale;
		if (Bounds.renderX + expandedBoxWidth - (double)args.X < GuiElement.scaled(10.0))
		{
			scrollbar.OnMouseDown(api, args);
			return;
		}
		double num2 = (double)args.Y - Bounds.renderY - unscaledLineHeight * num;
		if (num2 < 0.0 || num2 > visibleBounds.OuterHeight)
		{
			expanded = false;
			args.Handled = true;
			api.Gui.PlaySound("menubutton");
			return;
		}
		int num3 = (int)(((double)api.Input.MouseY - Bounds.renderY - Bounds.InnerHeight + scrollOffY) / (unscaledLineHeight * num));
		if (num3 >= 0 && num3 < Values.Length)
		{
			if (multiSelect)
			{
				switches[num3].OnMouseDownOnElement(api, args);
				onSelectionChanged?.Invoke(Values[num3], switches[num3].On);
			}
			else
			{
				SelectedIndex = num3;
				onSelectionChanged?.Invoke(Values[SelectedIndex], selected: true);
			}
			api.Gui.PlaySound("toggleswitch");
			if (!multiSelect)
			{
				expanded = false;
			}
			args.Handled = true;
		}
	}

	public override void OnMouseWheel(ICoreClientAPI api, MouseWheelEventArgs args)
	{
		if (expanded && visibleBounds.PointInside(api.Input.MouseX, api.Input.MouseY))
		{
			scrollbar.OnMouseWheel(api, args);
		}
	}

	public override void OnFocusLost()
	{
		base.OnFocusLost();
		expanded = false;
	}

	public void SetSelectedIndex(int selectedIndex)
	{
		SelectedIndex = selectedIndex;
	}

	public void SetSelectedValue(params string[] value)
	{
		if (value == null)
		{
			SelectedIndices = Array.Empty<int>();
			return;
		}
		List<int> list = new List<int>();
		for (int i = 0; i < Values.Length; i++)
		{
			if (multiSelect)
			{
				switches[i].On = false;
			}
			for (int j = 0; j < value.Length; j++)
			{
				if (Values[i] == value[j])
				{
					list.Add(i);
					if (multiSelect)
					{
						switches[i].On = true;
					}
				}
			}
		}
		SelectedIndices = list.ToArray();
	}

	public void SetList(string[] values, string[] names)
	{
		Values = values;
		Names = names;
		ComposeDynamicElements();
	}

	public override void Dispose()
	{
		base.Dispose();
		hoverTexture.Dispose();
		dropDownTexture.Dispose();
		scrollbarTexture.Dispose();
		scrollbar?.Dispose();
	}
}
