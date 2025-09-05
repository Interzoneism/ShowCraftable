using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.GameContent;
using Vintagestory.ServerMods.WorldEdit;

namespace Vintagestory.ServerMods;

public class UpgradeTasks : ModSystem
{
	private ICoreAPI api;

	private ICoreClientAPI capi;

	public override bool ShouldLoad(EnumAppSide forSide)
	{
		return true;
	}

	public override void Start(ICoreAPI api)
	{
		api.Network.RegisterChannel("upgradeTasks").RegisterMessageType<UpgradeHerePacket>();
	}

	public override void StartClientSide(ICoreClientAPI api)
	{
		this.api = api;
		capi = api;
		api.Input.InWorldAction += Input_InWorldAction;
	}

	private void Input_InWorldAction(EnumEntityAction action, bool on, ref EnumHandling handled)
	{
		if (action != EnumEntityAction.RightMouseDown)
		{
			return;
		}
		BlockSelection currentBlockSelection = capi.World.Player.CurrentBlockSelection;
		if (currentBlockSelection == null)
		{
			return;
		}
		Block block = api.World.BlockAccessor.GetBlock(currentBlockSelection.Position);
		if (!(block.Code == null) && (block.Variant["color"] == null || block.Variant["state"] == null))
		{
			string[] array = block.Code.Path.Split(new char[1] { '-' }, 3);
			if ((array[0] == "clayplanter" || array[0] == "flowerpot") && array.Length >= 3)
			{
				capi.Network.GetChannel("upgradeTasks").SendPacket(new UpgradeHerePacket
				{
					Pos = currentBlockSelection.Position
				});
			}
		}
	}

	public override void StartServerSide(ICoreServerAPI api)
	{
		this.api = api;
		api.Network.GetChannel("upgradeTasks").SetMessageHandler<UpgradeHerePacket>(didUseBlock);
		api.Event.DidBreakBlock += Event_DidBreakBlock;
		api.ChatCommands.GetOrCreate("we").BeginSubCommand("chisel").WithDescription("chisel")
			.BeginSubCommand("upgradearea")
			.WithDescription("Fixes chiseled blocks, pots and planters broken in v1.13")
			.HandleWith(OnUpgradeCmd)
			.EndSubCommand()
			.BeginSubCommand("setchiselblockmat")
			.WithDescription("Sets the material of a currently looked at chisel block to the material in the active hands")
			.HandleWith(OnSetChiselMat)
			.EndSubCommand()
			.EndSubCommand();
	}

	private TextCommandResult OnSetChiselMat(TextCommandCallingArgs textCommandCallingArgs)
	{
		IPlayer player = textCommandCallingArgs.Caller.Player;
		BlockPos blockPos = player.CurrentBlockSelection?.Position;
		if (blockPos == null)
		{
			return TextCommandResult.Success("Look at a block first");
		}
		if (!(api.World.BlockAccessor.GetBlockEntity(blockPos) is BlockEntityChisel blockEntityChisel))
		{
			return TextCommandResult.Success("Not looking at a chiseled block");
		}
		Block block = player.InventoryManager?.ActiveHotbarSlot?.Itemstack?.Block;
		if (block == null)
		{
			return TextCommandResult.Success("You need a block in your active hand");
		}
		for (int i = 0; i < blockEntityChisel.BlockIds.Length; i++)
		{
			blockEntityChisel.BlockIds[i] = block.Id;
		}
		blockEntityChisel.MarkDirty(redrawOnClient: true);
		return TextCommandResult.Success("Ok material set");
	}

