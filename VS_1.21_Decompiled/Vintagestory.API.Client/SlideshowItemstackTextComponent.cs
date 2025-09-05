using System;
using System.Collections.Generic;
using Cairo;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vintagestory.API.Client;

public class SlideshowItemstackTextComponent : ItemstackComponentBase
{
	public bool ShowTooltip = true;

	public ItemStack[] Itemstacks;

	protected ItemSlot slot;

	protected Action<ItemStack> onStackClicked;

	protected float secondsVisible = 1f;

	protected int curItemIndex;

	public string ExtraTooltipText;

	public Vec3f renderOffset = new Vec3f();

	public float renderSize = 0.58f;

	private double unscaledSize;

	public StackDisplayDelegate overrideCurrentItemStack;

	public bool ShowStackSize { get; set; }

	public bool Background { get; set; }

	public SlideshowItemstackTextComponent(ICoreClientAPI capi, ItemStack[] itemstacks, double unscaledSize, EnumFloat floatType, Action<ItemStack> onStackClicked = null)
		: base(capi)
	{
		initSlot();
		this.unscaledSize = unscaledSize;
		Itemstacks = itemstacks;
		Float = floatType;
		BoundsPerLine = new LineRectangled[1]
		{
			new LineRectangled(0.0, 0.0, GuiElement.scaled(unscaledSize), GuiElement.scaled(unscaledSize))
		};
		this.onStackClicked = onStackClicked;
	}

	public SlideshowItemstackTextComponent(ICoreClientAPI capi, ItemStack itemstackgroup, List<ItemStack> allstacks, double unscaleSize, EnumFloat floatType, Action<ItemStack> onStackClicked = null)
		: base(capi)
	{
		initSlot();
		this.onStackClicked = onStackClicked;
		unscaledSize = unscaleSize;
		string[] array = itemstackgroup.Collectible.Attributes?["handbook"]?["groupBy"]?.AsArray<string>();
		List<ItemStack> list = new List<ItemStack>();
		List<ItemStack> list2 = new List<ItemStack>();
		list.Add(itemstackgroup);
		list2.Add(itemstackgroup);
		if (allstacks != null)
		{
			if (array != null)
			{
				AssetLocation[] array2 = new AssetLocation[array.Length];
				for (int i = 0; i < array.Length; i++)
				{
					if (!array[i].Contains(":"))
					{
						array2[i] = new AssetLocation(itemstackgroup.Collectible.Code.Domain, array[i]);
					}
					else
					{
						array2[i] = new AssetLocation(array[i]);
					}
				}
				foreach (ItemStack allstack in allstacks)
				{
					JsonObject attributes = allstack.Collectible.Attributes;
					if (attributes != null && attributes["handbook"]?["isDuplicate"].AsBool() == true)
					{
						list.Add(allstack);
						continue;
					}
					for (int j = 0; j < array2.Length; j++)
					{
						if (allstack.Collectible.WildCardMatch(array2[j]))
						{
							list2.Add(allstack);
							list.Add(allstack);
							break;
						}
					}
				}
			}
			foreach (ItemStack item in list)
			{
				allstacks.Remove(item);
			}
		}
		Itemstacks = list2.ToArray();
		Float = floatType;
		BoundsPerLine = new LineRectangled[1]
		{
			new LineRectangled(0.0, 0.0, GuiElement.scaled(unscaleSize), GuiElement.scaled(unscaleSize))
		};
	}

	private void initSlot()
	{
		dummyInv = new DummyInventory(capi);
		dummyInv.OnAcquireTransitionSpeed += (EnumTransitionType transType, ItemStack stack, float mul) => 0f;
		slot = new DummySlot(null, dummyInv);
	}

