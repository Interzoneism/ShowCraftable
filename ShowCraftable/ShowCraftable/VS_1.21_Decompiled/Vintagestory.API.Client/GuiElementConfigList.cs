using System;
using System.Collections.Generic;
using Cairo;
using Vintagestory.API.Config;

namespace Vintagestory.API.Client;

public class GuiElementConfigList : GuiElementTextBase
{
	public static double unscaledPadding = 2.0;

	public double leftWidthRel = 0.65;

	public double rightWidthRel = 0.3;

	public List<ConfigItem> items;

	private ConfigItemClickDelegate OnItemClick;

	private int textureId;

	private LoadedTexture hoverTexture;

	public ElementBounds innerBounds;

	public CairoFont errorFont;

	public CairoFont stdFont;

	public CairoFont titleFont;

	public GuiElementConfigList(ICoreClientAPI capi, List<ConfigItem> items, ConfigItemClickDelegate OnItemClick, CairoFont font, ElementBounds bounds)
		: base(capi, "", font, bounds)
	{
		hoverTexture = new LoadedTexture(capi);
		this.items = items;
		this.OnItemClick = OnItemClick;
		errorFont = font.Clone();
		stdFont = font;
		titleFont = font.Clone().WithWeight((FontWeight)1);
		titleFont.Color[3] = 0.6;
	}

	public void Autoheight()
	{
		double num = 9.0;
		double num2 = GuiElement.scaled(unscaledPadding);
		Bounds.CalcWorldBounds();
		bool flag = true;
		foreach (ConfigItem item in items)
		{
			double num3 = Math.Max(textUtil.GetMultilineTextHeight(Font, item.Key, Bounds.InnerWidth * leftWidthRel), textUtil.GetMultilineTextHeight(Font, item.Value, Bounds.InnerWidth * rightWidthRel));
			if (!flag && item.Type == EnumItemType.Title)
			{
				num3 += GuiElement.scaled(20.0);
			}
			num += num2 + num3 + num2;
			flag = false;
		}
		innerBounds = Bounds.FlatCopy();
		innerBounds.fixedHeight = num / (double)RuntimeEnv.GUIScale;
		innerBounds.CalcWorldBounds();
	}

	public override void ComposeTextElements(Context ctxs, ImageSurface surfaces)
	{
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		//IL_000e: Expected O, but got Unknown
		ImageSurface val = new ImageSurface((Format)0, 200, 10);
		Context obj = genContext(val);
		obj.SetSourceRGBA(1.0, 1.0, 1.0, 0.4);
		obj.Paint();
		generateTexture(val, ref hoverTexture);
		obj.Dispose();
		((Surface)val).Dispose();
		Refresh();
	}

	public void Refresh()
	{
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0023: Expected O, but got Unknown
		Autoheight();
		ImageSurface val = new ImageSurface((Format)0, innerBounds.OuterWidthInt, innerBounds.OuterHeightInt);
		Context val2 = genContext(val);
		double num = 4.0;
		double num2 = GuiElement.scaled(unscaledPadding);
		bool flag = true;
		foreach (ConfigItem item in items)
		{
			if (item.error)
			{
				Font = errorFont;
			}
			else
			{
				Font = stdFont;
			}
			if (item.Type == EnumItemType.Title)
			{
				Font = titleFont;
			}
			double num3 = ((!flag && item.Type == EnumItemType.Title) ? GuiElement.scaled(20.0) : 0.0);
			double val3 = textUtil.AutobreakAndDrawMultilineTextAt(val2, Font, item.Key, 0.0, num3 + num + num2, innerBounds.InnerWidth * leftWidthRel);
			double val4 = textUtil.AutobreakAndDrawMultilineTextAt(val2, Font, item.Value, innerBounds.InnerWidth * (1.0 - rightWidthRel), num3 + num + num2, innerBounds.InnerWidth * rightWidthRel);
			double num4 = num3 + num2 + Math.Max(val3, val4) + num2;
			item.posY = num;
			item.height = num4;
			num += num4;
			flag = false;
		}
		generateTexture(val, ref textureId);
		((Surface)val).Dispose();
		val2.Dispose();
	}

	public override void RenderInteractiveElements(float deltaTime)
	{
		api.Render.Render2DTexturePremultipliedAlpha(textureId, innerBounds);
		int mouseX = api.Input.MouseX;
		int mouseY = api.Input.MouseY;
		if (!innerBounds.PointInside(mouseX, mouseY))
		{
			return;
		}
		foreach (ConfigItem item in items)
		{
			double num = (double)mouseY - (item.posY + innerBounds.absY);
			if (item.Type != EnumItemType.Title && num > 0.0 && num < item.height)
			{
				api.Render.Render2DTexturePremultipliedAlpha(hoverTexture.TextureId, (int)innerBounds.absX, (int)innerBounds.absY + (int)item.posY, innerBounds.InnerWidth, item.height);
			}
		}
	}

	public override void OnMouseDownOnElement(ICoreClientAPI api, MouseEvent args)
	{
		if (!innerBounds.ParentBounds.PointInside(args.X, args.Y))
		{
			return;
		}
		int mouseX = api.Input.MouseX;
		int mouseY = api.Input.MouseY;
		if (!innerBounds.PointInside(mouseX, mouseY))
		{
			return;
		}
		int num = 0;
		int num2 = 0;
		foreach (ConfigItem item in items)
		{
			double num3 = (double)mouseY - (item.posY + innerBounds.absY);
			if (item.Type != EnumItemType.Title && num3 > 0.0 && num3 < item.height)
			{
				OnItemClick(num, num2);
			}
			num++;
			if (item.Type != EnumItemType.Title)
			{
				num2++;
			}
		}
	}

	public override void Dispose()
	{
		base.Dispose();
		hoverTexture.Dispose();
	}
}
