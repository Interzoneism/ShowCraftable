using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Common.CommandAbbr;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace Vintagestory.ServerMods;

public class MicroblockCommands
{
	private ICoreServerAPI sapi;

	private Block materialBlock;

	public void Start(ICoreServerAPI api)
	{
		sapi = api;
		CommandArgumentParsers parsers = sapi.ChatCommands.Parsers;
		api.ChatCommands.GetOrCreate("we").BeginSub("microblock").WithDesc("Microblock operations")
			.BeginSub("fill")
			.WithDesc("Fill empty space of microblocks with held block")
			.HandleWith((TextCommandCallingArgs args) => onCmdFill(args, delete: false))
			.EndSub()
			.BeginSub("clearname")
			.WithDesc("Delete all block names")
			.HandleWith((TextCommandCallingArgs args) => onCmdClearName(args, v: false))
			.EndSub()
			.BeginSub("setname")
			.WithDesc("Set multiple block names")
			.WithArgs(parsers.All("name"))
			.HandleWith((TextCommandCallingArgs args) => onCmdSetName(args, v: false))
			.EndSub()
			.BeginSub("delete")
			.WithDesc("Delete a material from microblocks (select material with held block)")
			.HandleWith((TextCommandCallingArgs args) => onCmdFill(args, delete: true))
			.EndSub()
			.BeginSub("deletemat")
			.WithDesc("Delete a named material from microblocks")
			.WithArgs(parsers.Word("material code"))
			.HandleWith(onCmdDeleteMat)
			.EndSub()
			.BeginSub("removeunused")
			.WithDesc("Remove any unused materials from microblocks")
			.HandleWith(onCmdRemoveUnused)
			.EndSub()
			.BeginSubCommand("editable")
			.WithDescription("Upgrade/Downgrade chiseled blocks to an editable/non-editable state in given area")
			.WithArgs(parsers.Bool("editable"))
			.HandleWith(onCmdEditable)
			.EndSubCommand()
			.EndSub();
	}

	private int WalkMicroBlocks(BlockPos startPos, BlockPos endPos, ActionBoolReturn<BlockEntityMicroBlock> action)
	{
		int cnt = 0;
		IBlockAccessor ba = sapi.World.BlockAccessor;
		BlockPos tmpPos = new BlockPos(startPos.dimension);
		BlockPos.Walk(startPos, endPos, ba.MapSize, delegate(int x, int y, int z)
		{
			tmpPos.Set(x, y, z);
			BlockEntityMicroBlock blockEntity = ba.GetBlockEntity<BlockEntityMicroBlock>(tmpPos);
			if (blockEntity != null && action(blockEntity))
			{
				cnt++;
			}
		});
		return cnt;
	}

	private void GetMarkedArea(Caller caller, out BlockPos startPos, out BlockPos endPos)
	{
		string playerUID = caller.Player.PlayerUID;
		startPos = null;
		endPos = null;
		if (sapi.ObjectCache.TryGetValue("weStartMarker-" + playerUID, out var value))
		{
			startPos = value as BlockPos;
		}
		if (sapi.ObjectCache.TryGetValue("weEndMarker-" + playerUID, out var value2))
		{
			endPos = value2 as BlockPos;
		}
	}

	private TextCommandResult onCmdSetName(TextCommandCallingArgs args, bool v)
	{
		string name = args[0] as string;
		GetMarkedArea(args.Caller, out var startPos, out var endPos);
		if (startPos == null || endPos == null)
		{
			return TextCommandResult.Error("Please mark area with world edit");
		}
		return TextCommandResult.Success(WalkMicroBlocks(startPos, endPos, delegate(BlockEntityMicroBlock be)
		{
			be.BlockName = name;
			be.MarkDirty(redrawOnClient: true);
			return true;
		}) + " microblocks modified");
	}

	private TextCommandResult onCmdClearName(TextCommandCallingArgs args, bool v)
	{
		GetMarkedArea(args.Caller, out var startPos, out var endPos);
		if (startPos == null || endPos == null)
		{
			return TextCommandResult.Error("Please mark area with world edit");
		}
		return TextCommandResult.Success(WalkMicroBlocks(startPos, endPos, delegate(BlockEntityMicroBlock be)
		{
			if (be.BlockName != null && be.BlockName != "")
			{
				be.BlockName = null;
				be.MarkDirty(redrawOnClient: true);
				return true;
			}
			return false;
		}) + " microblocks modified");
	}

	private TextCommandResult onCmdFill(TextCommandCallingArgs args, bool delete)
	{
		GetMarkedArea(args.Caller, out var startPos, out var endPos);
		if (startPos == null || endPos == null)
		{
			return TextCommandResult.Error("Please mark area with world edit");
		}
		Block block = args.Caller.Player?.InventoryManager.ActiveHotbarSlot.Itemstack?.Block;
		if (block == null)
		{
			return TextCommandResult.Error("Please hold replacement material in active hands");
		}
		if (block is BlockMicroBlock)
		{
			return TextCommandResult.Error("Cannot use micro block as a material inside microblocks");
		}
		materialBlock = block;
		return TextCommandResult.Success(WalkMicroBlocks(startPos, endPos, delete ? new ActionBoolReturn<BlockEntityMicroBlock>(DeleteMaterial) : new ActionBoolReturn<BlockEntityMicroBlock>(FillMaterial)) + " microblocks modified");
	}