	public override EnumCalcBoundsResult CalcBounds(TextFlowPath[] flowPath, double currentLineHeight, double offsetX, double lineY, out double nextOffsetX)
	{
		TextFlowPath currentFlowPathSection = GetCurrentFlowPathSection(flowPath, lineY);
		offsetX += GuiElement.scaled(PaddingLeft);
		bool flag = offsetX + BoundsPerLine[0].Width > currentFlowPathSection.X2;
		BoundsPerLine[0].X = (flag ? 0.0 : offsetX);
		BoundsPerLine[0].Y = lineY + (flag ? currentLineHeight : 0.0);
		BoundsPerLine[0].Width = GuiElement.scaled(unscaledSize) + GuiElement.scaled(PaddingRight);
		nextOffsetX = (flag ? 0.0 : offsetX) + BoundsPerLine[0].Width;
		if (!flag)
		{
			return EnumCalcBoundsResult.Continue;
		}
		return EnumCalcBoundsResult.Nextline;
	}

	public override void ComposeElements(Context ctx, ImageSurface surface)
	{
		if (Background)
		{
			ctx.SetSourceRGBA(1.0, 1.0, 1.0, 0.2);
			ctx.Rectangle(BoundsPerLine[0].X, BoundsPerLine[0].Y, BoundsPerLine[0].Width, BoundsPerLine[0].Height);
			ctx.Fill();
		}
	}

	protected override string OnRequireInfoText(ItemSlot slot)
	{
		return base.OnRequireInfoText(slot) + ExtraTooltipText;
	}

	public override void RenderInteractiveElements(float deltaTime, double renderX, double renderY, double renderZ)
	{
		int num = (int)((double)api.Input.MouseX - renderX + (double)renderOffset.X);
		int num2 = (int)((double)api.Input.MouseY - renderY + (double)renderOffset.Y);
		LineRectangled lineRectangled = BoundsPerLine[0];
		bool num3 = lineRectangled.PointInside(num, num2);
		ItemStack itemstack = Itemstacks[curItemIndex];
		if (!num3 && (secondsVisible -= deltaTime) <= 0f)
		{
			secondsVisible = 1f;
			curItemIndex = (curItemIndex + 1) % Itemstacks.Length;
		}
		if (overrideCurrentItemStack != null)
		{
			itemstack = overrideCurrentItemStack();
		}
		slot.Itemstack = itemstack;
		ElementBounds elementBounds = ElementBounds.FixedSize((int)(lineRectangled.Width / (double)RuntimeEnv.GUIScale), (int)(lineRectangled.Height / (double)RuntimeEnv.GUIScale));
		elementBounds.ParentBounds = capi.Gui.WindowBounds;
		elementBounds.CalcWorldBounds();
		elementBounds.absFixedX = renderX + lineRectangled.X + (double)renderOffset.X;
		elementBounds.absFixedY = renderY + lineRectangled.Y + (double)renderOffset.Y;
		elementBounds.absInnerWidth *= renderSize / 0.58f;
		elementBounds.absInnerHeight *= renderSize / 0.58f;
		api.Render.PushScissor(elementBounds, stacking: true);
		api.Render.RenderItemstackToGui(slot, renderX + lineRectangled.X + lineRectangled.Width * 0.5 + (double)renderOffset.X + offX, renderY + lineRectangled.Y + lineRectangled.Height * 0.5 + (double)renderOffset.Y + offY, 100f + renderOffset.Z, (float)lineRectangled.Width * renderSize, -1, shading: true, rotate: false, ShowStackSize);
		api.Render.PopScissor();
		if (num3 && ShowTooltip)
		{
			RenderItemstackTooltip(slot, renderX + (double)num, renderY + (double)num2, deltaTime);
		}
	}

	public override void OnMouseDown(MouseEvent args)
	{
		if (slot.Itemstack == null)
		{
			return;
		}
		LineRectangled[] boundsPerLine = BoundsPerLine;
		for (int i = 0; i < boundsPerLine.Length; i++)
		{
			if (boundsPerLine[i].PointInside(args.X, args.Y))
			{
				onStackClicked?.Invoke(slot.Itemstack);
			}
		}
	}
}
