using System;
using System.Collections.Generic;
using Cairo;

namespace Vintagestory.API.Client;

public class GuiElementSkillItemGrid : GuiElement
{
	private List<SkillItem> skillItems;

	private int cols;

	private int rows;

	public Action<int> OnSlotClick;

	public Action<int> OnSlotOver;

	public int selectedIndex = -1;

	private LoadedTexture hoverTexture;

	public override bool Focusable => true;

	public GuiElementSkillItemGrid(ICoreClientAPI capi, List<SkillItem> skillItems, int columns, int rows, Action<int> OnSlotClick, ElementBounds bounds)
		: base(capi, bounds)
	{
		hoverTexture = new LoadedTexture(capi);
		this.skillItems = skillItems;
		cols = columns;
		this.rows = rows;
		this.OnSlotClick = OnSlotClick;
		Bounds.fixedHeight = (double)rows * (GuiElementItemSlotGridBase.unscaledSlotPadding + GuiElementPassiveItemSlot.unscaledSlotSize);
		Bounds.fixedWidth = (double)columns * (GuiElementItemSlotGridBase.unscaledSlotPadding + GuiElementPassiveItemSlot.unscaledSlotSize);
	}

	public override void ComposeElements(Context ctx, ImageSurface surface)
	{
		ComposeSlots(ctx, surface);
		ComposeHover();
	}

	private void ComposeSlots(Context ctx, ImageSurface surface)
	{
		Bounds.CalcWorldBounds();
		double num = GuiElement.scaled(GuiElementItemSlotGridBase.unscaledSlotPadding);
		double num2 = GuiElement.scaled(GuiElementPassiveItemSlot.unscaledSlotSize);
		double num3 = GuiElement.scaled(GuiElementPassiveItemSlot.unscaledSlotSize);
		for (int i = 0; i < rows; i++)
		{
			for (int j = 0; j < cols; j++)
			{
				double num4 = (double)j * (num2 + num);
				double num5 = (double)i * (num3 + num);
				ctx.SetSourceRGBA(1.0, 1.0, 1.0, 0.2);
				GuiElement.RoundRectangle(ctx, Bounds.drawX + num4, Bounds.drawY + num5, num2, num3, GuiStyle.ElementBGRadius);
				ctx.Fill();
				EmbossRoundRectangleElement(ctx, Bounds.drawX + num4, Bounds.drawY + num5, num2, num3, inverse: true);
			}
		}
	}

	private void ComposeHover()
	{
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0025: Expected O, but got Unknown
		double num = GuiElement.scaled(GuiElementPassiveItemSlot.unscaledSlotSize);
		double num2 = GuiElement.scaled(GuiElementPassiveItemSlot.unscaledSlotSize);
		ImageSurface val = new ImageSurface((Format)0, (int)num - 2, (int)num2 - 2);
		Context obj = genContext(val);
		obj.SetSourceRGBA(1.0, 1.0, 1.0, 0.7);
		GuiElement.RoundRectangle(obj, 1.0, 1.0, num, num2, GuiStyle.ElementBGRadius);
		obj.Fill();
		generateTexture(val, ref hoverTexture);
		obj.Dispose();
		((Surface)val).Dispose();
	}

	public override void RenderInteractiveElements(float deltaTime)
	{
		double num = GuiElement.scaled(GuiElementItemSlotGridBase.unscaledSlotPadding);
		double num2 = GuiElement.scaled(GuiElementPassiveItemSlot.unscaledSlotSize);
		double num3 = GuiElement.scaled(GuiElementPassiveItemSlot.unscaledSlotSize);
		int num4 = api.Input.MouseX - (int)Bounds.absX;
		int num5 = api.Input.MouseY - (int)Bounds.absY;
		for (int i = 0; i < rows * cols; i++)
		{
			int num6 = i / cols;
			double num7 = (double)(i % cols) * (num2 + num);
			double num8 = (double)num6 * (num3 + num);
			bool flag = (double)num4 >= num7 && (double)num5 >= num8 && (double)num4 < num7 + num2 + num && (double)num5 < num8 + num3 + num;
			if (flag || i == selectedIndex)
			{
				api.Render.Render2DTexture(hoverTexture.TextureId, (float)(Bounds.renderX + num7), (float)(Bounds.renderY + num8), (float)num2, (float)num3);
				if (flag)
				{
					OnSlotOver?.Invoke(i);
				}
			}
			if (skillItems.Count <= i)
			{
				continue;
			}
			SkillItem skillItem = skillItems[i];
			if (skillItem == null)
			{
				continue;
			}
			if (skillItem.Texture != null)
			{
				if (skillItem.TexturePremultipliedAlpha)
				{
					api.Render.Render2DTexturePremultipliedAlpha(skillItem.Texture.TextureId, Bounds.renderX + num7 + 1.0, Bounds.renderY + num8 + 1.0, num2, num3);
				}
				else
				{
					api.Render.Render2DTexture(skillItem.Texture.TextureId, (float)(Bounds.renderX + num7 + 1.0), (float)(Bounds.renderY + num8 + 1.0), (float)num2, (float)num3);
				}
			}
			skillItem.RenderHandler?.Invoke(skillItem.Code, deltaTime, Bounds.renderX + num7 + 1.0, Bounds.renderY + num8 + 1.0);
		}
	}

	public override void OnMouseDownOnElement(ICoreClientAPI api, MouseEvent args)
	{
		base.OnMouseDownOnElement(api, args);
		int num = api.Input.MouseX - (int)Bounds.absX;
		int num2 = api.Input.MouseY - (int)Bounds.absY;
		double num3 = GuiElement.scaled(GuiElementItemSlotGridBase.unscaledSlotPadding);
		double num4 = GuiElement.scaled(GuiElementPassiveItemSlot.unscaledSlotSize);
		double num5 = GuiElement.scaled(GuiElementPassiveItemSlot.unscaledSlotSize);
		int num6 = (int)((double)num2 / (num5 + num3));
		int num7 = (int)((double)num / (num4 + num3));
		int num8 = num6 * cols + num7;
		if (num8 >= 0 && num8 < skillItems.Count)
		{
			OnSlotClick?.Invoke(num8);
		}
	}

	public override void Dispose()
	{
		base.Dispose();
		hoverTexture.Dispose();
	}
}
