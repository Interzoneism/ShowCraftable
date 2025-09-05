using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.Client.NoObf;

public class HudElementBlockAndEntityInfo : HudElement
{
	private Block currentBlock;

	private int currentSelectionIndex;

	private Entity currentEntity;

	private BlockPos currentPos;

	private string title;

	private string detail;

	private GuiComposer composer;

	public override string ToggleKeyCombinationCode => "blockinfohud";

	public HudElementBlockAndEntityInfo(ICoreClientAPI capi)
		: base(capi)
	{
		capi.Event.RegisterGameTickListener(Every15ms, 15);
		capi.Event.RegisterGameTickListener(Every500ms, 500);
		capi.Event.BlockChanged += OnBlockChanged;
		ComposeBlockInfoHud();
		if (ClientSettings.ShowBlockInfoHud)
		{
			TryOpen();
		}
		ClientSettings.Inst.AddWatcher("showBlockInfoHud", delegate(bool on)
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

	private void ComposeBlockInfoHud()
	{
		string text = "";
		string text2 = "";
		if (currentBlock != null)
		{
			if (currentBlock.Code == null)
			{
				text = "Unknown block ID " + capi.World.BlockAccessor.GetBlockId(currentPos);
				text2 = "";
			}
			else
			{
				text = currentBlock.GetPlacedBlockName(capi.World, currentPos);
				text2 = currentBlock.GetPlacedBlockInfo(capi.World, currentPos, capi.World.Player);
				if (text2 == null)
				{
					text2 = "";
				}
				if (text == null)
				{
					text = "Unknown";
				}
			}
		}
		if (currentEntity != null)
		{
			text = currentEntity.GetName();
			text2 = currentEntity.GetInfoText();
			if (text2 == null)
			{
				text2 = "";
			}
			if (text == null)
			{
				text = "Unknown Entity code " + currentEntity.Code;
			}
		}
		if (!(title == text) || !(detail == text2))
		{
			title = text;
			detail = text2;
			ElementBounds elementBounds = ElementBounds.Fixed(EnumDialogArea.CenterFixed, 0.0, 0.0, 500.0, 24.0);
			ElementBounds elementBounds2 = elementBounds.BelowCopy(0.0, 10.0);
			elementBounds2.Alignment = EnumDialogArea.None;
			ElementBounds elementBounds3 = new ElementBounds();
			elementBounds3.BothSizing = ElementSizing.FitToChildren;
			elementBounds3.WithFixedPadding(5.0, 5.0);
			ElementBounds elementBounds4 = ElementStdBounds.AutosizedMainDialog.WithAlignment(EnumDialogArea.CenterTop).WithFixedAlignmentOffset(0.0, GuiStyle.DialogToScreenPadding);
			LoadedTexture loadedTexture = null;
			GuiElementRichtext richtext;
			if (composer == null)
			{
				composer = capi.Gui.CreateCompo("blockinfohud", elementBounds4);
			}
			else
			{
				richtext = composer.GetRichtext("rt");
				loadedTexture = richtext.richtTextTexture;
				richtext.richtTextTexture = null;
				composer.Clear(elementBounds4);
			}
			Composers["blockinfohud"] = composer;
			composer.AddGameOverlay(elementBounds3).BeginChildElements(elementBounds3).AddStaticTextAutoBoxSize(title, CairoFont.WhiteSmallishText(), EnumTextOrientation.Left, elementBounds)
				.AddRichtext(detail, CairoFont.WhiteDetailText(), elementBounds2, "rt")
				.EndChildElements();
			richtext = composer.GetRichtext("rt");
			if (detail.Length == 0)
			{
				elementBounds2.fixedY = 0.0;
				elementBounds2.fixedHeight = 0.0;
			}
			if (loadedTexture != null)
			{
				richtext.richtTextTexture = loadedTexture;
			}
			richtext.BeforeCalcBounds();
			elementBounds2.fixedWidth = Math.Min(500.0, richtext.MaxLineWidth / (double)RuntimeEnv.GUIScale + 1.0);
			composer.Compose();
		}
	}

	private void Every15ms(float dt)
	{
		if (!IsOpened())
		{
			return;
		}
		if (capi.World.Player.CurrentEntitySelection == null)
		{
			currentEntity = null;
			if (capi.World.Player.CurrentBlockSelection == null)
			{
				currentBlock = null;
				return;
			}
			currentEntity = null;
			BlockInView();
		}
		else
		{
			currentBlock = null;
			EntityInView();
		}
	}

	private void BlockInView()
	{
		BlockSelection currentBlockSelection = capi.World.Player.CurrentBlockSelection;
		Block block;
		if (currentBlockSelection.DidOffset)
		{
			BlockFacing opposite = currentBlockSelection.Face.Opposite;
			block = capi.World.BlockAccessor.GetBlockOnSide(currentBlockSelection.Position, opposite);
		}
		else
		{
			block = capi.World.BlockAccessor.GetBlock(currentBlockSelection.Position);
		}
		if (block.BlockId == 0)
		{
			currentBlock = null;
		}
		else if (block != currentBlock || !currentPos.Equals(currentBlockSelection.Position) || currentSelectionIndex != currentBlockSelection.SelectionBoxIndex)
		{
			currentBlock = block;
			currentSelectionIndex = currentBlockSelection.SelectionBoxIndex;
			currentPos = (currentBlockSelection.DidOffset ? currentBlockSelection.Position.Copy().Add(currentBlockSelection.Face.Opposite) : currentBlockSelection.Position.Copy());
			ComposeBlockInfoHud();
		}
	}

	private void EntityInView()
	{
		Entity entity = capi.World.Player.CurrentEntitySelection.Entity;
		if (entity != currentEntity)
		{
			currentEntity = entity;
			ComposeBlockInfoHud();
		}
	}

	public override bool ShouldReceiveRenderEvents()
	{
		if (currentBlock == null)
		{
			return currentEntity != null;
		}
		return true;
	}

	private void OnBlockChanged(BlockPos pos, Block oldBlock)
	{
		IPlayer player = capi.World.Player;
		if (player?.CurrentBlockSelection != null && pos.Equals(player.CurrentBlockSelection.Position))
		{
			ComposeBlockInfoHud();
		}
	}

	private void Every500ms(float dt)
	{
		Every15ms(dt);
		ComposeBlockInfoHud();
	}

	public override void OnGuiOpened()
	{
		base.OnGuiOpened();
		ClientSettings.ShowBlockInfoHud = true;
	}

	public override void OnGuiClosed()
	{
		base.OnGuiClosed();
		ClientSettings.ShowBlockInfoHud = false;
	}

	public override void Dispose()
	{
		base.Dispose();
		composer?.Dispose();
	}
}