	private TextCommandResult OnUpgradeCmd(TextCommandCallingArgs textCommandCallingArgs)
	{
		WorldEditWorkspace workSpace = api.ModLoader.GetModSystem<WorldEdit>(withInheritance: true).GetWorkSpace(textCommandCallingArgs.Caller.Player.PlayerUID);
		if (workSpace == null || workSpace.StartMarker == null || workSpace.EndMarker == null)
		{
			return TextCommandResult.Success("Select an area with worldedit first");
		}
		int num = Math.Min(workSpace.StartMarker.X, workSpace.EndMarker.X);
		int num2 = Math.Max(workSpace.StartMarker.X, workSpace.EndMarker.X);
		int num3 = Math.Min(workSpace.StartMarker.Y, workSpace.EndMarker.Y);
		int num4 = Math.Max(workSpace.StartMarker.Y, workSpace.EndMarker.Y);
		int num5 = Math.Min(workSpace.StartMarker.Z, workSpace.EndMarker.Z);
		int num6 = Math.Max(workSpace.StartMarker.Z, workSpace.EndMarker.Z);
		BlockPos blockPos = new BlockPos(textCommandCallingArgs.Caller.Player.Entity.Pos.Dimension);
		Dictionary<string, Block> dictionary = new Dictionary<string, Block>();
		foreach (Block block2 in api.World.Blocks)
		{
			if (!block2.IsMissing)
			{
				dictionary[block2.GetHeldItemName(new ItemStack(block2))] = block2;
			}
		}
		int id = api.World.GetBlock(new AssetLocation("rock-granite")).Id;
		for (int i = num; i < num2; i++)
		{
			for (int j = num3; j < num4; j++)
			{
				for (int k = num5; k < num6; k++)
				{
					blockPos.Set(i, j, k);
					Block block = api.World.BlockAccessor.GetBlock(blockPos);
					if (block is BlockChisel)
					{
						BlockEntityChisel blockEntityChisel = api.World.BlockAccessor.GetBlockEntity(blockPos) as BlockEntityChisel;
						if (blockEntityChisel?.BlockIds != null && blockEntityChisel.BlockIds.Length != 0 && blockEntityChisel.BlockIds[0] == id && dictionary.TryGetValue(blockEntityChisel.BlockName, out var value))
						{
							blockEntityChisel.BlockIds[0] = value.Id;
							blockEntityChisel.MarkDirty(redrawOnClient: true);
						}
					}
					if (block is BlockPlantContainer)
					{
						FixOldPlantContainers(blockPos);
					}
				}
			}
		}
		return TextCommandResult.Success();
	}

	private void didUseBlock(IServerPlayer fromPlayer, UpgradeHerePacket networkMessage)
	{
		FixOldPlantContainers(networkMessage.Pos);
	}

	private void Event_DidBreakBlock(IServerPlayer byPlayer, int oldblockId, BlockSelection blockSel)
	{
		DropPlantContainer(oldblockId, blockSel.Position);
	}

	private void FixOldPlantContainers(BlockPos pos)
	{
		Block block = api.World.BlockAccessor.GetBlock(pos);
		if (block.Variant["color"] != null && block.Variant["state"] != null)
		{
			return;
		}
		string[] array = block.Code.Path.Split(new char[1] { '-' }, 3);
		if ((!(array[0] == "clayplanter") && !(array[0] == "flowerpot")) || array.Length < 3)
		{
			return;
		}
		Block block2 = api.World.GetBlock(new AssetLocation(array[0] + "-" + array[1]));
		if (block2 == null)
		{
			block2 = api.World.GetBlock(new AssetLocation(array[0] + "-blue-fired"));
		}
		if (block2 == null)
		{
			return;
		}
		api.World.BlockAccessor.SetBlock(block2.Id, pos);
		if (api.World.BlockAccessor.GetBlockEntity(pos) is BlockEntityPlantContainer blockEntityPlantContainer)
		{
			Block block3 = null;
			if (block3 == null)
			{
				block3 = api.World.GetBlock(new AssetLocation("flower-" + array[2] + "-free"));
			}
			if (block3 == null)
			{
				block3 = api.World.GetBlock(new AssetLocation("sapling-" + array[2] + "-free"));
			}
			if (block3 == null)
			{
				block3 = api.World.GetBlock(new AssetLocation("mushroom-" + array[2] + "-normal-free"));
			}
			if (block3 != null)
			{
				ItemStack stack = new ItemStack(block3);
				blockEntityPlantContainer.TrySetContents(stack);
			}
		}
	}

	private void DropPlantContainer(int blockid, BlockPos pos)
	{
		Block block = api.World.GetBlock(blockid);
		if (block.Code == null || (block.Variant["color"] != null && block.Variant["state"] != null))
		{
			return;
		}
		string[] array = block.Code.Path.Split(new char[1] { '-' }, 3);
		if (array.Length < 3 || (!(array[0] == "clayplanter") && !(array[1] == "flowerpot")) || array.Length < 3)
		{
			return;
		}
		Block block2 = api.World.GetBlock(new AssetLocation(array[0] + "-" + array[1]));
		if (block2 == null)
		{
			block2 = api.World.GetBlock(new AssetLocation(array[0] + "-blue-fired"));
		}
		if (block2 != null)
		{
			ItemStack itemstack = new ItemStack(block2);
			api.World.SpawnItemEntity(itemstack, pos);
			Block block3 = null;
			if (block3 == null)
			{
				block3 = api.World.GetBlock(new AssetLocation("flower-" + array[2]));
			}
			if (block3 == null)
			{
				block3 = api.World.GetBlock(new AssetLocation("sapling-" + array[2]));
			}
			if (block3 == null)
			{
				block3 = api.World.GetBlock(new AssetLocation("mushroom-" + array[2] + "-normal"));
			}
			if (block3 != null)
			{
				ItemStack itemstack2 = new ItemStack(block3);
				api.World.SpawnItemEntity(itemstack2, pos);
			}
		}
	}
}
