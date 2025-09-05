using System;
using Cairo;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vintagestory.API.Client;

public class GuiElementDialogTitleBar : GuiElementTextBase
{
	private GuiElementListMenu listMenu;

	private Action OnClose;

	internal GuiComposer baseComposer;

	public static int unscaledCloseIconSize = 15;

	private LoadedTexture closeIconHoverTexture;

	private LoadedTexture menuIconHoverTexture;

	private Rectangled closeIconRect;

	private Rectangled menuIconRect;

	private bool didInit;

	private bool firstFrameRendered;

	public bool drawBg;

	private bool movable;

	private bool moving;

	private Vec2i movingStartPos = new Vec2i();

	private ElementBounds parentBoundsBefore;

	public bool Movable => movable;

	public GuiElementDialogTitleBar(ICoreClientAPI capi, string text, GuiComposer composer, Action OnClose = null, CairoFont font = null, ElementBounds bounds = null)
		: base(capi, text, font, bounds)
	{
		closeIconHoverTexture = new LoadedTexture(capi);
		menuIconHoverTexture = new LoadedTexture(capi);
		if (bounds == null)
		{
			Bounds = ElementStdBounds.TitleBar();
		}
		if (font == null)
		{
			Font = CairoFont.WhiteSmallText();
		}
		this.OnClose = OnClose;
		ElementBounds bounds2 = ElementBounds.Fixed(0.0, 0.0, 100.0, 25.0);
		Bounds.WithChild(bounds2);
		listMenu = new GuiElementListMenu(capi, new string[2] { "auto", "manual" }, new string[2]
		{
			Lang.Get("Fixed"),
			Lang.Get("Movable")
		}, 0, onSelectionChanged, bounds2, CairoFont.WhiteSmallText(), multiSelect: false)
		{
			HoveredIndex = 0
		};
		baseComposer = composer;
	}

	private void onSelectionChanged(string val, bool on)
	{
		SetUpMovableState(val);
	}

	private void SetUpMovableState(string val)
	{
		if (val == null)
		{
			Vec2i dialogPosition = api.Gui.GetDialogPosition(baseComposer.DialogName);
			if (dialogPosition != null)
			{
				movable = true;
				parentBoundsBefore = Bounds.ParentBounds.FlatCopy();
				Bounds.ParentBounds.Alignment = EnumDialogArea.None;
				Bounds.ParentBounds.fixedX = dialogPosition.X;
				Bounds.ParentBounds.fixedY = Math.Max(0.0 - Bounds.ParentBounds.fixedOffsetY, dialogPosition.Y);
				Bounds.ParentBounds.absMarginX = 0.0;
				Bounds.ParentBounds.absMarginY = 0.0;
				Bounds.ParentBounds.MarkDirtyRecursive();
				Bounds.ParentBounds.CalcWorldBounds();
			}
		}
		else if (val == "auto")
		{
			if (parentBoundsBefore != null)
			{
				Bounds.ParentBounds.fixedX = parentBoundsBefore.fixedX;
				Bounds.ParentBounds.fixedY = parentBoundsBefore.fixedY;
				Bounds.ParentBounds.fixedOffsetX = parentBoundsBefore.fixedOffsetX;
				Bounds.ParentBounds.fixedOffsetY = parentBoundsBefore.fixedOffsetY;
				Bounds.ParentBounds.Alignment = parentBoundsBefore.Alignment;
				Bounds.ParentBounds.absMarginX = parentBoundsBefore.absMarginX;
				Bounds.ParentBounds.absMarginY = parentBoundsBefore.absMarginY;
				Bounds.ParentBounds.MarkDirtyRecursive();
				Bounds.ParentBounds.CalcWorldBounds();
			}
			movable = false;
			api.Gui.SetDialogPosition(baseComposer.DialogName, null);
		}
		else
		{
			movable = true;
			parentBoundsBefore = Bounds.ParentBounds.FlatCopy();
			Bounds.ParentBounds.Alignment = EnumDialogArea.None;
			Bounds.ParentBounds.fixedOffsetX = 0.0;
			Bounds.ParentBounds.fixedOffsetY = 0.0;
			Bounds.ParentBounds.fixedX = Bounds.ParentBounds.absX / (double)RuntimeEnv.GUIScale;
			Bounds.ParentBounds.fixedY = Bounds.ParentBounds.absY / (double)RuntimeEnv.GUIScale;
			Bounds.ParentBounds.absMarginX = 0.0;
			Bounds.ParentBounds.absMarginY = 0.0;
			Bounds.ParentBounds.MarkDirtyRecursive();
			Bounds.ParentBounds.CalcWorldBounds();
		}
	}

