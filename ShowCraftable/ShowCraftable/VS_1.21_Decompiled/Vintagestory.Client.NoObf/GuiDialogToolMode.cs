using System;
using System.Collections.Generic;
using Cairo;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.Common;

namespace Vintagestory.Client.NoObf;

public class GuiDialogToolMode : GuiDialog
{
	private List<List<SkillItem>> multilineItems;

	private BlockSelection blockSele;

	private bool keepOpen;

	private int prevSlotOver = -1;

	private readonly double floatyDialogPosition = 0.5;

	private readonly double floatyDialogAlign = 0.75;

	public override string ToggleKeyCombinationCode => "toolmodeselect";

	public override bool PrefersUngrabbedMouse => false;

	public GuiDialogToolMode(ICoreClientAPI capi)
		: base(capi)
	{
		capi.Event.RegisterEventBusListener(OnEventBusEvent, 0.5, "keepopentoolmodedlg");
	}

	private void OnEventBusEvent(string eventName, ref EnumHandling handling, IAttribute data)
	{
		keepOpen = true;
	}

	internal override bool OnKeyCombinationToggle(KeyCombination viaKeyComb)
	{
		ItemSlot itemSlot = capi.World.Player?.InventoryManager?.ActiveHotbarSlot;
		if (itemSlot?.Itemstack?.Collectible.GetToolModes(itemSlot, capi.World.Player, capi.World.Player.CurrentBlockSelection) == null)
		{
			return false;
		}
		blockSele = capi.World.Player.CurrentBlockSelection?.Clone();
		return base.OnKeyCombinationToggle(viaKeyComb);
	}

	public override void OnGuiOpened()
	{
		ComposeDialog();
	}

	private void ComposeDialog()
	{
		//IL_0141: Unknown result type (might be due to invalid IL or missing references)
		//IL_0146: Unknown result type (might be due to invalid IL or missing references)
		prevSlotOver = -1;
		ClearComposers();
		ItemSlot activeHotbarSlot = capi.World.Player.InventoryManager.ActiveHotbarSlot;
		SkillItem[] toolModes = activeHotbarSlot.Itemstack.Collectible.GetToolModes(activeHotbarSlot, capi.World.Player, blockSele);
		if (toolModes == null)
		{
			return;
		}
		multilineItems = new List<List<SkillItem>>();
		multilineItems.Add(new List<SkillItem>());
		int num = 1;
		for (int i = 0; i < toolModes.Length; i++)
		{
			List<SkillItem> list = multilineItems[multilineItems.Count - 1];
			if (toolModes[i].Linebreak)
			{
				multilineItems.Add(list = new List<SkillItem>());
			}
			list.Add(toolModes[i]);
		}
		foreach (List<SkillItem> multilineItem in multilineItems)
		{
			num = Math.Max(num, multilineItem.Count);
		}
		double num2 = GuiElementPassiveItemSlot.unscaledSlotSize + GuiElementItemSlotGridBase.unscaledSlotPadding;
		double num3 = (double)num * num2;
		int count = multilineItems.Count;
		SkillItem[] array = toolModes;
		foreach (SkillItem skillItem in array)
		{
			double val = num3;
			TextExtents textExtents = CairoFont.WhiteSmallishText().GetTextExtents(skillItem.Name);
			num3 = Math.Max(val, ((TextExtents)(ref textExtents)).Width / (double)RuntimeEnv.GUIScale + 1.0);
		}
		ElementBounds elementBounds = ElementBounds.Fixed(0.0, 0.0, num3, (double)count * num2);
		ElementBounds bounds = ElementBounds.Fixed(0.0, (double)count * (num2 + 2.0) + 5.0, num3, 25.0);
		base.SingleComposer = capi.Gui.CreateCompo("toolmodeselect", ElementStdBounds.AutosizedMainDialog).AddShadedDialogBG(ElementStdBounds.DialogBackground().WithFixedPadding(GuiStyle.ElementToDialogPadding / 2.0), withTitleBar: false).BeginChildElements();
		int num4 = 0;
		for (int k = 0; k < multilineItems.Count; k++)
		{
			int line = k;
			int baseIndex = num4;
			List<SkillItem> list2 = multilineItems[line];
			base.SingleComposer.AddSkillItemGrid(list2, list2.Count, 1, delegate(int num5)
			{
				OnSlotClick(baseIndex + num5);
			}, elementBounds, "skillitemgrid-" + line);
			base.SingleComposer.GetSkillItemGrid("skillitemgrid-" + line).OnSlotOver = delegate(int num5)
			{
				OnSlotOver(line, num5);
			};
			elementBounds = elementBounds.BelowCopy(0.0, 5.0);
			num4 += list2.Count;
		}
		base.SingleComposer.AddDynamicText("", CairoFont.WhiteSmallishText(), bounds, "name").EndChildElements().Compose();
	}

	private void OnSlotOver(int line, int num)
	{
		List<SkillItem> list = multilineItems[line];
		if (num < list.Count)
		{
			prevSlotOver = num;
			base.SingleComposer.GetDynamicText("name").SetNewText(list[num].Name);
		}
	}

	private void OnSlotClick(int num)
	{
		ItemSlot activeHotbarSlot = capi.World.Player.InventoryManager.ActiveHotbarSlot;
		CollectibleObject collectibleObject = activeHotbarSlot?.Itemstack?.Collectible;
		if (collectibleObject != null)
		{
			collectibleObject.SetToolMode(activeHotbarSlot, capi.World.Player, blockSele, num);
			Packet_ToolMode packet_ToolMode = new Packet_ToolMode
			{
				Mode = num
			};
			if (blockSele != null)
			{
				packet_ToolMode.X = blockSele.Position.X;
				packet_ToolMode.Y = blockSele.Position.InternalY;
				packet_ToolMode.Z = blockSele.Position.Z;
				packet_ToolMode.SelectionBoxIndex = blockSele.SelectionBoxIndex;
				packet_ToolMode.Face = blockSele.Face.Index;
				packet_ToolMode.HitX = CollectibleNet.SerializeDouble(blockSele.HitPosition.X);
				packet_ToolMode.HitY = CollectibleNet.SerializeDouble(blockSele.HitPosition.Y);
				packet_ToolMode.HitZ = CollectibleNet.SerializeDouble(blockSele.HitPosition.Z);
			}
			capi.Network.SendPacketClient(new Packet_Client
			{
				Id = 27,
				ToolMode = packet_ToolMode
			});
			activeHotbarSlot.MarkDirty();
		}
		if (keepOpen)
		{
			keepOpen = false;
			ComposeDialog();
		}
		else
		{
			TryClose();
		}
	}

	public override void OnRenderGUI(float deltaTime)
	{
		if (capi.Settings.Bool["immersiveMouseMode"] && blockSele?.Position != null)
		{
			Vec3d vec3d = MatrixToolsd.Project(new Vec3d((double)blockSele.Position.X + 0.5, (double)blockSele.Position.Y + floatyDialogPosition, (double)blockSele.Position.Z + 0.5), capi.Render.PerspectiveProjectionMat, capi.Render.PerspectiveViewMat, capi.Render.FrameWidth, capi.Render.FrameHeight);
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
