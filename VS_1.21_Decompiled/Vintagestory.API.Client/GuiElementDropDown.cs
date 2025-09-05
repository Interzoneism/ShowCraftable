using System;
using System.Linq;
using Cairo;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;

namespace Vintagestory.API.Client;

public class GuiElementDropDown : GuiElementTextBase
{
	public string SingularNameCode = "{0} item";

	public string PluralNameCode = "{0} items";

	public string PluralMoreNameCode = "+{0} more";

	public string SingularMoreNameCode = "+{0} more";

	public GuiElementListMenu listMenu;

	public GuiElementRichtext richTextElem;

	protected LoadedTexture highlightTexture;

	protected LoadedTexture currentValueTexture;

	protected LoadedTexture arrowDownButtonReleased;

	protected LoadedTexture arrowDownButtonPressed;

	protected ElementBounds highlightBounds;

	protected SelectionChangedDelegate onSelectionChanged;

	private bool multiSelect;

	private int valueWidth;

	private int valueHeight;

	public override double DrawOrder => 0.5;

	public override bool Focusable => enabled;

	public override double Scale
	{
		get
		{
			return base.Scale;
		}
		set
		{
			base.Scale = value;
			listMenu.Scale = value;
		}
	}

	public string SelectedValue
	{
		get
		{
			if (listMenu.SelectedIndex < 0)
			{
				return null;
			}
			return listMenu.Values[listMenu.SelectedIndex];
		}
	}

	public int[] SelectedIndices => listMenu.SelectedIndices;

	public string[] SelectedValues => listMenu.SelectedIndices.Select((int index) => listMenu.Values[index]).ToArray();

	public override bool Enabled
	{
		get
		{
			return base.Enabled;
		}
		set
		{
			if (enabled != value && currentValueTexture != null)
			{
				ComposeCurrentValue();
			}
			base.Enabled = value;
		}
	}

	public GuiElementDropDown(ICoreClientAPI capi, string[] values, string[] names, int selectedIndex, SelectionChangedDelegate onSelectionChanged, ElementBounds bounds, CairoFont font, bool multiSelect)
		: base(capi, "", font, bounds)
	{
		highlightTexture = new LoadedTexture(capi);
		currentValueTexture = new LoadedTexture(capi);
		arrowDownButtonReleased = new LoadedTexture(capi);
		arrowDownButtonPressed = new LoadedTexture(capi);
		ElementBounds bounds2 = bounds.ForkChildOffseted(0.0 - bounds.fixedX, 0.0 - bounds.fixedY).WithAlignment(EnumDialogArea.None);
		listMenu = new GuiElementListMenu(capi, values, names, selectedIndex, didSelect, bounds2, font, multiSelect)
		{
			HoveredIndex = selectedIndex
		};
		ElementBounds bounds3 = ElementBounds.Fixed(0.0, 0.0, 900.0, 100.0).WithEmptyParent();
		richTextElem = new GuiElementRichtext(capi, Array.Empty<RichTextComponentBase>(), bounds3);
		this.onSelectionChanged = onSelectionChanged;
		this.multiSelect = multiSelect;
	}

	private void didSelect(string newvalue, bool on)
	{
		onSelectionChanged?.Invoke(newvalue, on);
		ComposeCurrentValue();
	}

	public override void ComposeElements(Context ctx, ImageSurface surface)
	{
		Bounds.CalcWorldBounds();
		ctx.SetSourceRGBA(0.0, 0.0, 0.0, 0.2);
		GuiElement.RoundRectangle(ctx, Bounds.drawX, Bounds.drawY, Bounds.InnerWidth, Bounds.InnerHeight, 3.0);
		ctx.Fill();
		EmbossRoundRectangleElement(ctx, Bounds, inverse: true, 1, 1);
		listMenu.ComposeDynamicElements();
		ComposeDynamicElements();
	}

