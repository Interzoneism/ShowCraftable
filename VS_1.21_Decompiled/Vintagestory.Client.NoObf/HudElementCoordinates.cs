using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;

namespace Vintagestory.Client.NoObf;

public class HudElementCoordinates : HudElement
{
	public override string ToggleKeyCombinationCode => "coordinateshud";

	public HudElementCoordinates(ICoreClientAPI capi)
		: base(capi)
	{
	}

	public override void OnOwnPlayerDataReceived()
	{
		ElementBounds elementBounds = ElementBounds.Fixed(EnumDialogArea.None, 0.0, 0.0, 190.0, 48.0);
		ElementBounds bounds = elementBounds.ForkBoundingParent(5.0, 5.0, 5.0, 5.0);
		ElementBounds bounds2 = ElementStdBounds.AutosizedMainDialog.WithAlignment(EnumDialogArea.RightTop).WithFixedAlignmentOffset(0.0 - GuiStyle.DialogToScreenPadding, GuiStyle.DialogToScreenPadding);
		base.SingleComposer = capi.Gui.CreateCompo("coordinateshud", bounds2).AddGameOverlay(bounds).AddDynamicText("", CairoFont.WhiteSmallishText().WithOrientation(EnumTextOrientation.Center), elementBounds, "text")
			.Compose();
		if (ClientSettings.ShowCoordinateHud)
		{
			TryOpen();
		}
	}

	public override void OnBlockTexturesLoaded()
	{
		base.OnBlockTexturesLoaded();
		if (!capi.World.Config.GetBool("allowCoordinateHud", defaultValue: true))
		{
			(capi.World as ClientMain).EnqueueMainThreadTask(delegate
			{
				(capi.World as ClientMain).UnregisterDialog(this);
				capi.Input.SetHotKeyHandler("coordinateshud", null);
				Dispose();
			}, "unreg");
			return;
		}
		capi.Event.RegisterGameTickListener(Every250ms, 250);
		ClientSettings.Inst.AddWatcher("showCoordinateHud", delegate(bool on)
		{
			if (on)
			{
				TryOpen();
			}
			else
			{
				TryClose();
			}
		});
	}

	private void Every250ms(float dt)
	{
		if (!IsOpened())
		{
			return;
		}
		BlockPos asBlockPos = capi.World.Player.Entity.Pos.AsBlockPos;
		int y = asBlockPos.Y;
		asBlockPos.Sub(capi.World.DefaultSpawnPosition.AsBlockPos);
		string text = BlockFacing.HorizontalFromYaw(capi.World.Player.Entity.Pos.Yaw).ToString();
		text = Lang.Get("facing-" + text);
		string text2 = asBlockPos.X + ", " + y + ", " + asBlockPos.Z + "\n" + text;
		if (ClientSettings.ExtendedDebugInfo)
		{
			text2 += text switch
			{
				"North" => " / Z-", 
				"East" => " / X+", 
				"South" => " / Z+", 
				"West" => " / X-", 
				_ => string.Empty, 
			};
		}
		base.SingleComposer.GetDynamicText("text").SetNewText(text2);
		List<ElementBounds> dialogBoundsInArea = capi.Gui.GetDialogBoundsInArea(EnumDialogArea.RightTop);
		base.SingleComposer.Bounds.absOffsetY = GuiStyle.DialogToScreenPadding;
		for (int i = 0; i < dialogBoundsInArea.Count; i++)
		{
			if (dialogBoundsInArea[i] != base.SingleComposer.Bounds)
			{
				ElementBounds elementBounds = dialogBoundsInArea[i];
				base.SingleComposer.Bounds.absOffsetY = GuiStyle.DialogToScreenPadding + elementBounds.absY + elementBounds.OuterHeight;
				break;
			}
		}
	}

	public override void OnGuiOpened()
	{
		base.OnGuiOpened();
		ClientSettings.ShowCoordinateHud = true;
	}

	public override void OnGuiClosed()
	{
		base.OnGuiClosed();
		ClientSettings.ShowCoordinateHud = false;
	}

	public override void OnRenderGUI(float deltaTime)
	{
		base.OnRenderGUI(deltaTime);
	}
}