	public override void ComposeTextElements(Context ctx, ImageSurface surface)
	{
		//IL_0311: Unknown result type (might be due to invalid IL or missing references)
		//IL_0316: Unknown result type (might be due to invalid IL or missing references)
		if (!didInit)
		{
			SetUpMovableState(null);
			didInit = true;
		}
		Bounds.CalcWorldBounds();
		double num = 5.0;
		GuiElement.RoundRectangle(ctx, Bounds.bgDrawX, Bounds.bgDrawY, Bounds.OuterWidth, Bounds.OuterHeight, 0.0);
		ctx.SetSourceRGBA(GuiStyle.DialogStrongBgColor[0] * 1.2, GuiStyle.DialogStrongBgColor[1] * 1.2, GuiStyle.DialogStrongBgColor[2] * 1.2, GuiStyle.DialogStrongBgColor[3]);
		ctx.FillPreserve();
		GuiElement.RoundRectangle(ctx, Bounds.bgDrawX + num, Bounds.bgDrawY + num, Bounds.OuterWidth - 2.0 * num, Bounds.OuterHeight - 2.0 * num, 0.0);
		ctx.SetSourceRGBA(GuiStyle.DialogLightBgColor[0] * 1.6, GuiStyle.DialogStrongBgColor[1] * 1.6, GuiStyle.DialogStrongBgColor[2] * 1.6, 1.0);
		ctx.LineWidth = num * 1.75;
		ctx.StrokePreserve();
		double num2 = GuiElement.scaled(8.0);
		SurfaceTransformBlur.BlurPartial(surface, num2, (int)(2.0 * num2 + 1.0), (int)Bounds.bgDrawX, (int)(Bounds.bgDrawY + 0.0), (int)Bounds.OuterWidth, (int)Bounds.InnerHeight);
		double num3 = 0.0;
		ctx.NewPath();
		ctx.MoveTo(Bounds.drawX, Bounds.drawY + Bounds.InnerHeight);
		ctx.LineTo(Bounds.drawX, Bounds.drawY + num3);
		ctx.Arc(Bounds.drawX + num3, Bounds.drawY + num3, num3, 3.1415927410125732, 4.71238899230957);
		ctx.Arc(Bounds.drawX + Bounds.OuterWidth - num3, Bounds.drawY + num3, num3, -1.5707963705062866, 0.0);
		ctx.LineTo(Bounds.drawX + Bounds.OuterWidth, Bounds.drawY + Bounds.InnerHeight);
		ctx.SetSourceRGBA(new double[4]
		{
			0.17647058823529413,
			7.0 / 51.0,
			11.0 / 85.0,
			1.0
		});
		ctx.LineWidth = num;
		ctx.Stroke();
		Font.SetupContext(ctx);
		string obj = text;
		double posX = GuiElement.scaled(GuiStyle.ElementToDialogPadding);
		double innerHeight = Bounds.InnerHeight;
		FontExtents fontExtents = Font.GetFontExtents();
		DrawTextLineAt(ctx, obj, posX, (innerHeight - ((FontExtents)(ref fontExtents)).Height) / 2.0 + GuiElement.scaled(1.0));
		double num4 = GuiElement.scaled(unscaledCloseIconSize);
		double num5 = GuiElement.scaled(unscaledCloseIconSize + 2);
		double num6 = Bounds.drawX + Bounds.OuterWidth - num4 - GuiElement.scaled(12.0);
		double num7 = Bounds.drawY + GuiElement.scaled(7.0);
		double lineWidth = GuiElement.scaled(2.0);
		double num8 = Bounds.drawX + Bounds.OuterWidth - num4 - num5 - GuiElement.scaled(20.0);
		menuIconRect = new Rectangled(Bounds.OuterWidth - num4 - num5 - GuiElement.scaled(20.0), GuiElement.scaled(6.0), num4, num4);
		closeIconRect = new Rectangled(Bounds.OuterWidth - num4 - GuiElement.scaled(12.0), GuiElement.scaled(5.0), num5, num5);
		ctx.Operator = (Operator)2;
		ctx.SetSourceRGBA(0.0, 0.0, 0.0, 0.3);
		api.Gui.Icons.DrawCross(ctx, num6 + 2.0, num7 + 2.0, lineWidth, num4);
		ctx.Operator = (Operator)1;
		ctx.SetSourceRGBA(GuiStyle.DialogDefaultTextColor);
		api.Gui.Icons.DrawCross(ctx, num6, num7, lineWidth, num4);
		ctx.Operator = (Operator)2;
		api.Gui.Icons.Drawmenuicon_svg(ctx, (int)num8 + 2, (int)num7 + 2, (int)num5, (int)num5, new double[4] { 0.0, 0.0, 0.0, 0.3 });
		ctx.Operator = (Operator)1;
		api.Gui.Icons.Drawmenuicon_svg(ctx, (int)num8, (int)num7 + 1, (int)num5, (int)num5, GuiStyle.DialogDefaultTextColor);
		ctx.Operator = (Operator)2;
		ComposeHoverIcons();
		listMenu.Bounds.fixedX = (Bounds.absX + menuIconRect.X - Bounds.absX) / (double)RuntimeEnv.GUIScale;
		listMenu.ComposeDynamicElements();
	}