	private void ComposeDynamicElements()
	{
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		//IL_002d: Expected O, but got Unknown
		//IL_03fc: Unknown result type (might be due to invalid IL or missing references)
		//IL_0403: Expected O, but got Unknown
		int num = (int)(GuiElement.scaled(20.0) * Scale);
		int num2 = (int)Bounds.InnerHeight;
		ImageSurface val = new ImageSurface((Format)0, num, num2);
		Context val2 = genContext(val);
		val2.SetSourceRGB(GuiStyle.DialogDefaultBgColor[0], GuiStyle.DialogDefaultBgColor[1], GuiStyle.DialogDefaultBgColor[2]);
		GuiElement.RoundRectangle(val2, 0.0, 0.0, num, num2, GuiStyle.ElementBGRadius);
		val2.FillPreserve();
		val2.SetSourceRGBA(1.0, 1.0, 1.0, 0.1);
		val2.Fill();
		EmbossRoundRectangleElement(val2, 0.0, 0.0, num, num2, inverse: false, 2, 1);
		val2.SetSourceRGBA(GuiStyle.DialogHighlightColor);
		GuiElement.RoundRectangle(val2, 0.0, 0.0, num, num2, 1.0);
		val2.Fill();
		double num3 = Math.Min(Bounds.OuterHeight - GuiElement.scaled(6.0), GuiElement.scaled(16.0));
		double num4 = (Bounds.OuterHeight - num3) / 2.0;
		double num5 = num4;
		double num6 = num3 + num4;
		val2.NewPath();
		val2.LineTo((double)num - GuiElement.scaled(17.0) * Scale, num5 * Scale);
		val2.LineTo((double)num - GuiElement.scaled(3.0) * Scale, num5 * Scale);
		val2.LineTo((double)num - GuiElement.scaled(10.0) * Scale, num6 * Scale);
		val2.ClosePath();
		val2.SetSourceRGBA(1.0, 1.0, 1.0, 0.6);
		val2.Fill();
		generateTexture(val, ref arrowDownButtonReleased);
		val2.Operator = (Operator)0;
		val2.SetSourceRGBA(0.0, 0.0, 0.0, 0.0);
		val2.Paint();
		val2.Operator = (Operator)2;
		val2.SetSourceRGB(GuiStyle.DialogDefaultBgColor[0], GuiStyle.DialogDefaultBgColor[1], GuiStyle.DialogDefaultBgColor[2]);
		GuiElement.RoundRectangle(val2, 0.0, 0.0, num, num2, GuiStyle.ElementBGRadius);
		val2.FillPreserve();
		val2.SetSourceRGBA(0.0, 0.0, 0.0, 0.1);
		val2.Fill();
		EmbossRoundRectangleElement(val2, 0.0, 0.0, num, num2, inverse: true, 2, 1);
		val2.SetSourceRGBA(GuiStyle.DialogHighlightColor);
		GuiElement.RoundRectangle(val2, 0.0, 0.0, num, num2, 1.0);
		val2.Fill();
		val2.NewPath();
		val2.LineTo((double)num - GuiElement.scaled(17.0) * Scale, num5 * Scale);
		val2.LineTo((double)num - GuiElement.scaled(3.0) * Scale, num5 * Scale);
		val2.LineTo((double)num - GuiElement.scaled(10.0) * Scale, num6 * Scale);
		val2.ClosePath();
		val2.SetSourceRGBA(1.0, 1.0, 1.0, 0.4);
		val2.Fill();
		generateTexture(val, ref arrowDownButtonPressed);
		((Surface)val).Dispose();
		val2.Dispose();
		ImageSurface val3 = new ImageSurface((Format)0, (int)Bounds.OuterWidth - num, (int)Bounds.OuterHeight);
		Context obj = genContext(val3);
		obj.SetSourceRGBA(1.0, 1.0, 1.0, 0.3);
		obj.Paint();
		generateTexture(val3, ref highlightTexture);
		obj.Dispose();
		((Surface)val3).Dispose();
		highlightBounds = Bounds.ForkChildOffseted(0.0 - Bounds.fixedX, 0.0 - Bounds.fixedY).WithFixedWidth(Bounds.fixedWidth - (double)((float)num / RuntimeEnv.GUIScale)).WithAlignment(EnumDialogArea.None);
		highlightBounds.CalcWorldBounds();
		ComposeCurrentValue();
	}

	private void ComposeCurrentValue()
	{
		//IL_005d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0063: Expected O, but got Unknown
		//IL_00ac: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b1: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f7: Unknown result type (might be due to invalid IL or missing references)
		//IL_01fc: Unknown result type (might be due to invalid IL or missing references)
		double innerWidth = Bounds.InnerWidth;
		valueWidth = (int)((Bounds.InnerWidth - GuiElement.scaled(20.0)) * Scale);
		valueHeight = (int)(GuiElement.scaled(30.0) * Scale);
		ImageSurface val = new ImageSurface((Format)0, valueWidth, valueHeight);
		Context val2 = genContext(val);
		if (!enabled)
		{
			Font.Color[3] = 0.3499999940395355;
		}
		Font.SetupContext(val2);
		val2.SetSourceRGBA(GuiStyle.DialogDefaultTextColor);
		string text = "";
		FontExtents fontExtents = Font.GetFontExtents();
		double height = ((FontExtents)(ref fontExtents)).Height;
		if (listMenu.SelectedIndices.Length > 1)
		{
			for (int i = 0; i < listMenu.SelectedIndices.Length; i++)
			{
				int num = listMenu.SelectedIndices[i];
				string text2 = "";
				if (text.Length > 0)
				{
					text2 += ", ";
				}
				text2 += listMenu.Names[num];
				int num2 = listMenu.SelectedIndices.Length - i;
				int num3 = listMenu.SelectedIndices.Length;
				string text3 = ((text.Length > 0) ? (" " + ((num2 == 1) ? Lang.Get(SingularMoreNameCode, num2) : Lang.Get(PluralMoreNameCode, num2))) : ((num3 == 1) ? Lang.Get(SingularNameCode, num3) : Lang.Get(PluralNameCode, num3)));
				TextExtents textExtents = Font.GetTextExtents(text + text2 + Lang.Get(PluralMoreNameCode, num2));
				if (((TextExtents)(ref textExtents)).Width < innerWidth)
				{
					text += text2;
					continue;
				}
				text += text3;
				break;
			}
		}
		else if (listMenu.SelectedIndices.Length == 1 && listMenu.Names.Length != 0)
		{
			text = listMenu.Names[listMenu.SelectedIndex];
		}
		richTextElem.SetNewTextWithoutRecompose(text, Font);
		richTextElem.BeforeCalcBounds();
		richTextElem.Bounds.fixedX = 5.0;
		richTextElem.Bounds.fixedY = ((double)valueHeight - height) / 2.0 / (double)RuntimeEnv.GUIScale;
		richTextElem.BeforeCalcBounds();
		richTextElem.Bounds.CalcWorldBounds();
		richTextElem.ComposeFor(richTextElem.Bounds, val2, val);
		generateTexture(val, ref currentValueTexture);
		val2.Dispose();
		((Surface)val).Dispose();
	}