	private TextCommandResult onCmdDeleteMat(TextCommandCallingArgs args)
	{
		GetMarkedArea(args.Caller, out var startPos, out var endPos);
		if (startPos == null || endPos == null)
		{
			return TextCommandResult.Error("Please mark area with world edit");
		}
		string text = (string)args[0];
		Block block = sapi.World.BlockAccessor.GetBlock(new AssetLocation(text));
		if (block == null)
		{
			return TextCommandResult.Error("Unknown block code: " + text);
		}
		if (block is BlockMicroBlock)
		{
			return TextCommandResult.Error("Cannot use micro block as a material inside microblocks");
		}
		materialBlock = block;
		return TextCommandResult.Success(WalkMicroBlocks(startPos, endPos, DeleteMaterial) + " microblocks modified");
	}

	private TextCommandResult onCmdRemoveUnused(TextCommandCallingArgs args)
	{
		GetMarkedArea(args.Caller, out var startPos, out var endPos);
		if (startPos == null || endPos == null)
		{
			return TextCommandResult.Error("Please mark area with world edit");
		}
		HashSet<string> removedMaterials = new HashSet<string>();
		string text = WalkMicroBlocks(startPos, endPos, (BlockEntityMicroBlock be) => RemoveUnused(be, removedMaterials)) + " microblocks modified";
		if (removedMaterials.Count > 0)
		{
			text += ", removed materials: ";
			bool flag = false;
			foreach (string item in removedMaterials)
			{
				if (flag)
				{
					text += ", ";
				}
				else
				{
					flag = true;
				}
				text += item;
			}
		}
		return TextCommandResult.Success(text);
	}

	private bool DeleteMaterial(BlockEntityMicroBlock be)
	{
		int num = be.BlockIds.IndexOf(materialBlock.Id);
		if (num < 0)
		{
			return false;
		}
		List<uint> list = new List<uint>();
		List<int> list2 = new List<int>();
		CuboidWithMaterial cuboidWithMaterial = new CuboidWithMaterial();
		for (int i = 0; i < be.VoxelCuboids.Count; i++)
		{
			BlockEntityMicroBlock.FromUint(be.VoxelCuboids[i], cuboidWithMaterial);
			if (num != cuboidWithMaterial.Material)
			{
				int item = be.BlockIds[cuboidWithMaterial.Material];
				int num2 = list2.IndexOf(item);
				if (num2 < 0)
				{
					list2.Add(item);
					num2 = list2.Count - 1;
				}
				cuboidWithMaterial.Material = (byte)num2;
				list.Add(BlockEntityMicroBlock.ToUint(cuboidWithMaterial));
			}
		}
		be.VoxelCuboids = list;
		be.BlockIds = list2.ToArray();
		be.MarkDirty(redrawOnClient: true);
		return true;
	}

	private bool FillMaterial(BlockEntityMicroBlock be)
	{
		be.BeginEdit(out var voxels, out var voxelMaterial);
		if (fillMicroblock(materialBlock, be, voxels, voxelMaterial))
		{
			be.EndEdit(voxels, voxelMaterial);
			be.MarkDirty(redrawOnClient: true);
			return true;
		}
		return false;
	}

	private static bool fillMicroblock(Block fillWitBlock, BlockEntityMicroBlock be, BoolArray16x16x16 voxels, byte[,,] voxMats)
	{
		bool flag = false;
		byte b = 0;
		for (int i = 0; i < 16; i++)
		{
			for (int j = 0; j < 16; j++)
			{
				for (int k = 0; k < 16; k++)
				{
					if (voxels[i, j, k])
					{
						continue;
					}
					if (!flag)
					{
						int num = be.BlockIds.IndexOf(fillWitBlock.Id);
						if (be is BlockEntityChisel blockEntityChisel)
						{
							num = blockEntityChisel.AddMaterial(fillWitBlock);
						}
						else if (num < 0)
						{
							be.BlockIds = be.BlockIds.Append(fillWitBlock.Id);
							num = be.BlockIds.Length - 1;
						}
						b = (byte)num;
					}
					voxels[i, j, k] = true;
					voxMats[i, j, k] = b;
					flag = true;
				}
			}
		}
		return flag;
	}

	private bool RemoveUnused(BlockEntityMicroBlock be, HashSet<string> materialsRemoved)
	{
		bool flag = false;
		for (int i = 0; i < be.BlockIds.Length; i++)
		{
			if (be.NoVoxelsWithMaterial((uint)i))
			{
				Block block = sapi.World.BlockAccessor.GetBlock(be.BlockIds[i]);
				be.RemoveMaterial(block);
				materialsRemoved.Add(block.Code.ToShortString());
				flag = true;
				i--;
			}
		}
		if (flag)
		{
			be.MarkDirty(redrawOnClient: true);
		}
		return flag;
	}

	private TextCommandResult onCmdEditable(TextCommandCallingArgs args)
	{
		GetMarkedArea(args.Caller, out var startPos, out var endPos);
		if (startPos == null || endPos == null)
		{
			return TextCommandResult.Error("Please mark area with world edit");
		}
		bool flag = (bool)args.Parsers[0].GetValue();
		Block block = sapi.World.GetBlock(new AssetLocation("chiseledblock"));
		Block block2 = sapi.World.GetBlock(new AssetLocation("microblock"));
		Block targetBlock = (flag ? block : block2);
		IBlockAccessor ba = sapi.World.BlockAccessor;
		return TextCommandResult.Success(WalkMicroBlocks(startPos, endPos, delegate(BlockEntityMicroBlock be)
		{
			BlockPos pos = be.Pos;
			Block block3 = ba.GetBlock(pos);
			if (block3 is BlockMicroBlock && block3.Id != targetBlock.Id)
			{
				TreeAttribute tree = new TreeAttribute();
				be.ToTreeAttributes(tree);
				sapi.World.BlockAccessor.SetBlock(targetBlock.Id, pos);
				be = sapi.World.BlockAccessor.GetBlockEntity(pos) as BlockEntityMicroBlock;
				be.FromTreeAttributes(tree, sapi.World);
				return true;
			}
			return false;
		}) + " microblocks modified");
	}
}