	private void ComposeHoverIcons()
	{
		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		//IL_003e: Expected O, but got Unknown
		//IL_011f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0125: Expected O, but got Unknown
		double num = GuiElement.scaled(unscaledCloseIconSize);
		double num2 = GuiElement.scaled(unscaledCloseIconSize + 2);
		int num3 = (int)Math.Round(GuiElement.scaled(1.9));
		ImageSurface val = new ImageSurface((Format)0, (int)num + 4, (int)num + 4);
		Context val2 = genContext(val);
		val2.Operator = (Operator)1;
		val2.SetSourceRGBA(0.8, 0.0, 0.0, 1.0);
		api.Gui.Icons.DrawCross(val2, 0.5, 1.5, num3, num);
		val2.SetSourceRGBA(0.8, 0.2, 0.2, 1.0);
		api.Gui.Icons.DrawCross(val2, 1.0, 2.0, num3, num);
		generateTexture(val, ref closeIconHoverTexture);
		((Surface)val).Dispose();
		val2.Dispose();
		val = new ImageSurface((Format)0, (int)num2, (int)num2);
		val2 = genContext(val);
		val2.Operator = (Operator)1;
		api.Gui.Icons.Drawmenuicon_svg(val2, 0.0, GuiElement.scaled(1.0), (int)num2, (int)num2, new double[4] { 0.0, 0.8, 0.0, 0.6 });
		generateTexture(val, ref menuIconHoverTexture);
		((Surface)val).Dispose();
		val2.Dispose();
	}