	public override void RenderInteractiveElements(float deltaTime)
	{
		if (base.HasFocus)
		{
			api.Render.Render2DTexture(highlightTexture.TextureId, highlightBounds);
		}
		api.Render.Render2DTexturePremultipliedAlpha(currentValueTexture.TextureId, (int)Bounds.renderX, (double)(int)Bounds.renderY + (Bounds.InnerHeight - (double)valueHeight) / 2.0, valueWidth, valueHeight);
		double posX = Bounds.renderX + Bounds.InnerWidth - (double)arrowDownButtonReleased.Width;
		double renderY = Bounds.renderY;
		if (listMenu.IsOpened)
		{
			api.Render.Render2DTexturePremultipliedAlpha(arrowDownButtonPressed.TextureId, posX, renderY, arrowDownButtonReleased.Width, arrowDownButtonReleased.Height);
		}
		else
		{
			api.Render.Render2DTexturePremultipliedAlpha(arrowDownButtonReleased.TextureId, posX, renderY, arrowDownButtonReleased.Width, arrowDownButtonReleased.Height);
		}
		listMenu.RenderInteractiveElements(deltaTime);
	}

	public override void OnKeyDown(ICoreClientAPI api, KeyEvent args)
	{
		if (base.HasFocus)
		{
			listMenu.OnKeyDown(api, args);
		}
	}

	public override void OnMouseMove(ICoreClientAPI api, MouseEvent args)
	{
		listMenu.OnMouseMove(api, args);
	}

	public override void OnMouseWheel(ICoreClientAPI api, MouseWheelEventArgs args)
	{
		if (enabled && base.HasFocus)
		{
			if (!listMenu.IsOpened && IsPositionInside(api.Input.MouseX, api.Input.MouseY))
			{
				SetSelectedIndex(GameMath.Mod(listMenu.SelectedIndex + ((args.delta <= 0) ? 1 : (-1)), listMenu.Values.Length));
				args.SetHandled();
				onSelectionChanged?.Invoke(SelectedValue, selected: true);
			}
			else
			{
				listMenu.OnMouseWheel(api, args);
			}
		}
	}

	public override void OnMouseUp(ICoreClientAPI api, MouseEvent args)
	{
		if (enabled)
		{
			listMenu.OnMouseUp(api, args);
			args.Handled |= IsPositionInside(args.X, args.Y);
		}
	}

	public override bool IsPositionInside(int posX, int posY)
	{
		if (!base.IsPositionInside(posX, posY))
		{
			if (listMenu.IsOpened)
			{
				return listMenu.IsPositionInside(posX, posY);
			}
			return false;
		}
		return true;
	}

	public override void OnMouseDown(ICoreClientAPI api, MouseEvent args)
	{
		if (enabled)
		{
			listMenu.OnMouseDown(api, args);
			if (!listMenu.IsOpened && IsPositionInside(args.X, args.Y) && !args.Handled)
			{
				listMenu.Open();
				api.Gui.PlaySound("menubutton");
				args.Handled = true;
			}
		}
	}

	public override void OnFocusGained()
	{
		base.OnFocusGained();
		listMenu.OnFocusGained();
	}

	public override void OnFocusLost()
	{
		base.OnFocusLost();
		listMenu.OnFocusLost();
	}

	public void SetSelectedIndex(int selectedIndex)
	{
		listMenu.SetSelectedIndex(selectedIndex);
		ComposeCurrentValue();
	}

	public void SetSelectedValue(params string[] value)
	{
		listMenu.SetSelectedValue(value);
		ComposeCurrentValue();
	}

	public void SetList(string[] values, string[] names)
	{
		listMenu.SetList(values, names);
	}

	public override void Dispose()
	{
		base.Dispose();
		highlightTexture.Dispose();
		currentValueTexture.Dispose();
		listMenu?.Dispose();
		arrowDownButtonReleased.Dispose();
		arrowDownButtonPressed.Dispose();
	}
}
