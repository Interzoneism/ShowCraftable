using System;
using System.Collections.Generic;
using System.IO;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class BlockEntityBlockRandomizer : BlockEntityContainer
{
	private const int quantitySlots = 10;

	private ICoreClientAPI capi;

	public float[] Chances = new float[10];

	private InventoryGeneric inventory;

	private static AssetLocation airFillerblockCode = new AssetLocation("meta-filler");

	public override InventoryBase Inventory => inventory;

	public override string InventoryClassName => "randomizer";

	public BlockEntityBlockRandomizer()
	{
		inventory = new InventoryGeneric(10, null, null);
	}

	public override void Initialize(ICoreAPI api)
	{
		base.Initialize(api);
		capi = api as ICoreClientAPI;
		if (inventory == null)
		{
			InitInventory(base.Block);
		}
	}

	protected virtual void InitInventory(Block block)
	{
		inventory = new InventoryGeneric(10, null, null);
		inventory.BaseWeight = 1f;
	}

	public override void OnBlockPlaced(ItemStack byItemStack = null)
	{
		if (byItemStack != null && byItemStack.Attributes.HasAttribute("chances"))
		{
			Chances = (byItemStack.Attributes["chances"] as FloatArrayAttribute).value;
			inventory.FromTreeAttributes(byItemStack.Attributes);
		}
	}

	protected override void OnTick(float dt)
	{
	}

	public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
	{
		BlockCrate block = worldForResolving.GetBlock(new AssetLocation(tree.GetString("blockCode"))) as BlockCrate;
		if (inventory == null)
		{
			if (tree.HasAttribute("blockCode"))
			{
				InitInventory(block);
			}
			else
			{
				InitInventory(null);
			}
		}
		Chances = (tree["chances"] as FloatArrayAttribute).value;
		if (Chances == null)
		{
			Chances = new float[10];
		}
		if (Chances.Length < 10)
		{
			Chances = Chances.Append(ArrayUtil.CreateFilled(10 - Chances.Length, (int i) => 0f));
		}
		base.FromTreeAttributes(tree, worldForResolving);
	}

	public override void ToTreeAttributes(ITreeAttribute tree)
	{
		base.ToTreeAttributes(tree);
		tree["chances"] = new FloatArrayAttribute(Chances);
	}

	public void OnInteract(IPlayer byPlayer)
	{
		if (byPlayer.WorldData.CurrentGameMode == EnumGameMode.Creative && Api.Side == EnumAppSide.Client)
		{
			GuiDialogItemLootRandomizer dlg = new GuiDialogItemLootRandomizer(inventory, Chances, capi, "Block randomizer");
			dlg.TryOpen();
			dlg.OnClosed += delegate
			{
				DidCloseLootRandomizer(dlg);
			};
		}
	}

	private void DidCloseLootRandomizer(GuiDialogItemLootRandomizer dialog)
	{
		ITreeAttribute attributes = dialog.Attributes;
		if (attributes.GetInt("save") == 0)
		{
			return;
		}
		using MemoryStream memoryStream = new MemoryStream();
		BinaryWriter stream = new BinaryWriter(memoryStream);
		attributes.ToBytes(stream);
		capi.Network.SendBlockEntityPacket(Pos, 1130, memoryStream.ToArray());
	}

	public override void OnReceivedClientPacket(IPlayer fromPlayer, int packetid, byte[] data)
	{
		base.OnReceivedClientPacket(fromPlayer, packetid, data);
		if (packetid != 1130)
		{
			return;
		}
		if (!fromPlayer.HasPrivilege("controlserver"))
		{
			(fromPlayer as IServerPlayer).SendIngameError("noprivilege", "No privilege to set up a loot randomizer");
			return;
		}
		TreeAttribute treeAttribute = new TreeAttribute();
		treeAttribute.FromBytes(data);
		for (int i = 0; i < 10; i++)
		{
			if (!(treeAttribute["stack" + i] is TreeAttribute treeAttribute2))
			{
				Chances[i] = 0f;
				if (!inventory[i].Empty)
				{
					inventory[i].Itemstack = null;
				}
			}
			else
			{
				Chances[i] = treeAttribute2.GetFloat("chance");
				ItemStack itemstack = treeAttribute2.GetItemstack("stack");
				itemstack.ResolveBlockOrItem(Api.World);
				inventory[i].Itemstack = itemstack;
			}
		}
		MarkDirty();
	}

	public override void OnPlacementBySchematic(ICoreServerAPI api, IBlockAccessor blockAccessor, BlockPos pos, Dictionary<int, Dictionary<int, int>> replaceBlocks, int centerrockblockid, Block layerBlock, bool resolveImports)
	{
		base.OnPlacementBySchematic(api, blockAccessor, pos, replaceBlocks, centerrockblockid, layerBlock, resolveImports);
		if (!resolveImports)
		{
			return;
		}
		IBlockAccessor blockAccessor2 = ((blockAccessor is IBlockAccessorRevertable) ? api.World.BlockAccessor : blockAccessor);
		float num = 0f;
		for (int i = 0; i < 10; i++)
		{
			num += Chances[i];
		}
		double num2 = api.World.Rand.NextDouble() * (double)Math.Max(100f, num);
		for (int j = 0; j < 10; j++)
		{
			Block block = inventory[j].Itemstack?.Block;
			num2 -= (double)Chances[j];
			if (!(num2 <= 0.0) || block == null)
			{
				continue;
			}
			if (block.Code == airFillerblockCode)
			{
				blockAccessor2.SetBlock(0, pos);
				return;
			}
			if (block.Id == BlockMicroBlock.BlockLayerMetaBlockId)
			{
				blockAccessor2.SetBlock(layerBlock?.Id ?? 0, pos);
				return;
			}
			if (replaceBlocks != null && replaceBlocks.TryGetValue(block.Id, out var value) && value.TryGetValue(centerrockblockid, out var value2))
			{
				block = blockAccessor.GetBlock(value2);
			}
			if (block.GetBehavior<BlockBehaviorHorizontalAttachable>() != null)
			{
				int num3 = api.World.Rand.Next(BlockFacing.HORIZONTALS.Length);
				for (int k = 0; k < BlockFacing.HORIZONTALS.Length; k++)
				{
					BlockFacing blockFacing = BlockFacing.HORIZONTALS[(k + num3) % BlockFacing.HORIZONTALS.Length];
					Block block2 = blockAccessor2.GetBlock(block.CodeWithParts(blockFacing.Code));
					BlockPos pos2 = pos.AddCopy(blockFacing);
					if (blockAccessor2.GetBlock(pos2).CanAttachBlockAt(blockAccessor2, block2, pos, blockFacing))
					{
						blockAccessor2.SetBlock(block2.Id, pos);
						break;
					}
				}
				return;
			}
			ItemStack itemstack = inventory[j].Itemstack;
			if (blockAccessor is IWorldGenBlockAccessor)
			{
				blockAccessor2.SetBlock(block.Id, pos);
				if (block.EntityClass != null)
				{
					blockAccessor2.SpawnBlockEntity(block.EntityClass, pos, itemstack);
				}
			}
			else
			{
				blockAccessor2.SetBlock(block.Id, pos);
				BlockEntity blockEntity = blockAccessor.GetBlockEntity(pos);
				blockEntity?.Initialize(api);
				blockEntity?.OnBlockPlaced(itemstack);
			}
			return;
		}
		blockAccessor2.SetBlock(0, pos);
	}

	public override void OnBlockBroken(IPlayer byPlayer = null)
	{
	}
}