	public override void RenderInteractiveElements(float deltaTime)
	{
		if (!firstFrameRendered && movable)
		{
			float gUIScale = RuntimeEnv.GUIScale;
			double num = (float)api.Render.FrameWidth - 60f * gUIScale;
			double num2 = (float)api.Render.FrameHeight - 60f * gUIScale;
			double num3 = GameMath.Clamp((double)(int)Bounds.ParentBounds.fixedX + Bounds.ParentBounds.fixedOffsetX, 0.0, num / (double)gUIScale) - Bounds.ParentBounds.fixedOffsetX;
			double num4 = GameMath.Clamp((double)(int)Bounds.ParentBounds.fixedY + Bounds.ParentBounds.fixedOffsetY, 0.0, num2 / (double)gUIScale) - Bounds.ParentBounds.fixedOffsetY;
			api.Gui.SetDialogPosition(baseComposer.DialogName, new Vec2i((int)num3, (int)num4));
			Bounds.ParentBounds.fixedX = num3;
			Bounds.ParentBounds.fixedY = num4;
			Bounds.ParentBounds.CalcWorldBounds();
			firstFrameRendered = true;
		}
		int mouseX = api.Input.MouseX;
		int mouseY = api.Input.MouseY;
		if (closeIconRect.PointInside((double)mouseX - Bounds.absX, (double)mouseY - Bounds.absY))
		{
			api.Render.Render2DTexturePremultipliedAlpha(closeIconHoverTexture.TextureId, Bounds.absX + closeIconRect.X - GuiElement.scaled(1.0), Bounds.absY + closeIconRect.Y, closeIconRect.Width + 4.0, closeIconRect.Height + 4.0, 200f);
		}
		if (menuIconRect.PointInside((double)mouseX - Bounds.absX, (double)mouseY - Bounds.absY) || listMenu.IsOpened)
		{
			api.Render.Render2DTexturePremultipliedAlpha(menuIconHoverTexture.TextureId, Bounds.absX + menuIconRect.X, Bounds.absY + menuIconRect.Y, menuIconRect.Width + 4.0, menuIconRect.Height + 4.0, 200f);
		}
		listMenu.RenderInteractiveElements(deltaTime);
	}

	public override void OnMouseUpOnElement(ICoreClientAPI api, MouseEvent args)
	{
		int mouseX = api.Input.MouseX;
		int mouseY = api.Input.MouseY;
		if (closeIconRect.PointInside((double)mouseX - Bounds.absX, (double)mouseY - Bounds.absY))
		{
			args.Handled = true;
			OnClose?.Invoke();
		}
		else if (menuIconRect.PointInside((double)mouseX - Bounds.absX, (double)mouseY - Bounds.absY))
		{
			listMenu.Open();
		}
	}

	public override void OnKeyDown(ICoreClientAPI api, KeyEvent args)
	{
		listMenu.OnKeyDown(api, args);
	}

	public override void OnMouseUp(ICoreClientAPI api, MouseEvent args)
	{
		listMenu.OnMouseUp(api, args);
		base.OnMouseUp(api, args);
		if (moving)
		{
			api.Gui.SetDialogPosition(baseComposer.DialogName, new Vec2i((int)Bounds.ParentBounds.fixedX, (int)Bounds.ParentBounds.fixedY));
		}
		moving = false;
	}

	public override void OnMouseMove(ICoreClientAPI api, MouseEvent args)
	{
		listMenu.OnMouseMove(api, args);
		if (moving)
		{
			Bounds.ParentBounds.fixedX += (float)(args.X - movingStartPos.X) / RuntimeEnv.GUIScale;
			Bounds.ParentBounds.fixedY += (float)(args.Y - movingStartPos.Y) / RuntimeEnv.GUIScale;
			movingStartPos.Set(args.X, args.Y);
			Bounds.ParentBounds.CalcWorldBounds();
		}
	}

	public override void OnMouseDown(ICoreClientAPI api, MouseEvent args)
	{
		listMenu.OnMouseDown(api, args);
		if (movable && !args.Handled && IsPositionInside(args.X, args.Y))
		{
			moving = true;
			movingStartPos.Set(args.X, args.Y);
		}
		if (!args.Handled && !listMenu.IsPositionInside(args.X, args.Y))
		{
			listMenu.Close();
		}
	}

	public override void OnFocusLost()
	{
		base.OnFocusLost();
		listMenu.OnFocusLost();
	}

	internal void SetSelectedIndex(int selectedIndex)
	{
		listMenu.SetSelectedIndex(selectedIndex);
	}

	public override void Dispose()
	{
		base.Dispose();
		closeIconHoverTexture.Dispose();
		menuIconHoverTexture.Dispose();
		listMenu?.Dispose();
	}
}
