using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class GuiDialogBlockEntityRecipeSelector : GuiDialogGeneric
{
	private BlockPos blockEntityPos;

	private int prevSlotOver = -1;

	private List<SkillItem> skillItems;

	private bool didSelect;

	private Action<int> onSelectedRecipe;

	private Action onCancelSelect;

	private readonly double floatyDialogPosition = 0.5;

	private readonly double floatyDialogAlign = 0.75;

	public override bool PrefersUngrabbedMouse => false;

	public GuiDialogBlockEntityRecipeSelector(string DialogTitle, ItemStack[] recipeOutputs, Action<int> onSelectedRecipe, Action onCancelSelect, BlockPos blockEntityPos, ICoreClientAPI capi)
		: base(DialogTitle, capi)
	{
		this.blockEntityPos = blockEntityPos;
		this.onSelectedRecipe = onSelectedRecipe;
		this.onCancelSelect = onCancelSelect;
		skillItems = new List<SkillItem>();
		double size = GuiElementPassiveItemSlot.unscaledSlotSize + GuiElementItemSlotGridBase.unscaledSlotPadding;
		foreach (ItemStack itemStack in recipeOutputs)
		{
			ItemSlot dummySlot = new DummySlot(itemStack);
			string craftDescKey = GetCraftDescKey(itemStack);
			string text = Lang.GetMatching(craftDescKey);
			if (text == craftDescKey)
			{
				text = "";
			}
			skillItems.Add(new SkillItem
			{
				Code = itemStack.Collectible.Code.Clone(),
				Name = itemStack.GetName(),
				Description = text,
				Data = null,
				RenderHandler = delegate(AssetLocation code, float dt, double posX, double posY)
				{
					double num = GuiElement.scaled(size - 5.0);
					capi.Render.RenderItemstackToGui(dummySlot, posX + num / 2.0, posY + num / 2.0, 100.0, (float)GuiElement.scaled(GuiElementPassiveItemSlot.unscaledItemSize), -1);
				}
			});
		}
		SetupDialog();
	}

	public string GetCraftDescKey(ItemStack stack)
	{
		string text = stack.Class.Name();
		return stack.Collectible.Code?.Domain + ":" + text + "craftdesc-" + stack.Collectible.Code?.Path;
	}

	private void SetupDialog()
	{
		int num = Math.Max(1, skillItems.Count);
		int num2 = Math.Min(num, 7);
		int num3 = (int)Math.Ceiling((float)num / (float)num2);
		double num4 = GuiElementPassiveItemSlot.unscaledSlotSize + GuiElementItemSlotGridBase.unscaledSlotPadding;
		double fixedWidth = Math.Max(300.0, (double)num2 * num4);
		ElementBounds bounds = ElementBounds.Fixed(0.0, 30.0, fixedWidth, (double)num3 * num4);
		ElementBounds elementBounds = ElementBounds.Fixed(0.0, (double)num3 * num4 + 50.0, fixedWidth, 33.0);
		ElementBounds elementBounds2 = elementBounds.BelowCopy(0.0, 10.0);
		ElementBounds elementBounds3 = ElementBounds.Fill.WithFixedPadding(GuiStyle.ElementToDialogPadding);
		elementBounds3.BothSizing = ElementSizing.FitToChildren;
		base.SingleComposer = capi.Gui.CreateCompo("toolmodeselect" + blockEntityPos, ElementStdBounds.AutosizedMainDialog).AddShadedDialogBG(elementBounds3).AddDialogTitleBar(Lang.Get("Select Recipe"), OnTitleBarClose)
			.BeginChildElements(elementBounds3)
			.AddSkillItemGrid(skillItems, num2, num3, OnSlotClick, bounds, "skillitemgrid")
			.AddDynamicText("", CairoFont.WhiteSmallishText(), elementBounds, "name")
			.AddDynamicText("", CairoFont.WhiteDetailText(), elementBounds2, "desc")
			.AddDynamicText("", CairoFont.WhiteDetailText(), elementBounds2.BelowCopy(0.0, 20.0), "ingredient")
			.EndChildElements()
			.Compose();
		base.SingleComposer.GetSkillItemGrid("skillitemgrid").OnSlotOver = OnSlotOver;
	}

	public void SetIngredientCounts(int num, ItemStack[] ingredStacks)
	{
		skillItems[num].Data = ingredStacks;
	}

	private void OnSlotOver(int num)
	{
		if (num < skillItems.Count && num != prevSlotOver)
		{
			prevSlotOver = num;
			base.SingleComposer.GetDynamicText("name").SetNewText(skillItems[num].Name);
			base.SingleComposer.GetDynamicText("desc").SetNewText(skillItems[num].Description);
			string text = "";
			if (skillItems[num].Data is ItemStack[] array)
			{
				text = Lang.Get("recipeselector-requiredcount", array[0].StackSize, array[0].GetName().ToLower());
			}
			base.SingleComposer.GetDynamicText("ingredient").SetNewText(text);
		}
	}

	private void OnSlotClick(int num)
	{
		onSelectedRecipe(num);
		didSelect = true;
		TryClose();
	}

	public override void OnGuiClosed()
	{
		base.OnGuiClosed();
		if (!didSelect)
		{
			onCancelSelect();
		}
	}

	public override bool TryClose()
	{
		return base.TryClose();
	}

	public override bool TryOpen()
	{
		return base.TryOpen();
	}

	private void SendInvPacket(object packet)
	{
	}

	private void OnTitleBarClose()
	{
		TryClose();
	}

	public override void OnRenderGUI(float deltaTime)
	{
		if (capi.Settings.Bool["immersiveMouseMode"])
		{
			Vec3d vec3d = MatrixToolsd.Project(new Vec3d((double)blockEntityPos.X + 0.5, (double)blockEntityPos.Y + floatyDialogPosition, (double)blockEntityPos.Z + 0.5), capi.Render.PerspectiveProjectionMat, capi.Render.PerspectiveViewMat, capi.Render.FrameWidth, capi.Render.FrameHeight);
			if (vec3d.Z < 0.0)
			{
				return;
			}
			base.SingleComposer.Bounds.Alignment = EnumDialogArea.None;
			base.SingleComposer.Bounds.fixedOffsetX = 0.0;
			base.SingleComposer.Bounds.fixedOffsetY = 0.0;
			base.SingleComposer.Bounds.absFixedX = vec3d.X - base.SingleComposer.Bounds.OuterWidth / 2.0;
			base.SingleComposer.Bounds.absFixedY = (double)capi.Render.FrameHeight - vec3d.Y - base.SingleComposer.Bounds.OuterHeight * floatyDialogAlign;
			base.SingleComposer.Bounds.absMarginX = 0.0;
			base.SingleComposer.Bounds.absMarginY = 0.0;
		}
		base.OnRenderGUI(deltaTime);
	}
}
